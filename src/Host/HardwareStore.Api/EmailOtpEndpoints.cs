using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

public static class EmailOtpEndpoints
{
    public static IEndpointRouteBuilder MapEmailOtpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/identity").AllowAnonymous();
        group.MapPost("/email-otp/login/request", RequestLoginAsync)
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailOtpRequest);
        group.MapPost("/email-otp/login/verify", VerifyLoginAsync)
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailOtpVerify);
        group.MapPost("/password-reset/otp/request", RequestResetAsync)
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailOtpRequest);
        group.MapPost("/password-reset/otp/verify", VerifyResetAsync)
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailOtpVerify);
        return endpoints;
    }

    private static async Task<IResult> RequestLoginAsync(EmailOtpRequest request, EmailOtpService otp,
        HttpContext context, CancellationToken cancellationToken)
    {
        await otp.RequestAsync(request.Email, EmailOtpService.LoginPurpose, context.TraceIdentifier, cancellationToken);
        context.Response.Headers.CacheControl = "no-store";
        return Results.Accepted(value: new { status = "accepted" });
    }

    private static async Task<IResult> RequestResetAsync(EmailOtpRequest request, EmailOtpService otp,
        HttpContext context, CancellationToken cancellationToken)
    {
        await otp.RequestAsync(request.Email, EmailOtpService.PasswordResetPurpose, context.TraceIdentifier, cancellationToken);
        context.Response.Headers.CacheControl = "no-store";
        return Results.Accepted(value: new { status = "accepted" });
    }

    private static async Task<IResult> VerifyLoginAsync(EmailOtpVerify request, EmailOtpService otp,
        IdentityLoginService loginService, ISecurityAuditWriter auditWriter, HttpContext context,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var verified = await otp.VerifyAsync(request.Email, request.Code, EmailOtpService.LoginPurpose,
            context.TraceIdentifier, cancellationToken);
        if (!verified.Succeeded || verified.UserAccountId is null)
            return Results.Json(new { error = "Invalid or expired code." }, statusCode: StatusCodes.Status401Unauthorized);
        var login = await loginService.CreateVerifiedSessionAsync(verified.UserAccountId.Value, cancellationToken);
        if (login.Status != IdentityLoginStatus.Succeeded || login.Principal is null)
            return Results.Json(new { error = "Sign-in is unavailable for this account." }, statusCode: StatusCodes.Status401Unauthorized);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, login.Principal);
        try
        {
            await auditWriter.WriteAsync(new SecurityAuditEntry(
                "Identity.Login.Succeeded", "Succeeded", "User",
                ActorUserAccountId: login.UserAccountId, SubjectUserAccountId: login.UserAccountId,
                CorrelationId: context.TraceIdentifier,
                SafeMetadata: JsonSerializer.Serialize(new { method = "EmailOtp", sessionScope = "Full" })),
                cancellationToken);
        }
        catch
        {
            // Session is already valid; audit failure must not break sign-in silently.
        }
        return Results.Ok(new { sessionScope = "Full", method = "EmailOtp" });
    }

    private static async Task<IResult> VerifyResetAsync(EmailOtpResetVerify request, EmailOtpService otp,
        IdentityAccountLifecycleService lifecycle, HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        var verified = await otp.VerifyAsync(request.Email, request.Code, EmailOtpService.PasswordResetPurpose,
            context.TraceIdentifier, cancellationToken);
        if (!verified.Succeeded || verified.UserAccountId is null)
            return Results.Json(new { error = "Invalid or expired code." }, statusCode: StatusCodes.Status401Unauthorized);
        var result = await lifecycle.ResetPasswordWithVerifiedOtpAsync(
            verified.UserAccountId.Value, request.NewPassword, context.TraceIdentifier, cancellationToken);
        return result.Status == IdentityActionStatus.Succeeded
            ? Results.Ok(new { status = "password-reset" })
            : Results.BadRequest(new { error = "The new password does not meet the password policy." });
    }
}

public sealed record EmailOtpRequest(string? Email);
public sealed record EmailOtpVerify(string? Email, string? Code);
public sealed record EmailOtpResetVerify(string? Email, string? Code, string? NewPassword);
