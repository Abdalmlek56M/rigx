using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class PreviewInitializationTests
{
    [Fact]
    public async Task Preview_bootstrap_is_disabled_in_production()
    {
        var service = CreateService("Production", enabled: true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InitializeAsync("bootstrap-key"));

        Assert.Contains("Development", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preview_bootstrap_requires_explicit_development_enablement()
    {
        var service = CreateService("Development", enabled: false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InitializeAsync("bootstrap-key"));

        Assert.Contains("explicitly enabled", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preview_bootstrap_rejects_a_migration_target_that_differs_from_runtime()
    {
        var service = CreateService(
            "Development",
            enabled: true,
            runtimeConnectionString: "Host=runtime-host;Port=5432;Database=preview;Username=runtime",
            migrationConnectionString: "Host=migration-host;Port=5432;Database=preview;Username=migrations");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InitializeAsync("bootstrap-key"));

        Assert.Contains("same host, port, and database", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Preview_bootstrap_rejects_a_migration_database_that_differs_from_runtime()
    {
        var service = CreateService(
            "Development",
            enabled: true,
            runtimeConnectionString: "Host=localhost;Port=5432;Database=preview;Username=runtime",
            migrationConnectionString: "Host=localhost;Port=5432;Database=other;Username=migrations");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InitializeAsync("bootstrap-key"));

        Assert.Contains("same host, port, and database", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Existing_preview_owner_profile_is_reconciled_without_keeping_lockout_state()
    {
        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            Username = "old-owner",
            NormalizedUsername = "OLD-OWNER",
            Email = "old-owner@example.test",
            NormalizedEmail = "OLD-OWNER@EXAMPLE.TEST",
            PasswordHash = "old-hash",
            IsEmailVerified = false,
            IsActive = false,
            AccessFailedCount = 7,
            AccessFailedWindowStartedUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            LockoutEndUtc = DateTimeOffset.UtcNow.AddMinutes(3),
            Person = new Person
            {
                Id = Guid.NewGuid(),
                FullName = "Old owner",
                Email = "old-owner@example.test",
                IsActive = false,
                IsEmployee = false
            }
        };

        PreviewInitializationService.ApplyConfiguredOwnerProfile(
            account,
            " preview-owner ",
            "owner@example.test",
            "Preview Owner");

        Assert.Equal("preview-owner", account.Username);
        Assert.Equal("PREVIEW-OWNER", account.NormalizedUsername);
        Assert.Equal("owner@example.test", account.Email);
        Assert.Equal("OWNER@EXAMPLE.TEST", account.NormalizedEmail);
        Assert.Equal("Preview Owner", account.Person.FullName);
        Assert.Equal("owner@example.test", account.Person.Email);
        Assert.True(account.Person.IsActive);
        Assert.True(account.Person.IsEmployee);
        Assert.True(account.IsEmailVerified);
        Assert.True(account.IsActive);
        Assert.Equal(0, account.AccessFailedCount);
        Assert.Null(account.AccessFailedWindowStartedUtc);
        Assert.Null(account.LockoutEndUtc);
    }

    private static PreviewInitializationService CreateService(
        string environmentName,
        bool enabled,
        string runtimeConnectionString = "Host=localhost;Port=5432;Database=unused;Username=unused",
        string migrationConnectionString = "Host=localhost;Port=5432;Database=unused;Username=unused") =>
        new(
            new PreviewOptions
            {
                Enabled = enabled,
                BootstrapKey = "bootstrap-key",
                OwnerEmail = "owner@example.test",
                OwnerUsername = "preview-owner",
                OwnerFullName = "Preview Owner",
                OwnerPassword = "a sufficiently long password",
                MigrationConnectionString = migrationConnectionString,
                RuntimeConnectionString = runtimeConnectionString
            },
            new TestHostEnvironment(environmentName),
            null!,
            TimeProvider.System,
            TimeZoneInfo.Utc,
            null!,
            NullLogger<PreviewInitializationService>.Instance);

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "HardwareStore.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

[Trait("Category", "RequiresDocker")]
public sealed class PreviewInitializationPostgreSqlTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("preview_initialization_test")
        .WithUsername("preview_initialization_test")
        .WithPassword("test-only-password")
        .Build();

    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Initializes_all_module_schemas_and_repeats_without_duplicates()
    {
        var databaseName = $"preview_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(_connectionString))
        {
            await admin.OpenAsync();
            await using var command = admin.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(_connectionString) { Database = databaseName };
        var connectionString = builder.ConnectionString;
        await CreateAuditRolesAsync(connectionString);

        var service = CreateService(connectionString);
        var first = await service.InitializeAsync("bootstrap-key");
        Assert.Equal(14, first.Products);
        Assert.Equal("preview-owner", first.OwnerUsername);

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            foreach (var table in new[]
                     {
                         "supplier_profiles", "supplier_products", "warehouses",
                         "products", "product_variants", "security_audit_events", "store_settings"
                     })
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT to_regclass(@name) IS NOT NULL";
                command.Parameters.AddWithValue("name", $"public.{table}");
                Assert.True((bool)(await command.ExecuteScalarAsync())!, $"Missing table {table}");
            }
        }

        var second = await service.InitializeAsync("bootstrap-key");
        Assert.Equal(14, second.Products);
        Assert.Equal("preview-owner", second.OwnerUsername);

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            Assert.Equal(1L, await ScalarAsync(connection,
                "SELECT COUNT(*) FROM user_accounts ua JOIN user_roles ur ON ur.user_account_id = ua.id JOIN roles r ON r.id = ur.role_id WHERE r.code = 'Owner'"));
            Assert.Equal(14L, await ScalarAsync(connection, "SELECT COUNT(*) FROM products"));
            Assert.Equal(11L, await ScalarAsync(connection, "SELECT COUNT(*) FROM categories"));
        }
    }

    private static async Task<long> ScalarAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task CreateAuditRolesAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var role in new[] { "hardware_store_audit_owner", "hardware_store_app", "hardware_store_audit_writer" })
        {
            await using var exists = connection.CreateCommand();
            exists.CommandText = "SELECT 1 FROM pg_roles WHERE rolname = @name";
            exists.Parameters.AddWithValue("name", role);
            if (await exists.ExecuteScalarAsync() is null)
            {
                await using var create = connection.CreateCommand();
                create.CommandText = $"CREATE ROLE \"{role}\"";
                await create.ExecuteNonQueryAsync();
            }
        }
    }

    private static PreviewInitializationService CreateService(string connectionString) =>
        new(
            new PreviewOptions
            {
                Enabled = true,
                BootstrapKey = "bootstrap-key",
                OwnerEmail = "owner@example.test",
                OwnerUsername = "preview-owner",
                OwnerFullName = "Preview Owner",
                OwnerPassword = "Rigx-Preview-Owner-2026!",
                MigrationConnectionString = connectionString,
                RuntimeConnectionString = connectionString
            },
            new TestHostEnvironment("Development"),
            new PasswordHashingService(new PasswordHasher<UserAccount>()),
            TimeProvider.System,
            TimeZoneInfo.Utc,
            new FakeCatalogMediaStorage(),
            NullLogger<PreviewInitializationService>.Instance);

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "HardwareStore.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private sealed class FakeCatalogMediaStorage : ICatalogMediaStorage
    {
        public Task<CatalogStoredMedia> SaveImageAsync(
            Guid productId,
            Guid imageId,
            CatalogImageUpload image,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CatalogStoredMedia(
                $"products/{productId:N}/{imageId:N}.webp",
                $"products/{productId:N}/{imageId:N}-thumb.webp",
                $"products/{productId:N}/{imageId:N}.original",
                1400,
                900,
                400,
                257,
                1024));

        public Task<string> SaveAssetImageAsync(
            string collection,
            Guid ownerId,
            Guid assetId,
            CatalogImageUpload image,
            CancellationToken cancellationToken = default) =>
            Task.FromResult($"{collection}/{ownerId:N}/{assetId:N}.webp");

        public Task<CatalogStoredMedia> SaveContentImageAsync(
            Guid assetId,
            CatalogImageUpload image,
            bool replace,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CatalogStoredMedia(
                $"content/{assetId:N}/{assetId:N}.webp",
                $"content/{assetId:N}/{assetId:N}-thumb.webp",
                $"content/{assetId:N}/{assetId:N}.original",
                1200,
                800,
                400,
                267,
                2048));

        public Task<CatalogStoredVideo> SaveContentVideoAsync(
            Guid assetId,
            Stream content,
            long length,
            string extension,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CatalogStoredVideo($"videos/{assetId:N}/{assetId:N}.{extension}", length));

        public Stream OpenRead(string storageKey) => new MemoryStream();

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<StoredMediaFile>> ListFilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredMediaFile>>([]);

        public Task DeleteStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
