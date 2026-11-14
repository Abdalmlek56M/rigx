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
    /// <summary>Every module context in dependency order: its name and how to open it on a connection string.</summary>
    private static readonly (string Name, Func<string, DbContext> Open)[] Contexts =
    [
        (nameof(IdentityDbContext), connectionString => new IdentityDbContext(Options<IdentityDbContext>(connectionString))),
        (nameof(SecurityAuditDbContext), connectionString => new SecurityAuditDbContext(Options<SecurityAuditDbContext>(connectionString))),
        (nameof(CatalogDbContext), connectionString => new CatalogDbContext(Options<CatalogDbContext>(connectionString))),
        (nameof(StorefrontDbContext), connectionString => new StorefrontDbContext(Options<StorefrontDbContext>(connectionString))),
        (nameof(PurchasingDbContext), connectionString => new PurchasingDbContext(Options<PurchasingDbContext>(connectionString))),
        (nameof(InventoryDbContext), connectionString => new InventoryDbContext(Options<InventoryDbContext>(connectionString))),
        (nameof(RetailDbContext), connectionString => new RetailDbContext(Options<RetailDbContext>(connectionString))),
        (nameof(PcBuilderDbContext), connectionString => new PcBuilderDbContext(Options<PcBuilderDbContext>(connectionString)))
    ];

    public static async Task MigrateAllAsync(string connectionString, ILogger logger, CancellationToken cancellationToken = default)
    {
        foreach (var (name, open) in Contexts)
        {
            await using var context = open(connectionString);
            await context.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Migrated {Context}.", name);
        }
    }

    /// <summary>The migrations applied to the database, per module context (recorded in every backup's manifest).</summary>
    public static async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> AppliedMigrationsAsync(
        string connectionString, CancellationToken cancellationToken = default)
    {
        var applied = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var (name, open) in Contexts)
        {
            await using var context = open(connectionString);
            // The history table is shared by every module, so keep only the migrations this module owns.
            var owned = context.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
            applied[name] = [.. (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Where(owned.Contains)];
        }
        return applied;
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

    private static DbContextOptions<TContext> Options<TContext>(string connectionString) where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>().UseNpgsql(connectionString).Options;
}
