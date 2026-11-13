using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public static partial class RetailEndpoints
{
    private static async Task<IResult> ListUnitsAsync(HttpContext context, InventoryUnitService units, CancellationToken ct, string? status = null, Guid? productId = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var includeCosts = context.HasPermission("Inventory.ViewCosts");
        var includeSerials = context.HasPermission("Inventory.ViewSerials");
        return Results.Ok(await units.ListUnitsAsync(status, productId, includeCosts, includeSerials, ct));
    }

    /// <summary>
    /// Receives a purchase draft into stock. Orchestrates three modules (Purchasing draft, Catalog prices,
    /// Retail stock items), so it stays in Host per architecture decision 11.
    /// </summary>
    private static async Task<IResult> ReceiveFromDraftAsync(
        Guid draftId, ReceiveNotes request, HttpContext context, InventoryUnitService inventory,
        IDbContextFactory<PurchasingDbContext> purchasingFactory,
        IDbContextFactory<CatalogDbContext> catalogFactory, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        await using var purchasing = await purchasingFactory.CreateDbContextAsync(ct);
        var draft = await purchasing.PurchaseDrafts.AsNoTracking().Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == draftId, ct);
        if (draft is null) return Results.NotFound();
        var productIds = draft.Items.Where(item => item.ProductId != Guid.Empty).Select(item => item.ProductId).Distinct().ToList();
        await using var catalog = await catalogFactory.CreateDbContextAsync(ct);
        var prices = await catalog.Products.AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new { product.Id, product.BasePrice })
            .ToDictionaryAsync(item => item.Id, item => item.BasePrice, ct);
        var sourceName = draft.SupplierId is Guid supplierId
            ? await purchasing.SupplierProfiles.AsNoTracking().Where(supplier => supplier.Id == supplierId)
                .Select(supplier => supplier.CompanyName).SingleOrDefaultAsync(ct)
            : null;
        sourceName ??= draft.MarketplaceName ?? draft.ExternalSellerName;
        var lines = draft.Items.Select(item =>
        {
            var productId = item.ProductId == Guid.Empty ? (Guid?)null : item.ProductId;
            var price = productId is not null && prices.TryGetValue(productId.Value, out var basePrice) ? basePrice : 0m;
            var condition = item.Condition == StockCondition.New ? StockCondition.New : StockCondition.Used;
            return new ReceiptLine(string.IsNullOrWhiteSpace(item.Model) ? item.ItemName : item.Model, condition, item.Quantity, item.UnitPrice, price, ProductId: productId);
        }).ToList();
        var source = new ReceiptSource(sourceName, draft.SourceType, draft.PurchaseKind, draft.PurchaseDate);
        var receipt = await inventory.ReceiveAsync(new ReceiveRequest(draftId, request.Notes, source, lines), actorId, ct);
        await purchasing.PurchaseDrafts.Where(item => item.Id == draftId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, PurchaseDraftStatus.Received), ct);
        return Results.Ok(receipt);
    }
}
