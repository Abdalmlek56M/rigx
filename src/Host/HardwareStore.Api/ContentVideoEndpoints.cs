using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;

public static class ContentVideoEndpoints
{
    public static IEndpointRouteBuilder MapContentVideoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/content/videos", ListPublicAsync).AllowAnonymous();
        endpoints.MapGet("/api/v1/content/videos/{id:guid}", GetPublicAsync).AllowAnonymous();
        endpoints.MapGet("/api/v1/content/videos/{id:guid}/stream", StreamAsync).AllowAnonymous();

        var admin = endpoints.MapGroup("/api/v1/admin/media/videos");
        admin.MapGet("", ListAdminAsync).RequirePermission("Catalog.ManageMedia");
        admin.MapPost("", UploadAsync).RequirePermission("Catalog.ManageMedia");
        admin.MapPost("/youtube", CreateYouTubeAsync).RequirePermission("Catalog.ManageMedia");
        admin.MapPost("/{id:guid}/publish", PublishAsync).RequirePermission("Catalog.ManageMedia");
        admin.MapPost("/{id:guid}/unpublish", UnpublishAsync).RequirePermission("Catalog.ManageMedia");
        return endpoints;
    }

    private static async Task<IResult> ListPublicAsync(HttpContext context, ContentVideoService videos, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await videos.ListAsync(true, cancellationToken));
    }

    private static async Task<IResult> GetPublicAsync(Guid id, HttpContext context, ContentVideoService videos, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        try
        {
            var video = await videos.GetPublishedAsync(id, cancellationToken);
            return Results.Ok(new { video.Id, video.TitleAr, video.TitleEn, video.DescriptionAr, video.DescriptionEn, video.MimeType, video.FileSizeBytes, video.PosterStorageKey });
        }
        catch (CatalogValidationException) { return Results.NotFound(); }
    }

    private static async Task<IResult> StreamAsync(Guid id, HttpContext context, ContentVideoService videos, CancellationToken cancellationToken)
    {
        ContentVideoAsset video;
        try { video = await videos.GetPublishedAsync(id, cancellationToken); }
        catch (CatalogValidationException) { return Results.NotFound(); }

        Stream stream;
        try { stream = videos.OpenRead(video.StorageKey); }
        catch (FileNotFoundException) { return Results.NotFound(); }

        await using (stream)
        {
            var total = stream.CanSeek ? stream.Length : video.FileSizeBytes;
            var range = context.Request.Headers.Range.ToString();
            long start = 0;
            long end = total - 1;
            var partial = false;
            if (!string.IsNullOrWhiteSpace(range) && range.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            {
                var spec = range["bytes=".Length..].Split(',')[0].Split('-');
                if (long.TryParse(spec[0], out var parsedStart)) start = Math.Max(0, parsedStart);
                if (spec.Length > 1 && long.TryParse(spec[1], out var parsedEnd)) end = Math.Min(total - 1, parsedEnd);
                if (start > end || start >= total)
                {
                    context.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                    context.Response.Headers.ContentRange = $"bytes */{total}";
                    return new StreamedResult();
                }
                partial = true;
            }
            var length = end - start + 1;
            context.Response.StatusCode = partial ? StatusCodes.Status206PartialContent : StatusCodes.Status200OK;
            context.Response.ContentType = video.MimeType;
            context.Response.ContentLength = length;
            context.Response.Headers.AcceptRanges = "bytes";
            context.Response.Headers.CacheControl = "public, max-age=3600";
            if (partial) context.Response.Headers.ContentRange = $"bytes {start}-{end}/{total}";
            if (stream.CanSeek) stream.Seek(start, SeekOrigin.Begin);
            var buffer = new byte[128 * 1024];
            long remaining = length;
            while (remaining > 0)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken);
                if (read <= 0) break;
                await context.Response.Body.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                remaining -= read;
            }
            return new StreamedResult();
        }
    }

    private static async Task<IResult> ListAdminAsync(HttpContext context, ContentVideoService videos, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await videos.ListAsync(false, cancellationToken));
    }

    private static async Task<IResult> CreateYouTubeAsync(
        CreateYouTubeVideoRequest request, ContentVideoService videos, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await videos.CreateYouTubeAsync(request.TitleAr, request.TitleEn,
                request.DescriptionAr, request.DescriptionEn, request.Url, cancellationToken));
        }
        catch (CatalogValidationException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
    }

    private static async Task<IResult> UploadAsync(
        HttpContext context, ContentVideoService videos, CatalogMediaOptions options, CancellationToken cancellationToken)
    {
        if (!context.Request.HasFormContentType)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["form"] = ["A multipart form is required."] });
        context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = options.MaxVideoBytes + 128 * 1024;
        try
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("video");
            if (file is null || file.Length == 0)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["video"] = ["A video file is required."] });
            var result = await videos.UploadAsync(
                form["titleAr"], form["titleEn"], form["descriptionAr"], form["descriptionEn"],
                file.OpenReadStream(), file.Length, file.FileName, cancellationToken);
            return Results.Ok(result);
        }
        catch (CatalogValidationException exception)
        {
            return Results.ValidationProblem(exception.Errors);
        }
    }

    private static async Task<IResult> PublishAsync(Guid id, ContentVersionRequest request, ContentVideoService videos,
        ArticleVideoService articleVideos, CancellationToken cancellationToken) =>
        await SetPublishedAsync(id, true, request, videos, articleVideos, cancellationToken);

    private static async Task<IResult> UnpublishAsync(Guid id, ContentVersionRequest request, ContentVideoService videos,
        ArticleVideoService articleVideos, CancellationToken cancellationToken) =>
        await SetPublishedAsync(id, false, request, videos, articleVideos, cancellationToken);

    private static async Task<IResult> SetPublishedAsync(Guid id, bool published, ContentVersionRequest request,
        ContentVideoService videos, ArticleVideoService articleVideos, CancellationToken cancellationToken)
    {
        try
        {
            if (!published) await articleVideos.GuardVideoUnpublishAsync(id, cancellationToken);
            return Results.Ok(await videos.SetPublishedAsync(id, published, request.RowVersion, cancellationToken));
        }
        catch (StorefrontException exception) { return Results.Problem(statusCode: exception.StatusCode, title: exception.Message); }
        catch (CatalogValidationException exception) { return Results.Problem(statusCode: 400, title: exception.Errors.Values.FirstOrDefault()?.FirstOrDefault()); }
    }
}

public sealed record CreateYouTubeVideoRequest(
    string? TitleAr, string? TitleEn, string? DescriptionAr, string? DescriptionEn, string? Url);

public sealed class StreamedResult : IResult
{
    public Task ExecuteAsync(HttpContext httpContext) => Task.CompletedTask;
}
