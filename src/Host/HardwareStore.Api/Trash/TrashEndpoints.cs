using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>The trash page: list, restore (<c>System.Trash</c>) and purge now (owner-only <c>System.Purge</c> with a fresh code).</summary>
public static class TrashEndpoints
{
    private const string View = "System.Trash";
    private const string Purge = "System.Purge";

    public static IEndpointRouteBuilder MapTrashEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/system/trash");
        group.MapGet("", ListAsync).RequirePermission(View);
        group.MapPost("/{kind}/{id:guid}/restore", RestoreAsync).RequirePermission(View);
        group.MapDelete("/{kind}/{id:guid}", PurgeAsync).RequirePermission(Purge).RequireStepUp();
        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<TrashEntry>>> ListAsync(HttpContext context, TrashService trash, CancellationToken cancellationToken,
        string? kind = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await trash.ListAsync(kind, cancellationToken));
    }

    private static async Task<NoContent> RestoreAsync(string kind, Guid id, HttpContext context, TrashService trash, CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        await trash.RestoreAsync(kind, id, actorId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> PurgeAsync(string kind, Guid id, HttpContext context, TrashService trash, CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        await trash.PurgeAsync(kind, id, actorId, cancellationToken);
        return TypedResults.NoContent();
    }
}
