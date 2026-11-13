using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

public static class ContactEndpoints
{
    public static IEndpointRouteBuilder MapContactEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/contact", SubmitAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.Contact);
        var admin = endpoints.MapGroup("/api/v1/admin/contact-messages");
        admin.MapGet("", ListAsync).RequirePermission("System.StoreSettings");
        admin.MapPost("/{id:guid}/status", SetStatusAsync).RequirePermission("System.StoreSettings");
        return endpoints;
    }

    private static async Task<IResult> SubmitAsync(ContactSubmitRequest request, ContactService contact,
        HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            await contact.SubmitAsync(request.FullName, request.Email, request.Phone, request.Subject,
                request.Message, request.Website, cancellationToken);
            return Results.Accepted(value: new { status = "accepted" });
        }
        catch (StorefrontException exception)
        {
            return Results.BadRequest(new { error = exception.Message });
        }
    }

    private static async Task<IResult> ListAsync(HttpContext context, ContactService contact,
        CancellationToken cancellationToken, string? status = null, int page = 1, int pageSize = 50)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await contact.ListAsync(status, page, pageSize, cancellationToken));
    }

    private static async Task<IResult> SetStatusAsync(Guid id, ContactStatusRequest request, ContactService contact,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await contact.SetStatusAsync(id, request.Status, request.RowVersion, cancellationToken));
        }
        catch (StorefrontException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }
}

public sealed record ContactSubmitRequest(string? FullName, string? Email, string? Phone, string? Subject,
    string? Message, string? Website);
public sealed record ContactStatusRequest(string? Status, long RowVersion);
