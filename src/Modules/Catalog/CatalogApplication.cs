using System.Data;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

public sealed record CategoryInput(
    string? Name,
    string? Slug,
    Guid? ParentId,
    string? Description,
    string? IconStorageKey,
    int SortOrder,
    bool IsActive,
    int? RowVersion);

public sealed record BrandInput(
    string? Name,
    string? Slug,
    string? Description,
    string? LogoStorageKey,
    string? SupportUrl,
    bool IsActive,
    int? RowVersion);

public sealed record ProductVariantInput(
    Guid? Id,
    string? Name,
    string? Sku,
    string? Barcode,
    decimal? Price,
    bool IsActive);

public sealed record ProductSpecificationInput(
    Guid? Id,
    Guid? VariantId,
    string? VariantSku,
    string? GroupName,
    string? Name,
    string? Value,
    int SortOrder);

public sealed record ProductInput(
    string? Name,
    string? Slug,
    string? Sku,
    string? Barcode,
    Guid CategoryId,
    Guid? BrandId,
    string? ShortDescription,
    string? Description,
    decimal BasePrice,
    CatalogProductStatus Status,
    CatalogProductCondition Condition,
    IReadOnlyList<ProductVariantInput>? Variants,
    IReadOnlyList<ProductSpecificationInput>? Specifications,
    int? RowVersion);

public sealed record CategoryView(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    string? IconStorageKey,
    int SortOrder,
    bool IsActive,
    bool IsEffectivelyActive,
    int RowVersion,
    IReadOnlyList<CategoryView> Children);

public sealed record BrandView(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoStorageKey,
    string? SupportUrl,
    bool IsActive,
    int RowVersion);

public sealed record ProductVariantView(
    Guid Id,
    string Name,
    string Sku,
    string? Barcode,
    decimal? Price,
    bool IsActive);

public sealed record ProductSpecificationView(
    Guid Id,
    Guid? VariantId,
    string GroupName,
    string Name,
    string Value,
    int SortOrder);

public sealed record ProductSpecificationSummaryView(
    string GroupName,
    string Name,
    string Value);

public sealed record ProductImageView(
    Guid Id,
    string StorageKey,
    string ThumbnailStorageKey,
    string AltText,
    int SortOrder,
    bool IsPrimary,
    int Width,
    int Height,
    int ThumbnailWidth,
    int ThumbnailHeight);

public sealed record ProductSummaryView(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    decimal BasePrice,
    decimal FromPrice,
    CatalogProductStatus Status,
    CatalogProductCondition Condition,
    string CategoryName,
    string CategorySlug,
    string? BrandName,
    IReadOnlyList<ProductSpecificationSummaryView> Specifications,
    string? PrimaryImageStorageKey);

public sealed record ProductDetailView(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    string? Barcode,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    Guid? BrandId,
    string? BrandName,
    string? ShortDescription,
    string? Description,
    decimal BasePrice,
    CatalogProductStatus Status,
    CatalogProductCondition Condition,
    int RowVersion,
    IReadOnlyList<ProductVariantView> Variants,
    IReadOnlyList<ProductSpecificationView> Specifications,
    IReadOnlyList<ProductImageView> Images);

