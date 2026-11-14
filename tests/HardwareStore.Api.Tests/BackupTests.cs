using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Backups (roadmap 2.10 B): the archive, two destinations, retention, the restore drill and the owner-only download.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class BackupTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Backups = "/api/v1/admin/system/backups";
    private const string Owner = "backup-owner@example.test";
    private const string Manager = "backup-manager@example.test";
    private static readonly string[] OwnerPermissions = ["System.ManageBackups", "System.BackupDownload"];

    private static async Task<BackupRun> RunNowAsync(HttpClient client)
    {
        using var response = await TestSignIn.PostJsonAsync(client, Backups, new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<BackupRun>())!;
    }

    private static async Task<int> AuditCountAsync(OwnerBootstrapDatabase database, string eventType, string outcome = "Succeeded")
    {
        await using var audit = database.CreateAuditContext();
        return await audit.SecurityAuditEvents.AsNoTracking().CountAsync(item => item.EventType == eventType && item.Outcome == outcome);
    }

    private static async Task<long> RestoreCheckDatabasesAsync(OwnerBootstrapDatabase database)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM pg_database WHERE datname LIKE '{RestoreDrill.DatabasePrefix}%'", connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task Running_a_backup_writes_one_archive_to_both_destinations_with_a_matching_manifest_and_records_the_run()
    {
        using var environment = new BackupTestEnvironment();
        Directory.CreateDirectory(Path.Combine(environment.MediaRoot, "private", "receipts"));
        await File.WriteAllTextAsync(Path.Combine(environment.MediaRoot, "private", "receipts", "r1.webp"), "private-photo");
        await File.WriteAllTextAsync(Path.Combine(environment.MediaRoot, "logo.webp"), "public-photo");
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, OwnerPermissions);
        await using var factory = environment.CreateFactory(fixture, database);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);

        var run = await RunNowAsync(owner);

        Assert.Equal(("Succeeded", "Manual", 2, null), (run.Status, run.Trigger, run.DestinationsWritten, run.Error));
        Assert.Matches(@"^rigx-backup-\d{8}-\d{6}Z\.zip$", run.FileName);
        foreach (var destination in environment.Destinations)
            Assert.Equal(run.SizeBytes, new FileInfo(Path.Combine(destination, run.FileName!)).Length);
        Assert.Empty(Directory.GetFiles(environment.Local, "*.tmp"));
        Assert.Equal(await File.ReadAllBytesAsync(Path.Combine(environment.Local, run.FileName!)),
            await File.ReadAllBytesAsync(Path.Combine(environment.OffHost, run.FileName!)));

        using var archive = ZipFile.OpenRead(Path.Combine(environment.Local, run.FileName!));
        using var manifest = JsonDocument.Parse(archive.GetEntry("manifest.json")!.Open());
        byte[] dump;
        await using (var entry = archive.GetEntry("database.dump")!.Open())
        using (var memory = new MemoryStream())
        {
            await entry.CopyToAsync(memory);
            dump = memory.ToArray();
        }
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(dump)), manifest.RootElement.GetProperty("databaseDumpSha256").GetString());
        Assert.Equal(run.Sha256, manifest.RootElement.GetProperty("databaseDumpSha256").GetString());
        Assert.Equal(2, manifest.RootElement.GetProperty("fileCount").GetInt32());
        Assert.NotNull(archive.GetEntry("media/private/receipts/r1.webp"));
        Assert.NotNull(archive.GetEntry("media/logo.webp"));
        Assert.True(manifest.RootElement.GetProperty("databaseSizeBytes").GetInt64() > 0);
        Assert.NotEmpty(manifest.RootElement.GetProperty("migrations").GetProperty("RetailDbContext").EnumerateArray());
        Assert.Contains(archive.Entries, entry => entry.FullName == "database.dump" && entry.Length > 0);

        var listed = await owner.GetFromJsonAsync<List<BackupRun>>(Backups);
        Assert.Equal(run.Id, Assert.Single(listed!).Id);
        Assert.Equal(1, await AuditCountAsync(database, BackupAuditor.CreatedEvent));
    }

    [Fact]
    public async Task The_restore_drill_proves_the_archive_in_a_temporary_database_that_is_gone_afterwards()
    {
        using var environment = new BackupTestEnvironment();
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, OwnerPermissions);
        await using var factory = environment.CreateFactory(fixture, database);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        var run = await RunNowAsync(owner);
        File.Delete(Path.Combine(environment.Local, run.FileName!));

        using var response = await TestSignIn.PostJsonAsync(owner, $"{Backups}/{run.Id}/verify", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var verified = (await response.Content.ReadFromJsonAsync<BackupRun>())!;
        Assert.True(verified.VerifyPassed);
        Assert.NotNull(verified.VerifiedAtUtc);
        Assert.InRange(verified.VerifyTables!.Value, 30, 200);
        Assert.True(verified.VerifyRows > 0);
        Assert.Null(verified.VerifyError);
        Assert.Equal(0, await RestoreCheckDatabasesAsync(database));
        Assert.Equal(1, await AuditCountAsync(database, BackupAuditor.VerifiedEvent));
    }

    [Fact]
    public async Task A_tampered_archive_fails_the_drill_and_the_failure_is_recorded()
    {
        using var environment = new BackupTestEnvironment();
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, OwnerPermissions);
        await using var factory = environment.CreateFactory(fixture, database);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        var run = await RunNowAsync(owner);
        foreach (var destination in environment.Destinations)
        {
            using var archive = ZipFile.Open(Path.Combine(destination, run.FileName!), ZipArchiveMode.Update);
            archive.GetEntry("database.dump")!.Delete();
            await using var replacement = archive.CreateEntry("database.dump").Open();
            await replacement.WriteAsync("not a dump"u8.ToArray());
        }

        using var response = await TestSignIn.PostJsonAsync(owner, $"{Backups}/{run.Id}/verify", new { });

        var verified = (await response.Content.ReadFromJsonAsync<BackupRun>())!;
        Assert.False(verified.VerifyPassed);
        Assert.Contains("SHA-256", verified.VerifyError);
        Assert.Equal(0, await RestoreCheckDatabasesAsync(database));
        Assert.Equal(1, await AuditCountAsync(database, BackupAuditor.VerifiedEvent, "Failed"));
    }

    [Fact]
    public async Task Retention_removes_only_old_backup_archives_from_every_destination()
    {
        using var environment = new BackupTestEnvironment();
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, OwnerPermissions);
        foreach (var destination in environment.Destinations)
        {
            await File.WriteAllTextAsync(Path.Combine(destination, "rigx-backup-20200101-000000Z.zip"), "old");
            await File.WriteAllTextAsync(Path.Combine(destination, "rigx-backup-20200101-000000Z.zip.tmp"), "half-written");
            await File.WriteAllTextAsync(Path.Combine(destination, "family-photos-2020.zip"), "not ours");
        }
        await using var factory = environment.CreateFactory(fixture, database);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);

        var run = await RunNowAsync(owner);

        foreach (var destination in environment.Destinations)
        {
            Assert.False(File.Exists(Path.Combine(destination, "rigx-backup-20200101-000000Z.zip")));
            Assert.True(File.Exists(Path.Combine(destination, "rigx-backup-20200101-000000Z.zip.tmp")));
            Assert.True(File.Exists(Path.Combine(destination, "family-photos-2020.zip")));
            Assert.True(File.Exists(Path.Combine(destination, run.FileName!)));
        }
    }

    [Fact]
    public async Task A_destination_that_cannot_be_written_makes_the_run_fail_but_the_other_copy_is_kept()
    {
        using var environment = new BackupTestEnvironment();
        var blocked = Path.Combine(environment.Local, "blocked");
        await File.WriteAllTextAsync(blocked, "a file where the folder should be");
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, OwnerPermissions);
        await using var factory = environment.CreateFactory(fixture, database, [environment.OffHost, blocked]);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);

        var run = await RunNowAsync(owner);

        Assert.Equal(("Failed", 1), (run.Status, run.DestinationsWritten));
        Assert.Contains("1 of 2", run.Error);
        Assert.True(File.Exists(Path.Combine(environment.OffHost, run.FileName!)));
        Assert.Equal(1, await AuditCountAsync(database, BackupAuditor.CreatedEvent, "Failed"));
    }

    [Fact]
    public async Task Downloading_needs_the_owner_permission_and_a_fresh_code_and_is_audited_and_never_cached()
    {
        using var environment = new BackupTestEnvironment();
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, OwnerPermissions);
        await AdminApiFactory.StaffAsync(database, Manager, "System.ManageBackups");
        await using var factory = environment.CreateFactory(fixture, database);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        using var manager = await AdminApiFactory.SignedInAsync(factory, Manager);
        var run = await RunNowAsync(owner);
        var url = $"{Backups}/{run.Id}/download";

        var withoutPermission = await manager.SendAsync(await StepUpGetAsync(manager, factory, Manager, url));
        var withoutCode = await owner.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, withoutPermission.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, withoutCode.StatusCode);
        Assert.Equal(0, await AuditCountAsync(database, BackupAuditor.DownloadedEvent));
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(Backups)).StatusCode);

        using var download = await owner.SendAsync(await StepUpGetAsync(owner, factory, Owner, url));

        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/zip", download.Content.Headers.ContentType!.MediaType);
        Assert.Contains("no-store", download.Headers.CacheControl!.ToString());
        Assert.Equal(run.FileName, download.Content.Headers.ContentDisposition!.FileName);
        Assert.Equal(run.SizeBytes, (await download.Content.ReadAsByteArrayAsync()).Length);
        Assert.Equal(1, await AuditCountAsync(database, BackupAuditor.DownloadedEvent));
    }

    [Fact]
    public async Task With_no_destination_configured_backups_are_off_and_every_action_answers_409()
    {
        using var environment = new BackupTestEnvironment();
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, OwnerPermissions);
        await using var factory = environment.CreateFactory(fixture, database, []);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);

        Assert.Equal(HttpStatusCode.Conflict, (await TestSignIn.PostJsonAsync(owner, Backups, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await TestSignIn.PostJsonAsync(owner, $"{Backups}/{Guid.NewGuid()}/verify", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync(Backups)).StatusCode);
    }

    [Fact]
    public async Task The_scheduled_backup_runs_once_per_cairo_day_when_enabled_and_never_when_disabled()
    {
        using var environment = new BackupTestEnvironment();
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = await AdminApiFactory.StaffAsync(database, Owner, OwnerPermissions);
        await using var factory = environment.CreateFactory(fixture, database);
        using var scope = factory.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<StoreSettingsService>();
        var backups = scope.ServiceProvider.GetRequiredService<BackupService>();
        var current = (await settings.GetAdminAsync()).RowVersion;
        var disabled = await settings.UpdateSectionAsync(new BackupSettings { Enabled = false, HourCairo = 0 }, current, ownerId);

        Assert.Null(await backups.RunScheduledIfDueAsync(CancellationToken.None));

        await settings.UpdateSectionAsync(new BackupSettings { Enabled = true, HourCairo = 0 }, disabled.RowVersion, ownerId);
        var first = await backups.RunScheduledIfDueAsync(CancellationToken.None);
        var second = await backups.RunScheduledIfDueAsync(CancellationToken.None);

        Assert.Equal(("Succeeded", "Scheduled"), (first!.Status, first.Trigger));
        Assert.Null(second);
    }

    [Fact]
    public async Task The_backup_schedule_is_a_settings_section_with_limits()
    {
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, "System.StoreSettings");
        await using var factory = AdminApiFactory.Create(database.ConnectionString);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        var url = "/api/v1/admin/storefront/settings";

        using var current = JsonDocument.Parse(await owner.GetStringAsync(url));
        Assert.Equal(("True", 3, 14), (current.RootElement.GetProperty("backup").GetProperty("enabled").ToString(),
            current.RootElement.GetProperty("backup").GetProperty("hourCairo").GetInt32(), current.RootElement.GetProperty("backup").GetProperty("retentionDays").GetInt32()));
        var version = current.RootElement.GetProperty("rowVersion").GetInt64();

        var tooShort = await Put(owner, url, new { enabled = true, hourCairo = 3, retentionDays = 2 }, version);
        var badHour = await Put(owner, url, new { enabled = true, hourCairo = 24, retentionDays = 14 }, version);
        var saved = await Put(owner, url, new { enabled = true, hourCairo = 5, retentionDays = 30 }, version);

        Assert.Equal(HttpStatusCode.BadRequest, tooShort);
        Assert.Equal(HttpStatusCode.BadRequest, badHour);
        Assert.Equal(HttpStatusCode.OK, saved);
    }

    private static async Task<HttpStatusCode> Put(HttpClient client, string url, object value, long rowVersion) =>
        (await TestSignIn.SendJsonAsync(client, HttpMethod.Put, $"{url}/backup", new { value, rowVersion })).StatusCode;

    private static async Task<HttpRequestMessage> StepUpGetAsync(HttpClient client, IdentityLoginApiFactory factory, string email, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var header in await AdminApiFactory.StepUpAsync(client, factory, email))
            request.Headers.Add(header.Key, header.Value);
        return request;
    }
}
