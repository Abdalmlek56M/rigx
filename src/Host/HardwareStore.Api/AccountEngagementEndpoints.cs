using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>A signed-in customer's favourites (hearts) and notifications bell.</summary>
public static class AccountEngagementEndpoints
{
    public static IEndpointRouteBuilder MapAccountEngagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var favourites = endpoints.MapGroup("/api/v1/account/favourites").RequireAuthorization();
        favourites.MapGet("", ListFavouritesAsync);
        favourites.MapPut("/{code}", AddFavouriteAsync);
        favourites.MapDelete("/{code}", RemoveFavouriteAsync);
        favourites.MapPost("/merge", MergeFavouritesAsync);

        var notifications = endpoints.MapGroup("/api/v1/account/notifications").RequireAuthorization();
        notifications.MapGet("", ListNotificationsAsync);
        notifications.MapPost("/{id:guid}/read", MarkReadAsync);
        notifications.MapPost("/read-all", MarkAllReadAsync);
        return endpoints;
    }

    private static async Task<Ok<FavouriteList>> ListFavouritesAsync(HttpContext context, FavouriteService favourites, StoreItemService items, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await ReadAsync(context.RequireUserId(), favourites, items, ct));
    }

    private static async Task<NoContent> AddFavouriteAsync(string code, HttpContext context, FavouriteService favourites, CancellationToken ct)
    {
        await favourites.AddAsync(context.RequireUserId(), code, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> RemoveFavouriteAsync(string code, HttpContext context, FavouriteService favourites, CancellationToken ct)
    {
        await favourites.RemoveAsync(context.RequireUserId(), code, ct);
        return TypedResults.NoContent();
    }

    /// <summary>Adds the guest's local hearts after sign-in and returns the whole list.</summary>
    private static async Task<Ok<FavouriteList>> MergeFavouritesAsync(FavouriteMergeRequest request, HttpContext context, FavouriteService favourites,
        StoreItemService items, CancellationToken ct)
    {
        var accountId = context.RequireUserId();
        await favourites.MergeAsync(accountId, request.Codes ?? [], ct);
        return TypedResults.Ok(await ReadAsync(accountId, favourites, items, ct));
    }

    /// <summary>All saved codes (they drive the hearts) and the cards of the ones still published, newest first.</summary>
    private static async Task<FavouriteList> ReadAsync(Guid accountId, FavouriteService favourites, StoreItemService items, CancellationToken ct)
    {
        var codes = await favourites.ListCodesAsync(accountId, ct);
        var cards = (await items.GetCardsAsync(codes, ct)).ToDictionary(card => card.Code);
        return new FavouriteList(codes, [.. codes.Where(cards.ContainsKey).Select(code => cards[code])]);
    }

    private static async Task<Ok<CustomerNotificationFeed>> ListNotificationsAsync(HttpContext context, CustomerNotificationService notifications, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await notifications.ListAsync(context.RequireUserId(), ct));
    }

    private static async Task<NoContent> MarkReadAsync(Guid id, HttpContext context, CustomerNotificationService notifications, CancellationToken ct)
    {
        await notifications.MarkReadAsync(context.RequireUserId(), id, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> MarkAllReadAsync(HttpContext context, CustomerNotificationService notifications, CancellationToken ct)
    {
        await notifications.MarkAllReadAsync(context.RequireUserId(), ct);
        return TypedResults.NoContent();
    }
}

public sealed record FavouriteList(IReadOnlyList<string> Codes, IReadOnlyList<StoreItemCard> Items);

public sealed record FavouriteMergeRequest(IReadOnlyList<string?>? Codes);
