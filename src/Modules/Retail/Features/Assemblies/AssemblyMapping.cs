namespace HardwareStore.Modules.Retail;

/// <summary>Admin and storefront views of a build. Parts are listed in a stable order (by unit id, as the stock claim does).</summary>
internal static class AssemblyMapping
{
    public static AssemblyView ToView(Assembly assembly, bool includeCosts)
    {
        var units = assembly.Units.OrderBy(link => link.UnitId).Select(link =>
        {
            var item = link.Unit.StockItem;
            var (label, months) = AssemblyRules.EffectiveWarranty(link, assembly, item);
            return new AssemblyUnitView(link.UnitId, item.CategoryId, link.Unit.Name, item.Code, link.Unit.Condition,
                includeCosts ? link.Unit.AcquisitionCost : null, includeCosts ? AssemblyEditing.AlonePrice(item) : null,
                link.PartPrice, label, months, link.WarrantyLabel is not null,
                includeCosts ? link.Unit.PartnerPercentOverride : null, includeCosts ? link.Unit.PartnerId : null, link.AllocationPercent,
                includeCosts ? decimal.Round(assembly.SalePrice * link.AllocationPercent / 100m, 2) : null);
        }).ToList();
        return new AssemblyView(assembly.Id, assembly.Code, assembly.Name, assembly.PublicTitleAr, assembly.PublicTitleEn,
            assembly.SalePrice, assembly.Status, assembly.Usage, assembly.SellMode, assembly.WarrantyLabel, assembly.WarrantyMonths,
            assembly.Copies, assembly.SuitableForAr, assembly.SuitableForEn, assembly.Photos, units,
            includeCosts ? decimal.Round(units.Sum(unit => unit.AcquisitionCost ?? 0m), 2) : null,
            decimal.Round(units.Sum(unit => unit.PartPrice), 2),
            includeCosts ? decimal.Round(units.Sum(unit => unit.AllocatedAmount ?? 0m), 2) : null, assembly.RowVersion, assembly.PlannedParts);
    }

    public static AssemblyPublicView ToPublicView(Assembly assembly)
    {
        var split = assembly.SellMode == AssemblySellMode.FullOrSplit;
        var units = assembly.Units.OrderBy(link => link.UnitId).Select(link =>
        {
            var item = link.Unit.StockItem;
            return new PublicUnitView(link.Unit.ProductId, item.CategoryId, link.Unit.Name, item.Code, link.Unit.Condition,
                AssemblyRules.EffectiveWarranty(link, assembly, item).Label, split ? link.PartPrice : null);
        }).ToList();
        return new AssemblyPublicView(assembly.Id, assembly.Code, assembly.PublicTitleAr, assembly.PublicTitleEn, assembly.SalePrice,
            assembly.Usage, assembly.SellMode, assembly.Copies, assembly.WarrantyLabel, assembly.SuitableForAr, assembly.SuitableForEn,
            assembly.Photos, units);
    }
}
