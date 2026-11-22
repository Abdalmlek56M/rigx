using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>The names of brands, categories and products by id, for screens that store a link as an id (the audit log).</summary>
public sealed class CatalogNames(IDbContextFactory<CatalogDbContext> contextFactory)
{
    /// <summary>Each id that is a brand, a category or a product, with its name; an id that is none of them is left out.</summary>
    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return new Dictionary<Guid, string>();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var brands = await db.Brands.AsNoTracking().Where(brand => ids.Contains(brand.Id)).Select(brand => new { brand.Id, brand.Name }).ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().Where(category => ids.Contains(category.Id)).Select(category => new { category.Id, category.Name }).ToListAsync(cancellationToken);
        var products = await db.Products.AsNoTracking().Where(product => ids.Contains(product.Id)).Select(product => new { product.Id, product.Name }).ToListAsync(cancellationToken);
        return brands.Concat(categories).Concat(products).GroupBy(row => row.Id).ToDictionary(group => group.Key, group => group.First().Name);
    }
}
