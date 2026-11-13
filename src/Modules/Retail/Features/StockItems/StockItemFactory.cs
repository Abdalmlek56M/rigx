using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Turns one intake line into new stock items with their units (codes from the RX sequence) and adds them to the context.
/// Used by purchase receipts and by "add a part that is not in stock" in the build editor.
/// </summary>
internal static class StockItemFactory
{
    public static async Task<List<StockItem>> CreateAsync(RetailDbContext db, ReceiptLine line, ReceiptSource source,
        Guid? receiptId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var condition = StockCondition.Parse(line.Condition);
        var modelName = StockItemRules.CleanModelName(line.ModelName);
        StockItemRules.ValidatePrices(line.Price, line.WithBoardExtra, line.AloneExtra);
        StockItemRules.ValidateWarranty(line.WarrantyLabel, line.WarrantyMonths);
        var specs = StockItemRules.CleanSpecs(line.Specs);
        var note = StockItemRules.CleanNote(line.InternalNote);
        if (line.AcquisitionCost < 0) throw new RetailException(400, "Acquisition cost cannot be negative.");
        if (line.PartnerPercentOverride is < 0 or > 100) throw new RetailException(400, "Partner percentage must be between 0 and 100.");
        var items = new List<StockItem>();
        foreach (var unitCount in StockItemRules.UnitsPerItem(condition, line.Quantity))
        {
            var item = new StockItem
            {
                Id = Guid.CreateVersion7(),
                Code = await db.NextCodeAsync(CodeSequence.StockItem, PublicCode.StockItem, cancellationToken),
                Condition = condition,
                CategoryId = line.CategoryId,
                ProductId = line.ProductId,
                ModelName = modelName,
                ModelKey = ModelName.Key(modelName),
                Price = line.Price,
                WithBoardExtra = line.WithBoardExtra,
                AloneExtra = line.AloneExtra,
                WarrantyLabel = line.WarrantyLabel?.Trim(),
                WarrantyMonths = line.WarrantyMonths,
                HasBox = line.HasBox,
                IsHidden = line.IsHidden,
                InternalNote = note,
                Specs = new Dictionary<string, string>(specs),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            for (var index = 0; index < unitCount; index++)
                item.Units.Add(new InventoryUnit
                {
                    Id = Guid.CreateVersion7(),
                    StockItemId = item.Id,
                    ProductId = line.ProductId,
                    Name = modelName,
                    Condition = condition,
                    SerialNumber = unitCount == 1 && line.Quantity == 1 ? line.SerialNumber?.Trim() : null,
                    SourceName = source.SourceName?.Trim(),
                    SourceType = source.SourceType?.Trim(),
                    PurchaseKind = source.PurchaseKind?.Trim(),
                    PurchaseDate = source.PurchaseDate,
                    AcquisitionCost = line.AcquisitionCost,
                    PartnerPercentOverride = line.PartnerPercentOverride,
                    Status = UnitStatus.Available,
                    ReceiptId = receiptId,
                    ReceivedAtUtc = now,
                    CreatedAtUtc = now
                });
            db.StockItems.Add(item);
            items.Add(item);
        }
        return items;
    }
}
