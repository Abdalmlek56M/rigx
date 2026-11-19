using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HardwareStore.Api.Tests;

/// <summary>Builds a <see cref="PurchaseReceiptService"/> on a test database, with photo storage in its own temp folder.</summary>
internal sealed class PurchaseReceiptTesting(OwnerBootstrapDatabase database, PurchaseService purchases, RetailTestServices retail)
{
    public StoreSettingsService Settings { get; } = new(Factory<StorefrontDbContext>(database),
        new SecurityAuditWriter(Factory<SecurityAuditDbContext>(database)), TimeProvider.System);

    public LocalCatalogMediaStorage Media { get; } = NewMedia();

    public PurchaseReceiptService Create() => new(purchases, retail.Units, retail.Assemblies, Settings,
        new CategorySpecService(Factory<CatalogDbContext>(database), TimeProvider.System), retail.Money,
        new StockPhotoCopyService(retail.StockItems, Media, new MediaWatermarkSource(Settings, Media), NullLogger<StockPhotoCopyService>.Instance));

    private static LocalCatalogMediaStorage NewMedia()
    {
        var options = new CatalogMediaOptions { RootPath = Path.Combine(Path.GetTempPath(), "hardware-store-intake-tests", Guid.NewGuid().ToString("N")) };
        return new LocalCatalogMediaStorage(options, new SkiaCatalogImageProcessor(options));
    }

    private static ContextFactory<T> Factory<T>(OwnerBootstrapDatabase db) where T : DbContext => new(db.ConnectionString);

    private sealed class ContextFactory<T>(string connectionString) : IDbContextFactory<T> where T : DbContext
    {
        public T CreateDbContext() =>
            (T)Activator.CreateInstance(typeof(T), new DbContextOptionsBuilder<T>().UseNpgsql(connectionString).Options)!;
    }
}