public sealed record ProductPageView(
    IReadOnlyList<ProductSummaryView> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record ProductSuggestionView(
    Guid Id,
    string Name,
    string Slug,
    string Sku,
    decimal BasePrice,
    string Condition,
    string CategorySlug,
    string? BrandName,
    string? ThumbnailStorageKey);

public sealed record SpecificationFilterView(
    string GroupName,
    string Name,
    IReadOnlyList<string> Values);

public enum CatalogProductSort
{
    Newest,
    Oldest,
    PriceAscending,
    PriceDescending,
    NameAscending,
    NameDescending
}

public sealed class CatalogStorefrontService(IDbContextFactory<CatalogDbContext> contextFactory)
{
    public async Task<IReadOnlyList<ProductSuggestionView>> SuggestAsync(string? search, int limit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search)) return [];
        var term = search.Trim();
        if (term.Length > 80) throw new CatalogValidationException("search", "Search text must not exceed 80 characters.");
        limit = Math.Clamp(limit, 1, 10);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var categoryIds = await GetEffectiveCategoryIdsAsync(db, cancellationToken);
        if (categoryIds.Length == 0) return [];
        var rows = await db.Products.AsNoTracking()
            .Where(product => product.Status == CatalogProductStatus.Published &&
                              categoryIds.Contains(product.CategoryId) &&
                              (product.BrandId == null || product.Brand!.IsActive) &&
                              (EF.Functions.ILike(product.Name, $"%{term}%") ||
                               EF.Functions.ILike(product.Sku, $"%{term}%") ||
                               (product.Brand != null && EF.Functions.ILike(product.Brand.Name, $"%{term}%")) ||
                               EF.Functions.ILike(product.Category.Name, $"%{term}%")))
            .OrderBy(product => product.Name)
            .Take(limit)
            .Select(product => new
            {
                product.Id, product.Name, product.Slug, product.Sku, product.BasePrice, product.Condition,
                CategorySlug = product.Category.Slug,
                BrandName = product.Brand != null ? product.Brand.Name : null,
                ThumbnailStorageKey = product.Images.Where(image => image.IsPrimary)
                    .Select(image => image.ThumbnailStorageKey).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        return rows.Select(product => new ProductSuggestionView(
            product.Id, product.Name, product.Slug, product.Sku, product.BasePrice, product.Condition.ToString(),
            product.CategorySlug, product.BrandName, product.ThumbnailStorageKey)).ToList();
    }

    public async Task<IReadOnlyList<CategoryView>> GetCategoryTreeAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var allCategories = await db.Categories.AsNoTracking()
            .OrderBy(category => category.SortOrder).ThenBy(category => category.Name)
            .Select(category => new CategoryView(
                category.Id, category.ParentId, category.Name, category.Slug,
                category.Description, category.IconStorageKey, category.SortOrder,
                category.IsActive, false, category.RowVersion, Array.Empty<CategoryView>()))
            .ToListAsync(cancellationToken);
        var effectiveIds = CatalogCategoryPolicy.GetEffectivelyActiveIds(
            allCategories.Select(category => new CatalogCategoryNode(
                category.Id, category.ParentId, category.IsActive)));
        var categories = allCategories
            .Where(category => effectiveIds.Contains(category.Id))
            .Select(category => category with { IsEffectivelyActive = true })
            .ToList();
        return BuildTree(categories);
    }

    public async Task<IReadOnlyList<BrandView>> GetBrandsAsync(
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

    public async Task<ProductPageView> GetProductsAsync(
        string? categorySlug,
        string? brandSlug,
        string? search,
        CatalogProductCondition? condition,
        decimal? minimumPrice,
        decimal? maximumPrice,
        IReadOnlyCollection<string>? specificationFilters,
        CatalogProductSort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        if (minimumPrice is < 0 || maximumPrice is < 0)
            throw new CatalogValidationException("price", "Price filters must not be negative.");
        if (minimumPrice is not null && maximumPrice is not null && minimumPrice > maximumPrice)
            throw new CatalogValidationException("price", "Minimum price cannot exceed maximum price.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var effectiveCategoryIds = await GetEffectiveCategoryIdsAsync(db, cancellationToken);
        if (effectiveCategoryIds.Length == 0)
            return new ProductPageView([], page, pageSize, 0);
        var query = db.Products.AsNoTracking()
            .Where(product => product.Status == CatalogProductStatus.Published &&
                              effectiveCategoryIds.Contains(product.CategoryId) &&
                              (product.BrandId == null || product.Brand!.IsActive));
        if (condition.HasValue)
            query = query.Where(product => product.Condition == condition.Value);

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            var normalizedCategory = CatalogValidation.Slug(categorySlug);
            var category = await db.Categories.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Slug == normalizedCategory, cancellationToken)
                ?? throw new CatalogNotFoundException("Category was not found.");
            if (!effectiveCategoryIds.Contains(category.Id))
                throw new CatalogNotFoundException("Category was not found.");
            var categoryIds = await GetDescendantCategoryIdsAsync(
                db, category.Id, effectiveCategoryIds, cancellationToken);
            query = query.Where(product => categoryIds.Contains(product.CategoryId));
        }

        if (!string.IsNullOrWhiteSpace(brandSlug))
        {
            var normalizedBrand = CatalogValidation.Slug(brandSlug);
            query = query.Where(product => product.Brand != null && product.Brand.Slug == normalizedBrand);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 160)
                throw new CatalogValidationException("search", "Search text must not exceed 160 characters.");
            query = query.Where(product =>
                EF.Functions.ILike(product.Name, $"%{term}%") ||
                EF.Functions.ILike(product.Sku, $"%{term}%") ||
                (product.Brand != null && EF.Functions.ILike(product.Brand.Name, $"%{term}%")) ||
                EF.Functions.ILike(product.Category.Name, $"%{term}%"));
        }

        if (specificationFilters is { Count: > 0 })
        {
            var parsedFilters = specificationFilters
                .Select(ParseSpecificationFilter)
                .GroupBy(filter => (filter.GroupName, filter.Name))
                .ToArray();
            foreach (var filterGroup in parsedFilters)
            {
                var values = filterGroup.Select(filter => filter.Value).Distinct(StringComparer.Ordinal).ToArray();
                var groupName = filterGroup.Key.GroupName;
                var name = filterGroup.Key.Name;
                query = query.Where(product => product.Specifications.Any(specification =>
                    (specification.VariantId == null || specification.Variant!.IsActive) &&
                    specification.GroupName == groupName &&
                    specification.Name == name &&
                    values.Contains(specification.Value)));
            }
        }

        var priced = query.Select(product => new
        {
            Product = product,
            FromPrice = product.Variants
                .Where(variant => variant.IsActive && variant.Price != null)
                .Select(variant => variant.Price)
                .Min() ?? product.BasePrice
        });
        if (minimumPrice is not null)
            priced = priced.Where(item => item.FromPrice >= minimumPrice);
        if (maximumPrice is not null)
            priced = priced.Where(item => item.FromPrice <= maximumPrice);

        var total = await priced.CountAsync(cancellationToken);
        page = total == 0 ? 1 : Math.Min(page, (int)Math.Ceiling(total / (double)pageSize));
        priced = sort switch
        {
            CatalogProductSort.Oldest => priced.OrderBy(item => item.Product.CreatedAtUtc).ThenBy(item => item.Product.Name),
            CatalogProductSort.PriceAscending => priced.OrderBy(item => item.FromPrice).ThenBy(item => item.Product.Name),
            CatalogProductSort.PriceDescending => priced.OrderByDescending(item => item.FromPrice).ThenBy(item => item.Product.Name),
            CatalogProductSort.NameAscending => priced.OrderBy(item => item.Product.Name).ThenBy(item => item.FromPrice),
            CatalogProductSort.NameDescending => priced.OrderByDescending(item => item.Product.Name).ThenBy(item => item.FromPrice),
            _ => priced.OrderByDescending(item => item.Product.CreatedAtUtc).ThenBy(item => item.Product.Name)
        };
        var items = await priced
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new ProductSummaryView(
                item.Product.Id,
                item.Product.Name,
                item.Product.Slug,
                item.Product.Sku,
                item.Product.BasePrice,
                item.FromPrice,
                item.Product.Status,
                item.Product.Condition,
                item.Product.Category.Name,
                item.Product.Category.Slug,
                item.Product.Brand != null ? item.Product.Brand.Name : null,
                item.Product.Specifications
                    .Where(specification => specification.VariantId == null)
                    .OrderBy(specification => specification.SortOrder)
                    .ThenBy(specification => specification.Name)
                    .Take(3)
                    .Select(specification => new ProductSpecificationSummaryView(
                        specification.GroupName,
                        specification.Name,
                        specification.Value))
                    .ToList(),
                item.Product.Images.Where(image => image.IsPrimary).Select(image => image.ThumbnailStorageKey).FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new ProductPageView(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<SpecificationFilterView>> GetSpecificationFiltersAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var effectiveCategoryIds = await GetEffectiveCategoryIdsAsync(db, cancellationToken);
        if (effectiveCategoryIds.Length == 0)
            return [];

        var specifications = await db.ProductSpecifications.AsNoTracking()
            .Where(specification =>
                specification.Product.Status == CatalogProductStatus.Published &&
                effectiveCategoryIds.Contains(specification.Product.CategoryId) &&
                (specification.Product.BrandId == null || specification.Product.Brand!.IsActive) &&
                (specification.VariantId == null || specification.Variant!.IsActive))
            .Select(specification => new
            {
                specification.GroupName,
                specification.Name,
                specification.Value
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        return specifications
            .GroupBy(item => (item.GroupName, item.Name))
            .Where(group => group.Select(item => item.Value).Distinct(StringComparer.Ordinal).Count() > 1)
            .OrderBy(group => group.Key.GroupName)
            .ThenBy(group => group.Key.Name)
            .Select(group => new SpecificationFilterView(
                group.Key.GroupName,
                group.Key.Name,
                group.Select(item => item.Value)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();
    }

    public async Task<ProductDetailView> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var normalizedSlug = CatalogValidation.Slug(slug);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var effectiveCategoryIds = await GetEffectiveCategoryIdsAsync(db, cancellationToken);
        var product = await db.Products.AsNoTracking()
            .Where(item => item.Slug == normalizedSlug &&
                           item.Status == CatalogProductStatus.Published &&
                           effectiveCategoryIds.Contains(item.CategoryId) &&
                           (item.BrandId == null || item.Brand!.IsActive))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CatalogNotFoundException("Product was not found.");

        return await LoadDetailAsync(db, product.Id, cancellationToken, includeInactiveVariants: false);
    }

    internal static async Task<ProductDetailView> LoadDetailAsync(
        CatalogDbContext db,
        Guid productId,
        CancellationToken cancellationToken,
        bool includeInactiveVariants = true)
    {
        return await db.Products.AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => new ProductDetailView(
                product.Id,
                product.Name,
                product.Slug,
                product.Sku,
                product.Barcode,
                product.CategoryId,
                product.Category.Name,
                product.Category.Slug,
                product.BrandId,
                product.Brand != null ? product.Brand.Name : null,
                product.ShortDescription,
                product.Description,
                product.BasePrice,
                product.Status,
                product.Condition,
                product.RowVersion,
                product.Variants.Where(variant => includeInactiveVariants || variant.IsActive)
                    .OrderBy(variant => variant.Name)
                    .Select(variant => new ProductVariantView(
                        variant.Id, variant.Name, variant.Sku, variant.Barcode, variant.Price, variant.IsActive))
                    .ToList(),
                product.Specifications
                    .Where(specification =>
                        includeInactiveVariants ||
                        specification.VariantId == null ||
                        specification.Variant!.IsActive)
                    .OrderBy(specification => specification.SortOrder)
                    .ThenBy(specification => specification.Name)
                    .Select(specification => new ProductSpecificationView(
                        specification.Id, specification.VariantId, specification.GroupName, specification.Name,
                        specification.Value, specification.SortOrder))
                    .ToList(),
                product.Images.OrderByDescending(image => image.IsPrimary)
                    .ThenBy(image => image.SortOrder)
                    .Select(image => new ProductImageView(
                        image.Id, image.StorageKey, image.ThumbnailStorageKey,
                        image.AltText, image.SortOrder, image.IsPrimary,
                        image.Width, image.Height, image.ThumbnailWidth, image.ThumbnailHeight))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CatalogNotFoundException("Product was not found.");
    }

    private static IReadOnlyList<CategoryView> BuildTree(IReadOnlyList<CategoryView> categories)
    {
        var childrenByParent = categories
            .Where(category => category.ParentId is not null)
            .GroupBy(category => category.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        CategoryView Attach(CategoryView category) => category with
        {
            Children = childrenByParent.TryGetValue(category.Id, out var children)
                ? children.Select(Attach).ToList()
                : []
        };

        return categories.Where(category => category.ParentId is null).Select(Attach).ToList();
    }

    private static async Task<Guid[]> GetEffectiveCategoryIdsAsync(
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        var nodes = await db.Categories.AsNoTracking()
            .Select(category => new CatalogCategoryNode(
                category.Id, category.ParentId, category.IsActive))
            .ToListAsync(cancellationToken);
        var effective = CatalogCategoryPolicy.GetEffectivelyActiveIds(nodes);
        return effective.ToArray();
    }

    private static (string GroupName, string Name, string Value) ParseSpecificationFilter(string value)
    {
        var parts = value.Split('|');
        if (parts.Length != 3 || parts.Any(string.IsNullOrWhiteSpace))
            throw new CatalogValidationException(
                "specification",
                "Specification filters must use groupName|name|value.");
        if (parts.Any(part => part.Length > 128))
            throw new CatalogValidationException("specification", "Specification filter text is too long.");
        return (parts[0].Trim(), parts[1].Trim(), parts[2].Trim());
    }

    private static async Task<Guid[]> GetDescendantCategoryIdsAsync(
        CatalogDbContext db,
        Guid rootId,
        IReadOnlyCollection<Guid> effectiveCategoryIds,
        CancellationToken cancellationToken)
    {
        var edges = await db.Categories.AsNoTracking()
            .Where(category => effectiveCategoryIds.Contains(category.Id))
            .Select(category => new { category.Id, category.ParentId })
            .ToListAsync(cancellationToken);
        var result = new HashSet<Guid> { rootId };
        var pending = new Queue<Guid>();
        pending.Enqueue(rootId);
        while (pending.TryDequeue(out var parentId))
        {
            foreach (var child in edges.Where(item => item.ParentId == parentId))
            {
                if (result.Add(child.Id))
                    pending.Enqueue(child.Id);
            }
        }
        return result.ToArray();
    }

}

public sealed class CatalogManagementService(
    IDbContextFactory<CatalogDbContext> contextFactory,
    ICatalogMediaStorage mediaStorage,
    TimeProvider timeProvider,
    IEnumerable<IMediaReferenceGuard> referenceGuards)
{
    public async Task<IReadOnlyList<CategoryView>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking()
            .OrderBy(category => category.SortOrder).ThenBy(category => category.Name)
            .Select(category => new CategoryView(
                category.Id, category.ParentId, category.Name, category.Slug,
                category.Description, category.IconStorageKey, category.SortOrder,
                category.IsActive, false, category.RowVersion, Array.Empty<CategoryView>()))
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

    public async Task<IReadOnlyList<ProductSummaryView>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Products.AsNoTracking()
            .OrderByDescending(product => product.UpdatedAtUtc).ThenBy(product => product.Name)
            .Select(product => new ProductSummaryView(
                product.Id, product.Name, product.Slug, product.Sku,
                product.BasePrice,
                product.Variants.Where(variant => variant.IsActive && variant.Price != null)
                    .Select(variant => variant.Price).Min() ?? product.BasePrice,
                product.Status,
                product.Condition,
                product.Category.Name, product.Category.Slug,
                product.Brand != null ? product.Brand.Name : null,
                product.Specifications
                    .Where(specification => specification.VariantId == null)
                    .OrderBy(specification => specification.SortOrder)
                    .ThenBy(specification => specification.Name)
                    .Take(3)
                    .Select(specification => new ProductSpecificationSummaryView(
                        specification.GroupName,
                        specification.Name,
                        specification.Value))
                    .ToList(),
                product.Images.Where(image => image.IsPrimary).Select(image => image.ThumbnailStorageKey).FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDetailView> GetProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await CatalogStorefrontService.LoadDetailAsync(
            db, id, cancellationToken, includeInactiveVariants: true);
    }

    public async Task<CategoryView> SaveCategoryAsync(
        Guid? id,
        CategoryInput input,
        CancellationToken cancellationToken = default)
    {
        var name = CatalogValidation.Required(input.Name, "name", 160);
        var slug = CatalogValidation.Slug(input.Slug);
        var description = CatalogValidation.Optional(input.Description, "description", 2000);
        var iconKey = CatalogValidation.Optional(input.IconStorageKey, "iconStorageKey", 512);
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
        EnsureVersion(category.RowVersion, input.RowVersion, id);

        if (id is not null && input.ParentId == id)
            throw new CatalogValidationException("parentId", "A category cannot be its own parent.");
        if (input.ParentId is not null)
            await EnsureCategoryParentIsAcyclicAsync(db, category.Id, input.ParentId.Value, cancellationToken);
        if (await db.Categories.AnyAsync(item => item.Slug == slug && item.Id != category.Id, cancellationToken))
            throw new CatalogConflictException("Category slug is already in use.");

        category.Name = name;
        category.Slug = slug;
        category.ParentId = input.ParentId;
        category.Description = description;
        category.IconStorageKey = iconKey;
        category.SortOrder = input.SortOrder;
        category.IsActive = input.IsActive;
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
        return new CategoryView(
            category.Id, category.ParentId, category.Name, category.Slug,
            category.Description, category.IconStorageKey, category.SortOrder,
            category.IsActive,
            await IsCategoryEffectivelyActiveAsync(db, category.Id, cancellationToken),
            category.RowVersion, Array.Empty<CategoryView>());
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
        EnsureVersion(brand.RowVersion, input.RowVersion, id);
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
        return new BrandView(
            brand.Id, brand.Name, brand.Slug, brand.Description,
            brand.LogoStorageKey, brand.SupportUrl, brand.IsActive, brand.RowVersion);
    }

    public async Task<CategoryView> SetCategoryImageAsync(
        Guid categoryId,
        CatalogImageUpload image,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.SingleOrDefaultAsync(
            item => item.Id == categoryId, cancellationToken)
            ?? throw new CatalogNotFoundException("Category was not found.");
        var previous = category.IconStorageKey;
        var assetId = Guid.CreateVersion7();
        category.IconStorageKey = await mediaStorage.SaveAssetImageAsync(
            "categories", categoryId, assetId, image, cancellationToken);
        category.RowVersion++;
        category.UpdatedAtUtc = UtcNow();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await mediaStorage.DeleteStorageKeyAsync(category.IconStorageKey, CancellationToken.None);
            throw;
        }
        if (!string.IsNullOrWhiteSpace(previous))
            await mediaStorage.DeleteStorageKeyAsync(previous, CancellationToken.None);
        return new CategoryView(
            category.Id, category.ParentId, category.Name, category.Slug,
            category.Description, category.IconStorageKey, category.SortOrder,
            category.IsActive,
            await IsCategoryEffectivelyActiveAsync(db, category.Id, cancellationToken),
            category.RowVersion, Array.Empty<CategoryView>());
    }

    public async Task<CategoryView> DeleteCategoryImageAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.SingleOrDefaultAsync(
            item => item.Id == categoryId, cancellationToken)
            ?? throw new CatalogNotFoundException("Category was not found.");
        var previous = category.IconStorageKey;
        category.IconStorageKey = null;
        category.RowVersion++;
        category.UpdatedAtUtc = UtcNow();
        await db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(previous))
            await mediaStorage.DeleteStorageKeyAsync(previous, CancellationToken.None);
        return new CategoryView(
            category.Id, category.ParentId, category.Name, category.Slug,
            category.Description, category.IconStorageKey, category.SortOrder,
            category.IsActive,
            await IsCategoryEffectivelyActiveAsync(db, category.Id, cancellationToken),
            category.RowVersion, Array.Empty<CategoryView>());
    }

    public async Task<BrandView> SetBrandLogoAsync(
        Guid brandId,
        CatalogImageUpload image,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var brand = await db.Brands.SingleOrDefaultAsync(item => item.Id == brandId, cancellationToken)
            ?? throw new CatalogNotFoundException("Brand was not found.");
        var previous = brand.LogoStorageKey;
        var assetId = Guid.CreateVersion7();
        brand.LogoStorageKey = await mediaStorage.SaveAssetImageAsync(
            "brands", brandId, assetId, image, cancellationToken);
        brand.RowVersion++;
        brand.UpdatedAtUtc = UtcNow();
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await mediaStorage.DeleteStorageKeyAsync(brand.LogoStorageKey, CancellationToken.None);
            throw;
        }
        if (!string.IsNullOrWhiteSpace(previous))
            await mediaStorage.DeleteStorageKeyAsync(previous, CancellationToken.None);
        return new BrandView(
            brand.Id, brand.Name, brand.Slug, brand.Description,
            brand.LogoStorageKey, brand.SupportUrl, brand.IsActive, brand.RowVersion);
    }

    public async Task<BrandView> DeleteBrandLogoAsync(
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var brand = await db.Brands.SingleOrDefaultAsync(item => item.Id == brandId, cancellationToken)
            ?? throw new CatalogNotFoundException("Brand was not found.");
        var previous = brand.LogoStorageKey;
        brand.LogoStorageKey = null;
        brand.RowVersion++;
        brand.UpdatedAtUtc = UtcNow();
        await db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(previous))
            await mediaStorage.DeleteStorageKeyAsync(previous, CancellationToken.None);
        return new BrandView(
            brand.Id, brand.Name, brand.Slug, brand.Description,
            brand.LogoStorageKey, brand.SupportUrl, brand.IsActive, brand.RowVersion);
    }

    public async Task<ProductDetailView> SaveProductAsync(
        Guid? id,
        ProductInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateProductInput(input);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var category = await db.Categories.SingleOrDefaultAsync(
            item => item.Id == input.CategoryId, cancellationToken)
            ?? throw new CatalogValidationException("categoryId", "Category was not found.");
        var brand = input.BrandId is null
            ? null
            : await db.Brands.SingleOrDefaultAsync(item => item.Id == input.BrandId, cancellationToken)
              ?? throw new CatalogValidationException("brandId", "Brand was not found.");
        if (input.Status == CatalogProductStatus.Published &&
            (!await IsCategoryEffectivelyActiveAsync(db, category.Id, cancellationToken) ||
             (brand is not null && !brand.IsActive)))
            throw new CatalogValidationException(
                "status", "A product cannot be published under an inactive category or brand.");

        var product = id is null
            ? new CatalogProduct
            {
                Id = Guid.CreateVersion7(),
                Name = string.Empty,
                Slug = string.Empty,
                Sku = string.Empty,
                CreatedAtUtc = UtcNow()
            }
            : await db.Products
                .Include(item => item.Variants)
                .Include(item => item.Specifications)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
              ?? throw new CatalogNotFoundException("Product was not found.");
        EnsureVersion(product.RowVersion, input.RowVersion, id);

        var sku = CatalogValidation.Required(input.Sku, "sku", 128);
        var barcode = CatalogValidation.Optional(input.Barcode, "barcode", 128);
        if (await db.Products.AnyAsync(
                item => (item.Sku == sku || (barcode != null && item.Barcode == barcode)) &&
                        item.Id != product.Id,
                cancellationToken))
            throw new CatalogConflictException("Product SKU or barcode is already in use.");

        product.Name = CatalogValidation.Required(input.Name, "name", 255);
        product.Slug = CatalogValidation.Slug(input.Slug);
        product.Sku = sku;
        product.Barcode = barcode;
        product.CategoryId = category.Id;
        product.BrandId = brand?.Id;
        product.ShortDescription = CatalogValidation.Optional(input.ShortDescription, "shortDescription", 600);
        product.Description = CatalogValidation.Optional(input.Description, "description", 8000);
        product.BasePrice = CatalogValidation.NonNegativeMoney(input.BasePrice, "basePrice");
        product.Status = input.Status;
        product.Condition = input.Condition;
        product.UpdatedAtUtc = UtcNow();
        if (id is null)
            db.Products.Add(product);
        else
            product.RowVersion++;

        SynchronizeVariants(db, product, input.Variants ?? []);
        SynchronizeSpecifications(db, product, input.Specifications ?? []);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw new CatalogConflictException("Product, variant, or specification identifiers conflict with existing data.");
        }

        return await CatalogStorefrontService.LoadDetailAsync(
            db, product.Id, cancellationToken, includeInactiveVariants: true);
    }

    public async Task<ProductDetailView> SetProductStatusAsync(
        Guid id,
        CatalogProductStatus status,
        int rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var product = await db.Products.Include(item => item.Category).Include(item => item.Brand)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new CatalogNotFoundException("Product was not found.");
        EnsureVersion(product.RowVersion, rowVersion, id);
        if (status == CatalogProductStatus.Published &&
            (!await IsCategoryEffectivelyActiveAsync(db, product.Category.Id, cancellationToken) ||
             (product.Brand is not null && !product.Brand.IsActive)))
            throw new CatalogValidationException(
                "status", "A product cannot be published under an inactive category or brand.");
        product.Status = status;
        product.RowVersion++;
        product.UpdatedAtUtc = UtcNow();
        await db.SaveChangesAsync(cancellationToken);
        return await CatalogStorefrontService.LoadDetailAsync(
            db, product.Id, cancellationToken, includeInactiveVariants: true);
    }

    public async Task<ProductImageView> AddImageAsync(
        Guid productId,
        string? altText,
        bool isPrimary,
        CatalogImageUpload image,
        CancellationToken cancellationToken = default)
    {
        var normalizedAltText = CatalogValidation.Required(altText, "altText", 300);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Products.AnyAsync(product => product.Id == productId, cancellationToken))
            throw new CatalogNotFoundException("Product was not found.");

        var imageId = Guid.CreateVersion7();
        var stored = await mediaStorage.SaveImageAsync(
            productId, imageId, image, cancellationToken);
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var hasPrimary = await db.ProductImages.AnyAsync(
                item => item.ProductId == productId && item.IsPrimary, cancellationToken);
            var makePrimary = isPrimary || !hasPrimary;
            if (makePrimary)
            {
                await db.ProductImages
                    .Where(item => item.ProductId == productId && item.IsPrimary)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsPrimary, false), cancellationToken);
            }
            var sortOrder = await db.ProductImages
                .Where(item => item.ProductId == productId)
                .Select(item => (int?)item.SortOrder)
                .MaxAsync(cancellationToken) ?? -1;
            var imageEntity = new CatalogProductImage
            {
                Id = imageId,
                ProductId = productId,
                StorageKey = stored.StorageKey,
                ThumbnailStorageKey = stored.ThumbnailStorageKey,
                OriginalStorageKey = stored.OriginalStorageKey,
                AltText = normalizedAltText,
                FileSizeBytes = stored.FileSizeBytes,
                Width = stored.Width,
                Height = stored.Height,
                ThumbnailWidth = stored.ThumbnailWidth,
                ThumbnailHeight = stored.ThumbnailHeight,
                SortOrder = sortOrder + 1,
                IsPrimary = makePrimary,
                CreatedAtUtc = UtcNow()
            };
            db.ProductImages.Add(imageEntity);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ProductImageView(
                imageEntity.Id, imageEntity.StorageKey, imageEntity.ThumbnailStorageKey,
                imageEntity.AltText, imageEntity.SortOrder, imageEntity.IsPrimary,
                imageEntity.Width, imageEntity.Height,
                imageEntity.ThumbnailWidth, imageEntity.ThumbnailHeight);
        }
        catch
        {
            await mediaStorage.DeleteAsync(stored.StorageKey, CancellationToken.None);
            throw;
        }
    }

    public async Task<ProductImageView> UpdateImageAsync(
        Guid productId,
        Guid imageId,
        string? altText,
        int sortOrder,
        bool isPrimary,
        CancellationToken cancellationToken = default)
    {
        if (sortOrder < 0)
            throw new CatalogValidationException("sortOrder", "Sort order must not be negative.");
        var normalizedAltText = CatalogValidation.Required(altText, "altText", 300);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var image = await db.ProductImages.SingleOrDefaultAsync(
            item => item.Id == imageId && item.ProductId == productId, cancellationToken)
            ?? throw new CatalogNotFoundException("Product image was not found.");
        if (isPrimary)
        {
            await db.ProductImages
                .Where(item => item.ProductId == productId && item.IsPrimary && item.Id != imageId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsPrimary, false), cancellationToken);
        }
        image.AltText = normalizedAltText;
        image.SortOrder = sortOrder;
        image.IsPrimary = isPrimary;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ProductImageView(
            image.Id, image.StorageKey, image.ThumbnailStorageKey,
            image.AltText, image.SortOrder, image.IsPrimary,
            image.Width, image.Height, image.ThumbnailWidth, image.ThumbnailHeight);
    }

    public async Task DeleteImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var image = await db.ProductImages.SingleOrDefaultAsync(
            item => item.Id == imageId && item.ProductId == productId, cancellationToken)
            ?? throw new CatalogNotFoundException("Product image was not found.");
        foreach (var guard in referenceGuards)
        {
            var references = await guard.FindReferencesAsync(image.StorageKey, cancellationToken);
            if (references.Count > 0)
                throw new CatalogValidationException("image",
                    "This image is kept by historical inventory or operational records and cannot be deleted.");
        }
        var wasPrimary = image.IsPrimary;
        db.ProductImages.Remove(image);
        await db.SaveChangesAsync(cancellationToken);
        await mediaStorage.DeleteAsync(image.StorageKey, cancellationToken);
        if (wasPrimary)
        {
            var replacement = await db.ProductImages
                .Where(item => item.ProductId == productId)
                .OrderBy(item => item.SortOrder).ThenBy(item => item.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (replacement is not null)
            {
                replacement.IsPrimary = true;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private DateTimeOffset UtcNow() => timeProvider.GetUtcNow().ToUniversalTime();

    private static void ValidateProductInput(ProductInput input)
    {
        CatalogValidation.Required(input.Name, "name", 255);
        CatalogValidation.Slug(input.Slug);
        CatalogValidation.Required(input.Sku, "sku", 128);
        CatalogValidation.Optional(input.Barcode, "barcode", 128);
        CatalogValidation.Optional(input.ShortDescription, "shortDescription", 600);
        CatalogValidation.Optional(input.Description, "description", 8000);
        CatalogValidation.NonNegativeMoney(input.BasePrice, "basePrice");
        if (input.CategoryId == Guid.Empty)
            throw new CatalogValidationException("categoryId", "Category is required.");

        var variants = input.Variants ?? [];
        foreach (var variant in variants)
        {
            CatalogValidation.Required(variant.Name, "variants.name", 160);
            CatalogValidation.Required(variant.Sku, "variants.sku", 128);
            CatalogValidation.Optional(variant.Barcode, "variants.barcode", 128);
            if (variant.Price is not null)
                CatalogValidation.NonNegativeMoney(variant.Price.Value, "variants.price");
        }
        EnsureUnique(variants.Select(item => item.Sku!.Trim()), "variants.sku", "Variant SKUs must be unique.");
        EnsureUnique(
            variants.Where(item => !string.IsNullOrWhiteSpace(item.Barcode)).Select(item => item.Barcode!.Trim()),
            "variants.barcode",
            "Variant barcodes must be unique.");

        var specifications = input.Specifications ?? [];
        foreach (var specification in specifications)
        {
            CatalogValidation.Required(specification.GroupName, "specifications.groupName", 100);
            CatalogValidation.Required(specification.Name, "specifications.name", 128);
            CatalogValidation.Required(specification.Value, "specifications.value", 500);
            if (specification.SortOrder < 0)
                throw new CatalogValidationException("specifications.sortOrder", "Sort order must not be negative.");
        }
        EnsureUnique(
            specifications.Select(item =>
                $"{item.VariantId?.ToString("N") ?? item.VariantSku?.Trim().ToUpperInvariant() ?? "product"}\u001f{item.GroupName!.Trim()}\u001f{item.Name!.Trim().ToLowerInvariant()}"),
            "specifications",
            "Specification names must be unique within each product or variant scope and group.");
    }

    private static void EnsureUnique(IEnumerable<string> values, string field, string message)
    {
        if (values.GroupBy(value => value, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new CatalogValidationException(field, message);
    }

    private static void SynchronizeVariants(
        CatalogDbContext db,
        CatalogProduct product,
        IReadOnlyList<ProductVariantInput> inputs)
    {
        var retainedIds = inputs.Where(input => input.Id is not null).Select(input => input.Id!.Value).ToHashSet();
        foreach (var removed in product.Variants.Where(variant => !retainedIds.Contains(variant.Id)).ToList())
            db.ProductVariants.Remove(removed);

        foreach (var input in inputs)
        {
            var variant = input.Id is null
                ? null
                : product.Variants.SingleOrDefault(item => item.Id == input.Id)
                  ?? throw new CatalogValidationException("variants.id", "A variant does not belong to this product.");
            if (variant is null)
            {
                variant = new CatalogProductVariant
                {
                    Id = Guid.CreateVersion7(),
                    Name = string.Empty,
                    Sku = string.Empty,
                    Product = product
                };
                product.Variants.Add(variant);
            }
            variant.Name = input.Name!.Trim();
            variant.Sku = input.Sku!.Trim();
            variant.Barcode = string.IsNullOrWhiteSpace(input.Barcode) ? null : input.Barcode.Trim();
            variant.Price = input.Price;
            variant.IsActive = input.IsActive;
        }
    }

    private static void SynchronizeSpecifications(
        CatalogDbContext db,
        CatalogProduct product,
        IReadOnlyList<ProductSpecificationInput> inputs)
    {
        var retainedIds = inputs.Where(input => input.Id is not null).Select(input => input.Id!.Value).ToHashSet();
        foreach (var removed in product.Specifications.Where(item => !retainedIds.Contains(item.Id)).ToList())
            db.ProductSpecifications.Remove(removed);

        foreach (var input in inputs)
        {
            var specification = input.Id is null
                ? null
                : product.Specifications.SingleOrDefault(item => item.Id == input.Id)
                  ?? throw new CatalogValidationException(
                      "specifications.id", "A specification does not belong to this product.");
            if (specification is null)
            {
                specification = new CatalogProductSpecification
                {
                    Id = Guid.CreateVersion7(),
                    GroupName = string.Empty,
                    Name = string.Empty,
                    Value = string.Empty,
                    Product = product
                };
                product.Specifications.Add(specification);
            }
            specification.GroupName = input.GroupName!.Trim();
            specification.Name = input.Name!.Trim();
            specification.Value = input.Value!.Trim();
            specification.SortOrder = input.SortOrder;
            var variant = input.VariantId is not null
                ? product.Variants.SingleOrDefault(item => item.Id == input.VariantId)
                : input.VariantSku is not null
                    ? product.Variants.SingleOrDefault(item =>
                        string.Equals(item.Sku, input.VariantSku.Trim(), StringComparison.OrdinalIgnoreCase))
                    : null;
            if ((input.VariantId is not null || input.VariantSku is not null) && variant is null)
                throw new CatalogValidationException(
                    "specifications.variant", "A specification references an unknown product variant.");
            specification.VariantId = variant?.Id;
        }
    }

    private static void EnsureVersion(int currentVersion, int? requestedVersion, Guid? id)
    {
        if (id is not null && requestedVersion != currentVersion)
            throw new CatalogConflictException("The record was changed by another request.");
    }

    private static async Task EnsureCategoryParentIsAcyclicAsync(
        CatalogDbContext db,
        Guid categoryId,
        Guid parentId,
        CancellationToken cancellationToken)
    {
        var parents = await db.Categories.AsNoTracking()
            .Select(category => new { category.Id, category.ParentId })
            .ToDictionaryAsync(category => category.Id, category => category.ParentId, cancellationToken);
        if (!parents.ContainsKey(parentId))
            throw new CatalogValidationException("parentId", "Parent category was not found.");
        var visited = new HashSet<Guid>();
        Guid? current = parentId;
        while (current is not null)
        {
            if (current == categoryId)
                throw new CatalogValidationException("parentId", "Category hierarchy cannot contain a cycle.");
            if (!visited.Add(current.Value) ||
                !parents.TryGetValue(current.Value, out var next))
                break;
            current = next;
        }
    }

    private static async Task<bool> IsCategoryEffectivelyActiveAsync(
        CatalogDbContext db,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var nodes = await db.Categories.AsNoTracking()
            .Select(category => new CatalogCategoryNode(
                category.Id, category.ParentId, category.IsActive))
            .ToListAsync(cancellationToken);
        return CatalogCategoryPolicy.GetEffectivelyActiveIds(nodes).Contains(categoryId);
    }
}
