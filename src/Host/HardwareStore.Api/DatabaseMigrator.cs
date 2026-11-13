using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Inventory;
using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Applies every module's EF Core migrations in dependency order.
/// Single source of truth for "which contexts exist and in what order" — used by the
/// <c>--migrate</c> command and by development initialization.
/// Uses <c>ConnectionStrings:Migrations</c> (schema-owner role) when set, otherwise <c>Default</c>.
/// </summary>
public static class DatabaseMigrator
{
    public static async Task MigrateAllAsync(string connectionString, ILogger logger, CancellationToken cancellationToken = default)
    {
        await MigrateAsync<IdentityDbContext>(connectionString, options => new IdentityDbContext(options), logger, cancellationToken);
        await MigrateAsync<SecurityAuditDbContext>(connectionString, options => new SecurityAuditDbContext(options), logger, cancellationToken);
        await MigrateAsync<CatalogDbContext>(connectionString, options => new CatalogDbContext(options), logger, cancellationToken);
        await MigrateAsync<StorefrontDbContext>(connectionString, options => new StorefrontDbContext(options), logger, cancellationToken);
        await MigrateAsync<PurchasingDbContext>(connectionString, options => new PurchasingDbContext(options), logger, cancellationToken);
        await MigrateAsync<InventoryDbContext>(connectionString, options => new InventoryDbContext(options), logger, cancellationToken);
        await MigrateAsync<RetailDbContext>(connectionString, options => new RetailDbContext(options), logger, cancellationToken);
        await MigrateAsync<PcBuilderDbContext>(connectionString, options => new PcBuilderDbContext(options), logger, cancellationToken);
    }

    /// <summary>Entry point for <c>dotnet run -- --migrate</c>. Returns a process exit code.</summary>
    public static async Task<int> RunCommandAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseMigrator));
        var connectionString = configuration.GetConnectionString("Migrations") ?? configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogError("No connection string: set ConnectionStrings:Migrations or ConnectionStrings:Default.");
            return 2;
        }

        try
        {
            await MigrateAllAsync(connectionString, logger, cancellationToken);
            logger.LogInformation("All module migrations applied.");
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Migration failed; the database was left at the last successful module.");
            return 1;
        }
    }

    private static async Task MigrateAsync<TContext>(
        string connectionString,
        Func<DbContextOptions<TContext>, TContext> create,
        ILogger logger,
        CancellationToken cancellationToken) where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>().UseNpgsql(connectionString).Options;
        await using var context = create(options);
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migrated {Context}.", typeof(TContext).Name);
    }
}
