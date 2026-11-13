using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Partner earning entries written inside the Sales transactions (order completion, return inspection).</summary>
internal static class PartnerEarningsPosting
{
    /// <summary>
    /// Creates partner earnings for a completed order (unit, product or category allocation, else the active agreement),
    /// turns explicit loss sharing into recoverable balances, then offsets those balances. Idempotent per component/partner.
    /// </summary>
    public static async Task PostForOrderAsync(RetailDbContext db, StoreOrder order, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var orderDate = DateOnly.FromDateTime(order.CreatedAtUtc.UtcDateTime);
        var agreement = await db.PartnerAgreements.AsNoTracking()
            .Where(item => item.IsActive && item.EffectiveFrom <= orderDate && (item.EffectiveTo == null || item.EffectiveTo >= orderDate))
            .OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var allocations = await db.PartnerUnitAllocations.AsNoTracking()
            .Where(item => item.IsActive).ToListAsync(cancellationToken);
        var unitIds = order.Items.SelectMany(item => item.Components).Select(component => component.UnitId).Distinct().ToList();
        var units = await db.Units.AsNoTracking().Where(unit => unitIds.Contains(unit.Id))
            .ToDictionaryAsync(unit => unit.Id, cancellationToken);
        var existing = (await db.PartnerEarnings.AsNoTracking()
                .Where(earning => earning.OrderId == order.Id)
                .Select(earning => new { earning.OrderItemComponentId, earning.PartnerName })
                .ToListAsync(cancellationToken))
            .Select(entry => (entry.OrderItemComponentId, entry.PartnerName))
            .ToHashSet();
        var touchedPartners = new HashSet<string>(StringComparer.Ordinal);

        void AddEarning(OrderItem item, OrderItemComponent component, string partnerName, decimal percent, bool lossSharing)
        {
            if (string.IsNullOrWhiteSpace(partnerName) || percent <= 0) return;
            if (!existing.Add((component.Id, partnerName))) return;
            var profit = decimal.Round(component.NetProfit, 2);
            var amount = decimal.Round(profit * percent / 100m, 2);
            if (amount < 0 && !lossSharing) amount = 0;
            db.PartnerEarnings.Add(new PartnerEarning
            {
                Id = Guid.CreateVersion7(), OrderItemId = item.Id, OrderItemComponentId = component.Id,
                UnitId = component.UnitId, OrderId = order.Id, PartnerName = partnerName,
                Percent = percent, LossSharingEnabled = lossSharing,
                RevenueAmount = component.RevenueAmount, DiscountAmount = component.DiscountAmount,
                DirectExpenseAmount = component.DirectExpenseAmount, CostAmount = component.CostAmount,
                GrossProfit = profit, Amount = amount, PaidAmount = 0, OffsetAmount = 0,
                Status = EarningStatus.Outstanding, CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
            });
            touchedPartners.Add(partnerName);
        }

        foreach (var item in order.Items)
        {
            foreach (var component in item.Components)
            {
                if (!units.TryGetValue(component.UnitId, out var unit)) continue;
                var scoped = allocations.Where(allocation => allocation.UnitId == component.UnitId).ToList();
                if (scoped.Count == 0 && unit.ProductId is not null)
                    scoped = allocations.Where(allocation => allocation.UnitId == null && allocation.ProductId == unit.ProductId).ToList();
                if (scoped.Count == 0 && !string.IsNullOrWhiteSpace(unit.Category))
                    scoped = allocations.Where(allocation => allocation.UnitId == null && allocation.ProductId == null &&
                        allocation.Category == unit.Category).ToList();
                if (scoped.Count > 0)
                {
                    if (scoped.Sum(allocation => allocation.Percent) > 100.01m)
                        throw new RetailException(409, "Partner allocations for a sold unit exceed 100%.");
                    foreach (var allocation in scoped)
                        AddEarning(item, component, allocation.PartnerName, allocation.Percent, allocation.LossSharingEnabled);
                    continue;
                }
                if (agreement is null) continue;
                AddEarning(item, component, agreement.PartnerName,
                    unit.PartnerPercentOverride ?? agreement.DefaultPercent, agreement.LossSharingEnabled);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        var losses = await db.PartnerEarnings
            .Where(item => item.OrderId == order.Id && item.Amount < 0 && item.Status == EarningStatus.Outstanding)
            .ToListAsync(cancellationToken);
        foreach (var loss in losses)
        {
            db.PartnerAdjustments.Add(new PartnerAdjustment
            {
                Id = Guid.CreateVersion7(), PartnerName = loss.PartnerName, PartnerEarningId = loss.Id,
                Amount = loss.Amount, AppliedAmount = 0, Status = AdjustmentStatus.Open,
                Reason = "Loss sharing converted to recoverable partner balance",
                CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
            });
            loss.Status = EarningStatus.Reversed;
            loss.ReversedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
            loss.ReversalReason = "Converted to recoverable balance under explicit loss sharing";
            touchedPartners.Add(loss.PartnerName);
        }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var partnerName in touchedPartners)
            await ApplyRecoverableAsync(db, partnerName, cancellationToken);
    }

    /// <summary>
    /// Reverses the earnings of a returned unit. Amounts already paid or offset become a recoverable
    /// (negative) adjustment, which is then offset against the partner's outstanding earnings.
    /// </summary>
    public static async Task ReverseForReturnAsync(RetailDbContext db, OrderReturn orderReturn, OrderItem item, Guid unitId,
        string condition, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var earnings = await db.PartnerEarnings
            .Where(entry => entry.OrderItemId == item.Id && entry.UnitId == unitId)
            .ToListAsync(cancellationToken);
        var touchedPartners = new HashSet<string>(StringComparer.Ordinal);
        foreach (var earning in earnings.Where(entry => entry.Status != EarningStatus.Reversed))
        {
            var settled = decimal.Round(earning.PaidAmount + earning.OffsetAmount, 2);
            if (settled > 0)
            {
                db.PartnerAdjustments.Add(new PartnerAdjustment
                {
                    Id = Guid.CreateVersion7(), PartnerName = earning.PartnerName,
                    PartnerEarningId = earning.Id, OrderReturnId = orderReturn.Id,
                    Amount = -settled, AppliedAmount = 0, Status = AdjustmentStatus.Open,
                    Reason = $"Return recovery for {item.Name}", CreatedAtUtc = now
                });
            }
            earning.Status = EarningStatus.Reversed;
            earning.ReversedAtUtc = now;
            earning.ReversalReason = $"Order return {orderReturn.Id} ({condition})";
            touchedPartners.Add(earning.PartnerName);
        }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var partnerName in touchedPartners)
            await ApplyRecoverableAsync(db, partnerName, cancellationToken);
    }

