using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Maps stock items (with their units loaded) to admin views; sensitive fields follow the caller's permissions.</summary>
internal static class StockItemMapping
{
    public static StockItemAdminView ToAdminView(StockItem item, DateTimeOffset now, bool includeCosts, bool includeSerials,
        IReadOnlyDictionary<Guid, StockBuildRef>? builds = null) => new(
        item.Id, item.Code, item.Condition, item.CategoryId, item.BrandId, item.ProductId, item.ModelName, item.Price, item.WithBoardExtra,
        item.AloneExtra, StockItemRules.Prices(item.Price, item.WithBoardExtra, item.AloneExtra), item.WarrantyLabel,
        item.WarrantyMonths, item.HasBox, item.IsHidden, StockItemRules.IsPublished(item.IsHidden, item.Photos.Count),
        item.InternalNote, item.Specs, item.Photos, item.Units.Count(unit => unit.Status == UnitStatus.Available),
        Math.Max(0, (int)(now - item.CreatedAtUtc).TotalDays), item.CreatedAtUtc, item.RowVersion,
        item.Units.OrderBy(unit => unit.ReceivedAtUtc).ThenBy(unit => unit.Id)
            .Select(unit => new StockUnitView(unit.Id, unit.Status, unit.Condition, includeSerials ? unit.SerialNumber : null,
                includeCosts ? unit.AcquisitionCost : null, includeCosts ? unit.PartnerPercentOverride : null, includeCosts ? unit.PartnerId : null,
                unit.AssemblyId, unit.ReceivedAtUtc, unit.RowVersion,
                unit.AssemblyId is Guid assemblyId && builds is not null && builds.TryGetValue(assemblyId, out var build) ? build : null)).ToList());

    /// <summary>The builds the units of these items sit in, by build id (one query).</summary>
    public static async Task<Dictionary<Guid, StockBuildRef>> BuildsOfAsync(RetailDbContext db, IEnumerable<StockItem> items, CancellationToken cancellationToken)
    {
        var ids = items.SelectMany(item => item.Units).Select(unit => unit.AssemblyId).OfType<Guid>().Distinct().ToList();
        if (ids.Count == 0) return [];
        var rows = await db.Assemblies.AsNoTracking().Where(build => ids.Contains(build.Id))
            .Select(build => new { build.Id, build.Code, build.Name, build.Status }).ToListAsync(cancellationToken);
        return rows.ToDictionary(row => row.Id, row => new StockBuildRef(row.Code, row.Name, row.Status));
    }
}
