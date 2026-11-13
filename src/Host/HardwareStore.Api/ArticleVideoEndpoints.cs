using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;

public static class ArticleVideoEndpoints
{
    public static IEndpointRouteBuilder MapArticleVideoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/content/articles/{slug}/videos", ListPublicAsync).AllowAnonymous();
        var admin = endpoints.MapGroup("/api/v1/admin/content/articles/{id:guid}/videos");
        admin.MapGet("", ListAdminAsync).RequirePermission("Catalog.ManageMedia");
        admin.MapPut("", SetAsync).RequirePermission("Catalog.ManageMedia");
        return endpoints;
    }

    private static async Task<IResult> ListPublicAsync(string slug, HttpContext context, ArticleVideoService service,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await service.ListBySlugAsync(slug, cancellationToken));
    }

    private static async Task<IResult> ListAdminAsync(Guid id, HttpContext context, ArticleVideoService service,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await service.ListAsync(id, false, cancellationToken)); }
        catch (StorefrontException exception) { return Results.Problem(statusCode: exception.StatusCode, title: exception.Message); }
    }

    private static async Task<IResult> SetAsync(Guid id, ArticleVideoIdsRequest request, ArticleVideoService service,
        CancellationToken cancellationToken)
    {
        try { return Results.Ok(await service.SetAsync(id, request.VideoIds, cancellationToken)); }
        catch (StorefrontException exception) { return Results.Problem(statusCode: exception.StatusCode, title: exception.Message); }
    }
}

public sealed record ArticleVideoIdsRequest(IReadOnlyList<Guid>? VideoIds);
