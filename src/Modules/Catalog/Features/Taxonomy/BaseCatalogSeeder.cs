using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

public sealed record BaseCatalogSeedResult(int CategoriesAdded, int BrandsAdded);

/// <summary>
/// Adds the <see cref="BaseCatalog"/> categories and brands that are missing. Runs after every migration (production too), so it
/// only adds: an entry counts as present when its slug is taken or the owner already has one with the same name (same parent
/// for a category), and existing rows are never changed. Categories and brands cannot be deleted, only switched off, so
/// nothing the owner removed comes back.
/// </summary>
public static class BaseCatalogSeeder
{
    public static async Task<BaseCatalogSeedResult> SeedAsync(CatalogDbContext db, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var categoriesAdded = await SeedCategoriesAsync(db, now, cancellationToken);
        var brandsAdded = await SeedBrandsAsync(db, now, cancellationToken);
        if (categoriesAdded + brandsAdded > 0) await db.SaveChangesAsync(cancellationToken);
        return new BaseCatalogSeedResult(categoriesAdded, brandsAdded);
    }

    private static async Task<int> SeedCategoriesAsync(CatalogDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var known = (await db.Categories.AsNoTracking()
                .Select(category => new { category.Id, category.ParentId, category.Name, category.Slug }).ToListAsync(cancellationToken))
            .Select(category => (category.Id, category.ParentId, category.Name, category.Slug)).ToList();
        var idBySlug = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var added = 0;
        foreach (var (entry, index) in BaseCatalog.Categories.Select((entry, index) => (entry, index)))
        {
            Guid? parentId = entry.ParentSlug is null ? null : idBySlug[entry.ParentSlug];
            var match = known.FirstOrDefault(category => category.Slug == entry.Slug).Id;
            if (match == Guid.Empty)
                match = known.FirstOrDefault(category => category.ParentId == parentId && SameName(category.Name, entry.Name)).Id;
            if (match != Guid.Empty)
            {
                idBySlug[entry.Slug] = match;
                continue;
            }
            var category = new CatalogCategory
            {
                Id = Guid.CreateVersion7(),
                ParentId = parentId,
                Name = entry.Name,
                Slug = entry.Slug,
                SortOrder = index,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            db.Categories.Add(category);
            known.Add((category.Id, category.ParentId, category.Name, category.Slug));
            idBySlug[entry.Slug] = category.Id;
            added++;
        }
        return added;
    }

    private static async Task<int> SeedBrandsAsync(CatalogDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await db.Brands.AsNoTracking().Select(brand => new { brand.Name, brand.Slug }).ToListAsync(cancellationToken);
        var added = 0;
        foreach (var entry in BaseCatalog.Brands)
        {
            if (existing.Any(brand => brand.Slug == entry.Slug || SameName(brand.Name, entry.Name))) continue;
            db.Brands.Add(new CatalogBrand
            {
                Id = Guid.CreateVersion7(),
                Name = entry.Name,
                Slug = entry.Slug,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            added++;
        }
        return added;
    }

    private static bool SameName(string left, string right) => string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
