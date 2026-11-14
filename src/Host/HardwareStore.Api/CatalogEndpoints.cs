using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Catalog HTTP API (public browsing + admin). Business errors (validation 400, not found 404, conflict 409)
/// are mapped once by <c>ProblemDetailsExceptionHandler</c>.
/// </summary>
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var publicCatalog = endpoints.MapGroup("/api/v1/catalog").AllowAnonymous();
        publicCatalog.MapGet("/categories", GetPublicCategoriesAsync);
        publicCatalog.MapGet("/categories/{id:guid}/specs", GetCategorySpecsAsync);
        publicCatalog.MapGet("/brands", GetPublicBrandsAsync);
        publicCatalog.MapGet("/specification-filters", GetPublicSpecificationFiltersAsync);
        publicCatalog.MapGet("/products", GetPublicProductsAsync);
        publicCatalog.MapGet("/products/{slug}", GetPublicProductAsync);
        publicCatalog.MapGet("/search/suggestions", GetSuggestionsAsync).RequireRateLimiting(IdentityRateLimitPolicies.CatalogSuggest);
        publicCatalog.MapGet("/media/{**storageKey}", GetMediaAsync).Produces<Stream>(StatusCodes.Status200OK, "image/webp");

        var admin = endpoints.MapGroup("/api/v1/admin/catalog");
        admin.MapGet("/categories", GetAdminCategoriesAsync)
            .RequirePermission("Catalog.View");
        admin.MapGet("/brands", GetAdminBrandsAsync)
            .RequirePermission("Catalog.View");
        admin.MapGet("/products", GetAdminProductsAsync)
            .RequirePermission("Catalog.View");
        admin.MapGet("/products/{id:guid}", GetAdminProductAsync)
            .RequirePermission("Catalog.View");
        admin.MapPost("/categories", CreateCategoryAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPut("/categories/{id:guid}", UpdateCategoryAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPut("/categories/{id:guid}/specs", SetCategorySpecsAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPost("/categories/{id:guid}/image", UploadCategoryImageAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapDelete("/categories/{id:guid}/image", DeleteCategoryImageAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapPost("/brands", CreateBrandAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPut("/brands/{id:guid}", UpdateBrandAsync)
            .RequirePermission("Catalog.ManageTaxonomy");
        admin.MapPost("/brands/{id:guid}/logo", UploadBrandLogoAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapDelete("/brands/{id:guid}/logo", DeleteBrandLogoAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapPost("/products", CreateProductAsync)
            .RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/products/{id:guid}", UpdateProductAsync)
            .RequirePermission("Catalog.ManageProducts");
        admin.MapPatch("/products/{id:guid}/status", UpdateProductStatusAsync)
            .RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/products/{id:guid}/images", UploadImageAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapPut("/products/{id:guid}/images/{imageId:guid}", UpdateImageAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapDelete("/products/{id:guid}/images/{imageId:guid}", DeleteImageAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapGet("/media-audit", GetMediaAuditAsync)
            .RequirePermission("Catalog.ManageMedia");
        admin.MapPost("/media-audit/cleanup", CleanupOrphanMediaAsync)
            .RequirePermission("Catalog.ManageMedia");
        return endpoints;
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

    private static Task<IReadOnlyList<CategoryView>> GetAdminCategoriesAsync(
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetCategoriesAsync(cancellationToken);

    private static Task<IReadOnlyList<BrandView>> GetAdminBrandsAsync(
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetBrandsAsync(cancellationToken);

    private static Task<IReadOnlyList<ProductSummaryView>> GetAdminProductsAsync(
        ProductManagementService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetProductsAsync(cancellationToken);

    private static async Task<Ok<ProductDetailView>> GetAdminProductAsync(
        Guid id,
        ProductManagementService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.GetProductAsync(id, cancellationToken));

    private static async Task<Created<CategoryView>> CreateCategoryAsync(
        CategoryInput input,
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Created(
            (string?)null,
            await catalog.SaveCategoryAsync(null, input, cancellationToken));

    private static async Task<Ok<CategoryView>> UpdateCategoryAsync(
        Guid id,
        CategoryInput input,
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.SaveCategoryAsync(id, input, cancellationToken));

    private static async Task<Ok<CategoryView>> SetCategorySpecsAsync(
        Guid id,
        CategorySpecsInput input,
        CategorySpecService specs,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await specs.SetSpecsAsync(id, input.Specs, input.RowVersion, cancellationToken));

    private static async Task<Ok<IReadOnlyList<SpecDefinition>>> GetCategorySpecsAsync(
        Guid id,
        CategorySpecService specs,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await specs.GetEffectiveSpecsAsync(id, cancellationToken));

    private static async Task<Created<BrandView>> CreateBrandAsync(
        BrandInput input,
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Created(
            (string?)null,
            await catalog.SaveBrandAsync(null, input, cancellationToken));

    private static async Task<Ok<BrandView>> UpdateBrandAsync(
        Guid id,
        BrandInput input,
        TaxonomyService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.SaveBrandAsync(id, input, cancellationToken));

    private static async Task<Created<ProductDetailView>> CreateProductAsync(
        ProductInput input,
        ProductManagementService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Created(
            (string?)null,
            await catalog.SaveProductAsync(null, input, cancellationToken));

    private static async Task<Ok<ProductDetailView>> UpdateProductAsync(
        Guid id,
        ProductInput input,
        ProductManagementService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.SaveProductAsync(id, input, cancellationToken));

    private static async Task<Ok<ProductDetailView>> UpdateProductStatusAsync(
        Guid id,
        ProductStatusInput input,
        ProductManagementService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.SetProductStatusAsync(
            id, input.Status, input.RowVersion, cancellationToken));

    private static async Task<Created<ProductImageView>> UploadImageAsync(
        Guid id,
        ProductImageService catalog,
        CatalogMediaOptions options,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var (form, upload) = await ReadImageFormAsync(context, options, cancellationToken);
        var altText = form["altText"].ToString();
        var isPrimary = bool.TryParse(form["isPrimary"].ToString(), out var parsedPrimary) && parsedPrimary;
        return TypedResults.Created((string?)null, await catalog.AddImageAsync(id, altText, isPrimary, upload, cancellationToken));
    }

    private static async Task<Ok<CategoryView>> UploadCategoryImageAsync(
        Guid id,
        TaxonomyImageService catalog,
        CatalogMediaOptions options,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var upload = await ReadSingleImageAsync(context, options, cancellationToken);
        return TypedResults.Ok(await catalog.SetCategoryImageAsync(id, upload, cancellationToken));
    }

    private static async Task<Ok<CategoryView>> DeleteCategoryImageAsync(
        Guid id,
        TaxonomyImageService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.DeleteCategoryImageAsync(id, cancellationToken));

    private static async Task<Ok<BrandView>> UploadBrandLogoAsync(
        Guid id,
        TaxonomyImageService catalog,
        CatalogMediaOptions options,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var upload = await ReadSingleImageAsync(context, options, cancellationToken);
        return TypedResults.Ok(await catalog.SetBrandLogoAsync(id, upload, cancellationToken));
    }

    private static async Task<Ok<BrandView>> DeleteBrandLogoAsync(
        Guid id,
        TaxonomyImageService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.DeleteBrandLogoAsync(id, cancellationToken));

    private static async Task<Ok<ProductImageView>> UpdateImageAsync(
        Guid id,
        Guid imageId,
        ImageMetadataInput input,
        ProductImageService catalog,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await catalog.UpdateImageAsync(
            id, imageId, input.AltText, input.SortOrder, input.IsPrimary, cancellationToken));

    private static async Task<NoContent> DeleteImageAsync(
        Guid id,
        Guid imageId,
        ProductImageService catalog,
        CancellationToken cancellationToken)
    {
        await catalog.DeleteImageAsync(id, imageId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<CatalogMediaAuditView> GetMediaAuditAsync(
        CatalogMediaMaintenanceService maintenance,
        CancellationToken cancellationToken) =>
        maintenance.AuditAsync(cancellationToken);

    private static async Task<Ok<CatalogMediaCleanupView>> CleanupOrphanMediaAsync(
        CatalogMediaMaintenanceService maintenance,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await maintenance.CleanupOrphansAsync(cancellationToken));


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

    internal static async Task<CatalogImageUpload> ReadSingleImageAsync(
        HttpContext context,
        CatalogMediaOptions options,
        CancellationToken cancellationToken) =>
        (await ReadImageFormAsync(context, options, cancellationToken)).Upload;

    /// <summary>The multipart form with its one <c>image</c> file; the form is returned too for endpoints that carry extra fields.</summary>
    internal static async Task<(IFormCollection Form, CatalogImageUpload Upload)> ReadImageFormAsync(
        HttpContext context,
        CatalogMediaOptions options,
        CancellationToken cancellationToken)
    {
        if (!context.Request.HasFormContentType)
            throw FormProblem("form", "A multipart form is required.");
        LimitRequestSize(context, options);
        if (context.Request.ContentLength > options.MaxUploadBytes + 64 * 1024)
            throw new ApiProblemException(StatusCodes.Status413PayloadTooLarge, "The uploaded image is too large.");
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("image") ?? throw FormProblem("image", "An image file is required.");
        return (form, new CatalogImageUpload(file.OpenReadStream(), file.Length, file.FileName, file.ContentType));
    }

    private static ApiProblemException FormProblem(string field, string message) => new(StatusCodes.Status400BadRequest,
        "One or more validation errors occurred.", new Dictionary<string, string[]> { [field] = [message] });

    private static void LimitRequestSize(HttpContext context, CatalogMediaOptions options)
    {
        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false })
            feature.MaxRequestBodySize = options.MaxUploadBytes + 64 * 1024;
    }

    public sealed record ProductStatusInput(CatalogProductStatus Status, int RowVersion);
    public sealed record ImageMetadataInput(string? AltText, int SortOrder, bool IsPrimary);
}
