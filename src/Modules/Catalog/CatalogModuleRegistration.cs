using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.Catalog;

public static class CatalogModuleRegistration
{
    public static IServiceCollection AddCatalogModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        var mediaRoot = configuration["Catalog:MediaRoot"];
        if (string.IsNullOrWhiteSpace(mediaRoot))
            mediaRoot = Path.Combine(contentRootPath, "App_Data", "catalog-media");

        services.AddSingleton(new CatalogMediaOptions
        {
            RootPath = Path.GetFullPath(mediaRoot),
            MaxUploadBytes = ReadInt(configuration, "Catalog:Media:MaxUploadBytes", 12 * 1024 * 1024),
            MaxStoredBytes = ReadInt(configuration, "Catalog:Media:MaxStoredBytes", 8 * 1024 * 1024),
            MaxSourceDimension = ReadInt(configuration, "Catalog:Media:MaxSourceDimension", 8192),
            MaxSourcePixels = ReadLong(configuration, "Catalog:Media:MaxSourcePixels", 40_000_000L),
            StoredMaxDimension = ReadInt(configuration, "Catalog:Media:StoredMaxDimension", 2000),
            ThumbnailMaxDimension = ReadInt(configuration, "Catalog:Media:ThumbnailMaxDimension", 480),
            WebpQuality = ReadInt(configuration, "Catalog:Media:WebpQuality", 82),
            OrphanGracePeriod = TimeSpan.FromMinutes(
                ReadInt(configuration, "Catalog:Media:OrphanGraceMinutes", 30))
        });
        services.AddDbContextFactory<CatalogDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<ICatalogImageProcessor, SkiaCatalogImageProcessor>();
        services.AddSingleton<ICatalogMediaStorage, LocalCatalogMediaStorage>();
        services.AddScoped<ProductSearchService>();
        services.AddScoped<ProductManagementService>();
        services.AddScoped<ProductImageService>();
        services.AddScoped<TaxonomyService>();
        services.AddScoped<CategorySpecService>();
        services.AddScoped<TaxonomyImageService>();
        services.AddScoped<CatalogMediaMaintenanceService>();
        services.AddScoped<ContentMediaService>();
        services.AddScoped<ITrashSource, ContentMediaTrashSource>();
        services.AddScoped<ReviewService>();
        services.AddScoped<IContentMediaUsageSink>(provider => provider.GetRequiredService<ContentMediaService>());
        return services;
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback) =>
        int.TryParse(configuration[key], out var value) ? value : fallback;

    private static long ReadLong(IConfiguration configuration, string key, long fallback) =>
        long.TryParse(configuration[key], out var value) ? value : fallback;
}
