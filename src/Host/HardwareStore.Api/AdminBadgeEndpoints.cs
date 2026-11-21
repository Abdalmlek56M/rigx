using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>What the admin sidebar shows next to a section: how much new work arrived since the staff member last opened it.</summary>
public static class AdminBadgeEndpoints
{
    public static IEndpointRouteBuilder MapAdminBadgeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/admin/badges", GetAsync);
        return endpoints;
    }

    /// <summary>
    /// New orders and new requests since the given moments (the staff member's last visit to each section; none = every new one).
    /// A count is null when the caller may not open that section (so the screen shows nothing for it); a caller who may open neither gets 403.
    /// </summary>
    private static async Task<Results<Ok<AdminBadges>, ForbidHttpResult>> GetAsync(HttpContext context, OrderService orders, CustomerRequestService requests,
        TimeProvider timeProvider, CancellationToken ct, DateTimeOffset? ordersSince = null, DateTimeOffset? requestsSince = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var canSeeOrders = context.HasPermission("Sales.ViewOrders");
        var canSeeRequests = context.HasPermission("Requests.View");
        if (!canSeeOrders && !canSeeRequests) return TypedResults.Forbid();
        var orderCount = canSeeOrders ? await orders.CountNewSinceAsync(ordersSince, ct) : (int?)null;
        var requestCount = canSeeRequests ? await requests.CountNewSinceAsync(requestsSince, ct) : (int?)null;
        return TypedResults.Ok(new AdminBadges(orderCount, requestCount, timeProvider.GetUtcNow()));
    }
}

/// <summary><paramref name="AsOf"/> is the server time the counts were taken: the screen keeps it as the moment the section was seen.</summary>
public sealed record AdminBadges(int? Orders, int? Requests, DateTimeOffset AsOf);
