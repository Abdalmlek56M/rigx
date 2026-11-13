using System.Security.Claims;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/api/v1/content");
        publicGroup.MapGet("/homepage", GetPublishedHomepageAsync).AllowAnonymous();
        publicGroup.MapGet("/articles", ListPublicArticlesAsync).AllowAnonymous();
        publicGroup.MapGet("/articles/{slug}", GetPublicArticleAsync).AllowAnonymous();
        publicGroup.MapGet("/faqs", ListPublicFaqsAsync).AllowAnonymous();
        publicGroup.MapGet("/policies/{kind}", GetPublicPolicyAsync).AllowAnonymous();

        var admin = endpoints.MapGroup("/api/v1/admin/content");
        admin.MapGet("/homepage", GetHomepageDraftAsync).RequirePermission(ContentPermission);
        admin.MapPut("/homepage", SaveHomepageDraftAsync).RequirePermission(ContentPermission);
        admin.MapPost("/homepage/publish", PublishHomepageAsync).RequirePermission(ContentPermission);

        admin.MapGet("/articles", ListAdminArticlesAsync).RequirePermission(ContentPermission);
        admin.MapGet("/articles/{id:guid}", GetAdminArticleAsync).RequirePermission(ContentPermission);
        admin.MapGet("/articles/{id:guid}/revisions", ListArticleRevisionsAsync).RequirePermission(ContentPermission);
        admin.MapPost("/articles", CreateArticleAsync).RequirePermission(ContentPermission);
        admin.MapPut("/articles/{id:guid}", UpdateArticleAsync).RequirePermission(ContentPermission);
        admin.MapPost("/articles/{id:guid}/publish", PublishArticleAsync).RequirePermission(ContentPermission);
        admin.MapPost("/articles/{id:guid}/unpublish", UnpublishArticleAsync).RequirePermission(ContentPermission);

        admin.MapGet("/faqs", ListAdminFaqsAsync).RequirePermission(ContentPermission);
        admin.MapPost("/faqs", CreateFaqAsync).RequirePermission(ContentPermission);
        admin.MapPut("/faqs/{id:guid}", UpdateFaqAsync).RequirePermission(ContentPermission);
        admin.MapPost("/faqs/{id:guid}/publish", PublishFaqAsync).RequirePermission(ContentPermission);
        admin.MapPost("/faqs/{id:guid}/unpublish", UnpublishFaqAsync).RequirePermission(ContentPermission);

        admin.MapGet("/policies", ListAdminPoliciesAsync).RequirePermission(ContentPermission);
        admin.MapPost("/policies/{kind}/versions", CreatePolicyVersionAsync).RequirePermission(ContentPermission);
        admin.MapPut("/policies/{id:guid}", UpdatePolicyAsync).RequirePermission(ContentPermission);
        admin.MapPost("/policies/{id:guid}/review", ReviewPolicyAsync).RequirePermission(ContentPermission);
        admin.MapPost("/policies/{id:guid}/publish", PublishPolicyAsync).RequirePermission(ContentPermission);
        return endpoints;
    }

    private const string ContentPermission = "System.StoreSettings";

    private static async Task<IResult> GetPublishedHomepageAsync(
        HttpContext context,
        HomepageContentService service,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await service.GetPublishedAsync(cancellationToken));
    }

    private static async Task<IResult> GetHomepageDraftAsync(
        HttpContext context,
        HomepageContentService service,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await service.GetDraftAsync(cancellationToken));
    }

    private static async Task<IResult> SaveHomepageDraftAsync(
        HomepageLayoutUpdateRequest request,
        HttpContext context,
        HomepageContentService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        return await GuardAsync(async () =>
            Results.Ok(await service.SaveDraftAsync(request, actorId, cancellationToken)));
    }

    private static async Task<IResult> PublishHomepageAsync(
        HomepagePublishRequest request,
        HttpContext context,
        HomepageContentService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        return await GuardAsync(async () =>
            Results.Ok(await service.PublishAsync(request.RowVersion, actorId, cancellationToken)));
    }

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
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return await GuardAsync(async () => Results.Ok(await service.GetPublicAsync(slug, cancellationToken)));
    }

    private static async Task<IResult> ListAdminArticlesAsync(
        ContentArticleService service,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 25,
        string? status = null,
        string? search = null)
    {
        return Results.Ok(await service.ListAdminAsync(page, pageSize, status, search, cancellationToken));
    }

    private static async Task<IResult> GetAdminArticleAsync(
        Guid id,
        ContentArticleService service,
        CancellationToken cancellationToken)
    {
        return await GuardAsync(async () => Results.Ok(await service.GetAdminAsync(id, cancellationToken)));
    }

    private static async Task<IResult> ListArticleRevisionsAsync(
        Guid id,
        ContentArticleService service,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await service.ListRevisionsAsync(id, cancellationToken));
    }

    private static async Task<IResult> CreateArticleAsync(
        ArticleInput request,
        HttpContext context,
        ContentArticleService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        return await GuardAsync(async () =>
            Results.Ok(await service.CreateAsync(request, actorId, cancellationToken)));
    }

    private static async Task<IResult> UpdateArticleAsync(
        Guid id,
        ArticleInput request,
        HttpContext context,
        ContentArticleService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        return await GuardAsync(async () =>
            Results.Ok(await service.UpdateAsync(id, request, actorId, cancellationToken)));
    }

    private static async Task<IResult> PublishArticleAsync(
        Guid id,
        ContentVersionRequest request,
        HttpContext context,
        ContentArticleService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        return await GuardAsync(async () =>
            Results.Ok(await service.PublishAsync(id, request.RowVersion, actorId, cancellationToken)));
    }

    private static async Task<IResult> UnpublishArticleAsync(
        Guid id,
        ContentVersionRequest request,
        HttpContext context,
        ContentArticleService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        return await GuardAsync(async () =>
            Results.Ok(await service.UnpublishAsync(id, request.RowVersion, actorId, cancellationToken)));
    }

    private static async Task<IResult> ListPublicFaqsAsync(
        HttpContext context,
        ContentFaqService service,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await service.ListPublicAsync(cancellationToken));
    }

    private static async Task<IResult> ListAdminFaqsAsync(
        ContentFaqService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.ListAdminAsync(cancellationToken));

    private static async Task<IResult> CreateFaqAsync(
        FaqInput request,
        ContentFaqService service,
        CancellationToken cancellationToken)
    {
        return await GuardAsync(async () =>
            Results.Ok(await service.CreateAsync(request, cancellationToken)));
    }

    private static async Task<IResult> UpdateFaqAsync(
        Guid id,
        FaqInput request,
        ContentFaqService service,
        CancellationToken cancellationToken,
        long rowVersion = 0)
    {
        return await GuardAsync(async () =>
            Results.Ok(await service.UpdateAsync(id, request, rowVersion, cancellationToken)));
    }

    private static async Task<IResult> PublishFaqAsync(
        Guid id,
        ContentVersionRequest request,
        ContentFaqService service,
        CancellationToken cancellationToken)
    {
        return await GuardAsync(async () =>
            Results.Ok(await service.SetPublishedAsync(id, true, request.RowVersion, cancellationToken)));
    }

    private static async Task<IResult> UnpublishFaqAsync(
        Guid id,
        ContentVersionRequest request,
        ContentFaqService service,
        CancellationToken cancellationToken)
    {
        return await GuardAsync(async () =>
            Results.Ok(await service.SetPublishedAsync(id, false, request.RowVersion, cancellationToken)));
    }

    private static async Task<IResult> GetPublicPolicyAsync(
        string kind,
        HttpContext context,
        ContentPolicyService service,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return await GuardAsync(async () => Results.Ok(await service.GetPublicAsync(kind, cancellationToken)));
    }

    private static async Task<IResult> ListAdminPoliciesAsync(
        ContentPolicyService service,
        CancellationToken cancellationToken,
        string? kind = null) =>
        Results.Ok(await service.ListAdminAsync(kind, cancellationToken));

    private static async Task<IResult> CreatePolicyVersionAsync(
        string kind,
        ContentPolicyService service,
        CancellationToken cancellationToken)
    {
        return await GuardAsync(async () =>
            Results.Ok(await service.CreateVersionAsync(kind, cancellationToken)));
    }

    private static async Task<IResult> UpdatePolicyAsync(
        Guid id,
        PolicyInput request,
        ContentPolicyService service,
        CancellationToken cancellationToken)
    {
        return await GuardAsync(async () =>
            Results.Ok(await service.UpdateAsync(id, request, cancellationToken)));
    }

    private static async Task<IResult> ReviewPolicyAsync(
        Guid id,
        ContentVersionRequest request,
        ContentPolicyService service,
        CancellationToken cancellationToken)
    {
        return await GuardAsync(async () =>
            Results.Ok(await service.MarkReviewAsync(id, request.RowVersion, cancellationToken)));
    }

    private static async Task<IResult> PublishPolicyAsync(
        Guid id,
        ContentVersionRequest request,
        ContentPolicyService service,
        CancellationToken cancellationToken)
    {
        return await GuardAsync(async () =>
            Results.Ok(await service.PublishAsync(id, request.RowVersion, cancellationToken)));
    }

    private static async Task<IResult> GuardAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (StorefrontException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static bool TryGetActor(HttpContext context, out Guid actorId) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId) &&
        actorId != Guid.Empty;
}

public sealed record HomepagePublishRequest(long RowVersion);
public sealed record ContentVersionRequest(long RowVersion);
