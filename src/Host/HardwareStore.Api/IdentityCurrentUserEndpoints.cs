using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.AspNetCore.Http;

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

    private static async Task<IResult> UpdateProfileAsync(
        UpdateIdentityProfileRequest request,
        HttpContext context,
        IdentityProfileService profiles,
        IdentityCurrentUserService users,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await profiles.UpdateAsync(accountId, request, cancellationToken);
        return await GetCurrentUserAsync(context, users, cancellationToken);
    }

    private static async Task<IResult> GetNewDeviceAlertsAsync(
        HttpContext context, NewDeviceSignInAlerts alerts, CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        return context.TryGetUserId(out var accountId)
            ? Results.Ok(new NewDeviceAlertsSetting(await alerts.GetAsync(accountId, cancellationToken)))
            : Results.Unauthorized();
    }

    private static async Task<IResult> SetNewDeviceAlertsAsync(
        NewDeviceAlertsSetting request, HttpContext context, NewDeviceSignInAlerts alerts, CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await alerts.SetAsync(accountId, request.Enabled, cancellationToken);
        return Results.Ok(request);
    }

    private static async Task<IResult> GetCurrentUserAsync(
        HttpContext context,
        IdentityCurrentUserService users,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();

        var user = await users.GetAsync(accountId, cancellationToken);
        return user is null
            ? Results.Unauthorized()
            : Results.Ok(new
            {
                user.Username,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.Roles,
                user.Permissions,
                user.IsEmailVerified,
                user.AvatarStorageKey
            });
    }

    public sealed record NewDeviceAlertsSetting(bool Enabled);
}
