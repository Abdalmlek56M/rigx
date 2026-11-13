using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;

public static partial class RetailEndpoints
{
    private static async Task<IResult> ListUnitsAsync(HttpContext context, InventoryUnitService units, CancellationToken ct, string? status = null, Guid? productId = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var includeCosts = context.HasPermission("Inventory.ViewCosts");
        var includeSerials = context.HasPermission("Inventory.ViewSerials");
        return Results.Ok(await units.ListUnitsAsync(status, productId, includeCosts, includeSerials, ct));
    }
}
