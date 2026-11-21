using HardwareStore.Common.Infrastructure.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.Catalog;

/// <summary>What the audit log can put back in Catalog: names, descriptions and display flags (never slugs, SKUs, prices or media).</summary>
internal static class CatalogAuditRestore
{
    public static IServiceCollection AddCatalogAuditRestore(this IServiceCollection services) => services
        .AddAuditRestoreTarget<CatalogDbContext, CatalogCategory>(
            nameof(CatalogCategory.Name), nameof(CatalogCategory.NameEn), nameof(CatalogCategory.Description), nameof(CatalogCategory.SortOrder), nameof(CatalogCategory.IsActive),
            nameof(CatalogCategory.ShowInMenu), nameof(CatalogCategory.ShowInBuilder), nameof(CatalogCategory.SpecDefinitions))
        .AddAuditRestoreTarget<CatalogDbContext, CatalogBrand>(
            nameof(CatalogBrand.Name), nameof(CatalogBrand.Description), nameof(CatalogBrand.SupportUrl), nameof(CatalogBrand.IsActive))
        .AddAuditRestoreTarget<CatalogDbContext, CatalogProduct>(
            nameof(CatalogProduct.Name), nameof(CatalogProduct.ShortDescription), nameof(CatalogProduct.Description));
}
