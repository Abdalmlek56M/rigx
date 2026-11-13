using System.Text.Json;
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
        var includeCosts = HasPermission(context, "Inventory.ViewCosts");
        var includeSerials = HasPermission(context, "Inventory.ViewSerials");
        return Results.Ok(await units.ListUnitsAsync(status, productId, includeCosts, includeSerials, ct));
    }

    /// <summary>
    /// Receives a purchase draft into stock. Orchestrates three modules (Purchasing draft, Catalog prices/images,
    /// Retail units), so it stays in Host per architecture decision 11.
    /// </summary>
    private static async Task<IResult> ReceiveFromDraftAsync(
        Guid draftId, ReceiveNotes request, HttpContext context, InventoryUnitService inventory,
        IDbContextFactory<PurchasingDbContext> purchasingFactory,
        IDbContextFactory<CatalogDbContext> catalogFactory, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
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
        var images = await catalog.ProductImages.AsNoTracking()
            .Where(image => productIds.Contains(image.ProductId))
            .OrderByDescending(image => image.IsPrimary).ThenBy(image => image.SortOrder)
            .Select(image => new { image.ProductId, image.StorageKey, image.ThumbnailStorageKey, image.OriginalStorageKey })
            .ToListAsync(ct);
        var imageKeys = images.GroupBy(image => image.ProductId)
            .ToDictionary(group => group.Key, group => JsonSerializer.Serialize(group.First()));
        var sourceName = draft.SupplierId is Guid supplierId
            ? await purchasing.SupplierProfiles.AsNoTracking().Where(supplier => supplier.Id == supplierId)
                .Select(supplier => supplier.CompanyName).SingleOrDefaultAsync(ct)
            : null;
        sourceName ??= draft.MarketplaceName ?? draft.ExternalSellerName;
        var lines = draft.Items.Select(item =>
        {
            var productId = item.ProductId == Guid.Empty ? (Guid?)null : item.ProductId;
            var standalone = productId is not null && prices.TryGetValue(productId.Value, out var price) ? price : (decimal?)null;
            var imageJson = productId is not null && imageKeys.TryGetValue(productId.Value, out var json) ? json : null;
            return new ReceiptLine(productId, item.VariantId, item.ItemName, item.Sku, item.Condition ?? "New", null,
                item.UnitPrice, item.Quantity, null, null, standalone, sourceName, item.Brand, item.Model, item.Category,
                item.Description, item.ExternalCode, item.SpecificationsJson, draft.PurchaseKind, draft.SourceType,
                draft.PurchaseDate, imageJson);
        }).ToList();
        var units = await inventory.ReceiveAsync(new ReceiveRequest(draftId, request.Notes, lines), actorId, ct);
        await purchasing.PurchaseDrafts.Where(item => item.Id == draftId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, PurchaseDraftStatus.Received), ct);
        return Results.Ok(units);
    }
}
