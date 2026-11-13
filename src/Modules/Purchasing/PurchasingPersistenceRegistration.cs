using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.Purchasing;

public static class PurchasingPersistenceRegistration
{
    public static IServiceCollection AddPurchasingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        services.AddDbContextFactory<PurchasingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<SupplierDirectoryService>();
        services.AddScoped<SupplierProductCatalogService>();
        services.AddScoped<PurchaseDraftService>();
        return services;
    }
}
