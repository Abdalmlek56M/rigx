using System.Security.Claims;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;

public static class OfferEndpoints
{
    private const string ContentPermission = "System.StoreSettings";

    public static IEndpointRouteBuilder MapOfferEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/content/offers", GetPublicAsync).AllowAnonymous();
        var admin = endpoints.MapGroup("/api/v1/admin/content/offers");
        admin.MapGet("", ListAsync).RequirePermission(ContentPermission);
        admin.MapPost("", CreateAsync).RequirePermission(ContentPermission);
        admin.MapPut("/{id:guid}", UpdateAsync).RequirePermission(ContentPermission);
        admin.MapPost("/{id:guid}/publish", PublishAsync).RequirePermission(ContentPermission);
        admin.MapPost("/{id:guid}/unpublish", UnpublishAsync).RequirePermission(ContentPermission);
        admin.MapPost("/{id:guid}/cancel", CancelAsync).RequirePermission(ContentPermission);
        return endpoints;
    }

    private static async Task<IResult> GetPublicAsync(
        HttpContext context,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await offers.GetPublicAsync(cancellationToken));
    }

    private static async Task<IResult> ListAsync(
        HttpContext context,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await offers.ListAdminAsync(cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        OfferInput request,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await offers.CreateAsync(request, cancellationToken));
        }
        catch (StorefrontException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        OfferUpdateRequest request,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await offers.UpdateAsync(id, request, cancellationToken));
        }
        catch (StorefrontException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> PublishAsync(
        Guid id,
        ContentVersionRequest request,
        OfferService offers,
        CancellationToken cancellationToken) =>
        await SetPublishedAsync(id, true, request, offers, cancellationToken);

    private static async Task<IResult> UnpublishAsync(
        Guid id,
        ContentVersionRequest request,
        OfferService offers,
        CancellationToken cancellationToken) =>
        await SetPublishedAsync(id, false, request, offers, cancellationToken);

    private static async Task<IResult> SetPublishedAsync(
        Guid id,
        bool published,
        ContentVersionRequest request,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await offers.SetPublishedAsync(id, published, request.RowVersion, cancellationToken));
        }
        catch (StorefrontException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> CancelAsync(
        Guid id,
        ContentVersionRequest request,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await offers.CancelAsync(id, request.RowVersion, cancellationToken));
        }
        catch (StorefrontException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }
}
