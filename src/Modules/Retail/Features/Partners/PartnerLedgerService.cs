using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Partner money: earnings and adjustments lists, per-partner ledger, and cash settlements (oldest earnings first).
/// </summary>
public sealed class PartnerLedgerService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<PartnerEarningView>> ListEarningsAsync(string? partner = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.PartnerEarnings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(partner)) { var value = partner.Trim(); query = query.Where(item => item.PartnerName == value); }
        return (await query.OrderByDescending(item => item.CreatedAtUtc).Take(500).ToListAsync(cancellationToken))
            .Select(item => new PartnerEarningView(item.Id, item.OrderId, item.OrderItemId, item.UnitId, item.PartnerName, item.Percent,
                item.RevenueAmount, item.DiscountAmount, item.DirectExpenseAmount, item.CostAmount, item.GrossProfit,
                item.Amount, item.PaidAmount, item.OffsetAmount, item.Status, item.ReversedAtUtc, item.CreatedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<PartnerAdjustmentView>> ListAdjustmentsAsync(string? partner = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.PartnerAdjustments.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(partner)) { var value = partner.Trim(); query = query.Where(item => item.PartnerName == value); }
        return (await query.OrderByDescending(item => item.CreatedAtUtc).Take(500).ToListAsync(cancellationToken))
            .Select(item => new PartnerAdjustmentView(item.Id, item.PartnerName, item.Amount, item.AppliedAmount,
                decimal.Round(item.Amount - item.AppliedAmount, 2), item.Status, item.Reason, item.OrderReturnId, item.CreatedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<PartnerLedgerView>> ListPartnerLedgerAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var earnings = await db.PartnerEarnings.AsNoTracking()
            .Where(item => item.Status == EarningStatus.Outstanding).ToListAsync(cancellationToken);
        var adjustments = await db.PartnerAdjustments.AsNoTracking()
            .Where(item => item.Status == AdjustmentStatus.Open).ToListAsync(cancellationToken);
        var names = earnings.Select(item => item.PartnerName).Concat(adjustments.Select(item => item.PartnerName)).Distinct();
        return names.Select(name =>
        {
            var due = earnings.Where(item => item.PartnerName == name && item.Amount > 0)
                .Sum(item => item.Amount - item.PaidAmount - item.OffsetAmount);
            var recoverable = adjustments.Where(item => item.PartnerName == name)
                .Sum(item => decimal.Round(item.Amount - item.AppliedAmount, 2));
            return new PartnerLedgerView(name, decimal.Round(due, 2), decimal.Round(due, 2), Math.Abs(decimal.Round(recoverable, 2)));
        }).OrderBy(item => item.PartnerName).ToList();
    }

    public async Task<PartnerSettlementView> SettleAsync(SettlementRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PartnerName) || request.Amount <= 0)
            throw new RetailException(400, "Partner and a positive settlement amount are required.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var name = request.PartnerName.Trim();
            var earnings = await db.PartnerEarnings
                .Where(item => item.PartnerName == name && item.Status == EarningStatus.Outstanding && item.Amount > 0)
                .OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
            var available = earnings.Sum(item => item.Amount - item.PaidAmount - item.OffsetAmount);
            if (request.Amount > available) throw new RetailException(400, "Settlement exceeds the cash due after offsets.");
            var remaining = request.Amount;
            foreach (var earning in earnings)
            {
                if (remaining <= 0) break;
                var due = earning.Amount - earning.PaidAmount - earning.OffsetAmount;
                var applied = Math.Min(due, remaining);
                earning.PaidAmount += applied; remaining -= applied;
                if (earning.PaidAmount + earning.OffsetAmount >= earning.Amount) earning.Status = EarningStatus.Paid;
            }
            var settlement = new PartnerSettlement
            {
                Id = Guid.CreateVersion7(), PartnerName = name, Amount = request.Amount,
                Method = request.Method?.Trim() is { Length: > 0 } method ? method : "Cash",
                Reference = request.Reference?.Trim(), Note = request.Note?.Trim(),
                ActorUserAccountId = actorId, CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
            };
            db.PartnerSettlements.Add(settlement);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new PartnerSettlementView(settlement.Id, settlement.PartnerName, settlement.Amount, settlement.OffsetApplied, settlement.Method, settlement.Reference, settlement.Note, settlement.CreatedAtUtc);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }
}
