using System.Security.Claims;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;

public static class ContentMediaEndpoints
{
    public static IEndpointRouteBuilder MapContentMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/media/assets");
        group.MapGet("", ListAsync).RequirePermission("Catalog.ManageMedia");
        group.MapPost("", UploadAsync).RequirePermission("Catalog.ManageMedia");
        group.MapPut("/{id:guid}", UpdateAsync).RequirePermission("Catalog.ManageMedia");
        group.MapPost("/{id:guid}/replace", ReplaceAsync).RequirePermission("Catalog.ManageMedia");
        group.MapDelete("/{id:guid}", DeleteAsync).RequirePermission("Catalog.ManageMedia");
        group.MapGet("/{id:guid}/usages", UsagesAsync).RequirePermission("Catalog.ManageMedia");
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        ContentMediaService media,
        CancellationToken cancellationToken,
        string? search = null,
        int page = 1,
        int pageSize = 24) =>
        Results.Ok(await media.ListAsync(search, page, pageSize, cancellationToken));

    private static async Task<IResult> UploadAsync(
        HttpContext context,
        ContentMediaService media,
        CatalogMediaOptions options,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        if (!context.Request.HasFormContentType)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["form"] = ["A multipart form is required."]
            });
        LimitRequestSize(context, options);
        if (context.Request.ContentLength > options.MaxUploadBytes + 64 * 1024)
            return Results.Problem(statusCode: 413, title: "The uploaded image is too large.");
        try
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            var image = form.Files.GetFile("image");
            if (image is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["image"] = ["An image file is required."]
                });
            var metadata = ReadMetadata(form);
            var result = await media.UploadAsync(
                new CatalogImageUpload(image.OpenReadStream(), image.Length, image.FileName, image.ContentType),
                metadata,
                actorId,
                cancellationToken);
            return Results.Created((string?)null, result);
        }
        catch (CatalogValidationException exception)
        {
            return Results.ValidationProblem(exception.Errors);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        ContentMediaMetadataInput request,
        ContentMediaService media,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await media.UpdateMetadataAsync(id, request, cancellationToken));
        }
        catch (ContentMediaNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CatalogValidationException exception)
        {
            return Results.ValidationProblem(exception.Errors);
        }
    }

    private static async Task<IResult> ReplaceAsync(
        Guid id,
        HttpContext context,
        ContentMediaService media,
        CatalogMediaOptions options,
        CancellationToken cancellationToken,
        long rowVersion = 0)
    {
        if (!context.Request.HasFormContentType)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["form"] = ["A multipart form is required."]
            });
        LimitRequestSize(context, options);
        if (context.Request.ContentLength > options.MaxUploadBytes + 64 * 1024)
            return Results.Problem(statusCode: 413, title: "The uploaded image is too large.");
        try
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            var image = form.Files.GetFile("image");
            if (image is null)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["image"] = ["An image file is required."]
                });
            return Results.Ok(await media.ReplaceImageAsync(
                id,
                new CatalogImageUpload(image.OpenReadStream(), image.Length, image.FileName, image.ContentType),
                rowVersion,
                cancellationToken));
        }
        catch (ContentMediaNotFoundException)
        {
            return Results.NotFound();
        }
        catch (CatalogValidationException exception)
        {
            return Results.ValidationProblem(exception.Errors);
        }
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ContentMediaService media,
        CancellationToken cancellationToken,
        long rowVersion = 0)
    {
        try
        {
            await media.DeleteAsync(id, rowVersion, cancellationToken);
            return Results.NoContent();
        }
        catch (ContentMediaNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ContentMediaInUseException exception)
        {
            return Results.Problem(statusCode: 409, title: exception.Message);
        }
        catch (CatalogValidationException exception)
        {
            return Results.ValidationProblem(exception.Errors);
        }
    }

    private static async Task<IResult> UsagesAsync(
        Guid id,
        ContentMediaService media,
        CancellationToken cancellationToken) =>
        Results.Ok(await media.ListUsagesAsync(id, cancellationToken));

    private static ContentMediaMetadataInput ReadMetadata(IFormCollection form) => new(
        form["titleAr"].ToString(),
        form["titleEn"].ToString(),
        form["altAr"].ToString(),
        form["altEn"].ToString(),
        form["captionAr"].ToString(),
        form["captionEn"].ToString(),
        form["tags"].ToString(),
        0);

    private static void LimitRequestSize(HttpContext context, CatalogMediaOptions options) =>
        context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>()!
            .MaxRequestBodySize = options.MaxUploadBytes + 64 * 1024;

    private static bool TryGetActor(HttpContext context, out Guid actorId) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId) &&
        actorId != Guid.Empty;
}
