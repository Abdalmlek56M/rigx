using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

public sealed record BaseCatalogSeedResult(int CategoriesAdded, int BrandsAdded, int CategoriesRenamed = 0);

/// <summary>
/// Adds the <see cref="BaseCatalog"/> categories and brands that are missing. Runs after every migration (production too), so it
/// only adds: an entry counts as present when its slug is taken or the owner already has one with the same name (same parent
/// for a category), and existing rows are never changed except by <see cref="BaseCatalog.Renames"/> on a row that still has the old
/// starting name. Categories and brands cannot be deleted, only switched off, so nothing the owner removed comes back.
/// </summary>
public static class BaseCatalogSeeder
{
    public static async Task<BaseCatalogSeedResult> SeedAsync(CatalogDbContext db, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var categoriesRenamed = await RenameCategoriesAsync(db, now, cancellationToken);
        var categoriesAdded = await SeedCategoriesAsync(db, now, cancellationToken);
        var brandsAdded = await SeedBrandsAsync(db, now, cancellationToken);
        var specsAdded = await SeedSpecsAsync(db, now, cancellationToken);
        var slotsAdded = await SeedBuilderSlotsAsync(db, now, cancellationToken);
        var englishFilled = await FillEnglishNamesAsync(db, now, cancellationToken);
        if (categoriesRenamed + categoriesAdded + brandsAdded + specsAdded + englishFilled + slotsAdded > 0) await db.SaveChangesAsync(cancellationToken);
        return new BaseCatalogSeedResult(categoriesAdded, brandsAdded, categoriesRenamed);
    }

    /// <summary>Applies a rename only where the category still has its old starting name (the owner never renamed it).</summary>
    private static async Task<int> RenameCategoriesAsync(CatalogDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var renamed = 0;
        foreach (var rename in BaseCatalog.Renames)
        {
            var category = await db.Categories.FirstOrDefaultAsync(category => category.Slug == rename.Slug, cancellationToken);
            if (category is null || !SameName(category.Name, rename.OldName)) continue;
            category.Name = rename.NewName;
            category.UpdatedAtUtc = now;
            renamed++;
        }
        return renamed;
    }

    /// <summary>Gives a base category its English name where it has none (a name the owner typed is never replaced).</summary>
    private static async Task<int> FillEnglishNamesAsync(CatalogDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        var filled = 0;
        foreach (var entry in BaseCatalog.Categories.Where(entry => entry.NameEn is not null))
        {
            var category = await db.Categories.FirstOrDefaultAsync(item => item.Slug == entry.Slug && item.NameEn == null, cancellationToken);
            if (category is null) continue;
            category.NameEn = entry.NameEn;
            category.UpdatedAtUtc = now;
            filled++;
        }
        return filled;
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
                match = known.FirstOrDefault(category => category.ParentId == parentId && KnownNames(entry).Any(name => SameName(category.Name, name))).Id;
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
                NameEn = entry.NameEn,
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

    /// <summary>The entry's name and its old starting names, so a category still named the old way is not added twice.</summary>
    private static IEnumerable<string> KnownNames(BaseCategory entry) =>
        BaseCatalog.Renames.Where(rename => rename.Slug == entry.Slug).Select(rename => rename.OldName).Prepend(entry.Name);

    /// <summary>Starter specs (<see cref="BaseCategorySpecs"/>) for base categories that have no specs of their own yet.</summary>
    private static async Task<int> SeedSpecsAsync(CatalogDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var slugs = BaseCategorySpecs.BySlug.Keys.ToList();
        await db.Categories.Where(category => slugs.Contains(category.Slug)).LoadAsync(cancellationToken);
        var added = 0;
        foreach (var category in db.Categories.Local.Where(category => slugs.Contains(category.Slug)).ToList())
        {
            // No specs yet: the starter list. The first starter list, untouched: upgraded to today's (the owner's own lists are never changed).
            var list = category.SpecDefinitions.Count == 0
                ? BaseCategorySpecs.BySlug[category.Slug]
                : BaseCategorySpecs.Upgrade(category.Slug, category.SpecDefinitions);
            if (list is null) continue;
            category.SpecDefinitions = CategorySpecSchema.Normalize(list).ToList();
            category.UpdatedAtUtc = now;
            if (db.Entry(category).State != EntityState.Added) category.RowVersion++;
            added++;
        }
        return added;
    }

    /// <summary>
    /// Makes the base categories the builder's slots (code and "show in builder") when no category has a builder code yet. Once the
    /// owner has set any slot up, nothing is touched, so a slot the owner removed does not come back.
    /// </summary>
    private static async Task<int> SeedBuilderSlotsAsync(CatalogDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await db.Categories.AnyAsync(category => category.Code != null || category.ShowInBuilder, cancellationToken)) return 0;
        var slugs = BaseCatalog.BuilderSlots.Keys.ToList();
        await db.Categories.Where(category => slugs.Contains(category.Slug)).LoadAsync(cancellationToken);
        var changed = 0;
        foreach (var category in db.Categories.Local.Where(category => slugs.Contains(category.Slug)).ToList())
        {
            category.Code = BaseCatalog.BuilderSlots[category.Slug];
            category.ShowInBuilder = true;
            category.UpdatedAtUtc = now;
            if (db.Entry(category).State != EntityState.Added) category.RowVersion++;
            changed++;
        }
        return changed;
    }

    private static bool SameName(string left, string right) => string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