    private static async Task ApplyRecoverableAsync(RetailDbContext db, string partnerName, CancellationToken cancellationToken)
    {
        var adjustments = await db.PartnerAdjustments
            .Where(item => item.PartnerName == partnerName && item.Status == AdjustmentStatus.Open && item.Amount < 0)
            .OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
        if (adjustments.Count == 0) return;
        var earnings = await db.PartnerEarnings
            .Where(item => item.PartnerName == partnerName && item.Status == EarningStatus.Outstanding && item.Amount > 0)
            .OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
        foreach (var adjustment in adjustments)
        {
            var remaining = decimal.Round(-(adjustment.Amount - adjustment.AppliedAmount), 2);
            foreach (var earning in earnings)
            {
                if (remaining <= 0) break;
                var due = decimal.Round(earning.Amount - earning.PaidAmount - earning.OffsetAmount, 2);
                if (due <= 0) continue;
                var offset = Math.Min(due, remaining);
                earning.OffsetAmount += offset;
                adjustment.AppliedAmount -= offset;
                remaining -= offset;
                if (earning.PaidAmount + earning.OffsetAmount >= earning.Amount) earning.Status = EarningStatus.Paid;
            }
            if (decimal.Round(adjustment.Amount - adjustment.AppliedAmount, 2) >= 0) adjustment.Status = AdjustmentStatus.Applied;
        }
    }
}
