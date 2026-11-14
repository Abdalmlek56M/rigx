using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;

public static partial class RetailEndpoints
{
    /// <summary>Public warranty search: shows only what the sticker owner needs (masked phone, no serial or prices).</summary>
    private static async Task<IResult> LookupWarrantyAsync(HttpContext context, WarrantyService warranties, CancellationToken ct, string? q = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await warranties.LookupAsync(q, ct));
    }

    private static async Task<IResult> ListMyWarrantiesAsync(HttpContext context, WarrantyService warranties, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var accountId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await warranties.ListMineAsync(accountId, ct));
    }

    private static async Task<IResult> ListWarrantiesAsync(HttpContext context, WarrantyService warranties, CancellationToken ct,
        string? q = null, string? status = null, int page = 1)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await warranties.ListAdminAsync(q, status, page, context.HasPermission("Sales.ViewCustomerContact"),
            context.HasPermission("Inventory.ViewSerials"), ct));
    }

    private static async Task<IResult> VoidWarrantyAsync(Guid id, WarrantyVoidRequest request, HttpContext context, WarrantyService warranties, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await warranties.VoidAsync(id, request, actorId, ct));
    }

    private static async Task<IResult> ExtendWarrantyAsync(Guid id, WarrantyExtendRequest request, HttpContext context, WarrantyService warranties, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await warranties.ExtendAsync(id, request, actorId, ct));
    }
}
