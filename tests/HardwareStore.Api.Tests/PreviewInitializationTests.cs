using HardwareStore.Common.Infrastructure.Audit;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            IsEmailVerified = false,
            IsActive = false,
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
                MigrationConnectionString = migrationConnectionString,
                RuntimeConnectionString = runtimeConnectionString
            },
            new TestHostEnvironment(environmentName),
            TimeProvider.System,
            TimeZoneInfo.Utc,
            null!,
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
        var connectionString = await CreateDatabaseAsync();

        await using var provider = CreateServices(connectionString);
        var service = CreateService(connectionString, provider.GetRequiredService<IServiceScopeFactory>());
        var first = await service.InitializeAsync("bootstrap-key");
        Assert.Equal(14, first.Products);
        Assert.Equal("preview-owner", first.OwnerUsername);

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            foreach (var table in new[]
                     {
                         "sellers", "purchases", "warehouses",
                         "products", "security_audit_events", "store_settings"
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

    [Fact]
    public async Task Seeds_a_realistic_demo_store_and_repeats_without_duplicates()
    {
        var connectionString = await CreateDatabaseAsync();
        await using var provider = CreateServices(connectionString);
        var service = CreateService(connectionString, provider.GetRequiredService<IServiceScopeFactory>());

        var first = await service.InitializeAsync("bootstrap-key");
        Assert.Equal(32, first.StockItems);
        Assert.Equal(3, first.Builds);
        Assert.Equal(10, first.Customers);
        Assert.Equal(16, first.OnlineOrders);
        Assert.Equal(5, first.PosSales);
        Assert.Equal(6, first.Expenses);

        var retail = provider.GetRequiredService<IDbContextFactory<RetailDbContext>>();
        var storefront = await new StoreItemService(retail).ListAsync(new StoreItemQuery(PageSize: 60));
        Assert.Equal(32, storefront.Total);
        var soldOutCards = storefront.Items.Where(item => item.Available == 0).ToList();
        Assert.Equal(4, soldOutCards.Count);
        Assert.True(storefront.Counts.New > 0);
        Assert.True(storefront.Counts.Used > 0);
        Assert.Equal(3, (await new AssemblyService(retail, TimeProvider.System).ListPublishedAssembliesAsync()).Count);
        await using (var retailDb = await retail.CreateDbContextAsync())
        {
            var soldOut = await retailDb.StockItems.AsNoTracking().Where(StockItemRules.PublishedFilter)
                .Where(item => !item.Units.Any(unit => unit.Status == UnitStatus.Available))
                .Select(item => item.ModelName)
                .ToListAsync();
            Assert.Equal(4, soldOut.Count);
            Assert.Contains("NVIDIA GeForce GTX 1660 Super 6GB", soldOut);
            Assert.Contains("Kingston 8GB DDR4-2666", soldOut);
            Assert.Contains("WD Blue 1TB HDD", soldOut);
            Assert.Contains("SeaSonic S12III 500W", soldOut);
        }

        var second = await service.InitializeAsync("bootstrap-key");
        Assert.Equal(0, second.StockItems);
        Assert.Equal(0, second.Builds);
        Assert.Equal(0, second.Customers);
        Assert.Equal(0, second.OnlineOrders);
        Assert.Equal(0, second.PosSales);
        Assert.Equal(0, second.Expenses);
        Assert.Equal(14, second.Products);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        Assert.Equal(32L, await ScalarAsync(connection, "SELECT COUNT(*) FROM stock_items"));
        Assert.Equal(3L, await ScalarAsync(connection, "SELECT COUNT(*) FROM assemblies"));
        Assert.Equal(21L, await ScalarAsync(connection, "SELECT COUNT(*) FROM store_orders"));
        Assert.Equal(3L, await OnlineOrdersAsync(connection, "New"));
        Assert.Equal(2L, await OnlineOrdersAsync(connection, "Confirmed"));
        Assert.Equal(2L, await OnlineOrdersAsync(connection, "Preparing"));
        Assert.Equal(2L, await OnlineOrdersAsync(connection, "Shipped"));
        Assert.Equal(2L, await OnlineOrdersAsync(connection, "Delivered"));
        Assert.Equal(4L, await OnlineOrdersAsync(connection, "Completed"));
        Assert.Equal(1L, await OnlineOrdersAsync(connection, "Cancelled"));
        Assert.Equal(5L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM store_orders WHERE channel = 'Store' AND status = 'Completed' AND payment_status = 'Paid'"));
        Assert.Equal(11L, await ScalarAsync(connection, "SELECT COUNT(*) FROM invoices"));
        Assert.Equal(6L, await ScalarAsync(connection, "SELECT COUNT(*) FROM retail_expenses"));
        Assert.Equal(5L, await ScalarAsync(connection, "SELECT COUNT(*) FROM content_policies WHERE status = 'Published'"));
        Assert.Equal(6L, await ScalarAsync(connection, "SELECT COUNT(*) FROM content_faqs WHERE status = 'Published'"));
        Assert.Equal(10L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM user_accounts ua JOIN user_roles ur ON ur.user_account_id = ua.id JOIN roles r ON r.id = ur.role_id WHERE r.code = 'Customer'"));

        using (var scope = provider.CreateScope())
        {
            var reports = scope.ServiceProvider.GetRequiredService<BusinessReportService>();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var report = await reports.BuildReportAsync(today.AddDays(-32), today);
            Assert.True(report.CompletedOrders >= 9,
                $"The dashboard counts completed sales; found {report.CompletedOrders} completed orders.");
        }
    }

    private static async Task<long> OnlineOrdersAsync(NpgsqlConnection connection, string status)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM store_orders WHERE channel = 'Online' AND status = @status";
        command.Parameters.AddWithValue("status", status);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<string> CreateDatabaseAsync()
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
        await CreateAuditRolesAsync(builder.ConnectionString);
        return builder.ConnectionString;
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

    private static PreviewInitializationService CreateService(string connectionString, IServiceScopeFactory scopeFactory) =>
        new(
            new PreviewOptions
            {
                Enabled = true,
                BootstrapKey = "bootstrap-key",
                OwnerEmail = "owner@example.test",
                OwnerUsername = "preview-owner",
                OwnerFullName = "Preview Owner",
                MigrationConnectionString = connectionString,
                RuntimeConnectionString = connectionString
            },
            new TestHostEnvironment("Development"),
            TimeProvider.System,
            TimeZoneInfo.Utc,
            new FakeCatalogMediaStorage(),
            scopeFactory,
            NullLogger<PreviewInitializationService>.Instance);

    /// <summary>The module services the demo seed resolves, over the same test database.</summary>
    private static ServiceProvider CreateServices(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Business:TimeZone"] = "Africa/Cairo",
                ["Catalog:MediaRoot"] = Path.Combine(Path.GetTempPath(), "rigx-preview-tests", Guid.NewGuid().ToString("N"))
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSecurityAuditPersistence(configuration);
        services.AddIdentityPersistence(configuration);
        services.AddIdentityAccounts(new TestHostEnvironment("Development"), configuration);
        services.AddCatalogModule(configuration, AppContext.BaseDirectory);
        services.AddStorefrontModule(configuration);
        services.AddRetailModule(configuration);
        return services.BuildServiceProvider();
    }

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
            ImageWatermark? watermark = null,
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
            ImageWatermark? watermark = null,
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

        public Task<string> SaveSettingsLogoAsync(
            string kind, CatalogImageUpload image, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> SavePrivateImageAsync(
            string area, Guid ownerId, CatalogImageUpload image, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool Exists(string storageKey) => false;

        public Stream OpenRead(string storageKey) => new MemoryStream();

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<StoredMediaFile>> ListFilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredMediaFile>>([]);

        public Task DeleteStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
