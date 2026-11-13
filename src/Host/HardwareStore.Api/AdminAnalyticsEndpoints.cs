using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;

public static class AdminAnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAdminAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/admin/analytics/dashboard", GetAsync)
            .RequirePermission("Catalog.View");
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        HttpContext context,
        AdminAnalyticsService analytics,
        CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await analytics.GetAsync(cancellationToken));
    }
}
