using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HardwareStore.Modules.Purchasing;

/// <summary>Design-time only factory used to generate Purchasing migrations.</summary>
public sealed class PurchasingDbContextFactory : IDesignTimeDbContextFactory<PurchasingDbContext>
{
    public PurchasingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PurchasingDbContext>()
            .UseNpgsql("Host=localhost;Database=purchasing_design;Username=unused;Password=unused")
            .Options;
        return new PurchasingDbContext(options);
    }
}
