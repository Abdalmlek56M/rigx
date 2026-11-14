using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/catalog/products/{productId:guid}/reviews", GetProductReviewsAsync)
            .AllowAnonymous();
        endpoints.MapGet("/api/v1/content/store-reviews", GetStoreReviewsAsync).AllowAnonymous();

        var mine = endpoints.MapGroup("/api/v1/reviews").RequireAuthorization();
        mine.MapGet("/mine", GetMineAsync);
        mine.MapPut("/products/{productId:guid}", UpsertProductAsync);
        mine.MapDelete("/products/{productId:guid}", DeleteProductAsync);
        mine.MapPut("/store", UpsertStoreAsync);
        mine.MapDelete("/store", DeleteStoreAsync);

        var admin = endpoints.MapGroup("/api/v1/admin/reviews");
        admin.MapGet("", ListAdminAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/{type}/{id:guid}/moderate", ModerateAsync)
            .RequirePermission("Catalog.ManageProducts");
        return endpoints;
    }

    private static async Task<Ok<PublicProductReviews>> GetProductReviewsAsync(
        Guid productId,
        HttpContext context,
        ReviewService reviews,
        IDbContextFactory<IdentityDbContext> identityFactory,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        var response = await reviews.GetProductReviewsAsync(productId, cancellationToken);
        var authors = await LoadAuthorsAsync(identityFactory, response.Items.Select(item => item.UserAccountId), cancellationToken);
        return TypedResults.Ok(new PublicProductReviews(response.Aggregate, response.Items.Select(item =>
        {
            authors.TryGetValue(item.UserAccountId, out var author);
            return new PublicProductReview(item.Id, item.ProductId, item.Rating, item.Title, item.Body,
                author?.Name ?? "RIGX customer", item.ShowAvatar ? author?.AvatarStorageKey : null, item.CreatedAtUtc);
        }).ToList()));
    }

    private static async Task<Ok<PublicStoreReviews>> GetStoreReviewsAsync(
        HttpContext context,
        ReviewService reviews,
        IDbContextFactory<IdentityDbContext> identityFactory,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        var response = await reviews.GetStoreReviewsAsync(cancellationToken);
        var authors = await LoadAuthorsAsync(identityFactory, response.Items.Select(item => item.UserAccountId), cancellationToken);
        return TypedResults.Ok(new PublicStoreReviews(response.Aggregate, response.Items.Select(item =>
        {
            authors.TryGetValue(item.UserAccountId, out var author);
            return new PublicStoreReview(item.Id, item.Rating, item.Body, author?.Name ?? "RIGX customer",
                item.ShowAvatar ? author?.AvatarStorageKey : null, item.CreatedAtUtc);
        }).ToList()));
    }

    private static async Task<Ok<MyReviewsResponse>> GetMineAsync(
        HttpContext context,
        ReviewService reviews,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await reviews.GetMineAsync(accountId, cancellationToken));
    }

    private static async Task<Ok<ProductReviewView>> UpsertProductAsync(
        Guid productId,
        ReviewInput request,
        HttpContext context,
        ReviewService reviews,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        return TypedResults.Ok(await reviews.UpsertProductReviewAsync(accountId, productId, request, cancellationToken));
    }

    private static async Task<NoContent> DeleteProductAsync(
        Guid productId,
        HttpContext context,
        ReviewService reviews,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        await reviews.DeleteProductReviewAsync(accountId, productId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<StoreReviewView>> UpsertStoreAsync(
        ReviewInput request,
        HttpContext context,
        ReviewService reviews,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        return TypedResults.Ok(await reviews.UpsertStoreReviewAsync(accountId, request, cancellationToken));
    }

    private static async Task<NoContent> DeleteStoreAsync(
        HttpContext context,
        ReviewService reviews,
        CancellationToken cancellationToken)
    {
        var accountId = context.RequireUserId();
        await reviews.DeleteStoreReviewAsync(accountId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedResult<AdminReviewView>>> ListAdminAsync(
        ReviewService reviews,
        CancellationToken cancellationToken,
        string? type = null,
        string? status = null,
        int page = 1,
        int pageSize = 25)
    {
        if (status is not null && status is not ("Pending" or "Published" or "Rejected" or "Hidden"))
            throw new ApiProblemException(StatusCodes.Status400BadRequest, "Unsupported review status filter.");
        if (type is not null && type is not ("Product" or "Store"))
            throw new ApiProblemException(StatusCodes.Status400BadRequest, "Unsupported review type filter.");
        return TypedResults.Ok(await reviews.ListAdminAsync(type, status, page, pageSize, cancellationToken));
    }

    private static async Task<Ok<AdminReviewView>> ModerateAsync(
        string type,
        Guid id,
        ModerationRequest request,
        HttpContext context,
        ReviewService reviews,
        NotificationService notifications,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        var result = await reviews.ModerateAsync(type, id, request, actorId, cancellationToken);
        var titleKey = result.Status switch
        {
            ReviewStatus.Published => "notifications.reviewPublished",
            ReviewStatus.Rejected => "notifications.reviewRejected",
            ReviewStatus.Hidden => "notifications.reviewHidden",
            _ => "notifications.reviewUpdated"
        };
        await notifications.PublishAsync(
            result.UserAccountId,
            "ReviewModeration",
            titleKey,
            "notifications.reviewBody",
            "/account/reviews",
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Dictionary<Guid, AuthorProjection>> LoadAuthorsAsync(
        IDbContextFactory<IdentityDbContext> identityFactory,
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToArray();
        if (ids.Length == 0)
            return [];
        await using var db = await identityFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking()
            .Where(account => ids.Contains(account.Id) && account.IsActive)
            .Select(account => new AuthorProjection(
                account.Id,
                account.Username,
                account.AvatarStorageKey))
            .ToDictionaryAsync(author => author.Id, cancellationToken);
    }

    private sealed record AuthorProjection(Guid Id, string Name, string? AvatarStorageKey);
}

public sealed record PublicProductReview(Guid Id, Guid ProductId, int Rating, string? Title, string Body, string AuthorName,
    string? AuthorAvatarStorageKey, DateTimeOffset CreatedAtUtc);
public sealed record PublicProductReviews(ReviewAggregate Aggregate, IReadOnlyList<PublicProductReview> Items);
public sealed record PublicStoreReview(Guid Id, int Rating, string Body, string AuthorName, string? AuthorAvatarStorageKey,
    DateTimeOffset CreatedAtUtc);
public sealed record PublicStoreReviews(ReviewAggregate Aggregate, IReadOnlyList<PublicStoreReview> Items);
