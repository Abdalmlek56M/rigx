using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>Saves a hand-chosen order (drag and drop in the admin) of the brands and of the categories under one parent.</summary>
public sealed class TaxonomyOrderService(IDbContextFactory<CatalogDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task ReorderBrandsAsync(IReadOnlyList<Guid>? ids, CancellationToken cancellationToken = default)
    {
        var order = CatalogOrdering.Require(ids);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var brands = await db.Brands.ToListAsync(cancellationToken);
        CatalogOrdering.EnsureSameRecords(order, [.. brands.Select(brand => brand.Id)]);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var position = CatalogOrdering.Positions(order);
        foreach (var brand in brands.Where(brand => brand.SortOrder != position[brand.Id]))
        {
            brand.SortOrder = position[brand.Id];
            brand.RowVersion++;
            brand.UpdatedAtUtc = now;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderCategoriesAsync(Guid? parentId, IReadOnlyList<Guid>? ids, CancellationToken cancellationToken = default)
    {
        var order = CatalogOrdering.Require(ids);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var siblings = await db.Categories.Where(category => category.ParentId == parentId).ToListAsync(cancellationToken);
        CatalogOrdering.EnsureSameRecords(order, [.. siblings.Select(category => category.Id)]);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var position = CatalogOrdering.Positions(order);
        foreach (var category in siblings.Where(category => category.SortOrder != position[category.Id]))
        {
            category.SortOrder = position[category.Id];
            category.RowVersion++;
            category.UpdatedAtUtc = now;
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
