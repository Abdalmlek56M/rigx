using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HardwareStore.Modules.Catalog;
using Testcontainers.PostgreSql;

namespace HardwareStore.Api.Tests;

/// <summary>
/// Runs <c>pg_dump</c> / <c>pg_restore</c> inside the test PostgreSQL container (the build machine has no PostgreSQL client)
/// and moves the files in and out, so the backup and restore drill are real.
/// </summary>
internal sealed class ContainerBackupTool(PostgreSqlContainer container, string connectionString) : IDatabaseBackupTool
{
    public async Task DumpAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var remote = $"/tmp/{Guid.NewGuid():N}.dump";
        await RunAsync(["pg_dump", "--format=custom", "--no-owner", $"--file={remote}", "-U", connection.Username!, "-d", connection.Database!], cancellationToken);
        await File.WriteAllBytesAsync(outputPath, await container.ReadFileAsync(remote, cancellationToken), cancellationToken);
    }

    public async Task RestoreAsync(string dumpPath, string databaseName, CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var remote = $"/tmp/{Guid.NewGuid():N}.dump";
        await container.CopyAsync(await File.ReadAllBytesAsync(dumpPath, cancellationToken), remote, ct: cancellationToken);
        await RunAsync(["pg_restore", "--no-owner", "--no-acl", "--exit-on-error", "-U", connection.Username!, "-d", databaseName, remote], cancellationToken);
    }

    private async Task RunAsync(IList<string> command, CancellationToken cancellationToken)
    {
        var result = await container.ExecAsync(command, cancellationToken);
        if (result.ExitCode != 0)
            throw new BackupToolException($"{command[0]} failed ({result.ExitCode}): {result.Stderr}");
    }
}

/// <summary>Two temporary backup destinations and a temporary media folder, wired into the test host.</summary>
internal sealed class BackupTestEnvironment : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "rigx-backup-test-" + Guid.NewGuid().ToString("N"));

    public BackupTestEnvironment()
    {
        Local = Directory.CreateDirectory(Path.Combine(_root, "local")).FullName;
        OffHost = Directory.CreateDirectory(Path.Combine(_root, "off-host")).FullName;
        MediaRoot = Directory.CreateDirectory(Path.Combine(_root, "media")).FullName;
    }

    public string Local { get; }
    public string OffHost { get; }
    public string MediaRoot { get; }
    public string[] Destinations => [Local, OffHost];

    /// <summary>The API host with real dump/restore through the container; <paramref name="destinations"/> defaults to both folders.</summary>
    public IdentityLoginApiFactory CreateFactory(OwnerBootstrapPostgreSqlFixture fixture, OwnerBootstrapDatabase database, string[]? destinations = null) =>
        AdminApiFactory.Create(database.ConnectionString, services =>
        {
            services.RemoveAll<BackupOptions>();
            services.AddSingleton(new BackupOptions { Destinations = destinations ?? Destinations, ConnectionString = database.ConnectionString });
            services.RemoveAll<IDatabaseBackupTool>();
            services.AddSingleton<IDatabaseBackupTool>(new ContainerBackupTool(fixture.Container, database.ConnectionString));
            services.RemoveAll<CatalogMediaOptions>();
            services.AddSingleton(new CatalogMediaOptions { RootPath = MediaRoot });
        });

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}

internal static class SqlCounts
{
    public static async Task<long> CountAsync(OwnerBootstrapDatabase database, string table)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM \"{table}\"", connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
