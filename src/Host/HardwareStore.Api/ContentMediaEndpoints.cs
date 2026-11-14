using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

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

    private static async Task<Ok<PagedResult<ContentMediaAssetView>>> ListAsync(
        ContentMediaService media,
        CancellationToken cancellationToken,
        string? search = null,
        int page = 1,
        int pageSize = 24) =>
        TypedResults.Ok(await media.ListAsync(search, page, pageSize, cancellationToken));

    private static async Task<Created<ContentMediaAssetView>> UploadAsync(
        HttpContext context,
        ContentMediaService media,
        CatalogMediaOptions options,
        MediaWatermarkSource watermarks,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        var (form, upload) = await CatalogEndpoints.ReadImageFormAsync(context, options, cancellationToken);
        return TypedResults.Created((string?)null, await media.UploadAsync(
            upload,
            ReadMetadata(form),
            actorId,
            await watermarks.ForArticlesAsync(cancellationToken),
            cancellationToken));
    }

    private static async Task<Ok<ContentMediaAssetView>> UpdateAsync(
        Guid id,
        ContentMediaMetadataInput request,
        ContentMediaService media,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await media.UpdateMetadataAsync(id, request, cancellationToken));

    private static async Task<Ok<ContentMediaAssetView>> ReplaceAsync(
        Guid id,
        HttpContext context,
        ContentMediaService media,
        CatalogMediaOptions options,
        MediaWatermarkSource watermarks,
        CancellationToken cancellationToken,
        long rowVersion = 0)
    {
        var upload = await CatalogEndpoints.ReadSingleImageAsync(context, options, cancellationToken);
        return TypedResults.Ok(await media.ReplaceImageAsync(
            id,
            upload,
            rowVersion,
            await watermarks.ForArticlesAsync(cancellationToken),
            cancellationToken));
    }

    private static async Task<NoContent> DeleteAsync(
        Guid id,
        HttpContext context,
        ContentMediaService media,
        TrashAuditor audit,
        CancellationToken cancellationToken,
        long rowVersion = 0)
    {
        var actorId = context.RequireUserId();
        await media.DeleteAsync(id, rowVersion, actorId, cancellationToken);
        await audit.DeletedAsync(ContentMediaTrashSource.TrashKind, id, actorId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<ContentMediaUsageView>>> UsagesAsync(
        Guid id,
        ContentMediaService media,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await media.ListUsagesAsync(id, cancellationToken));

    private static ContentMediaMetadataInput ReadMetadata(IFormCollection form) => new(
        form["titleAr"].ToString(),
        form["titleEn"].ToString(),
        form["altAr"].ToString(),
        form["altEn"].ToString(),
        form["captionAr"].ToString(),
        form["captionEn"].ToString(),
        form["tags"].ToString(),
        0);
}
