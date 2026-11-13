using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Stock claim/release used by the order status workflow (runs inside the caller's transaction).</summary>
internal static class OrderStockAllocator
{
    /// <summary>
    /// Claims stock for every line at confirmation: locks the build or the oldest available units
    /// (FOR UPDATE SKIP LOCKED), marks them Sold and records per-unit revenue/cost components.
    /// </summary>
    public static async Task ClaimAsync(RetailDbContext db, StoreOrder order, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            if (item.AssemblyId is not null)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SELECT 1 FROM assemblies WHERE id = {0} FOR UPDATE",
                    new object[] { item.AssemblyId.Value }, cancellationToken);
                var assembly = await db.Assemblies.WithUnits()
                    .SingleOrDefaultAsync(a => a.Id == item.AssemblyId && a.Status == AssemblyStatus.Published, cancellationToken)
                    ?? throw new RetailException(409, "The selected build is no longer available.");
                if (assembly.Units.Count == 0 || !AllocationMath.SumsToHundred(assembly.Units.Select(link => link.AllocationPercent)))
                    throw new RetailException(409, "The build allocation is incomplete.");
                if (assembly.Units.Any(link => link.Unit.Status is not (UnitStatus.InAssembly or UnitStatus.Available)))
                    throw new RetailException(409, "One or more build components are no longer available.");
                var lines = assembly.Units.OrderBy(link => link.UnitId).ToList();
                var revenues = AllocationMath.AllocateAmount(item.LineTotal, lines.Select(link => link.AllocationPercent).ToList());
                var discounts = AllocationMath.AllocateAmount(item.DiscountAmount, lines.Select(link => link.AllocationPercent).ToList());
                var expenses = AllocationMath.AllocateAmount(item.DirectExpenseAmount, lines.Select(link => link.AllocationPercent).ToList());
                for (var index = 0; index < lines.Count; index++)
                {
                    var link = lines[index];
                    var cost = link.Unit.AcquisitionCost;
                    item.Components.Add(new OrderItemComponent
                    {
                        Id = Guid.CreateVersion7(), OrderItemId = item.Id, UnitId = link.UnitId,
                        AllocationPercent = link.AllocationPercent,
                        RevenueAmount = revenues[index], DiscountAmount = discounts[index],
                        DirectExpenseAmount = expenses[index], CostAmount = cost,
                        NetProfit = AllocationMath.Net(revenues[index], discounts[index], expenses[index], cost)
                    });
                    link.Unit.Status = UnitStatus.Sold; link.Unit.RowVersion++;
                }
                item.UnitCostSnapshot = decimal.Round(item.Components.Sum(component => component.CostAmount), 2);
                item.NetProfit = decimal.Round(item.Components.Sum(component => component.NetProfit), 2);
                assembly.Status = AssemblyStatus.Sold;
                continue;
            }
            await ClaimStockLineAsync(db, item, cancellationToken);
        }
    }

    /// <summary>Claims the oldest available units of a stock item (its lot) or, for product lines, of a product.</summary>
    private static async Task ClaimStockLineAsync(RetailDbContext db, OrderItem item, CancellationToken cancellationToken)
    {
        if (item.StockItemId is null && item.ProductId is null)
            throw new RetailException(409, "A stock line is missing its item reference.");
        var lockQuery = item.StockItemId is Guid stockItemId
            ? db.Database.SqlQuery<Guid>(
                $"SELECT id AS \"Value\" FROM inventory_units WHERE stock_item_id = {stockItemId} AND status = 'Available' ORDER BY received_at_utc LIMIT {item.Quantity} FOR UPDATE SKIP LOCKED")
            : db.Database.SqlQuery<Guid>(
                $"SELECT id AS \"Value\" FROM inventory_units WHERE product_id = {item.ProductId!.Value} AND status = 'Available' ORDER BY received_at_utc LIMIT {item.Quantity} FOR UPDATE SKIP LOCKED");
        var lockedIds = await lockQuery.ToListAsync(cancellationToken);
        if (lockedIds.Count != item.Quantity)
            throw new RetailException(409, "Not enough available stock to confirm this order.");
        var available = await db.Units
            .Where(unit => lockedIds.Contains(unit.Id) && unit.Status == UnitStatus.Available)
            .ToListAsync(cancellationToken);
        if (available.Count != item.Quantity)
            throw new RetailException(409, "Not enough available stock to confirm this order.");
        var percents = AllocationMath.EqualPercentages(available.Count);
        var lineRevenues = AllocationMath.AllocateAmount(item.LineTotal, percents);
        var lineDiscounts = AllocationMath.AllocateAmount(item.DiscountAmount, percents);
        var lineExpenses = AllocationMath.AllocateAmount(item.DirectExpenseAmount, percents);
        for (var index = 0; index < available.Count; index++)
        {
            var unit = available[index];
            item.Components.Add(new OrderItemComponent
            {
                Id = Guid.CreateVersion7(), OrderItemId = item.Id, UnitId = unit.Id,
                AllocationPercent = percents[index],
                RevenueAmount = lineRevenues[index], DiscountAmount = lineDiscounts[index],
                DirectExpenseAmount = lineExpenses[index], CostAmount = unit.AcquisitionCost,
                NetProfit = AllocationMath.Net(lineRevenues[index], lineDiscounts[index], lineExpenses[index], unit.AcquisitionCost)
            });
            unit.Status = UnitStatus.Sold; unit.RowVersion++;
        }
        item.UnitId = available[0].Id;
        item.UnitCostSnapshot = decimal.Round(item.Components.Sum(component => component.CostAmount), 2);
        item.NetProfit = decimal.Round(item.Components.Sum(component => component.NetProfit), 2);
    }

    /// <summary>Returns claimed units on cancellation: build parts go back to the build, single parts to stock.</summary>
    public static async Task ReleaseAsync(RetailDbContext db, StoreOrder order, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            var unitIds = item.Components.Select(component => component.UnitId).ToList();
            var units = unitIds.Count > 0
                ? await db.Units.Where(unit => unit.Status == UnitStatus.Sold && unitIds.Contains(unit.Id)).ToListAsync(cancellationToken)
                : await db.Units.Where(unit => unit.Status == UnitStatus.Sold &&
                    (unit.Id == item.UnitId || (item.AssemblyId != null && unit.AssemblyId == item.AssemblyId)))
                    .ToListAsync(cancellationToken);
            foreach (var unit in units) { unit.Status = item.AssemblyId is null ? UnitStatus.Available : UnitStatus.InAssembly; unit.RowVersion++; }
            if (item.AssemblyId is not null)
            {
                var assembly = await db.Assemblies.SingleOrDefaultAsync(a => a.Id == item.AssemblyId, cancellationToken);
                if (assembly is not null) { assembly.Status = AssemblyStatus.Published; assembly.RowVersion++; }
            }
        }
    }
}
