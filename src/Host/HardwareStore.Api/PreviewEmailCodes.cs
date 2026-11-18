using HardwareStore.Common.Domain;
using HardwareStore.Modules.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>The newest sign-in/step-up code the in-memory email provider kept for an address (Development preview only).</summary>
public sealed record PreviewEmailCode(string Code, string Purpose, DateTimeOffset ExpiresAtUtc);

public sealed partial class PreviewInitializationService
{
    /// <summary>
    /// Lets local browser checks and Playwright sign in: Development keeps emails in memory and never sends them.
    /// Same guards as the seed (Development + Preview:Enabled + bootstrap key); null when no code was sent to that address.
    /// </summary>
    public PreviewEmailCode? LatestEmailCode(string bootstrapKey, string email)
    {
        EnsureEnabled(bootstrapKey);
        using var scope = scopeFactory.CreateScope();
        var inbox = scope.ServiceProvider.GetService<InMemoryTestEmailProvider>();
        var latest = inbox?.EmailCodes.LastOrDefault(message =>
            string.Equals(message.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));
        return latest is null ? null : new PreviewEmailCode(latest.Code, latest.Purpose, latest.ExpiresAtUtc);
    }
}

public static class PreviewEmailCodeEndpoints
{
    public static IEndpointRouteBuilder MapPreviewEmailCodeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/development/preview/email-code", Get).AllowAnonymous();
        return endpoints;
    }

    private static Results<Ok<PreviewEmailCode>, NotFound> Get(string email, HttpContext context, PreviewInitializationService service)
    {
        context.Response.Headers.CacheControl = "no-store";
        var key = context.Request.Headers["X-Preview-Bootstrap-Key"].ToString();
        try
        {
            var code = service.LatestEmailCode(key, email);
            return code is null ? TypedResults.NotFound() : TypedResults.Ok(code);
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthenticatedException();
        }
        catch (InvalidOperationException exception)
        {
            throw new ApiProblemException(StatusCodes.Status409Conflict, exception.Message);
        }
    }
}
