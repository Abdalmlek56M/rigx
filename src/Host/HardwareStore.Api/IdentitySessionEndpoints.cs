using HardwareStore.Common.Domain;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>The signed-in user's devices: list them and sign out one, the others, or all.</summary>
public static class IdentitySessionEndpoints
{
    public static IEndpointRouteBuilder MapIdentitySessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity/sessions").RequireAuthorization();
        group.MapGet("", GetSessionsAsync);
        group.MapDelete("/current", LogoutCurrentAsync);
        group.MapDelete("/others", LogoutOtherSessionsAsync);
        group.MapDelete("/{id:guid}", RevokeSessionAsync);
        group.MapDelete("", LogoutAllAsync);
        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<IdentitySessionView>>> GetSessionsAsync(
        HttpContext context,
        IdentitySessionService sessions,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        var accountId = context.RequireUserId();
        DatabaseTicketStore.TryGetCurrentRequestSessionKey(context, out var currentKey);
        return TypedResults.Ok(await sessions.ListAsync(accountId, currentKey, cancellationToken));
    }

    private static async Task<NoContent> LogoutCurrentAsync(
        HttpContext context,
        IdentitySessionService sessions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        var accountId = context.RequireUserId();
        if (!DatabaseTicketStore.TryGetCurrentRequestSessionKey(context, out var sessionKey))
            throw new UnauthenticatedException();

        await sessions.RevokeCurrentAsync(accountId, sessionKey, context.TraceIdentifier, cancellationToken);
        await IdentityHttp.SignOutCookieAsync(context, loggerFactory);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> RevokeSessionAsync(
        Guid id,
        HttpContext context,
        IdentitySessionService sessions,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        var accountId = context.RequireUserId();
        await sessions.RevokeSessionAsync(accountId, id, context.TraceIdentifier, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> LogoutOtherSessionsAsync(
        HttpContext context,
        IdentitySessionService sessions,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        var accountId = context.RequireUserId();
        if (!DatabaseTicketStore.TryGetCurrentRequestSessionKey(context, out var currentKey))
            throw new UnauthenticatedException();
        await sessions.RevokeOtherSessionsAsync(accountId, currentKey, context.TraceIdentifier, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> LogoutAllAsync(
        HttpContext context,
        IdentitySessionService sessions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        var accountId = context.RequireUserId();
        await sessions.RevokeAllAsync(accountId, context.TraceIdentifier, cancellationToken);
        await IdentityHttp.SignOutCookieAsync(context, loggerFactory);
        return TypedResults.NoContent();
    }
}

/// <summary>Small HTTP helpers shared by the Identity endpoints.</summary>
public static class IdentityHttp
{
    /// <summary>Identity responses carry personal or security data and must never be cached.</summary>
    public static void NoStore(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
    }

    /// <summary>
    /// Deletes the browser cookie after the server-side session was revoked. A failure here is not a
    /// business error: the stale cookie can no longer authenticate, so it is logged, not returned.
    /// </summary>
    public static async Task SignOutCookieAsync(HttpContext context, ILoggerFactory loggerFactory)
    {
        try
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("Identity.Sessions")
                .LogCritical(exception, "Unable to delete the authentication cookie after session revocation.");
        }
    }
}
