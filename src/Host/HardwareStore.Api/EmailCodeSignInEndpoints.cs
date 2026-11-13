using System.Security.Claims;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>Sign-in with a 6-digit email code (X-Login): request a code, then enter it.</summary>
public static class EmailCodeSignInEndpoints
{
    public static IEndpointRouteBuilder MapEmailCodeSignInEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity/email-code").AllowAnonymous();
        group.MapPost("/request", RequestAsync)
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailCodeRequest);
        group.MapPost("/verify", VerifyAsync)
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailCodeVerify);
        return endpoints;
    }

    private static async Task<IResult> RequestAsync(
        EmailCodeRequest request,
        EmailCodeSignInService signIn,
        TimeProvider timeProvider,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        var ticket = await signIn.RequestAsync(request.Email, cancellationToken);
        return Results.Accepted(value: EmailCodeTicketView.From(ticket, timeProvider));
    }

    private static async Task<IResult> VerifyAsync(
        EmailCodeVerifyRequest request,
        EmailCodeSignInService signIn,
        ISecurityAuditWriter auditWriter,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        var result = await signIn.VerifyAsync(request.Email, request.Code, cancellationToken);
        if (result.Status != EmailCodeSignInStatus.SignedIn)
        {
            await auditWriter.WriteAsync(new SecurityAuditEntry(
                "Identity.Login.Failed", "Rejected", "Anonymous",
                SubjectUserAccountId: result.UserAccountId,
                CorrelationId: context.TraceIdentifier,
                SafeMetadata: JsonSerializer.Serialize(new { method = "EmailCode", reason = result.AuditReason })),
                cancellationToken);
            return result.Status == EmailCodeSignInStatus.InvalidCode
                ? Results.Json(new { error = "The code is wrong or expired.", attemptsLeft = result.AttemptsLeft },
                    statusCode: StatusCodes.Status401Unauthorized)
                : Results.Json(new { error = "Sign-in is unavailable for this account." },
                    statusCode: StatusCodes.Status401Unauthorized);
        }

        await IdentitySignIn.CompleteAsync(context, result.Principal!, result.UserAccountId!.Value,
            request.RememberDevice, "EmailCode", auditWriter, cancellationToken);
        return Results.Ok(new { sessionScope = "Full", isNewAccount = result.IsNewAccount });
    }

    public sealed record EmailCodeRequest(string? Email);
    public sealed record EmailCodeVerifyRequest(string? Email, string? Code, bool RememberDevice);
}

/// <summary>Seconds until the code expires and until "resend" is allowed (the UI's countdown).</summary>
public sealed record EmailCodeTicketView(int ExpiresInSeconds, int ResendAfterSeconds)
{
    public static EmailCodeTicketView From(EmailCodeTicket ticket, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        return new EmailCodeTicketView(
            Math.Max(0, (int)Math.Ceiling((ticket.ExpiresAtUtc - now).TotalSeconds)),
            Math.Max(0, (int)Math.Ceiling((ticket.ResendAvailableAtUtc - now).TotalSeconds)));
    }
}

/// <summary>The one way a verified sign-in (email code or Google) becomes a cookie session.</summary>
public static class IdentitySignIn
{
    /// <summary>
    /// The success audit is written first: if it fails, no session is created. "Remember this device"
    /// makes the cookie persistent; otherwise it ends with the browser session (and within 12 hours).
    /// </summary>
    public static async Task CompleteAsync(
        HttpContext context,
        ClaimsPrincipal principal,
        Guid accountId,
        bool rememberDevice,
        string method,
        ISecurityAuditWriter auditWriter,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(new SecurityAuditEntry(
            "Identity.Login.Succeeded", "Succeeded", "User",
            ActorUserAccountId: accountId,
            SubjectUserAccountId: accountId,
            CorrelationId: context.TraceIdentifier,
            SafeMetadata: JsonSerializer.Serialize(new { method, rememberDevice, sessionScope = "Full" })),
            cancellationToken);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = rememberDevice });
    }
}
