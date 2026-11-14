using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/api/v1/content");
        publicGroup.MapGet("/articles", ListPublicArticlesAsync).AllowAnonymous();
        publicGroup.MapGet("/articles/{slug}", GetPublicArticleAsync).AllowAnonymous();
        publicGroup.MapPost("/articles/{slug}/view", RecordArticleViewAsync).AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.ArticleView);
        publicGroup.MapGet("/faqs", ListPublicFaqsAsync).AllowAnonymous();
        publicGroup.MapGet("/faqs/item/{code}", ListItemFaqsAsync).AllowAnonymous();
        publicGroup.MapGet("/policies/{kind}", GetPublicPolicyAsync).AllowAnonymous();

        var admin = endpoints.MapGroup("/api/v1/admin/content");
        admin.MapGet("/articles", ListAdminArticlesAsync).RequirePermission(ContentPermission);
        admin.MapGet("/articles/{id:guid}", GetAdminArticleAsync).RequirePermission(ContentPermission);
        admin.MapGet("/articles/{id:guid}/revisions", ListArticleRevisionsAsync).RequirePermission(ContentPermission);
        admin.MapPost("/articles", CreateArticleAsync).RequirePermission(ContentPermission);
        admin.MapPut("/articles/{id:guid}", UpdateArticleAsync).RequirePermission(ContentPermission);
        admin.MapPost("/articles/{id:guid}/publish", PublishArticleAsync).RequirePermission(ContentPermission);
        admin.MapPost("/articles/{id:guid}/unpublish", UnpublishArticleAsync).RequirePermission(ContentPermission);
        admin.MapDelete("/articles/{id:guid}", DeleteArticleAsync).RequirePermission(ContentPermission);

        admin.MapGet("/faqs", ListAdminFaqsAsync).RequirePermission(ContentPermission);
        admin.MapPost("/faqs", CreateFaqAsync).RequirePermission(ContentPermission);
        admin.MapPut("/faqs/{id:guid}", UpdateFaqAsync).RequirePermission(ContentPermission);
        admin.MapPost("/faqs/{id:guid}/publish", PublishFaqAsync).RequirePermission(ContentPermission);
        admin.MapPost("/faqs/{id:guid}/unpublish", UnpublishFaqAsync).RequirePermission(ContentPermission);
        admin.MapDelete("/faqs/{id:guid}", DeleteFaqAsync).RequirePermission(ContentPermission);

        admin.MapGet("/policies", ListAdminPoliciesAsync).RequirePermission(ContentPermission);
        admin.MapPost("/policies/{kind}/versions", CreatePolicyVersionAsync).RequirePermission(ContentPermission);
        admin.MapPut("/policies/{id:guid}", UpdatePolicyAsync).RequirePermission(ContentPermission);
        admin.MapPost("/policies/{id:guid}/review", ReviewPolicyAsync).RequirePermission(ContentPermission);
        admin.MapPost("/policies/{id:guid}/publish", PublishPolicyAsync).RequirePermission(ContentPermission);
        return endpoints;
    }

    private const string ContentPermission = "System.StoreSettings";

    private static async Task<IResult> ListPublicArticlesAsync(
        HttpContext context,
        ContentArticleService service,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 12,
        string? category = null)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await service.ListPublicAsync(page, pageSize, category, cancellationToken));
    }

    private static async Task<IResult> GetPublicArticleAsync(
        string slug,
        HttpContext context,
        ContentArticleService service,
        StoreItemService items,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await WithProductsAsync(await service.GetPublicAsync(slug, cancellationToken), items, cancellationToken));
    }

    private static async Task<IResult> RecordArticleViewAsync(
        string slug,
        ContentArticleService service,
        CancellationToken cancellationToken)
    {
        await service.RecordViewAsync(slug, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<ArticleDetailView> WithProductsAsync(
        ArticleView article,
        StoreItemService items,
        CancellationToken cancellationToken)
    {
        var codes = ArticleBlockRules.ItemCodes(article.BlocksAr.Concat(article.BlocksEn)).Distinct().ToList();
        return new ArticleDetailView(article, await items.GetCardsAsync(codes, cancellationToken));
    }

    private static async Task<IResult> ListAdminArticlesAsync(
        ContentArticleService service,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 25,
        string? status = null,
        string? search = null) =>
        Results.Ok(await service.ListAdminAsync(page, pageSize, status, search, cancellationToken));

    private static async Task<IResult> GetAdminArticleAsync(
        Guid id,
        ContentArticleService service,
        StoreItemService items,
        CancellationToken cancellationToken) =>
        Results.Ok(await WithProductsAsync(await service.GetAdminAsync(id, cancellationToken), items, cancellationToken));

    private static async Task<IResult> ListArticleRevisionsAsync(
        Guid id,
        ContentArticleService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.ListRevisionsAsync(id, cancellationToken));

    private static async Task<IResult> CreateArticleAsync(
        ArticleInput request,
        HttpContext context,
        ContentArticleService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        return Results.Ok(await service.CreateAsync(request, actorId, cancellationToken));
    }

    private static async Task<IResult> UpdateArticleAsync(
        Guid id,
        ArticleInput request,
        HttpContext context,
        ContentArticleService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        return Results.Ok(await service.UpdateAsync(id, request, actorId, cancellationToken));
    }

    private static async Task<IResult> PublishArticleAsync(
        Guid id,
        ArticlePublishRequest request,
        HttpContext context,
        ContentArticleService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        return Results.Ok(await service.PublishAsync(id, request, actorId, cancellationToken));
    }

    private static async Task<IResult> UnpublishArticleAsync(
        Guid id,
        ContentVersionRequest request,
        HttpContext context,
        ContentArticleService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        return Results.Ok(await service.UnpublishAsync(id, request.RowVersion, actorId, cancellationToken));
    }

    private static async Task<IResult> DeleteArticleAsync(
        Guid id,
        HttpContext context,
        ContentArticleService service,
        TrashAuditor audit,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        await service.DeleteAsync(id, actorId, cancellationToken);
        await audit.DeletedAsync(ArticleTrashSource.TrashKind, id, actorId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteFaqAsync(
        Guid id,
        HttpContext context,
        ContentFaqService service,
        TrashAuditor audit,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        await service.DeleteAsync(id, actorId, cancellationToken);
        await audit.DeletedAsync(FaqTrashSource.TrashKind, id, actorId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListPublicFaqsAsync(
        HttpContext context,
        ContentFaqService service,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await service.ListPublicAsync(cancellationToken));
    }

    private static async Task<IResult> ListItemFaqsAsync(
        string code,
        HttpContext context,
        ContentFaqService service,
        StoreItemService items,
        TaxonomyService taxonomy,
        CancellationToken cancellationToken)
    {
        var item = await items.GetAsync(code, cancellationToken);
        var categoryPath = item.CategoryId is { } categoryId
            ? await taxonomy.GetCategoryPathIdsAsync(categoryId, cancellationToken)
            : [];
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await service.ListForItemAsync(item.Code, categoryPath, cancellationToken));
    }

    private static async Task<IResult> ListAdminFaqsAsync(
        ContentFaqService service,
        CancellationToken cancellationToken,
        string? scope = null,
        Guid? categoryId = null,
        string? itemCode = null) =>
        Results.Ok(await service.ListAdminAsync(scope, categoryId, itemCode, cancellationToken));

    private static async Task<IResult> CreateFaqAsync(
        FaqInput request,
        ContentFaqService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.CreateAsync(request, cancellationToken));

    private static async Task<IResult> UpdateFaqAsync(
        Guid id,
        FaqInput request,
        ContentFaqService service,
        CancellationToken cancellationToken,
        long rowVersion = 0) =>
        Results.Ok(await service.UpdateAsync(id, request, rowVersion, cancellationToken));

    private static async Task<IResult> PublishFaqAsync(
        Guid id,
        ContentVersionRequest request,
        ContentFaqService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.SetPublishedAsync(id, true, request.RowVersion, cancellationToken));

    private static async Task<IResult> UnpublishFaqAsync(
        Guid id,
        ContentVersionRequest request,
        ContentFaqService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.SetPublishedAsync(id, false, request.RowVersion, cancellationToken));

    private static async Task<IResult> GetPublicPolicyAsync(
        string kind,
        HttpContext context,
        ContentPolicyService service,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await service.GetPublicAsync(kind, cancellationToken));
    }

    private static async Task<IResult> ListAdminPoliciesAsync(
        ContentPolicyService service,
        CancellationToken cancellationToken,
        string? kind = null) =>
        Results.Ok(await service.ListAdminAsync(kind, cancellationToken));

    private static async Task<IResult> CreatePolicyVersionAsync(
        string kind,
        ContentPolicyService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.CreateVersionAsync(kind, cancellationToken));

    private static async Task<IResult> UpdatePolicyAsync(
        Guid id,
        PolicyInput request,
        ContentPolicyService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.UpdateAsync(id, request, cancellationToken));

    private static async Task<IResult> ReviewPolicyAsync(
        Guid id,
        ContentVersionRequest request,
        ContentPolicyService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.MarkReviewAsync(id, request.RowVersion, cancellationToken));

    private static async Task<IResult> PublishPolicyAsync(
        Guid id,
        ContentVersionRequest request,
        ContentPolicyService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.PublishAsync(id, request.RowVersion, cancellationToken));
}

public sealed record ContentVersionRequest(long RowVersion);

/// <summary>An article with the live cards of the store items its product blocks show (hidden or unknown codes are missing).</summary>
public sealed record ArticleDetailView(ArticleView Article, IReadOnlyList<StoreItemCard> Products);
