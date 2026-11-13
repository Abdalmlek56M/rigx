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
        var effectiveCategoryIds = await CategoryHierarchy.GetEffectiveIdsAsync(db, cancellationToken);
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
        var effectiveCategoryIds = await CategoryHierarchy.GetEffectiveIdsAsync(db, cancellationToken);
        var product = await db.Products.AsNoTracking()
            .Where(item => item.Slug == normalizedSlug &&
                           item.Status == CatalogProductStatus.Published &&
                           effectiveCategoryIds.Contains(item.CategoryId) &&
                           (item.BrandId == null || item.Brand!.IsActive))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new CatalogNotFoundException("Product was not found.");

        return await ProductDetailQuery.LoadAsync(db, product.Id, cancellationToken, includeInactiveVariants: false);
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
