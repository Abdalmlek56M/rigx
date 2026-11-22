using HardwareStore.Modules.Catalog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Catalog HTTP API (public browsing + admin). Business errors (validation 400, not found 404, conflict 409)
/// are mapped once by <c>ProblemDetailsExceptionHandler</c>.
/// </summary>
public static partial class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapPublicCatalog(endpoints.MapGroup("/api/v1/catalog").AllowAnonymous());
        var admin = endpoints.MapGroup("/api/v1/admin/catalog");
        MapTaxonomyAdmin(admin);
        MapProductAdmin(admin);
        return endpoints;
    }

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
