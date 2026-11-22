using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.PcBuilder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Customers' builds: the approved public gallery, hearts and likes on a shared build (signed-in only, idempotent, rate-limited) and the
/// admin's approve / reject queue (same permission as moderating reviews; every decision is audited with who made it).
/// </summary>
public static class CommunityBuildEndpoints
{
    private const string Moderate = "Catalog.ManageProducts";

    public static IEndpointRouteBuilder MapCommunityBuildEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/builder/community", GalleryAsync).AllowAnonymous();

        var reactions = endpoints.MapGroup("/api/v1/builder/saved/{token}/reactions/{kind}").RequireAuthorization()
            .RequireRateLimiting(IdentityRateLimitPolicies.BuilderReaction);
        reactions.MapPut("", AddReactionAsync);
        reactions.MapDelete("", RemoveReactionAsync);

        var admin = endpoints.MapGroup("/api/v1/admin/customer-builds");
        admin.MapGet("", ModerationAsync).RequirePermission(Moderate);
        admin.MapPost("/{token}/decision", DecideAsync).RequirePermission(Moderate);
        return endpoints;
    }

    private static async Task<Ok<PagedResult<CommunityBuildCard>>> GalleryAsync(HttpContext context, CommunityBuildService community, CancellationToken ct,
        int page = 1, int pageSize = 12)
    {
        context.Response.Headers.CacheControl = "no-store";
        Guid? viewerId = context.TryGetUserId(out var id) ? id : null;
        return TypedResults.Ok(await community.GalleryAsync(page, pageSize, viewerId, ct));
    }

    private static async Task<Ok<BuildReactionSummary>> AddReactionAsync(string token, BuildReactionKind kind, HttpContext context, BuildReactionStore reactions,
        CancellationToken ct) =>
        TypedResults.Ok(await reactions.AddAsync(token, context.RequireUserId(), kind, ct));

    private static async Task<Ok<BuildReactionSummary>> RemoveReactionAsync(string token, BuildReactionKind kind, HttpContext context, BuildReactionStore reactions,
        CancellationToken ct) =>
        TypedResults.Ok(await reactions.RemoveAsync(token, context.RequireUserId(), kind, ct));

    private static async Task<Ok<PagedResult<CustomerBuildModerationView>>> ModerationAsync(CommunityBuildService community, CancellationToken ct,
        BuildModerationStatus status = BuildModerationStatus.Pending, int page = 1, int pageSize = 25) =>
        TypedResults.Ok(await community.ModerationAsync(status, page, pageSize, ct));

    private static async Task<Ok<CustomerBuildModerationView>> DecideAsync(string token, BuildDecisionRequest request, HttpContext context, CommunityBuildService community,
        CancellationToken ct) =>
        TypedResults.Ok(await community.DecideAsync(token, request.Decision, context.RequireUserId(), ct));
}
