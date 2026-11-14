using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.Storefront;

public static class StorefrontPersistenceRegistration
{
    public static IServiceCollection AddStorefrontModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        services.AddDbContextFactory<StorefrontDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<StoreSettingsService>();
        services.AddScoped<WishlistService>();
        services.AddScoped<ContentArticleService>();
        services.AddScoped<ContentFaqService>();
        services.AddScoped<ContentPolicyService>();
        services.AddScoped<OfferService>();
        services.AddScoped<CustomerRequestService>();
        services.AddScoped<RequestFollowUpService>();
        return services;
    }
}

public sealed class StorefrontDbContextFactory : IDesignTimeDbContextFactory<StorefrontDbContext>
{
    public StorefrontDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<StorefrontDbContext>()
            .UseNpgsql("Host=localhost;Database=storefront_design;Username=unused;Password=unused")
            .Options;
        return new StorefrontDbContext(options);
    }
}
