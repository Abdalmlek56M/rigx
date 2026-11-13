using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Authorization;

/// <summary>
/// "Sign in with Google" and the profile's login methods (X-Login, X-Profile). The frontend gets a
/// Google ID token from Google's button and posts it here; the server validates it.
/// </summary>
public static class GoogleSignInEndpoints
{
    public static IEndpointRouteBuilder MapGoogleSignInEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/identity/google", SignInAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.EmailCodeVerify);

        var methods = endpoints.MapGroup("/api/v1/identity/login-methods").RequireAuthorization();
        methods.MapGet("", GetAsync);
        methods.MapPost("/google", LinkAsync).RequireStepUp();
        methods.MapDelete("/google", UnlinkAsync);

        endpoints.MapDelete("/api/v1/admin/identity/users/{id:guid}/google", AdminUnlinkAsync)
            .RequirePermission("System.ManageUsers")
            .RequireStepUp();
        return endpoints;
    }

    private static async Task<IResult> SignInAsync(
        GoogleSignInRequest request,
        GoogleSignInService google,
        ISecurityAuditWriter auditWriter,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        var result = await google.SignInAsync(request.IdToken, cancellationToken);
        if (!result.Succeeded)
        {
            await auditWriter.WriteAsync(new SecurityAuditEntry(
                "Identity.Login.Failed", "Rejected", "Anonymous",
                SubjectUserAccountId: result.UserAccountId,
                CorrelationId: context.TraceIdentifier,
                SafeMetadata: JsonSerializer.Serialize(new { method = "Google", reason = result.AuditReason })),
                cancellationToken);
            return Results.Json(new { error = "Google sign-in is unavailable for this account. Use an email code." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        await IdentitySignIn.CompleteAsync(context, result.Principal!, result.UserAccountId!.Value,
            request.RememberDevice, "Google", auditWriter, cancellationToken);
        return Results.Ok(new { sessionScope = "Full", isNewAccount = result.IsNewAccount });
    }

    private static async Task<IResult> GetAsync(HttpContext context, GoogleSignInService google, CancellationToken cancellationToken)
    {
        IdentityHttp.NoStore(context);
        return context.TryGetUserId(out var accountId)
            ? Results.Ok(await google.GetLoginMethodsAsync(accountId, cancellationToken))
            : Results.Unauthorized();
    }

    private static async Task<IResult> LinkAsync(
        GoogleLinkRequest request, HttpContext context, GoogleSignInService google, CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await google.LinkAsync(accountId, request.IdToken, cancellationToken);
        return Results.Ok(await google.GetLoginMethodsAsync(accountId, cancellationToken));
    }

    private static async Task<IResult> UnlinkAsync(HttpContext context, GoogleSignInService google, CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await google.UnlinkAsync(accountId, accountId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AdminUnlinkAsync(
        Guid id, HttpContext context, GoogleSignInService google, CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        await google.UnlinkAsync(id, actorId, cancellationToken);
        return Results.NoContent();
    }

    public sealed record GoogleSignInRequest(string? IdToken, bool RememberDevice);
    public sealed record GoogleLinkRequest(string? IdToken);
}
