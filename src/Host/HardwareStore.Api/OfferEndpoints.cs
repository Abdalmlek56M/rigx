using System.Security.Claims;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

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

    private static async Task<Ok<IReadOnlyList<PublicOfferView>>> GetPublicAsync(
        HttpContext context,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return TypedResults.Ok(await offers.GetPublicAsync(cancellationToken));
    }

    private static async Task<Ok<IReadOnlyList<OfferView>>> ListAsync(
        HttpContext context,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await offers.ListAdminAsync(cancellationToken));
    }

    private static async Task<Ok<OfferView>> CreateAsync(
        OfferInput request,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await offers.CreateAsync(request, cancellationToken));
    }

    private static async Task<Ok<OfferView>> UpdateAsync(
        Guid id,
        OfferUpdateRequest request,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await offers.UpdateAsync(id, request, cancellationToken));
    }

    private static async Task<Ok<OfferView>> PublishAsync(
        Guid id,
        ContentVersionRequest request,
        OfferService offers,
        CancellationToken cancellationToken) =>
        await SetPublishedAsync(id, true, request, offers, cancellationToken);

    private static async Task<Ok<OfferView>> UnpublishAsync(
        Guid id,
        ContentVersionRequest request,
        OfferService offers,
        CancellationToken cancellationToken) =>
        await SetPublishedAsync(id, false, request, offers, cancellationToken);

    private static async Task<Ok<OfferView>> SetPublishedAsync(
        Guid id,
        bool published,
        ContentVersionRequest request,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await offers.SetPublishedAsync(id, published, request.RowVersion, cancellationToken));
    }

    private static async Task<Ok<OfferView>> CancelAsync(
        Guid id,
        ContentVersionRequest request,
        OfferService offers,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await offers.CancelAsync(id, request.RowVersion, cancellationToken));
    }
}
