using HardwareStore.Modules.Identity.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

public static class IdentityAccountLifecycleEndpoints
{
    public static IEndpointRouteBuilder MapIdentityAccountLifecycleEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/identity/email-verifications", VerifyEmailAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailVerification);
        endpoints.MapPost("/api/v1/identity/email-verification-resends", ResendVerificationAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailVerificationResend);
        endpoints.MapPost("/api/v1/identity/password-reset-requests", RequestPasswordResetAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.PasswordResetRequest);
        endpoints.MapPost("/api/v1/identity/password-resets", ResetPasswordAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.PasswordReset);
        endpoints.MapPost("/api/v1/identity/password-changes", ChangePasswordAsync)
            .RequireAuthorization();
        endpoints.MapDelete("/api/v1/identity/sessions/current", LogoutCurrentAsync)
            .RequireAuthorization();
        endpoints.MapDelete("/api/v1/identity/sessions/others", LogoutOtherSessionsAsync)
            .RequireAuthorization();
        endpoints.MapDelete("/api/v1/identity/sessions/{id:guid}", RevokeSessionAsync)
            .RequireAuthorization();
        endpoints.MapDelete("/api/v1/identity/sessions", LogoutAllAsync)
            .RequireAuthorization();
        endpoints.MapGet("/api/v1/identity/sessions", GetSessionsAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> VerifyEmailAsync(
        EmailVerificationRequest request,
        HttpContext context,
        IdentityAccountLifecycleService lifecycle,
        CancellationToken cancellationToken)
    {
        NoStore(context);
        var result = await lifecycle.VerifyEmailAsync(
            request.Token, request.NewPassword, context.TraceIdentifier, cancellationToken);
        return MapSensitiveResult(result, "newPassword");
    }

    private static async Task<IResult> ResendVerificationAsync(
        EmailVerificationResendRequest request,
        HttpContext context,
        IdentityAccountLifecycleService lifecycle,
        CancellationToken cancellationToken)
    {
        NoStore(context);
        var result = await lifecycle.ResendEmailVerificationAsync(
            request.Email, context.TraceIdentifier, cancellationToken);
        return result.Status == IdentityActionStatus.InvalidInput
            ? ValidationProblem(result.InvalidField ?? "email", "The supplied email is invalid.")
            : result.Status == IdentityActionStatus.Failed
                ? TemporaryUnavailable()
                : GenericAccepted();
    }

    private static async Task<IResult> RequestPasswordResetAsync(
        PasswordResetRequest request,
        HttpContext context,
        IdentityAccountLifecycleService lifecycle,
        CancellationToken cancellationToken)
    {
        NoStore(context);
        var result = await lifecycle.RequestPasswordResetAsync(
            request.Email, context.TraceIdentifier, cancellationToken);
        return result.Status == IdentityActionStatus.InvalidInput
            ? ValidationProblem(result.InvalidField ?? "email", "The supplied email is invalid.")
            : result.Status == IdentityActionStatus.Failed
                ? TemporaryUnavailable()
                : GenericAccepted();
    }

    private static async Task<IResult> ResetPasswordAsync(
        PasswordResetCompletionRequest request,
        HttpContext context,
        IdentityAccountLifecycleService lifecycle,
        CancellationToken cancellationToken)
    {
        NoStore(context);
        var result = await lifecycle.ResetPasswordAsync(
            request.Token, request.NewPassword, context.TraceIdentifier, cancellationToken);
        return MapSensitiveResult(result, "newPassword");
    }

    private static async Task<IResult> ChangePasswordAsync(
        PasswordChangeRequest request,
        HttpContext context,
        IdentityAccountLifecycleService lifecycle,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        NoStore(context);
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();

        var result = await lifecycle.ChangePasswordAsync(
            accountId, request.CurrentPassword, request.NewPassword, context.TraceIdentifier, cancellationToken);
        if (result.Status == IdentityActionStatus.InvalidPassword)
            return PasswordPolicyProblem(result.InvalidField);
        if (result.Status == IdentityActionStatus.InvalidInput)
            return ValidationProblem(result.InvalidField ?? "currentPassword", "The current password is required.");
        if (result.Status == IdentityActionStatus.InvalidCredentials)
            return GenericBadRequest();
        if (result.Status == IdentityActionStatus.Conflict)
            return Results.Conflict(new { error = "The account changed while the request was processing." });
        if (result.Status == IdentityActionStatus.Failed)
            return TemporaryUnavailable();

        await SignOutCurrentCookieAsync(context, loggerFactory);
        return Results.NoContent();
    }

    private static async Task<IResult> LogoutCurrentAsync(
        HttpContext context,
        IdentitySessionService sessions,
        DatabaseTicketStore ticketStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        NoStore(context);
        if (!context.TryGetUserId(out var accountId))
        {
            loggerFactory.CreateLogger("Identity.Sessions")
                .LogWarning("Logout is missing an authenticated account identifier.");
            return Results.Unauthorized();
        }
        if (!DatabaseTicketStore.TryGetCurrentRequestSessionKey(context, out var sessionKey))
        {
            loggerFactory.CreateLogger("Identity.Sessions")
                .LogWarning("Logout is missing the authenticated request session key.");
            return Results.Unauthorized();
        }

        var result = await sessions.RevokeCurrentAsync(
            accountId, sessionKey, context.TraceIdentifier, cancellationToken);
        if (result.Status == IdentityActionStatus.Failed)
            return TemporaryUnavailable();
        if (result.Status == IdentityActionStatus.InvalidCredentials)
            return Results.Unauthorized();

        await SignOutCurrentCookieAsync(context, loggerFactory);
        return Results.NoContent();
    }

    private static async Task<IResult> GetSessionsAsync(
        HttpContext context,
        IdentitySessionService sessions,
        CancellationToken cancellationToken)
    {
        NoStore(context);
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        DatabaseTicketStore.TryGetCurrentRequestSessionKey(context, out var currentKey);
        return Results.Ok(await sessions.ListAsync(accountId, currentKey, cancellationToken));
    }

    private static async Task<IResult> RevokeSessionAsync(
        Guid id,
        HttpContext context,
        IdentitySessionService sessions,
        CancellationToken cancellationToken)
    {
        NoStore(context);
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        var result = await sessions.RevokeSessionAsync(
            accountId, id, context.TraceIdentifier, cancellationToken);
        return result.Status switch
        {
            IdentityActionStatus.Succeeded => Results.NoContent(),
            IdentityActionStatus.InvalidCredentials => Results.NotFound(),
            _ => TemporaryUnavailable()
        };
    }

    private static async Task<IResult> LogoutOtherSessionsAsync(
        HttpContext context,
        IdentitySessionService sessions,
        CancellationToken cancellationToken)
    {
        NoStore(context);
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        if (!DatabaseTicketStore.TryGetCurrentRequestSessionKey(context, out var currentKey))
            return Results.Unauthorized();
        var result = await sessions.RevokeOtherSessionsAsync(
            accountId, currentKey, context.TraceIdentifier, cancellationToken);
        return result.Status switch
        {
            IdentityActionStatus.Succeeded => Results.NoContent(),
            IdentityActionStatus.InvalidCredentials => Results.Unauthorized(),
            _ => TemporaryUnavailable()
        };
    }

    private static async Task<IResult> LogoutAllAsync(
        HttpContext context,
        IdentitySessionService sessions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        NoStore(context);
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();

        var result = await sessions.RevokeAllAsync(accountId, context.TraceIdentifier, cancellationToken);
        if (result.Status == IdentityActionStatus.Failed)
            return TemporaryUnavailable();
        if (result.Status == IdentityActionStatus.InvalidCredentials)
            return Results.Unauthorized();

        await SignOutCurrentCookieAsync(context, loggerFactory);
        return Results.NoContent();
    }

    private static IResult MapSensitiveResult(IdentityActionResult result, string passwordField) =>
        result.Status switch
        {
            IdentityActionStatus.Succeeded => Results.NoContent(),
            IdentityActionStatus.InvalidPassword => PasswordPolicyProblem(result.InvalidField),
            IdentityActionStatus.InvalidInput => ValidationProblem(
                result.InvalidField ?? passwordField, "The supplied value is invalid."),
            IdentityActionStatus.InvalidToken or IdentityActionStatus.InvalidCredentials => GenericBadRequest(),
            IdentityActionStatus.Conflict => Results.Conflict(
                new { error = "The account changed while the request was processing." }),
            _ => TemporaryUnavailable()
        };

    private static IResult PasswordPolicyProblem(string? reason)
    {
        var message = reason switch
        {
            "minimumLength" => "Password must contain at least 15 Unicode characters.",
            "maximumLength" => "Password must contain no more than 128 Unicode characters.",
            "commonPassword" => "Password does not meet the security policy.",
            _ => "Password does not meet the security policy."
        };
        return ValidationProblem("newPassword", message);
    }

    private static IResult ValidationProblem(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });

    private static IResult GenericAccepted() =>
        Results.Accepted(value: new
        {
            message = "If the account is eligible, instructions will be sent."
        });

    private static IResult GenericBadRequest() =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "The request could not be completed.",
            detail: "The token is invalid or expired.");

    private static IResult TemporaryUnavailable() =>
        Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "The request is temporarily unavailable.");

    private static void NoStore(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
    }

    private static async Task SignOutCurrentCookieAsync(HttpContext context, ILoggerFactory loggerFactory)
    {
        try
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        catch (Exception exception)
        {
            // The database session is already revoked. A stale browser cookie cannot authenticate,
            // but the security event must remain visible to operators.
            loggerFactory.CreateLogger("Identity.Sessions")
                .LogCritical(exception, "Unable to delete the authentication cookie after session revocation.");
        }
    }

    public sealed record EmailVerificationRequest(string? Token, string? NewPassword);
    public sealed record EmailVerificationResendRequest(string? Email);
    public sealed record PasswordResetRequest(string? Email);
    public sealed record PasswordResetCompletionRequest(string? Token, string? NewPassword);
    public sealed record PasswordChangeRequest(string? CurrentPassword, string? NewPassword);
}
