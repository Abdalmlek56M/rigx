using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

public static class IdentityLoginEndpoints
{
    public static IEndpointRouteBuilder MapIdentityLoginEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/identity/sessions", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.Login);
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext context,
        IdentityLoginService loginService,
        ISecurityAuditWriter auditWriter,
        DatabaseTicketStore ticketStore,
        IOptionsMonitor<CookieAuthenticationOptions> cookieOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";

        if (string.IsNullOrWhiteSpace(request.Identifier) || request.Password is null)
            return Results.BadRequest(new { error = "Identifier and password are required." });

        var result = await loginService.AuthenticateAsync(request.Identifier, request.Password, cancellationToken);
        if (result.Status != IdentityLoginStatus.Succeeded)
        {
            try
            {
                await auditWriter.WriteAsync(new SecurityAuditEntry(
                    "Identity.Login.Failed",
                    result.Status == IdentityLoginStatus.InvalidCredentials ? "Failed" : "Rejected",
                    "Anonymous",
                    SubjectUserAccountId: result.UserAccountId,
                    CorrelationId: context.TraceIdentifier,
                    SafeMetadata: JsonSerializer.Serialize(new { reason = result.AuditReason })),
                    cancellationToken);
            }
            catch (Exception exception)
            {
                loggerFactory.CreateLogger("Identity.Login").LogError(exception, "Unable to persist login audit event.");
                return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Authentication is temporarily unavailable.");
            }

            return Results.Json(new { error = "Invalid credentials." }, statusCode: StatusCodes.Status401Unauthorized);
        }

        try
        {
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, result.Principal!);
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("Identity.Login").LogError(exception, "Unable to issue the authenticated session.");
            await RevokeFailedLoginSessionAsync(context, ticketStore, cookieOptions, loggerFactory);
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Authentication is temporarily unavailable.");
        }

        try
        {
            await auditWriter.WriteAsync(new SecurityAuditEntry(
                "Identity.Login.Succeeded",
                "Succeeded",
                "User",
                ActorUserAccountId: result.UserAccountId,
                SubjectUserAccountId: result.UserAccountId,
                CorrelationId: context.TraceIdentifier,
                SafeMetadata: JsonSerializer.Serialize(new { sessionScope = "Full" })),
                cancellationToken);
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("Identity.Login").LogError(exception, "Unable to persist successful login audit event.");
            await RevokeFailedLoginSessionAsync(context, ticketStore, cookieOptions, loggerFactory);
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Authentication is temporarily unavailable.");
        }

        return Results.Ok(new { sessionScope = "Full" });
    }

    private static async Task RevokeFailedLoginSessionAsync(
        HttpContext context,
        DatabaseTicketStore ticketStore,
        IOptionsMonitor<CookieAuthenticationOptions> cookieOptions,
        ILoggerFactory loggerFactory)
    {
        try
        {
            if (!await ticketStore.RevokeSessionCreatedByRequestAsync(context))
                loggerFactory.CreateLogger("Identity.Login").LogError("The request had no recorded session key to revoke.");
        }
        catch (Exception exception)
        {
            loggerFactory.CreateLogger("Identity.Login").LogCritical(exception, "Unable to revoke a session after login finalization failed.");
        }

        var cookie = cookieOptions.Get(CookieAuthenticationDefaults.AuthenticationScheme).Cookie;
        var cookieName = cookie.Name ?? throw new InvalidOperationException("The authentication cookie name is not configured.");
        var prefix = $"{cookieName}=";
        var existingCookies = context.Response.Headers.SetCookie.ToArray();
        context.Response.Headers.SetCookie = new StringValues(existingCookies
            .Where(value => value is null || !value.StartsWith(prefix, StringComparison.Ordinal))
            .ToArray());
        context.Response.Cookies.Delete(cookieName, new CookieOptions
        {
            Path = string.IsNullOrWhiteSpace(cookie.Path) ? "/" : cookie.Path,
            Domain = cookie.Domain,
            HttpOnly = cookie.HttpOnly,
            Secure = cookie.SecurePolicy switch
            {
                CookieSecurePolicy.Always => true,
                CookieSecurePolicy.SameAsRequest => context.Request.IsHttps,
                _ => false
            },
            SameSite = cookie.SameSite,
            IsEssential = cookie.IsEssential
        });
    }

    public sealed record LoginRequest(string? Identifier, string? Password);
}
