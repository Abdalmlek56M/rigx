using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Builds partner views with the numbers the list needs: capital balance, cash due, recoverable and unsold financed units.</summary>
internal static class PartnerViews
{
    public static async Task<List<PartnerView>> BuildAsync(RetailDbContext db, IReadOnlyList<Partner> partners, CancellationToken cancellationToken)
    {
        var ids = partners.Select(partner => partner.Id).ToList();
        var capital = (await db.PartnerCapitalEntries.AsNoTracking().Where(entry => ids.Contains(entry.PartnerId)).ToListAsync(cancellationToken))
            .GroupBy(entry => entry.PartnerId).ToDictionary(group => group.Key, group => PartnerRules.CapitalBalance(group));
        var due = await db.PartnerEarnings.AsNoTracking()
            .Where(item => ids.Contains(item.PartnerId) && item.Status == EarningStatus.Outstanding && item.Amount > 0)
            .GroupBy(item => item.PartnerId)
            .Select(group => new { group.Key, Value = group.Sum(item => item.Amount - item.PaidAmount - item.OffsetAmount) })
            .ToDictionaryAsync(item => item.Key, item => item.Value, cancellationToken);
        var recoverable = await db.PartnerAdjustments.AsNoTracking()
            .Where(item => ids.Contains(item.PartnerId) && item.Status == AdjustmentStatus.Open)
            .GroupBy(item => item.PartnerId)
            .Select(group => new { group.Key, Value = group.Sum(item => item.Amount - item.AppliedAmount) })
            .ToDictionaryAsync(item => item.Key, item => item.Value, cancellationToken);
        var unsold = await db.Units.AsNoTracking()
            .Where(unit => unit.PartnerId != null && ids.Contains(unit.PartnerId.Value) && unit.Status != UnitStatus.Sold && unit.Status != UnitStatus.Scrapped)
            .GroupBy(unit => unit.PartnerId!.Value)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);
        return partners.Select(partner => new PartnerView(partner.Id, partner.Name, partner.Phone, partner.JoinDate, partner.DefaultPercent,
            partner.Basis, partner.LossSharingEnabled, partner.IsActive, partner.Notes, capital.GetValueOrDefault(partner.Id),
            decimal.Round(due.GetValueOrDefault(partner.Id), 2), Math.Abs(decimal.Round(recoverable.GetValueOrDefault(partner.Id), 2)),
            unsold.GetValueOrDefault(partner.Id), partner.RowVersion)).ToList();
    }

    public static PartnerEarningView EarningView(PartnerEarning item, string partnerName) => new(item.Id, item.PartnerId, partnerName,
        item.OrderId, item.OrderItemId, item.UnitId, item.PeriodMonth, item.Percent, item.RevenueAmount, item.DiscountAmount,
        item.DirectExpenseAmount, item.CostAmount, item.GrossProfit, item.Amount, item.PaidAmount, item.OffsetAmount, item.Status,
        item.ReversedAtUtc, item.CreatedAtUtc);

    public static async Task<PartnerView> BuildOneAsync(RetailDbContext db, Partner partner, CancellationToken cancellationToken) =>
        (await BuildAsync(db, [partner], cancellationToken)).Single();
}
