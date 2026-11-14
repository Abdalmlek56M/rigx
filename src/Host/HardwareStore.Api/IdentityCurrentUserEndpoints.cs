using HardwareStore.Common.Domain;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static class IdentityCurrentUserEndpoints
{
    public static IEndpointRouteBuilder MapIdentityCurrentUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/identity/me", GetCurrentUserAsync)
            .RequireAuthorization();
        endpoints.MapPatch("/api/v1/identity/profile", UpdateProfileAsync)
            .RequireAuthorization();
        endpoints.MapGet("/api/v1/identity/settings/new-device-alerts", GetNewDeviceAlertsAsync)
            .RequireAuthorization();
        endpoints.MapPut("/api/v1/identity/settings/new-device-alerts", SetNewDeviceAlertsAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<Ok<CurrentIdentityUser>> UpdateProfileAsync(
        UpdateIdentityProfileRequest request,
        HttpContext context,
        IdentityProfileService profiles,
        IdentityCurrentUserService users,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        await profiles.UpdateAsync(accountId, request, cancellationToken);
        return await GetCurrentUserAsync(context, users, cancellationToken);
    }

    private static async Task<Ok<NewDeviceAlertsSetting>> GetNewDeviceAlertsAsync(
        HttpContext context, NewDeviceSignInAlerts alerts, CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        var accountId = context.RequireUserId();
        return TypedResults.Ok(new NewDeviceAlertsSetting(await alerts.GetAsync(accountId, cancellationToken)));
    }

    private static async Task<Ok<NewDeviceAlertsSetting>> SetNewDeviceAlertsAsync(
        NewDeviceAlertsSetting request, HttpContext context, NewDeviceSignInAlerts alerts, CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        await alerts.SetAsync(accountId, request.Enabled, cancellationToken);
        return TypedResults.Ok(request);
    }

    private static async Task<Ok<CurrentIdentityUser>> GetCurrentUserAsync(
        HttpContext context,
        IdentityCurrentUserService users,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        var accountId = context.RequireUserId();

        var user = await users.GetAsync(accountId, cancellationToken);
        return TypedResults.Ok(user ?? throw new UnauthenticatedException());
    }

    public sealed record NewDeviceAlertsSetting(bool Enabled);
}
