using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;

public static class StorefrontEndpoints
{
    public static IEndpointRouteBuilder MapStorefrontEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/storefront/settings", GetPublicSettingsAsync)
            .AllowAnonymous();
        endpoints.MapGet("/api/v1/admin/storefront/settings", GetAdminSettingsAsync)
            .RequirePermission("System.StoreSettings");
        endpoints.MapPut("/api/v1/admin/storefront/settings", UpdateSettingsAsync)
            .RequirePermission("System.StoreSettings");

        var wishlist = endpoints.MapGroup("/api/v1/wishlist").RequireAuthorization();
        wishlist.MapGet("", ListWishlistAsync);
        wishlist.MapPost("/{productId:guid}", AddWishlistAsync);
        wishlist.MapDelete("/{productId:guid}", RemoveWishlistAsync);
        wishlist.MapDelete("", ClearWishlistAsync);
        wishlist.MapPost("/merge", MergeWishlistAsync);
        return endpoints;
    }

    private static async Task<IResult> ListWishlistAsync(
        HttpContext context,
        WishlistService wishlist,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await wishlist.ListAsync(accountId, cancellationToken));
    }

    private static async Task<IResult> AddWishlistAsync(
        Guid productId,
        HttpContext context,
        WishlistService wishlist,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        try
        {
            await wishlist.AddAsync(accountId, productId, cancellationToken);
            return Results.NoContent();
        }
        catch (StorefrontException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> RemoveWishlistAsync(
        Guid productId,
        HttpContext context,
        WishlistService wishlist,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await wishlist.RemoveAsync(accountId, productId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ClearWishlistAsync(
        HttpContext context,
        WishlistService wishlist,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await wishlist.ClearAsync(accountId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> MergeWishlistAsync(
        WishlistMergeRequest request,
        HttpContext context,
        WishlistService wishlist,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await wishlist.MergeAsync(
            accountId,
            request.ProductIds ?? [],
            cancellationToken);
        return Results.Ok(await wishlist.ListAsync(accountId, cancellationToken));
    }

    private static async Task<IResult> GetPublicSettingsAsync(
        HttpContext context,
        StoreSettingsService settings,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await settings.GetAsync(cancellationToken));
    }

    private static async Task<IResult> GetAdminSettingsAsync(
        HttpContext context,
        StoreSettingsService settings,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await settings.GetAsync(cancellationToken));
    }

    private static async Task<IResult> UpdateSettingsAsync(
        StoreSettingsUpdateRequest request,
        HttpContext context,
        StoreSettingsService settings,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await settings.UpdateAsync(request, actorId, cancellationToken));
        }
        catch (StorefrontException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }
}

public sealed record WishlistMergeRequest(IReadOnlyList<Guid>? ProductIds);
