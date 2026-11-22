using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Catalog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>Public browsing: categories, brands, filters, products, suggestions and media.</summary>
public static partial class CatalogEndpoints
{
    private static void MapPublicCatalog(RouteGroupBuilder publicCatalog)
    {
        publicCatalog.MapGet("/categories", GetPublicCategoriesAsync);
        publicCatalog.MapGet("/categories/{id:guid}/specs", GetCategorySpecsAsync);
        publicCatalog.MapGet("/brands", GetPublicBrandsAsync);
        publicCatalog.MapGet("/specification-filters", GetPublicSpecificationFiltersAsync);
        publicCatalog.MapGet("/products", GetPublicProductsAsync);
        publicCatalog.MapGet("/products/{slug}", GetPublicProductAsync);
        publicCatalog.MapGet("/search/suggestions", GetSuggestionsAsync).RequireRateLimiting(IdentityRateLimitPolicies.CatalogSuggest);
        publicCatalog.MapGet("/media/{**storageKey}", GetMediaAsync).Produces<Stream>(StatusCodes.Status200OK, "image/webp");
    }

    private static Task<IReadOnlyList<CategoryView>> GetPublicCategoriesAsync(
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetCategoryTreeAsync(cancellationToken);

    private static Task<IReadOnlyList<BrandView>> GetPublicBrandsAsync(
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetActiveBrandsAsync(cancellationToken);

    private static Task<IReadOnlyList<SpecificationFilterView>> GetPublicSpecificationFiltersAsync(
        ProductSearchService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetSpecificationFiltersAsync(cancellationToken);

    private static async Task<Ok<IReadOnlyList<ProductSuggestionView>>> GetSuggestionsAsync(
        string? q,
        int? limit,
        HttpContext context,
        ProductSearchService catalog,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=30";
        return TypedResults.Ok(await catalog.SuggestAsync(q, limit ?? 8, cancellationToken));
    }

    private static async Task<Ok<PagedResult<ProductSummaryView>>> GetPublicProductsAsync(
        string? category,
        string? brand,
        string? search,
        string? condition,
        decimal? minimumPrice,
        decimal? maximumPrice,
        string[]? specification,
        string? sort,
        int? page,
        int? pageSize,
        ProductSearchService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.GetProductsAsync(
            category, brand, search, ParseCondition(condition), minimumPrice, maximumPrice,
            specification, ParseSort(sort), page ?? 1, pageSize ?? 12, cancellationToken));

    private static async Task<Ok<ProductDetailView>> GetPublicProductAsync(
        string slug,
        ProductSearchService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.GetProductBySlugAsync(slug, cancellationToken));

    private static FileStreamHttpResult GetMediaAsync(
        string storageKey,
        HttpContext context,
        ICatalogMediaStorage storage)
    {
        try
        {
            if (!storageKey.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) || CatalogMediaKeys.IsPrivate(storageKey))
                throw MediaNotFound();
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return TypedResults.File(storage.OpenRead(storageKey), "image/webp");
        }
        catch (Exception exception) when (exception is CatalogValidationException or FileNotFoundException)
        {
            throw MediaNotFound();
        }
    }

    private static ApiProblemException MediaNotFound() => new(StatusCodes.Status404NotFound, "The image was not found.");

    private static async Task<Ok<IReadOnlyList<SpecDefinition>>> GetCategorySpecsAsync(
        Guid id,
        CategorySpecService specs,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await specs.GetEffectiveSpecsAsync(id, cancellationToken));

    private static CatalogProductCondition? ParseCondition(string? condition) => condition?.ToLowerInvariant() switch
    {
        null or "" => null,
        "new" => CatalogProductCondition.New,
        "used" => CatalogProductCondition.Used,
        _ => throw new CatalogValidationException(
            "condition", "Condition must be new or used.")
    };

    private static CatalogProductSort ParseSort(string? sort) => sort switch
    {
        "oldest" => CatalogProductSort.Oldest,
        "price-asc" => CatalogProductSort.PriceAscending,
        "price-desc" => CatalogProductSort.PriceDescending,
        "name-asc" => CatalogProductSort.NameAscending,
        "name-desc" => CatalogProductSort.NameDescending,
        "newest" or null or "" => CatalogProductSort.Newest,
        _ => throw new CatalogValidationException(
            "sort", "Sort must be newest, oldest, price-asc, price-desc, name-asc, or name-desc.")
    };
}
