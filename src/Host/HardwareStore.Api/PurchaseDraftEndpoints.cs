using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Purchasing;
using Microsoft.AspNetCore.Http;

public static class PurchaseDraftEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseDraftEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/purchasing/drafts");
        group.MapGet("", ListAsync).RequirePermission("Purchasing.View");
        group.MapGet("/{id:guid}", GetAsync).RequirePermission("Purchasing.View");
        group.MapPost("", CreateAsync).RequirePermission("Purchasing.CreateOrder");
        group.MapPut("/{id:guid}", UpdateAsync).RequirePermission("Purchasing.CreateOrder");
        group.MapPost("/{id:guid}/cancel", CancelAsync).RequirePermission("Purchasing.CreateOrder");
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        HttpContext context,
        PurchaseDraftService drafts,
        CancellationToken cancellationToken,
        string? sourceType = null,
        string? status = null,
        string? search = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int page = 1,
        int pageSize = 25)
    {
        if (!context.TryGetUserId(out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await drafts.GetAsync(
            sourceType, status, search, fromDate, toDate, page, pageSize, cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        HttpContext context,
        PurchaseDraftService drafts,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await drafts.GetByIdAsync(id, cancellationToken));
        }
        catch (PurchaseDraftException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> CreateAsync(
        PurchaseDraftCreateRequest request,
        HttpContext context,
        PurchaseDraftService drafts,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Created(
                (string?)null,
                await drafts.CreateAsync(request, actorId, cancellationToken));
        }
        catch (PurchaseDraftException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        PurchaseDraftUpdateRequest request,
        HttpContext context,
        PurchaseDraftService drafts,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await drafts.UpdateAsync(id, request, actorId, cancellationToken));
        }
        catch (PurchaseDraftException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> CancelAsync(
        Guid id,
        PurchaseDraftVersionRequest request,
        HttpContext context,
        PurchaseDraftService drafts,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await drafts.CancelAsync(id, request.RowVersion, actorId, cancellationToken));
        }
        catch (PurchaseDraftException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }
}

public sealed record PurchaseDraftVersionRequest(long RowVersion);
