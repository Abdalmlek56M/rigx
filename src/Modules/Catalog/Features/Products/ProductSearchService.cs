using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>
/// Public product queries: search suggestions, filtered/sorted product pages, specification filters and
/// product detail. Only published products under effectively active categories and active brands are visible.
/// </summary>
public sealed class ProductSearchService(
    IDbContextFactory<CatalogDbContext> contextFactory)
{
    public async Task<IReadOnlyList<ProductSuggestionView>> SuggestAsync(string? search, int limit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(search)) return [];
        var term = search.Trim();
        if (term.Length > 80) throw new CatalogValidationException("search", "Search text must not exceed 80 characters.");
        limit = Math.Clamp(limit, 1, 10);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var categoryIds = await CategoryHierarchy.GetEffectiveIdsAsync(db, cancellationToken);
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
        var effectiveCategoryIds = await CategoryHierarchy.GetEffectiveIdsAsync(db, cancellationToken);
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
            var categoryIds = await CategoryHierarchy.GetDescendantIdsAsync(
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
                    specification.GroupName == groupName &&
                    specification.Name == name &&
                    values.Contains(specification.Value)));
            }
        }

        if (minimumPrice is not null)
            query = query.Where(product => product.BasePrice >= minimumPrice);
        if (maximumPrice is not null)
            query = query.Where(product => product.BasePrice <= maximumPrice);

        var total = await query.CountAsync(cancellationToken);
        page = total == 0 ? 1 : Math.Min(page, (int)Math.Ceiling(total / (double)pageSize));
        query = sort switch
        {
            CatalogProductSort.Oldest => query.OrderBy(product => product.CreatedAtUtc).ThenBy(product => product.Name),
            CatalogProductSort.PriceAscending => query.OrderBy(product => product.BasePrice).ThenBy(product => product.Name),
            CatalogProductSort.PriceDescending => query.OrderByDescending(product => product.BasePrice).ThenBy(product => product.Name),
            CatalogProductSort.NameAscending => query.OrderBy(product => product.Name).ThenBy(product => product.BasePrice),
            CatalogProductSort.NameDescending => query.OrderByDescending(product => product.Name).ThenBy(product => product.BasePrice),
            _ => query.OrderByDescending(product => product.CreatedAtUtc).ThenBy(product => product.Name)
        };
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductSummaryView(
                product.Id,
                product.Name,
                product.Slug,
                product.Sku,
                product.BasePrice,
                product.Status,
                product.Condition,
                product.Category.Name,
                product.Category.Slug,
                product.Brand != null ? product.Brand.Name : null,
                product.Specifications
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

        return new ProductPageView(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<SpecificationFilterView>> GetSpecificationFiltersAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var effectiveCategoryIds = await CategoryHierarchy.GetEffectiveIdsAsync(db, cancellationToken);
        if (effectiveCategoryIds.Length == 0)
            return [];

        var specifications = await db.ProductSpecifications.AsNoTracking()
            .Where(specification =>
                specification.Product.Status == CatalogProductStatus.Published &&
                effectiveCategoryIds.Contains(specification.Product.CategoryId) &&
                (specification.Product.BrandId == null || specification.Product.Brand!.IsActive))
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
        var effectiveCategoryIds = await CategoryHierarchy.GetEffectiveIdsAsync(db, cancellationToken);
        var product = await db.Products.AsNoTracking()
            .Where(item => item.Slug == normalizedSlug &&
                           item.Status == CatalogProductStatus.Published &&
                           effectiveCategoryIds.Contains(item.CategoryId) &&
                           (item.BrandId == null || item.Brand!.IsActive))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CatalogNotFoundException("Product was not found.");

        return await ProductDetailQuery.LoadAsync(db, product.Id, cancellationToken);
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
}
