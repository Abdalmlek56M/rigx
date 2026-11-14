using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

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

    private static async Task<Ok<IReadOnlyList<SellerView>>> ListSellersAsync(HttpContext context, SellerService sellers, CancellationToken ct,
        string? phone = null, string? q = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await sellers.ListAsync(phone, q, context.HasPermission(ManageSuppliers), ct));
    }

    private static async Task<Ok<SellerView>> GetSellerAsync(Guid id, HttpContext context, SellerService sellers, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await sellers.GetAsync(id, context.HasPermission(ManageSuppliers), ct));
    }

    private static async Task<Created<SellerView>> CreateSellerAsync(SellerRequest request, SellerService sellers, CancellationToken ct) =>
        TypedResults.Created((string?)null, await sellers.CreateAsync(request, ct));

    private static async Task<Ok<SellerView>> UpdateSellerAsync(Guid id, SellerRequest request, SellerService sellers, CancellationToken ct) =>
        TypedResults.Ok(await sellers.UpdateAsync(id, request, ct));

    private static async Task<Ok<PagedResult<PurchaseListItem>>> ListPurchasesAsync(HttpContext context, PurchaseService purchases, CancellationToken ct,
        string? status = null, string? q = null, int page = 1, int pageSize = 25)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await purchases.ListAsync(new PurchaseQuery(status, q, page, pageSize), ct));
    }

    private static async Task<Ok<PurchaseView>> GetPurchaseAsync(Guid id, HttpContext context, PurchaseService purchases, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await purchases.GetAsync(id, ct));
    }

    private static async Task<Created<PurchaseView>> CreatePurchaseAsync(PurchaseRequest request, HttpContext context, PurchaseService purchases,
        PartnerService partners, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        await EnsureFinancingPartnersAsync(request, partners, ct);
        return TypedResults.Created((string?)null, await purchases.CreateAsync(request, actorId, ct));
    }

    private static async Task<Ok<PurchaseView>> UpdatePurchaseAsync(Guid id, PurchaseRequest request, PurchaseService purchases, PartnerService partners,
        CancellationToken ct)
    {
        await EnsureFinancingPartnersAsync(request, partners, ct);
        return TypedResults.Ok(await purchases.UpdateAsync(id, request, ct));
    }

    /// <summary>A draft line may only name an active financed-parts partner (Retail owns partners; the receipt checks again).</summary>
    private static async Task EnsureFinancingPartnersAsync(PurchaseRequest request, PartnerService partners, CancellationToken ct)
    {
        foreach (var partnerId in (request.Lines ?? []).Select(line => line.PartnerId).OfType<Guid>().Distinct())
            await partners.EnsureCanFinanceAsync(partnerId, ct);
    }

    private static async Task<Ok<PurchaseView>> CancelPurchaseAsync(Guid id, PurchaseVersionRequest request, PurchaseService purchases, CancellationToken ct) =>
        TypedResults.Ok(await purchases.CancelAsync(id, request.RowVersion, ct));

    private static async Task<Ok<PurchaseReceived>> ReceivePurchaseAsync(Guid id, HttpContext context, PurchaseReceiptService receipts, CancellationToken ct) =>
        TypedResults.Ok(await receipts.ReceiveAsync(id, context.RequireUserId(), ct));
}

public sealed record PurchaseVersionRequest(long RowVersion);
