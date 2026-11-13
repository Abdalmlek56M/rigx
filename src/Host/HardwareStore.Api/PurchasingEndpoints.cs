using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Purchasing;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Purchasing HTTP API: sellers, purchases and receiving a purchase into stock. Business errors are
/// <c>PurchasingException</c>s mapped once by <c>ProblemDetailsExceptionHandler</c>, so handlers do not catch them.
/// </summary>
public static class PurchasingEndpoints
{
    private const string ManageSuppliers = "Purchasing.ManageSuppliers";

    public static IEndpointRouteBuilder MapPurchasingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/v1/admin/purchasing");
        admin.MapGet("/sellers", ListSellersAsync).RequirePermission("Purchasing.View");
        admin.MapGet("/sellers/{id:guid}", GetSellerAsync).RequirePermission("Purchasing.View");
        admin.MapPost("/sellers", CreateSellerAsync).RequirePermission(ManageSuppliers);
        admin.MapPut("/sellers/{id:guid}", UpdateSellerAsync).RequirePermission(ManageSuppliers);
        admin.MapGet("/purchases", ListPurchasesAsync).RequirePermission("Purchasing.View");
        admin.MapGet("/purchases/{id:guid}", GetPurchaseAsync).RequirePermission("Purchasing.View");
        admin.MapPost("/purchases", CreatePurchaseAsync).RequirePermission("Purchasing.CreateOrder");
        admin.MapPut("/purchases/{id:guid}", UpdatePurchaseAsync).RequirePermission("Purchasing.CreateOrder");
        admin.MapPost("/purchases/{id:guid}/cancel", CancelPurchaseAsync).RequirePermission("Purchasing.CreateOrder");
        admin.MapPost("/purchases/{id:guid}/receive", ReceivePurchaseAsync).RequirePermission("Purchasing.ReceiveGoods");
        return endpoints;
    }

    private static async Task<IResult> ListSellersAsync(HttpContext context, SellerService sellers, CancellationToken ct,
        string? phone = null, string? q = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await sellers.ListAsync(phone, q, context.HasPermission(ManageSuppliers), ct));
    }

    private static async Task<IResult> GetSellerAsync(Guid id, HttpContext context, SellerService sellers, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await sellers.GetAsync(id, context.HasPermission(ManageSuppliers), ct));
    }

    private static async Task<IResult> CreateSellerAsync(SellerRequest request, SellerService sellers, CancellationToken ct) =>
        Results.Created((string?)null, await sellers.CreateAsync(request, ct));

    private static async Task<IResult> UpdateSellerAsync(Guid id, SellerRequest request, SellerService sellers, CancellationToken ct) =>
        Results.Ok(await sellers.UpdateAsync(id, request, ct));

    private static async Task<IResult> ListPurchasesAsync(HttpContext context, PurchaseService purchases, CancellationToken ct,
        string? status = null, string? q = null, int page = 1, int pageSize = 25)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await purchases.ListAsync(new PurchaseQuery(status, q, page, pageSize), ct));
    }

    private static async Task<IResult> GetPurchaseAsync(Guid id, HttpContext context, PurchaseService purchases, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await purchases.GetAsync(id, ct));
    }

    private static async Task<IResult> CreatePurchaseAsync(PurchaseRequest request, HttpContext context, PurchaseService purchases, CancellationToken ct) =>
        context.TryGetUserId(out var actorId)
            ? Results.Created((string?)null, await purchases.CreateAsync(request, actorId, ct))
            : Results.Unauthorized();

    private static async Task<IResult> UpdatePurchaseAsync(Guid id, PurchaseRequest request, PurchaseService purchases, CancellationToken ct) =>
        Results.Ok(await purchases.UpdateAsync(id, request, ct));

    private static async Task<IResult> CancelPurchaseAsync(Guid id, PurchaseVersionRequest request, PurchaseService purchases, CancellationToken ct) =>
        Results.Ok(await purchases.CancelAsync(id, request.RowVersion, ct));

    private static async Task<IResult> ReceivePurchaseAsync(Guid id, HttpContext context, PurchaseReceiptService receipts, CancellationToken ct) =>
        context.TryGetUserId(out var actorId) ? Results.Ok(await receipts.ReceiveAsync(id, actorId, ct)) : Results.Unauthorized();
}

public sealed record PurchaseVersionRequest(long RowVersion);
