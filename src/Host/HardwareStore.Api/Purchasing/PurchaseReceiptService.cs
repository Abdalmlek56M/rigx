using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;

public sealed record PurchaseReceived(PurchaseView Purchase, ReceiptResult Items);

/// <summary>
/// Receives a draft purchase into stock. It spans four modules (Purchasing purchase, Storefront warranty list,
/// Catalog spec schema, Retail stock), so it lives in Host per architecture decision 11.
/// </summary>
public sealed class PurchaseReceiptService(PurchaseService purchases, InventoryUnitService inventory, AssemblyService assemblies,
    StoreSettingsService settings, CategorySpecService categorySpecs)
{
    public async Task<PurchaseReceived> ReceiveAsync(Guid purchaseId, Guid actorId, CancellationToken cancellationToken)
    {
        var purchase = await purchases.GetDraftAsync(purchaseId, cancellationToken);
        var warranties = await WarrantyChoice.LoadOptionsAsync(settings, cancellationToken);
        var lines = new List<ReceiptLine>();
        foreach (var line in purchase.Lines)
        {
            var (label, months) = WarrantyChoice.Resolve(warranties, line.WarrantyLabel);
            var specs = await categorySpecs.CheckValuesAsync(line.CategoryId, line.Specs, cancellationToken);
            lines.Add(new ReceiptLine(line.ModelName, line.Condition, line.Quantity, line.UnitCost, line.Price, line.CategoryId,
                line.ProductId, line.WithBoardExtra, line.AloneExtra, line.SerialNumber, line.PartnerPercent, label, months,
                line.HasBox, line.IsHidden, line.InternalNote, specs, line.PartnerId));
        }
        var source = new ReceiptSource(purchase.Seller.Name, purchase.Seller.Kind, purchase.Kind, purchase.PurchaseDate);
        var receipt = await inventory.ReceiveAsync(new ReceiveRequest(purchase.Id, purchase.Notes, source, lines), actorId, cancellationToken);
        Guid? assemblyId = null;
        if (purchase.KeepAsBuild)
        {
            var unitIds = receipt.Items.SelectMany(item => item.Units).Select(unit => unit.Id).ToList();
            var build = await assemblies.CreateAssemblyAsync(new AssemblyCreateRequest(purchase.BuildName ?? $"PC {purchase.Code}", unitIds), cancellationToken: cancellationToken);
            assemblyId = build.Id;
        }
        return new PurchaseReceived(await purchases.MarkReceivedAsync(purchase.Id, assemblyId, cancellationToken), receipt);
    }
}
