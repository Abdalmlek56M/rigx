using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Stock claim/release used by the order status workflow (runs inside the caller's transaction).</summary>
internal static class OrderStockAllocator
{
    /// <summary>
    /// Claims stock for every line at confirmation: whole builds, parts sold out of a build (split sale) and stock lines
    /// (oldest available units, FOR UPDATE SKIP LOCKED). Claimed units become Sold with per-unit revenue/cost components.
    /// </summary>
    public static async Task ClaimAsync(RetailDbContext db, StoreOrder order, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items.Where(item => item.AssemblyId is not null && item.UnitId is null))
            await ClaimBuildAsync(db, item, cancellationToken);
        foreach (var group in order.Items.Where(item => item.AssemblyId is not null && item.UnitId is not null).GroupBy(item => item.AssemblyId!.Value))
            await ClaimSplitPartsAsync(db, group.Key, group.ToList(), cancellationToken);
        foreach (var item in order.Items.Where(item => item.AssemblyId is null))
            await ClaimStockLineAsync(db, item, cancellationToken);
    }

    private static async Task<Assembly> LockBuildAsync(RetailDbContext db, Guid assemblyId, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1 FROM assemblies WHERE id = {0} FOR UPDATE", [assemblyId], cancellationToken);
        var assembly = await db.Assemblies.WithUnits().SingleOrDefaultAsync(a => a.Id == assemblyId, cancellationToken);
        if (assembly is null || assembly.Status is not (AssemblyStatus.Published or AssemblyStatus.Draft))
            throw new RetailException(409, "The selected build is no longer available.");
        if (assembly.Units.Any(link => link.Unit.Status is not (UnitStatus.InAssembly or UnitStatus.Available)))
            throw new RetailException(409, "One or more build components are no longer available.");
        return assembly;
    }

    private static async Task ClaimBuildAsync(RetailDbContext db, OrderItem item, CancellationToken cancellationToken)
    {
        var assembly = await LockBuildAsync(db, item.AssemblyId!.Value, cancellationToken);
        if (assembly.Units.Count == 0 || !AllocationMath.SumsToHundred(assembly.Units.Select(link => link.AllocationPercent)))
            throw new RetailException(409, "The build allocation is incomplete.");
        var links = assembly.Units.OrderBy(link => link.UnitId).ToList();
        var percents = links.Select(link => link.AllocationPercent).ToList();
        AddComponents(item, links.Select(link => link.Unit).ToList(), percents,
            links.Select(link => AssemblyRules.EffectiveWarranty(link, assembly, link.Unit.StockItem)).ToList());
        if (assembly.Copies > 1) await RefillCopyAsync(db, assembly, links, cancellationToken);
        else assembly.Status = AssemblyStatus.Sold;
        assembly.RowVersion++;
    }

    /// <summary>
    /// One copy of a multi-copy build was sold (owner decision, 13 Nov 2026): the sold parts leave the build and each is
    /// replaced by an available unit of the same RX item, keeping its price, warranty and share. A part with no
    /// replacement stays out and the build goes back to Draft for the owner to complete.
    /// </summary>
    private static async Task RefillCopyAsync(RetailDbContext db, Assembly assembly, List<AssemblyUnit> soldLinks, CancellationToken cancellationToken)
    {
        var missing = false;
        foreach (var sold in soldLinks)
        {
            assembly.Units.Remove(sold);
            db.AssemblyUnits.Remove(sold);
            sold.Unit.AssemblyId = null;
            var replacementId = await db.Database.SqlQuery<Guid>(
                $"SELECT id AS \"Value\" FROM inventory_units WHERE stock_item_id = {sold.Unit.StockItemId} AND status = 'Available' ORDER BY received_at_utc LIMIT 1 FOR UPDATE SKIP LOCKED")
                .ToListAsync(cancellationToken);
            if (replacementId.Count == 0) { missing = true; continue; }
            var unit = await db.Units.Include(entry => entry.StockItem).SingleAsync(entry => entry.Id == replacementId[0], cancellationToken);
            var link = AssemblyEditing.Link(assembly, unit, sold.AllocationPercent);
            link.PartPrice = sold.PartPrice;
            link.WarrantyLabel = sold.WarrantyLabel;
            link.WarrantyMonths = sold.WarrantyMonths;
        }
        assembly.Copies--;
        if (!missing) return;
        AssemblyEditing.ResetAllocations(assembly);
        assembly.Status = AssemblyStatus.Draft;
    }

    /// <summary>Split sale: the chosen parts are sold at their line prices, the rest return to stock and the build closes.</summary>
    private static async Task ClaimSplitPartsAsync(RetailDbContext db, Guid assemblyId, List<OrderItem> items, CancellationToken cancellationToken)
    {
        var assembly = await LockBuildAsync(db, assemblyId, cancellationToken);
        if (assembly.SellMode != AssemblySellMode.FullOrSplit)
            throw new RetailException(409, "This build is sold as a whole only.");
        foreach (var item in items)
        {
            var link = assembly.Units.SingleOrDefault(entry => entry.UnitId == item.UnitId)
                ?? throw new RetailException(409, "A selected part is no longer in the build.");
            AddComponents(item, [link.Unit], [100m], [AssemblyRules.EffectiveWarranty(link, assembly, link.Unit.StockItem)]);
        }
        foreach (var link in assembly.Units.ToList())
        {
            if (link.Unit.Status != UnitStatus.Sold) { link.Unit.Status = UnitStatus.Available; link.Unit.RowVersion++; }
            link.Unit.AssemblyId = null;
            AssemblyEditing.Unlink(db, assembly, link);
        }
        assembly.Status = AssemblyStatus.Disassembled;
        assembly.RowVersion++;
    }

    /// <summary>Claims the oldest available units of a stock item (its lot).</summary>
    private static async Task ClaimStockLineAsync(RetailDbContext db, OrderItem item, CancellationToken cancellationToken)
    {
        if (item.StockItemId is not Guid stockItemId)
            throw new RetailException(409, "A stock line is missing its item reference.");
        var lockedIds = await db.Database.SqlQuery<Guid>(
                $"SELECT id AS \"Value\" FROM inventory_units WHERE stock_item_id = {stockItemId} AND status = 'Available' ORDER BY received_at_utc LIMIT {item.Quantity} FOR UPDATE SKIP LOCKED")
            .ToListAsync(cancellationToken);
        var available = lockedIds.Count == item.Quantity
            ? await db.Units.Include(unit => unit.StockItem).Where(unit => lockedIds.Contains(unit.Id) && unit.Status == UnitStatus.Available).ToListAsync(cancellationToken)
            : [];
        if (available.Count != item.Quantity)
            throw new RetailException(409, "Not enough available stock to confirm this order.");
        AddComponents(item, available, AllocationMath.EqualPercentages(available.Count),
            available.Select(unit => (unit.StockItem.WarrantyLabel, unit.StockItem.WarrantyMonths)).ToList());
        item.UnitId = available[0].Id;
    }

    /// <summary>Records what each unit earned (revenue, discount, direct expense split by share) and its warranty as sold, and marks it Sold.</summary>
    private static void AddComponents(OrderItem item, IReadOnlyList<InventoryUnit> units, IReadOnlyList<decimal> percents,
        IReadOnlyList<(string? Label, int? Months)> warranties)
    {
        var revenues = AllocationMath.AllocateAmount(item.LineTotal, percents);
        var discounts = AllocationMath.AllocateAmount(item.DiscountAmount, percents);
        var expenses = AllocationMath.AllocateAmount(item.DirectExpenseAmount, percents);
        for (var index = 0; index < units.Count; index++)
        {
            var unit = units[index];
            item.Components.Add(new OrderItemComponent
            {
                Id = Guid.CreateVersion7(), OrderItemId = item.Id, UnitId = unit.Id, AllocationPercent = percents[index],
                RevenueAmount = revenues[index], DiscountAmount = discounts[index], DirectExpenseAmount = expenses[index],
                CostAmount = unit.AcquisitionCost, WarrantyLabel = warranties[index].Label, WarrantyMonths = warranties[index].Months,
                NetProfit = AllocationMath.Net(revenues[index], discounts[index], expenses[index], unit.AcquisitionCost)
            });
            unit.Status = UnitStatus.Sold; unit.RowVersion++;
        }
        item.UnitCostSnapshot = decimal.Round(item.Components.Sum(component => component.CostAmount), 2);
        item.NetProfit = decimal.Round(item.Components.Sum(component => component.NetProfit), 2);
    }

    /// <summary>
    /// Returns claimed units on cancellation: parts still linked to their build go back into it (and the build is
    /// published again); everything else (stock lines, split parts, parts of a refilled copy) goes back to stock.
    /// </summary>
    public static async Task ReleaseAsync(RetailDbContext db, StoreOrder order, CancellationToken cancellationToken)
    {
        var unitIds = order.Items.SelectMany(item => item.Components).Select(component => component.UnitId).ToList();
        var units = await db.Units.Where(unit => unit.Status == UnitStatus.Sold && unitIds.Contains(unit.Id)).ToListAsync(cancellationToken);
        foreach (var unit in units)
        {
            unit.Status = unit.AssemblyId is null ? UnitStatus.Available : UnitStatus.InAssembly;
            unit.RowVersion++;
        }
        var buildIds = units.Where(unit => unit.AssemblyId is not null).Select(unit => unit.AssemblyId!.Value).Distinct().ToList();
        foreach (var assembly in await db.Assemblies.Where(a => buildIds.Contains(a.Id) && a.Status == AssemblyStatus.Sold).ToListAsync(cancellationToken))
        {
            assembly.Status = AssemblyStatus.Published;
            assembly.RowVersion++;
        }
    }
}
