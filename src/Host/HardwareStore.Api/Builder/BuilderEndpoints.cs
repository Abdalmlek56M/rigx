using Microsoft.AspNetCore.Http;
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
        endpoints.MapPut("/api/v1/builder/saved/{token}", UpdateSavedAsync).RequireAuthorization();
        endpoints.MapDelete("/api/v1/builder/saved/{token}", DeleteSavedAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/account/builds", ListMineAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> SlotsAsync(HttpContext context, BuilderService builder, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "public, max-age=60";
        return Results.Ok(await builder.SlotsAsync(ct));
    }

    private static async Task<IResult> OptionsAsync(BuilderOptionsRequest request, HttpContext context, BuilderService builder, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await builder.OptionsAsync(request.Slot, request.Parts, request.Condition, request.Q, request.Page, ct));
    }

    private static async Task<IResult> CheckAsync(BuilderCheckRequest request, HttpContext context, BuilderService builder, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await builder.CheckAsync(request.Parts, ct));
    }

    private static async Task<IResult> FromBuildAsync(string code, HttpContext context, BuilderService builder, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await builder.FromBuildAsync(code, ct));
    }

    private static async Task<IResult> QuoteAsync(BuilderQuoteRequest request, HttpContext context, BuilderService builder, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await builder.QuoteAsync(request.Name, request.Usage, request.Parts, ct));
    }

    private static async Task<IResult> SaveAsync(BuilderSaveRequest request, HttpContext context, SavedBuildService saved, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        Guid? userId = context.TryGetUserId(out var id) ? id : null;
        return Results.Ok(await saved.SaveAsync(userId, request.Name, request.Usage, request.Parts, ct));
    }

    private static async Task<IResult> GetSavedAsync(string token, HttpContext context, SavedBuildService saved, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        Guid? userId = context.TryGetUserId(out var id) ? id : null;
        return Results.Ok(await saved.GetAsync(token, userId, ct));
    }

    private static async Task<IResult> UpdateSavedAsync(string token, BuilderSaveUpdateRequest request, HttpContext context, SavedBuildService saved,
        CancellationToken ct)
    {
        if (!context.TryGetUserId(out var userId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await saved.UpdateAsync(token, userId, request.Name, request.Usage, request.Parts, request.RowVersion, ct));
    }

    private static async Task<IResult> DeleteSavedAsync(string token, HttpContext context, SavedBuildService saved, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var userId)) return Results.Unauthorized();
        await saved.DeleteAsync(token, userId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListMineAsync(HttpContext context, SavedBuildService saved, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var userId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await saved.ListMineAsync(userId, ct));
    }
}
