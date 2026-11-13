using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Issues and voids warranties from the order and return workflows (runs inside the caller's transaction).</summary>
internal static class WarrantyRegistry
{
    /// <summary>Delivery starts the warranty of every sold unit that had one when its stock was claimed.</summary>
    public static async Task IssueForOrderAsync(RetailDbContext db, StoreOrder order, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var components = order.Items.SelectMany(item => item.Components.Where(c => c.WarrantyLabel is not null).Select(c => (Item: item, Component: c))).ToList();
        if (components.Count == 0) return;
        var unitIds = components.Select(entry => entry.Component.UnitId).ToList();
        var units = await db.Units.AsNoTracking().Include(unit => unit.StockItem)
            .Where(unit => unitIds.Contains(unit.Id)).ToDictionaryAsync(unit => unit.Id, cancellationToken);
        var existing = (await db.Warranties.Where(w => w.OrderId == order.Id).Select(w => new { w.OrderItemId, w.UnitId }).ToListAsync(cancellationToken))
            .Select(w => (w.OrderItemId, w.UnitId)).ToHashSet();
        foreach (var (item, component) in components)
        {
            if (existing.Contains((item.Id, component.UnitId))) continue;
            var unit = units[component.UnitId];
            db.Warranties.Add(new Warranty
            {
                Id = Guid.CreateVersion7(), OrderId = order.Id, OrderItemId = item.Id, UnitId = unit.Id, StockItemId = unit.StockItemId,
                Code = unit.StockItem.Code, Name = unit.Name, Condition = unit.Condition, SerialNumber = unit.SerialNumber,
                BuildCode = item.AssemblyId is null ? null : item.Code, Label = component.WarrantyLabel!, Months = component.WarrantyMonths,
                StartsAtUtc = now, EndsAtUtc = WarrantyRules.EndsAt(now, component.WarrantyMonths),
                PhoneNumber = order.PhoneNumber, UserAccountId = order.UserAccountId, CreatedAtUtc = now
            });
        }
    }

    public static async Task VoidForOrderAsync(RetailDbContext db, Guid orderId, string reason, Guid? actorId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        foreach (var warranty in await db.Warranties.Where(w => w.OrderId == orderId && w.VoidedAtUtc == null).ToListAsync(cancellationToken))
            WarrantyRules.Void(warranty, reason, actorId, now);
    }

    public static async Task VoidForUnitAsync(RetailDbContext db, Guid orderId, Guid unitId, string reason, Guid? actorId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var warranty = await db.Warranties.SingleOrDefaultAsync(w => w.OrderId == orderId && w.UnitId == unitId && w.VoidedAtUtc == null, cancellationToken);
        if (warranty is not null) WarrantyRules.Void(warranty, reason, actorId, now);
    }
}
