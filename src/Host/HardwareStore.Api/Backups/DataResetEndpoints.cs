using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;

/// <summary>The owner data reset: <c>System.ResetData</c> plus a fresh email code. Always preceded by a backup.</summary>
public static class DataResetEndpoints
{
    public sealed record ResetRequest(IReadOnlyList<string>? Sections, string? Confirm);

    public static IEndpointRouteBuilder MapDataResetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/admin/system/reset", ResetAsync).RequirePermission("System.ResetData").RequireStepUp();
        return endpoints;
    }

    private static async Task<IResult> ResetAsync(ResetRequest request, HttpContext context, DataResetService reset, CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        return Results.Ok(await reset.ResetAsync(request.Sections, request.Confirm, actorId, cancellationToken));
    }
}
