using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static partial class RetailEndpoints
{
    /// <summary>Public warranty search: shows only what the sticker owner needs (masked phone, no serial or prices).</summary>
    private static async Task<Ok<WarrantyLookupResult>> LookupWarrantyAsync(HttpContext context, WarrantyService warranties, CancellationToken ct, string? q = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await warranties.LookupAsync(q, ct));
    }

    private static async Task<Ok<IReadOnlyList<WarrantyAccountView>>> ListMyWarrantiesAsync(HttpContext context, WarrantyService warranties, CancellationToken ct)
    {
        var accountId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await warranties.ListMineAsync(accountId, ct));
    }

    private static async Task<Ok<PagedResult<WarrantyAdminView>>> ListWarrantiesAsync(HttpContext context, WarrantyService warranties, CancellationToken ct,
        string? q = null, string? status = null, int page = 1)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await warranties.ListAdminAsync(q, status, page, context.HasPermission("Sales.ViewCustomerContact"),
            context.HasPermission("Inventory.ViewSerials"), ct));
    }

    private static async Task<Ok<WarrantyAdminView>> VoidWarrantyAsync(Guid id, WarrantyVoidRequest request, HttpContext context, WarrantyService warranties, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await warranties.VoidAsync(id, request, actorId, ct));
    }

    private static async Task<Ok<WarrantyAdminView>> ExtendWarrantyAsync(Guid id, WarrantyExtendRequest request, HttpContext context, WarrantyService warranties, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await warranties.ExtendAsync(id, request, actorId, ct));
    }
}
