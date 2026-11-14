using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Partner earning entries written inside the Sales transactions (order completion, return inspection).</summary>
internal static class PartnerEarningsPosting
{
    /// <summary>
    /// Creates partner earnings for a completed order: every sold unit that has a financing partner earns that partner's share
    /// (unit percent, else the partner default). Explicit loss sharing becomes a recoverable balance, which is then offset.
    /// Idempotent per component/partner.
    /// </summary>
    public static async Task PostForOrderAsync(RetailDbContext db, StoreOrder order, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var unitIds = order.Items.SelectMany(item => item.Components).Select(component => component.UnitId).Distinct().ToList();
        var units = await db.Units.AsNoTracking().Where(unit => unitIds.Contains(unit.Id) && unit.PartnerId != null)
            .ToDictionaryAsync(unit => unit.Id, cancellationToken);
        if (units.Count == 0) return;
        var partnerIds = units.Values.Select(unit => unit.PartnerId!.Value).Distinct().ToList();
        var partners = await db.Partners.AsNoTracking().Where(partner => partnerIds.Contains(partner.Id)).ToDictionaryAsync(partner => partner.Id, cancellationToken);
        var existing = (await db.PartnerEarnings.AsNoTracking()
                .Where(earning => earning.OrderId == order.Id)
                .Select(earning => new { earning.OrderItemComponentId, earning.PartnerId })
                .ToListAsync(cancellationToken))
            .Select(entry => (entry.OrderItemComponentId, entry.PartnerId))
            .ToHashSet();
        var added = new List<PartnerEarning>();
        foreach (var item in order.Items)
        {
            foreach (var component in item.Components)
            {
                if (!units.TryGetValue(component.UnitId, out var unit)) continue;
                var partner = partners[unit.PartnerId!.Value];
                var percent = PartnerRules.EffectivePercent(unit.PartnerPercentOverride, partner.DefaultPercent);
                if (percent <= 0 || !existing.Add((component.Id, partner.Id))) continue;
                var profit = decimal.Round(component.NetProfit, 2);
                added.Add(new PartnerEarning
                {
                    Id = Guid.CreateVersion7(), PartnerId = partner.Id, OrderItemId = item.Id, OrderItemComponentId = component.Id,
                    UnitId = component.UnitId, OrderId = order.Id, Percent = percent, LossSharingEnabled = partner.LossSharingEnabled,
                    RevenueAmount = component.RevenueAmount, DiscountAmount = component.DiscountAmount,
                    DirectExpenseAmount = component.DirectExpenseAmount, CostAmount = component.CostAmount,
                    GrossProfit = profit, Amount = PartnerRules.Share(profit, percent, partner.LossSharingEnabled),
                    Status = EarningStatus.Outstanding, CreatedAtUtc = now
                });
            }
        }
        await SaveAndApplyAsync(db, added, now, cancellationToken);
    }

    /// <summary>Adds new earnings (losses turned into recoverable balances), saves, then offsets the recoverable balances.</summary>
    public static async Task SaveAndApplyAsync(RetailDbContext db, IReadOnlyList<PartnerEarning> added, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        db.PartnerEarnings.AddRange(added);
        foreach (var loss in added.Where(item => item.Amount < 0))
        {
            db.PartnerAdjustments.Add(new PartnerAdjustment
            {
                Id = Guid.CreateVersion7(), PartnerId = loss.PartnerId, PartnerEarningId = loss.Id,
                Amount = loss.Amount, AppliedAmount = 0, Status = AdjustmentStatus.Open,
                Reason = "Loss sharing converted to recoverable partner balance", CreatedAtUtc = now
            });
            loss.Status = EarningStatus.Reversed;
            loss.ReversedAtUtc = now;
            loss.ReversalReason = "Converted to recoverable balance under explicit loss sharing";
        }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var partnerId in added.Select(item => item.PartnerId).Distinct())
            await ApplyRecoverableAsync(db, partnerId, cancellationToken);
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
        var touchedPartners = new HashSet<Guid>();
        foreach (var earning in earnings.Where(entry => entry.Status != EarningStatus.Reversed))
        {
            var settled = decimal.Round(earning.PaidAmount + earning.OffsetAmount, 2);
            if (settled > 0)
            {
                db.PartnerAdjustments.Add(new PartnerAdjustment
                {
                    Id = Guid.CreateVersion7(), PartnerId = earning.PartnerId,
                    PartnerEarningId = earning.Id, OrderReturnId = orderReturn.Id,
                    Amount = -settled, AppliedAmount = 0, Status = AdjustmentStatus.Open,
                    Reason = $"Return recovery for {item.Name}", CreatedAtUtc = now
                });
            }
            earning.Status = EarningStatus.Reversed;
            earning.ReversedAtUtc = now;
            earning.ReversalReason = $"Order return {orderReturn.Id} ({condition})";
            touchedPartners.Add(earning.PartnerId);
        }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var partnerId in touchedPartners)
            await ApplyRecoverableAsync(db, partnerId, cancellationToken);
    }

    private static async Task ApplyRecoverableAsync(RetailDbContext db, Guid partnerId, CancellationToken cancellationToken)
    {
        var adjustments = await db.PartnerAdjustments
            .Where(item => item.PartnerId == partnerId && item.Status == AdjustmentStatus.Open && item.Amount < 0)
            .OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
        if (adjustments.Count == 0) return;
        var earnings = await db.PartnerEarnings
            .Where(item => item.PartnerId == partnerId && item.Status == EarningStatus.Outstanding && item.Amount > 0)
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
