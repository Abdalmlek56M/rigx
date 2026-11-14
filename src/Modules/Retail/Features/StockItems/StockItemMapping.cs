namespace HardwareStore.Modules.Retail;

/// <summary>Maps stock items (with their units loaded) to admin views; sensitive fields follow the caller's permissions.</summary>
internal static class StockItemMapping
{
    public static StockItemAdminView ToAdminView(StockItem item, DateTimeOffset now, bool includeCosts, bool includeSerials) => new(
        item.Id, item.Code, item.Condition, item.CategoryId, item.ProductId, item.ModelName, item.Price, item.WithBoardExtra,
        item.AloneExtra, StockItemRules.Prices(item.Price, item.WithBoardExtra, item.AloneExtra), item.WarrantyLabel,
        item.WarrantyMonths, item.HasBox, item.IsHidden, StockItemRules.IsPublished(item.IsHidden, item.Photos.Count),
        item.InternalNote, item.Specs, item.Photos, item.Units.Count(unit => unit.Status == UnitStatus.Available),
        Math.Max(0, (int)(now - item.CreatedAtUtc).TotalDays), item.CreatedAtUtc, item.RowVersion,
        item.Units.OrderBy(unit => unit.ReceivedAtUtc).ThenBy(unit => unit.Id)
            .Select(unit => new StockUnitView(unit.Id, unit.Status, unit.Condition, includeSerials ? unit.SerialNumber : null,
                includeCosts ? unit.AcquisitionCost : null, includeCosts ? unit.PartnerPercentOverride : null, includeCosts ? unit.PartnerId : null,
                unit.AssemblyId, unit.ReceivedAtUtc, unit.RowVersion)).ToList());
}
