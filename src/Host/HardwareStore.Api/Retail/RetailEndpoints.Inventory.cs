using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static partial class RetailEndpoints
{
    private static async Task<Ok<IReadOnlyList<UnitView>>> ListUnitsAsync(HttpContext context, InventoryUnitService units, CancellationToken ct, string? status = null, Guid? productId = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var includeCosts = context.HasPermission("Inventory.ViewCosts");
        var includeSerials = context.HasPermission("Inventory.ViewSerials");
        return TypedResults.Ok(await units.ListUnitsAsync(status, productId, includeCosts, includeSerials, ct));
    }
}
