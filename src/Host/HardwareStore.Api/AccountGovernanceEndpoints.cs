using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

public static class AccountGovernanceEndpoints
{
    public static IEndpointRouteBuilder MapAccountGovernanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/identity/email-change/request", RequestEmailChangeAsync)
            .RequireAuthorization()
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailChange);
        endpoints.MapPost("/api/v1/identity/email-change/confirm", ConfirmEmailChangeAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailVerification);

        var notifications = endpoints.MapGroup("/api/v1/notifications").RequireAuthorization();
        notifications.MapGet("", GetNotificationsAsync);
        notifications.MapPost("/{id:guid}/read", MarkReadAsync);
        notifications.MapPost("/read-all", MarkAllReadAsync);

        var admin = endpoints.MapGroup("/api/v1/admin/identity");
        admin.MapGet("/users/{id:guid}/restrictions", ListRestrictionsAsync)
            .RequirePermission("System.ManageUsers");
        admin.MapPost("/users/{id:guid}/restrictions", ApplyRestrictionAsync)
            .RequirePermission("System.ManageUsers");
        admin.MapPost("/restrictions/{id:guid}/reinstate", ReinstateAsync)
            .RequirePermission("System.ManageUsers");
        return endpoints;
    }

    private static async Task<IResult> RequestEmailChangeAsync(
        EmailChangeRequestInput request,
        HttpContext context,
        IdentityEmailChangeService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await service.RequestAsync(accountId, request, cancellationToken));
        }
        catch (IdentityAdministrationException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> ConfirmEmailChangeAsync(
        EmailChangeConfirmRequest request,
        IdentityEmailChangeService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(new { status = await service.ConfirmAsync(request.Token, cancellationToken) });
        }
        catch (IdentityAdministrationException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> GetNotificationsAsync(
        HttpContext context,
        NotificationService notifications,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await notifications.GetAsync(accountId, cancellationToken));
    }

    private static async Task<IResult> MarkReadAsync(
        Guid id,
        HttpContext context,
        NotificationService notifications,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await notifications.MarkReadAsync(accountId, id, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllReadAsync(
        HttpContext context,
        NotificationService notifications,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await notifications.MarkAllReadAsync(accountId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListRestrictionsAsync(
        Guid id,
        AccountRestrictionService restrictions,
        CancellationToken cancellationToken) =>
        Results.Ok(await restrictions.ListAsync(id, cancellationToken));

    private static async Task<IResult> ApplyRestrictionAsync(
        Guid id,
        RestrictionRequest request,
        HttpContext context,
        AccountRestrictionService restrictions,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await restrictions.ApplyAsync(id, actorId, request, cancellationToken));
        }
        catch (IdentityAdministrationException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> ReinstateAsync(
        Guid id,
        ReinstatementRequest request,
        HttpContext context,
        AccountRestrictionService restrictions,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await restrictions.ReinstateAsync(id, actorId, request, cancellationToken));
        }
        catch (IdentityAdministrationException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }
}

public sealed record EmailChangeConfirmRequest(string? Token);
