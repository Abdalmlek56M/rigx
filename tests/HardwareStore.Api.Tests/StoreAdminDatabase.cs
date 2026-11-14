using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Inventory;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class StoreAdminPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("store_admin_test")
        .WithUsername("store_admin_test")
        .WithPassword("test-only-password")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();
    }

    public async Task<StoreAdminDatabase> CreateDatabaseAsync()
    {
        var databaseName = $"store_admin_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(ConnectionString))
        {
            await admin.OpenAsync();
            await using var command = admin.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Database = databaseName
        };
        var database = new StoreAdminDatabase(builder.ConnectionString);
        await using var identity = database.CreateIdentityContext();
        await using var audit = database.CreateAuditContext();
        await using var catalog = database.CreateCatalogContext();
        await using var inventory = database.CreateInventoryContext();
        await using var storefront = database.CreateStorefrontContext();
        await identity.Database.MigrateAsync();
        await CreateAuditRolesAsync(builder.ConnectionString);
        await audit.Database.MigrateAsync();
        await catalog.Database.MigrateAsync();
        await inventory.Database.MigrateAsync();
        await storefront.Database.MigrateAsync();
        return database;
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

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
}

public sealed class StoreAdminDatabase(string connectionString)
{
    public string ConnectionString => connectionString;

    public IdentityDbContext CreateIdentityContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);

    public SecurityAuditDbContext CreateAuditContext() =>
        new(new DbContextOptionsBuilder<SecurityAuditDbContext>().UseNpgsql(connectionString).Options);

    public CatalogDbContext CreateCatalogContext() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString).Options);

    public InventoryDbContext CreateInventoryContext() =>
        new(new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(connectionString).Options);

    public StorefrontDbContext CreateStorefrontContext() =>
        new(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);

    public StoreSettingsService CreateStoreSettingsService()
    {
        var auditFactory = new TestAuditFactory(connectionString);
        return new StoreSettingsService(
            new TestStorefrontFactory(connectionString),
            new SecurityAuditWriter(auditFactory),
            TimeProvider.System);
    }

    public WarehouseDirectoryService CreateWarehouseService()
    {
        var auditFactory = new TestAuditFactory(connectionString);
        return new WarehouseDirectoryService(
            new TestInventoryFactory(connectionString),
            new SecurityAuditWriter(auditFactory),
            TimeProvider.System);
    }

    private sealed class TestAuditFactory(string connectionString) : IDbContextFactory<SecurityAuditDbContext>
    {
        public SecurityAuditDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<SecurityAuditDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class TestInventoryFactory(string connectionString) : IDbContextFactory<InventoryDbContext>
    {
        public InventoryDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class TestStorefrontFactory(string connectionString) : IDbContextFactory<StorefrontDbContext>
    {
        public StorefrontDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);
    }
}
