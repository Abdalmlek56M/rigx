using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

public static class AccountGovernanceEndpoints
{
    public static IEndpointRouteBuilder MapAccountGovernanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/identity/email-change/request", RequestEmailChangeAsync)
            .RequireAuthorization()
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailChange)
            .RequireStepUp();
        endpoints.MapPost("/api/v1/identity/email-change/confirm", ConfirmEmailChangeAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailChangeConfirm);

        var notifications = endpoints.MapGroup("/api/v1/notifications").RequireAuthorization();
        notifications.MapGet("", GetNotificationsAsync);
        notifications.MapPost("/{id:guid}/read", MarkReadAsync);
        notifications.MapPost("/read-all", MarkAllReadAsync);

        var admin = endpoints.MapGroup("/api/v1/admin/identity");
        admin.MapGet("/users/{id:guid}/restrictions", ListRestrictionsAsync)
            .RequirePermission("System.ManageUsers");
        admin.MapPost("/users/{id:guid}/restrictions", ApplyRestrictionAsync)
            .RequirePermission("System.ManageUsers")
            .RequireStepUp();
        admin.MapPost("/restrictions/{id:guid}/reinstate", ReinstateAsync)
            .RequirePermission("System.ManageUsers");
        return endpoints;
    }

    private static async Task<Ok<EmailChangeResult>> RequestEmailChangeAsync(
        EmailChangeRequestInput request,
        HttpContext context,
        IdentityEmailChangeService service,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        return TypedResults.Ok(await service.RequestAsync(accountId, request, cancellationToken));
    }

    private static async Task<Ok<EmailChangeConfirmResponse>> ConfirmEmailChangeAsync(
        EmailChangeConfirmRequest request,
        IdentityEmailChangeService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(new EmailChangeConfirmResponse(await service.ConfirmAsync(request.Token, cancellationToken)));
    }

    private static async Task<Ok<NotificationPage>> GetNotificationsAsync(
        HttpContext context,
        NotificationService notifications,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await notifications.GetAsync(accountId, cancellationToken));
    }

    private static async Task<NoContent> MarkReadAsync(
        Guid id,
        HttpContext context,
        NotificationService notifications,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        await notifications.MarkReadAsync(accountId, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> MarkAllReadAsync(
        HttpContext context,
        NotificationService notifications,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        await notifications.MarkAllReadAsync(accountId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<RestrictionView>>> ListRestrictionsAsync(
        Guid id,
        AccountRestrictionService restrictions,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await restrictions.ListAsync(id, cancellationToken));

    private static async Task<Ok<RestrictionView>> ApplyRestrictionAsync(
        Guid id,
        RestrictionRequest request,
        HttpContext context,
        AccountRestrictionService restrictions,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await restrictions.ApplyAsync(id, actorId, request, cancellationToken));
    }

    private static async Task<Ok<RestrictionView>> ReinstateAsync(
        Guid id,
        ReinstatementRequest request,
        HttpContext context,
        AccountRestrictionService restrictions,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await restrictions.ReinstateAsync(id, actorId, request, cancellationToken));
    }
}

public sealed record EmailChangeConfirmRequest(string? Token);
public sealed record EmailChangeConfirmResponse(string Status);
