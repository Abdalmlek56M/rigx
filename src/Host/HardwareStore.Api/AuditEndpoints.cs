using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/admin/audit/events", QueryAsync)
            .RequirePermission("System.ViewAuditLogs");
        return endpoints;
    }

    private static async Task<IResult> QueryAsync(
        HttpContext context,
        SecurityAuditQueryService service,
        CancellationToken cancellationToken,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        string? eventType = null,
        string? outcome = null,
        Guid? actorUserAccountId = null,
        Guid? subjectUserAccountId = null,
        string? correlationId = null,
        string? search = null,
        int page = 1,
        int pageSize = 25)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (outcome is not null && outcome is not ("Succeeded" or "Failed" or "Rejected" or "Recorded"))
            return Results.Problem(statusCode: 400, title: "Unsupported audit outcome filter.");
        try
        {
            var result = await service.QueryAsync(
                new SecurityAuditQuery(
                    fromUtc,
                    toUtc,
                    eventType,
                    outcome,
                    actorUserAccountId,
                    subjectUserAccountId,
                    correlationId,
                    search,
                    page,
                    pageSize),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(statusCode: 400, title: exception.Message);
        }
    }
}
