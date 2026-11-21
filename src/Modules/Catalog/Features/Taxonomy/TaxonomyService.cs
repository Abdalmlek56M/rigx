using System.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>
/// Categories and brands: public tree/active brands, admin lists, and create/update with slug, version
/// and hierarchy (no cycles) checks.
/// </summary>
public sealed class TaxonomyService(
    IDbContextFactory<CatalogDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<CategoryView>> GetCategoryTreeAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var allCategories = await db.Categories.AsNoTracking()
            .OrderBy(category => category.SortOrder).ThenBy(category => category.Name)
            .Select(category => new CategoryView(
                category.Id, category.ParentId, category.Name, category.Slug,
                category.Description, category.IconStorageKey, category.SortOrder,
                category.IsActive, false, category.RowVersion, category.Code, category.ShowInMenu, category.ShowInBuilder,
                category.SpecDefinitions, Array.Empty<CategoryView>(), category.NameEn))
            .ToListAsync(cancellationToken);
        var effectiveIds = CatalogCategoryPolicy.GetEffectivelyActiveIds(
            allCategories.Select(category => new CatalogCategoryNode(
                category.Id, category.ParentId, category.IsActive)));
        var categories = allCategories
            .Where(category => effectiveIds.Contains(category.Id))
            .Select(category => category with { IsEffectivelyActive = true })
            .ToList();
        return CategoryHierarchy.BuildTree(categories);
    }

    /// <summary>A category plus every active descendant, for filters that include sub-categories.</summary>
    public async Task<IReadOnlyList<Guid>> GetCategoryWithDescendantIdsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var effectiveIds = await CategoryHierarchy.GetEffectiveIdsAsync(db, cancellationToken);
        return effectiveIds.Contains(categoryId)
            ? await CategoryHierarchy.GetDescendantIdsAsync(db, categoryId, effectiveIds, cancellationToken)
            : [categoryId];
    }

    /// <summary>A category and its parents up to the root (the category first); empty when the category does not exist.</summary>
    public async Task<IReadOnlyList<Guid>> GetCategoryPathIdsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var parents = await db.Categories.AsNoTracking()
            .ToDictionaryAsync(category => category.Id, category => category.ParentId, cancellationToken);
        var path = new List<Guid>();
        for (Guid? current = categoryId; current is { } id && parents.TryGetValue(id, out var parent) && !path.Contains(id); current = parent)
            path.Add(id);
        return path;
    }

    public async Task<IReadOnlyList<BrandView>> GetActiveBrandsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Brands.AsNoTracking()
            .Where(brand => brand.IsActive)
            .OrderBy(brand => brand.Name)
            .Select(brand => new BrandView(
                brand.Id, brand.Name, brand.Slug, brand.Description,
                brand.LogoStorageKey, brand.SupportUrl, brand.IsActive, brand.RowVersion))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryView>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking()
            .OrderBy(category => category.SortOrder).ThenBy(category => category.Name)
            .Select(category => new CategoryView(
                category.Id, category.ParentId, category.Name, category.Slug,
                category.Description, category.IconStorageKey, category.SortOrder,
                category.IsActive, false, category.RowVersion, category.Code, category.ShowInMenu, category.ShowInBuilder,
                category.SpecDefinitions, Array.Empty<CategoryView>(), category.NameEn))
            .ToListAsync(cancellationToken);
        var effectiveIds = CatalogCategoryPolicy.GetEffectivelyActiveIds(
            categories.Select(category => new CatalogCategoryNode(
                category.Id, category.ParentId, category.IsActive)));
        return categories.Select(category =>
            category with { IsEffectivelyActive = effectiveIds.Contains(category.Id) }).ToList();
    }

    public async Task<IReadOnlyList<BrandView>> GetBrandsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Brands.AsNoTracking().OrderBy(brand => brand.Name)
            .Select(brand => new BrandView(
                brand.Id, brand.Name, brand.Slug, brand.Description,
                brand.LogoStorageKey, brand.SupportUrl, brand.IsActive, brand.RowVersion))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryView> SaveCategoryAsync(
        Guid? id,
        CategoryInput input,
        CancellationToken cancellationToken = default)
    {
        var name = CatalogValidation.Required(input.Name, "name", 160);
        var nameEn = CatalogValidation.Optional(input.NameEn, "nameEn", 160);
        var slug = CatalogValidation.Slug(input.Slug);
        var description = CatalogValidation.Optional(input.Description, "description", 2000);
        var iconKey = CatalogValidation.Optional(input.IconStorageKey, "iconStorageKey", 512);
        var code = CategorySpecSchema.NormalizeCategoryCode(input.Code);
        if (input.SortOrder < 0)
            throw new CatalogValidationException("sortOrder", "Sort order must not be negative.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var category = id is null
            ? new CatalogCategory
            {
                Id = Guid.CreateVersion7(),
                Name = string.Empty,
                Slug = string.Empty,
                CreatedAtUtc = UtcNow()
            }
            : await db.Categories.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
              ?? throw new CatalogNotFoundException("Category was not found.");
        CatalogValidation.EnsureVersion(category.RowVersion, input.RowVersion, id);

        if (id is not null && input.ParentId == id)
            throw new CatalogValidationException("parentId", "A category cannot be its own parent.");
        if (input.ParentId is not null)
            await CategoryHierarchy.EnsureParentIsAcyclicAsync(db, category.Id, input.ParentId.Value, cancellationToken);
        if (await db.Categories.AnyAsync(item => item.Slug == slug && item.Id != category.Id, cancellationToken))
            throw new CatalogConflictException("Category slug is already in use.");
        if (code is not null && await db.Categories.AnyAsync(item => item.Code == code && item.Id != category.Id, cancellationToken))
            throw new CatalogConflictException("Category code is already in use.");

        category.Name = name;
        category.NameEn = nameEn;
        category.Slug = slug;
        category.ParentId = input.ParentId;
        category.Description = description;
        category.IconStorageKey = iconKey;
        category.SortOrder = input.SortOrder;
        category.IsActive = input.IsActive;
        category.Code = code;
        category.ShowInMenu = input.ShowInMenu;
        category.ShowInBuilder = input.ShowInBuilder;
        category.UpdatedAtUtc = UtcNow();
        if (id is not null)
            category.RowVersion++;
        else
            db.Categories.Add(category);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new CatalogConflictException("Category data conflicts with an existing record.");
        }
        return category.ToView(await CategoryHierarchy.IsEffectivelyActiveAsync(db, category.Id, cancellationToken));
    }

    public async Task<BrandView> SaveBrandAsync(
        Guid? id,
        BrandInput input,
        CancellationToken cancellationToken = default)
    {
        var name = CatalogValidation.Required(input.Name, "name", 160);
        var slug = CatalogValidation.Slug(input.Slug);
        var description = CatalogValidation.Optional(input.Description, "description", 2000);
        var logoKey = CatalogValidation.Optional(input.LogoStorageKey, "logoStorageKey", 512);
        var supportUrl = CatalogValidation.Optional(input.SupportUrl, "supportUrl", 512);
        if (supportUrl is not null &&
            (!Uri.TryCreate(supportUrl, UriKind.Absolute, out var supportUri) ||
             supportUri.Scheme is not ("http" or "https")))
            throw new CatalogValidationException("supportUrl", "Support URL must be an absolute HTTP or HTTPS URL.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var brand = id is null
            ? new CatalogBrand
            {
                Id = Guid.CreateVersion7(),
                Name = string.Empty,
                Slug = string.Empty,
                CreatedAtUtc = UtcNow()
            }
            : await db.Brands.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
              ?? throw new CatalogNotFoundException("Brand was not found.");
        CatalogValidation.EnsureVersion(brand.RowVersion, input.RowVersion, id);
        if (await db.Brands.AnyAsync(item => item.Slug == slug && item.Id != brand.Id, cancellationToken))
            throw new CatalogConflictException("Brand slug is already in use.");

        brand.Name = name;
        brand.Slug = slug;
        brand.Description = description;
        brand.LogoStorageKey = logoKey;
        brand.SupportUrl = supportUrl;
        brand.IsActive = input.IsActive;
        brand.UpdatedAtUtc = UtcNow();
        if (id is null)
            db.Brands.Add(brand);
        else
            brand.RowVersion++;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new CatalogConflictException("Brand data conflicts with an existing record.");
        }
        return brand.ToView();
    }

    private DateTimeOffset UtcNow() => timeProvider.GetUtcNow().ToUniversalTime();
}
