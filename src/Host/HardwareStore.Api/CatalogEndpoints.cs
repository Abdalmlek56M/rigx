using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var publicCatalog = endpoints.MapGroup("/api/v1/catalog").AllowAnonymous();
        publicCatalog.MapGet("/categories", GetPublicCategoriesAsync);
        publicCatalog.MapGet("/brands", GetPublicBrandsAsync);
        publicCatalog.MapGet("/specification-filters", GetPublicSpecificationFiltersAsync);
        publicCatalog.MapGet("/products", GetPublicProductsAsync);
        publicCatalog.MapGet("/products/{slug}", GetPublicProductAsync);
        publicCatalog.MapGet("/search/suggestions", GetSuggestionsAsync).RequireRateLimiting(IdentityRateLimitPolicies.CatalogSuggest);
        publicCatalog.MapGet("/media/{**storageKey}", GetMediaAsync);

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
        CatalogStorefrontService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetCategoryTreeAsync(cancellationToken);

    private static Task<IReadOnlyList<BrandView>> GetPublicBrandsAsync(
        CatalogStorefrontService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetBrandsAsync(cancellationToken);

    private static Task<IReadOnlyList<SpecificationFilterView>> GetPublicSpecificationFiltersAsync(
        CatalogStorefrontService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetSpecificationFiltersAsync(cancellationToken);

    private static async Task<IResult> GetSuggestionsAsync(
        string? q,
        int? limit,
        HttpContext context,
        CatalogStorefrontService catalog,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=30";
        try
        {
            return Results.Ok(await catalog.SuggestAsync(q, limit ?? 8, cancellationToken));
        }
        catch (CatalogValidationException exception)
        {
            return Results.BadRequest(new { error = exception.Errors.Values.FirstOrDefault()?.FirstOrDefault() });
        }
    }

    private static async Task<IResult> GetPublicProductsAsync(
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
        CatalogStorefrontService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.GetProductsAsync(
                category, brand, search, ParseCondition(condition), minimumPrice, maximumPrice,
                specification, ParseSort(sort), page ?? 1, pageSize ?? 12, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> GetPublicProductAsync(
        string slug,
        CatalogStorefrontService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.GetProductBySlugAsync(slug, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static IResult GetMediaAsync(
        string storageKey,
        HttpContext context,
        ICatalogMediaStorage storage)
    {
        try
        {
            if (!storageKey.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                return Results.NotFound();
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.File(storage.OpenRead(storageKey), "image/webp");
        }
        catch (CatalogValidationException)
        {
            return Results.NotFound();
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static Task<IReadOnlyList<CategoryView>> GetAdminCategoriesAsync(
        CatalogManagementService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetCategoriesAsync(cancellationToken);

    private static Task<IReadOnlyList<BrandView>> GetAdminBrandsAsync(
        CatalogManagementService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetBrandsAsync(cancellationToken);

    private static Task<IReadOnlyList<ProductSummaryView>> GetAdminProductsAsync(
        CatalogManagementService catalog,
        CancellationToken cancellationToken) =>
        catalog.GetProductsAsync(cancellationToken);

    private static async Task<IResult> GetAdminProductAsync(
        Guid id,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.GetProductAsync(id, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> CreateCategoryAsync(
        CategoryInput input,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Created(
                (string?)null,
                await catalog.SaveCategoryAsync(null, input, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> UpdateCategoryAsync(
        Guid id,
        CategoryInput input,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.SaveCategoryAsync(id, input, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> CreateBrandAsync(
        BrandInput input,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Created(
                (string?)null,
                await catalog.SaveBrandAsync(null, input, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> UpdateBrandAsync(
        Guid id,
        BrandInput input,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.SaveBrandAsync(id, input, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> CreateProductAsync(
        ProductInput input,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Created(
                (string?)null,
                await catalog.SaveProductAsync(null, input, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> UpdateProductAsync(
        Guid id,
        ProductInput input,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.SaveProductAsync(id, input, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> UpdateProductStatusAsync(
        Guid id,
        ProductStatusInput input,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.SetProductStatusAsync(
                id, input.Status, input.RowVersion, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> UploadImageAsync(
        Guid id,
        CatalogManagementService catalog,
        CatalogMediaOptions options,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.HasFormContentType)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["form"] = ["A multipart form is required."]
            });
        LimitRequestSize(context, options);
        if (context.Request.ContentLength > options.MaxUploadBytes + 64 * 1024)
            return Results.Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "The uploaded image is too large.");
        try
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            var image = form.Files.GetFile("image");
            if (image is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["image"] = ["An image file is required."]
                });
            var altText = form["altText"].ToString();
            var isPrimary = bool.TryParse(form["isPrimary"].ToString(), out var parsedPrimary) && parsedPrimary;
            var result = await catalog.AddImageAsync(
                id,
                altText,
                isPrimary,
                new CatalogImageUpload(
                    image.OpenReadStream(), image.Length, image.FileName, image.ContentType),
                cancellationToken);
            return Results.Created((string?)null, result);
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> UploadCategoryImageAsync(
        Guid id,
        CatalogManagementService catalog,
        CatalogMediaOptions options,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var image = await ReadSingleImageAsync(context, options, cancellationToken);
        if (image.Result is not null) return image.Result;
        try
        {
            return Results.Ok(await catalog.SetCategoryImageAsync(
                id, image.Upload!, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> DeleteCategoryImageAsync(
        Guid id,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.DeleteCategoryImageAsync(id, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> UploadBrandLogoAsync(
        Guid id,
        CatalogManagementService catalog,
        CatalogMediaOptions options,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var image = await ReadSingleImageAsync(context, options, cancellationToken);
        if (image.Result is not null) return image.Result;
        try
        {
            return Results.Ok(await catalog.SetBrandLogoAsync(
                id, image.Upload!, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> DeleteBrandLogoAsync(
        Guid id,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.DeleteBrandLogoAsync(id, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> UpdateImageAsync(
        Guid id,
        Guid imageId,
        ImageMetadataInput input,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await catalog.UpdateImageAsync(
                id, imageId, input.AltText, input.SortOrder, input.IsPrimary, cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> DeleteImageAsync(
        Guid id,
        Guid imageId,
        CatalogManagementService catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            await catalog.DeleteImageAsync(id, imageId, cancellationToken);
            return Results.NoContent();
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static Task<CatalogMediaAuditView> GetMediaAuditAsync(
        CatalogMediaMaintenanceService maintenance,
        CancellationToken cancellationToken) =>
        maintenance.AuditAsync(cancellationToken);

    private static async Task<IResult> CleanupOrphanMediaAsync(
        CatalogMediaMaintenanceService maintenance,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await maintenance.CleanupOrphansAsync(cancellationToken));
        }
        catch (Exception exception)
        {
            return MapException(exception);
        }
    }

    private static IResult MapException(Exception exception) => exception switch
    {
        CatalogValidationException validation => Results.ValidationProblem(validation.Errors),
        CatalogNotFoundException notFound => Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Catalog record was not found.",
            detail: notFound.Message),
        CatalogConflictException conflict => Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Catalog change conflicts with existing data.",
            detail: conflict.Message),
        _ => throw exception
    };

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

    private static async Task<(CatalogImageUpload? Upload, IResult? Result)> ReadSingleImageAsync(
        HttpContext context,
        CatalogMediaOptions options,
        CancellationToken cancellationToken)
    {
        if (!context.Request.HasFormContentType)
            return (null, Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["form"] = ["A multipart form is required."]
            }));
        LimitRequestSize(context, options);
        if (context.Request.ContentLength > options.MaxUploadBytes + 64 * 1024)
            return (null, Results.Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "The uploaded image is too large."));
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("image");
        if (file is null)
            return (null, Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["image"] = ["An image file is required."]
            }));
        return (new CatalogImageUpload(
            file.OpenReadStream(), file.Length, file.FileName, file.ContentType), null);
    }

    private static void LimitRequestSize(HttpContext context, CatalogMediaOptions options)
    {
        var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false })
            feature.MaxRequestBodySize = options.MaxUploadBytes + 64 * 1024;
    }

    public sealed record ProductStatusInput(CatalogProductStatus Status, int RowVersion);
    public sealed record ImageMetadataInput(string? AltText, int SortOrder, bool IsPrimary);
}
