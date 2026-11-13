using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Http;
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

    private static async Task<IResult> GetProductReviewsAsync(
        Guid productId,
        HttpContext context,
        ReviewService reviews,
        IDbContextFactory<IdentityDbContext> identityFactory,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        try
        {
            var response = await reviews.GetProductReviewsAsync(productId, cancellationToken);
            var authors = await LoadAuthorsAsync(identityFactory, response.Items.Select(item => item.UserAccountId), cancellationToken);
            return Results.Ok(new
            {
                response.Aggregate,
                Items = response.Items.Select(item =>
                {
                    authors.TryGetValue(item.UserAccountId, out var author);
                    return new
                    {
                        item.Id,
                        item.ProductId,
                        item.Rating,
                        item.Title,
                        item.Body,
                        AuthorName = author?.Name ?? "RIGX customer",
                        AuthorAvatarStorageKey = item.ShowAvatar ? author?.AvatarStorageKey : null,
                        item.CreatedAtUtc
                    };
                }).ToList()
            });
        }
        catch (ReviewException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> GetStoreReviewsAsync(
        HttpContext context,
        ReviewService reviews,
        IDbContextFactory<IdentityDbContext> identityFactory,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        var response = await reviews.GetStoreReviewsAsync(cancellationToken);
        var authors = await LoadAuthorsAsync(identityFactory, response.Items.Select(item => item.UserAccountId), cancellationToken);
        return Results.Ok(new
        {
            response.Aggregate,
            Items = response.Items.Select(item =>
            {
                authors.TryGetValue(item.UserAccountId, out var author);
                return new
                {
                    item.Id,
                    item.Rating,
                    item.Body,
                    AuthorName = author?.Name ?? "RIGX customer",
                    AuthorAvatarStorageKey = item.ShowAvatar ? author?.AvatarStorageKey : null,
                    item.CreatedAtUtc
                };
            }).ToList()
        });
    }

    private static async Task<IResult> GetMineAsync(
        HttpContext context,
        ReviewService reviews,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await reviews.GetMineAsync(accountId, cancellationToken));
    }

    private static async Task<IResult> UpsertProductAsync(
        Guid productId,
        ReviewInput request,
        HttpContext context,
        ReviewService reviews,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await reviews.UpsertProductReviewAsync(accountId, productId, request, cancellationToken));
        }
        catch (ReviewException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> DeleteProductAsync(
        Guid productId,
        HttpContext context,
        ReviewService reviews,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await reviews.DeleteProductReviewAsync(accountId, productId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> UpsertStoreAsync(
        ReviewInput request,
        HttpContext context,
        ReviewService reviews,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await reviews.UpsertStoreReviewAsync(accountId, request, cancellationToken));
        }
        catch (ReviewException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> DeleteStoreAsync(
        HttpContext context,
        ReviewService reviews,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var accountId))
            return Results.Unauthorized();
        await reviews.DeleteStoreReviewAsync(accountId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListAdminAsync(
        ReviewService reviews,
        CancellationToken cancellationToken,
        string? type = null,
        string? status = null,
        int page = 1,
        int pageSize = 25)
    {
        if (status is not null && status is not ("Pending" or "Published" or "Rejected" or "Hidden"))
            return Results.Problem(statusCode: 400, title: "Unsupported review status filter.");
        if (type is not null && type is not ("Product" or "Store"))
            return Results.Problem(statusCode: 400, title: "Unsupported review type filter.");
        return Results.Ok(await reviews.ListAdminAsync(type, status, page, pageSize, cancellationToken));
    }

    private static async Task<IResult> ModerateAsync(
        string type,
        Guid id,
        ModerationRequest request,
        HttpContext context,
        ReviewService reviews,
        NotificationService notifications,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        try
        {
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
            return Results.Ok(result);
        }
        catch (ReviewException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
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
