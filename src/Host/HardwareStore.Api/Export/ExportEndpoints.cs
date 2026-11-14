using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// <c>GET /api/v1/admin/system/export/{dataset}?format=csv|xlsx|xml&amp;from&amp;to</c>: one route per dataset, each with the permission its
/// list page needs (customers also need a fresh email code). An unknown dataset has no route, so it is a 404.
/// </summary>
public static class ExportEndpoints
{
    private const string ViewCosts = "Inventory.ViewCosts";
    private const string ViewContact = "Sales.ViewCustomerContact";

    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/system/export");
        foreach (var dataset in ExportDatasets.All)
        {
            var route = group.MapGet(dataset.Name, (HttpContext context, ExportRunner runner, CancellationToken cancellationToken,
                    string? format, DateOnly? from, DateOnly? to) => ExportAsync(dataset, context, runner, format, from, to, cancellationToken))
                .RequirePermission(dataset.Permission)
                .Produces<Stream>(StatusCodes.Status200OK, "text/csv", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "application/xml");
            if (dataset.RequiresStepUp) route.RequireStepUp();
        }
        return endpoints;
    }

    private static async Task<FileContentHttpResult> ExportAsync(ExportDataset dataset, HttpContext context, ExportRunner runner, string? format,
        DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        var result = await runner.RunAsync(dataset, format ?? "csv", from, to, context.HasPermission(ViewCosts),
            context.HasPermission(ViewContact), actorId, cancellationToken);
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.File(result.File.Content, result.File.ContentType, result.FileName);
    }
}
