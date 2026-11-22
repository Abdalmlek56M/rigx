using HardwareStore.Modules.PcBuilder;
using HardwareStore.Common.Domain.Paging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// PC builder: slots, options with compatibility, check, "edit this build", quote and saved builds. Business errors are
/// <c>AppException</c>s mapped once by <c>ProblemDetailsExceptionHandler</c>, so handlers do not catch them.
/// </summary>
public static class BuilderEndpoints
{
    public static IEndpointRouteBuilder MapBuilderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var builder = endpoints.MapGroup("/api/v1/builder").AllowAnonymous();
        builder.MapGet("/slots", SlotsAsync);
        builder.MapPost("/options", OptionsAsync);
        builder.MapPost("/check", CheckAsync);
        builder.MapGet("/from-build/{code}", FromBuildAsync);
        builder.MapPost("/quote", QuoteAsync);
        builder.MapPost("/saved", SaveAsync).RequireRateLimiting(IdentityRateLimitPolicies.BuilderSave);
        builder.MapGet("/saved/{token}", GetSavedAsync);
        builder.MapPost("/saved/{token}/views", RecordViewAsync).RequireRateLimiting(IdentityRateLimitPolicies.BuilderView);
        endpoints.MapPut("/api/v1/builder/saved/{token}", UpdateSavedAsync).RequireAuthorization();
        endpoints.MapPut("/api/v1/builder/saved/{token}/visibility", SetVisibilityAsync).RequireAuthorization();
        endpoints.MapPost("/api/v1/builder/saved/{token}/copy", CopyToAccountAsync).RequireAuthorization()
            .RequireRateLimiting(IdentityRateLimitPolicies.BuilderSave);
        endpoints.MapDelete("/api/v1/builder/saved/{token}", DeleteSavedAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/account/builds", ListMineAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<Ok<IReadOnlyList<BuilderSlotView>>> SlotsAsync(HttpContext context, BuilderService builder, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return TypedResults.Ok(await builder.SlotsAsync(ct));
    }

    private static async Task<Ok<PagedResult<BuilderOptionView>>> OptionsAsync(BuilderOptionsRequest request, HttpContext context, BuilderService builder, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await builder.OptionsAsync(request.Slot, request.Parts, request.Condition, request.Q, request.Page, ct));
    }

    private static async Task<Ok<BuilderEvaluation>> CheckAsync(BuilderCheckRequest request, HttpContext context, BuilderService builder, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await builder.CheckAsync(request.Parts, ct));
    }

    private static async Task<Ok<BuilderFromBuildView>> FromBuildAsync(string code, HttpContext context, BuilderService builder, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await builder.FromBuildAsync(code, ct));
    }

    private static async Task<Ok<QuoteDocument>> QuoteAsync(BuilderQuoteRequest request, HttpContext context, BuilderService builder, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await builder.QuoteAsync(request.Name, request.Usage, request.Parts, ct));
    }

    private static async Task<Ok<SavedBuildView>> SaveAsync(BuilderSaveRequest request, HttpContext context, SavedBuildService saved, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        Guid? userId = context.TryGetUserId(out var id) ? id : null;
        return TypedResults.Ok(await saved.SaveAsync(userId, request.Name, request.Usage, request.Parts, request.Visibility, ct));
    }

    private static async Task<Ok<SavedBuildView>> GetSavedAsync(string token, HttpContext context, SavedBuildService saved, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        Guid? userId = context.TryGetUserId(out var id) ? id : null;
        return TypedResults.Ok(await saved.GetAsync(token, userId, ct));
    }

    /// <summary>The browser tells the API a visitor opened the build; the count is kept in memory (see <c>BuildViewStore</c>), so this answers at once.</summary>
    private static async Task<NoContent> RecordViewAsync(string token, HttpContext context, BuildViewStore views, CancellationToken ct)
    {
        Guid? viewerId = context.TryGetUserId(out var id) ? id : null;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var visitor = BuildViewRules.VisitorKey(viewerId, context.Connection.RemoteIpAddress?.ToString(), userAgent);
        await views.RecordAsync(token, viewerId, visitor, userAgent, ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<SavedBuildSummary>> CopyToAccountAsync(string token, HttpContext context, SavedBuildService saved, CancellationToken ct)
    {
        var userId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await saved.CopyToAccountAsync(token, userId, ct));
    }

    private static async Task<Ok<SavedBuildView>> UpdateSavedAsync(string token, BuilderSaveUpdateRequest request, HttpContext context, SavedBuildService saved,
        CancellationToken ct)
    {
        var userId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await saved.UpdateAsync(token, userId, request.Name, request.Usage, request.Parts, request.Visibility, request.RowVersion, ct));
    }

    private static async Task<Ok<SavedBuildSummary>> SetVisibilityAsync(string token, BuilderVisibilityRequest request, HttpContext context, SavedBuildService saved,
        CancellationToken ct)
    {
        var userId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await saved.SetVisibilityAsync(token, userId, request.Visibility, ct));
    }

    private static async Task<NoContent> DeleteSavedAsync(string token, HttpContext context, SavedBuildService saved, CancellationToken ct)
    {
        var userId = context.RequireUserId();
        await saved.DeleteAsync(token, userId, ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<SavedBuildSummary>>> ListMineAsync(HttpContext context, SavedBuildService saved, CancellationToken ct)
    {
        var userId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await saved.ListMineAsync(userId, ct));
    }
}
