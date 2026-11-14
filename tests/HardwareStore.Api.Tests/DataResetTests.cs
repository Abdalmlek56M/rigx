using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Data reset (roadmap 2.10 B): owner only, fresh code, backup first, explicit sections, kept data stays.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class DataResetTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Reset = "/api/v1/admin/system/reset";
    private const string Owner = "reset-owner@example.test";
    private const string Manager = "reset-manager@example.test";

    private sealed record ResetResult(string[] Sections, Guid BackupRunId, int RowsDeleted);

    private static async Task<HttpResponseMessage> ResetAsync(HttpClient client, IdentityLoginApiFactory factory, string email, object body, bool withCode = true) =>
        await TestSignIn.PostJsonAsync(client, Reset, body, withCode ? await AdminApiFactory.StepUpAsync(client, factory, email) : null);

    /// <summary>A finished POS sale (stock item, units, order, payment, money movement, invoice-ready order) and a published article.</summary>
    private static async Task SeedBusinessAsync(OwnerBootstrapDatabase database, IServiceProvider services)
    {
        var retail = database.CreateRetailServices();
        var item = await retail.PublishedItemAsync("RTX 4070", 3, 1000m);
        await retail.CompletedSaleAsync(RetailMoney.PaidSale(item, 1000m));
        using var scope = services.CreateScope();
        var articles = scope.ServiceProvider.GetRequiredService<ContentArticleService>();
        await articles.CreateAsync(ContentArticles.Input("to-reset"), Guid.NewGuid());
    }

    private static async Task<Dictionary<string, long>> CountsAsync(OwnerBootstrapDatabase database, params string[] tables)
    {
        var counts = new Dictionary<string, long>();
        foreach (var table in tables) counts[table] = await SqlCounts.CountAsync(database, table);
        return counts;
    }

    private static readonly string[] BusinessTables = ["store_orders", "order_items", "order_payments", "stock_items", "inventory_units", "money_movements"];
    private static readonly string[] KeptTables = ["money_accounts", "partners", "expense_types", "categories", "brands", "roles", "permissions", "role_permissions", "user_accounts", "persons", "person_code_counters"];

    [Fact]
    public async Task Only_the_owner_with_a_fresh_code_can_reset_and_a_bad_request_deletes_nothing()
    {
        using var environment = new BackupTestEnvironment();
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, "System.ManageBackups", "System.ResetData");
        await AdminApiFactory.StaffAsync(database, Manager, "System.ManageBackups");
        await using var factory = environment.CreateFactory(fixture, database);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        using var manager = await AdminApiFactory.SignedInAsync(factory, Manager);
        await SeedBusinessAsync(database, factory.Services);
        var before = await CountsAsync(database, BusinessTables);
        var good = new { sections = new[] { "business" }, confirm = "RESET" };

        Assert.Equal(HttpStatusCode.Forbidden, (await ResetAsync(owner, factory, Owner, good, withCode: false)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await ResetAsync(manager, factory, Manager, good)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(owner, factory, Owner, new { sections = new[] { "business" }, confirm = "reset" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(owner, factory, Owner, new { sections = new[] { "customers" }, confirm = "RESET" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(owner, factory, Owner, new { sections = new[] { "stuff" }, confirm = "RESET" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(owner, factory, Owner, new { sections = Array.Empty<string>(), confirm = "RESET" })).StatusCode);

        Assert.Equal(before, await CountsAsync(database, BusinessTables));
        Assert.All(before.Values, count => Assert.True(count > 0));
        Assert.Empty(Directory.GetFiles(environment.Local));
    }

    [Fact]
    public async Task With_backups_off_the_reset_answers_409_and_nothing_is_deleted()
    {
        using var environment = new BackupTestEnvironment();
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, "System.ManageBackups", "System.ResetData");
        await using var factory = environment.CreateFactory(fixture, database, []);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        await SeedBusinessAsync(database, factory.Services);
        var before = await CountsAsync(database, BusinessTables);

        var response = await ResetAsync(owner, factory, Owner, new { sections = new[] { "business" }, confirm = "RESET" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(before, await CountsAsync(database, BusinessTables));
    }

    [Fact]
    public async Task A_failed_backup_stops_the_reset_before_anything_is_deleted()
    {
        using var environment = new BackupTestEnvironment();
        var blocked = Path.Combine(environment.Local, "blocked");
        await File.WriteAllTextAsync(blocked, "a file where the folder should be");
        var database = await fixture.CreateDatabaseAsync();
        await AdminApiFactory.StaffAsync(database, Owner, "System.ManageBackups", "System.ResetData");
        await using var factory = environment.CreateFactory(fixture, database, [environment.OffHost, blocked]);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        await SeedBusinessAsync(database, factory.Services);
        var before = await CountsAsync(database, BusinessTables);

        var response = await ResetAsync(owner, factory, Owner, new { sections = new[] { "business" }, confirm = "RESET" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(before, await CountsAsync(database, BusinessTables));
    }

    [Fact]
    public async Task Resetting_business_after_a_backup_clears_orders_stock_and_money_but_keeps_accounts_settings_catalog_and_staff()
    {
        using var environment = new BackupTestEnvironment();
        Directory.CreateDirectory(Path.Combine(environment.MediaRoot, "private", "requests"));
        var requestPhoto = Path.Combine(environment.MediaRoot, "private", "requests", "p.webp");
        await File.WriteAllTextAsync(requestPhoto, "photo");
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = await AdminApiFactory.StaffAsync(database, Owner, "System.ManageBackups", "System.ResetData");
        await using var factory = environment.CreateFactory(fixture, database);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        await SeedBusinessAsync(database, factory.Services);
        var customerId = await CustomerAdminTests.CustomerAsync(database, "kept-customer");
        var keptBefore = await CountsAsync(database, KeptTables);
        var articlesBefore = await SqlCounts.CountAsync(database, "content_articles");

        var response = await ResetAsync(owner, factory, Owner, new { sections = new[] { "business" }, confirm = "RESET" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<ResetResult>())!;
        Assert.Equal(["business"], result.Sections);
        Assert.True(result.RowsDeleted > 0);
        Assert.All(await CountsAsync(database, BusinessTables), pair => Assert.Equal(0, pair.Value));
        Assert.Equal(keptBefore, await CountsAsync(database, KeptTables));
        Assert.Equal(articlesBefore, await SqlCounts.CountAsync(database, "content_articles"));
        Assert.False(File.Exists(requestPhoto));
        await using (var identity = database.CreateIdentityContext())
            Assert.True(await identity.UserAccounts.AnyAsync(account => account.Id == customerId || account.Id == ownerId));

        await using var audit = database.CreateAuditContext();
        var backup = await audit.BackupRuns.AsNoTracking().SingleAsync(run => run.Id == result.BackupRunId);
        Assert.Equal((BackupStatus.Succeeded, BackupTrigger.BeforeReset), (backup.Status, backup.Trigger));
        Assert.True(File.Exists(Path.Combine(environment.Local, backup.FileName!)));
        var reset = await audit.SecurityAuditEvents.AsNoTracking().SingleAsync(item => item.EventType == DataResetService.ResetEvent);
        Assert.Equal(("Succeeded", ownerId), (reset.Outcome, reset.ActorUserAccountId));
        using var metadata = JsonDocument.Parse(reset.SafeMetadata);
        Assert.Equal(result.BackupRunId, metadata.RootElement.GetProperty("backupRunId").GetGuid());
        Assert.Equal("business", Assert.Single(metadata.RootElement.GetProperty("sections").EnumerateArray()).GetString());
    }

    [Fact]
    public async Task Resetting_content_and_customers_removes_articles_and_customer_accounts_but_not_staff_and_the_audit_trail_stays()
    {
        using var environment = new BackupTestEnvironment();
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = await AdminApiFactory.StaffAsync(database, Owner, "System.ManageBackups", "System.ResetData");
        await using var factory = environment.CreateFactory(fixture, database);
        using var owner = await AdminApiFactory.SignedInAsync(factory, Owner);
        await SeedBusinessAsync(database, factory.Services);
        var customerId = await CustomerAdminTests.CustomerAsync(database, "gone-customer");
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ISecurityAuditWriter>().WriteAsync(
                new SecurityAuditEntry("Test.CustomerEvent", "Recorded", "User", customerId, customerId));
        var personCodesBefore = await SqlCounts.CountAsync(database, "person_code_counters");

        var response = await ResetAsync(owner, factory, Owner, new { sections = new[] { "business", "content", "customers" }, confirm = "RESET" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, await SqlCounts.CountAsync(database, "content_articles"));
        await using (var identity = database.CreateIdentityContext())
        {
            Assert.False(await identity.UserAccounts.AnyAsync(account => account.Id == customerId));
            Assert.True(await identity.UserAccounts.AnyAsync(account => account.Id == ownerId));
            Assert.Equal(1, await identity.UserAccounts.CountAsync());
            Assert.Equal(1, await identity.Persons.CountAsync());
        }
        Assert.Equal(personCodesBefore, await SqlCounts.CountAsync(database, "person_code_counters"));
        await using var audit = database.CreateAuditContext();
        var kept = await audit.SecurityAuditEvents.AsNoTracking().SingleAsync(item => item.EventType == "Test.CustomerEvent");
        Assert.Null(kept.ActorUserAccountId);
        Assert.Null(kept.SubjectUserAccountId);
    }
}
