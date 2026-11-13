using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Purchasing;

public sealed class PurchasingDbContext(DbContextOptions<PurchasingDbContext> options) : DbContext(options)
{
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseLine> PurchaseLines => Set<PurchaseLine>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.ApplyConfiguration(new SellerConfiguration());
        model.ApplyConfiguration(new PurchaseConfiguration());
        model.ApplyConfiguration(new PurchaseLineConfiguration());
    }
}
