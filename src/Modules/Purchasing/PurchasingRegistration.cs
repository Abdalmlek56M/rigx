using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.Purchasing;

/// <summary>Purchasing module registration: one DbContext factory plus one scoped service per feature area.</summary>
public static class PurchasingRegistration
{
    public static IServiceCollection AddPurchasingModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        services.AddDbContextFactory<PurchasingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<SellerService>();
        services.AddScoped<PurchaseService>();
        return services;
    }
}
