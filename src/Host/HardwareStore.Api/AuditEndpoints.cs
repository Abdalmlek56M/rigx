using HardwareStore.Common.Domain;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;

public static class AuditEndpoints
{
    private const string View = "System.ViewAuditLogs";
    private const string Restore = "Audit.Restore";

    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/admin/audit/events", QueryAsync)
            .RequirePermission(View);
        var entries = endpoints.MapGroup("/api/v1/admin/audit");
        entries.MapGet("/entries", ListEntriesAsync).RequirePermission(View);
        entries.MapGet("/entries/{id:guid}", GetEntryAsync).RequirePermission(View);
        entries.MapGet("/filters", FiltersAsync).RequirePermission(View);
        entries.MapPost("/entries/{id:guid}/restore", RestoreAsync).RequirePermission(Restore).RequireStepUp();
        return endpoints;
    }

    private static async Task<IResult> ListEntriesAsync(HttpContext context, AuditLogService audit, CancellationToken cancellationToken,
        Guid? actorUserId = null, string? module = null, string? entity = null, string? entityId = null, string? action = null,
        DateOnly? from = null, DateOnly? to = null, int page = 1, int pageSize = AuditRules.DefaultPageSize)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await audit.QueryAsync(actorUserId, module, entity, entityId, action, from, to, page, pageSize, cancellationToken));
    }

    private static async Task<IResult> GetEntryAsync(Guid id, HttpContext context, AuditLogService audit, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await audit.GetAsync(id, cancellationToken));
    }

    private static async Task<IResult> FiltersAsync(HttpContext context, AuditLogService audit, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await audit.FiltersAsync(cancellationToken));
    }

    private static async Task<IResult> RestoreAsync(Guid id, AuditLogService audit, CancellationToken cancellationToken)
    {
        await audit.RestoreAsync(id, cancellationToken);
        return Results.NoContent();
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
