using System.Linq.Expressions;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Which units count in the stock value: the owner can leave an item or one unit out. It stays in stock and sellable,
/// only its cost is left out of the stock money totals and reports.
/// </summary>
public static class StockValueRules
{
    /// <summary>The SQL-translatable form of <see cref="Counts"/>, for sums over units (the item must be reachable as <c>unit.StockItem</c>).</summary>
    public static Expression<Func<InventoryUnit, bool>> Counted { get; } =
        unit => !unit.ExcludedFromStockValue && !unit.StockItem.ExcludedFromStockValue;

    public static bool Counts(InventoryUnit unit, StockItem item) => !unit.ExcludedFromStockValue && !item.ExcludedFromStockValue;
}
