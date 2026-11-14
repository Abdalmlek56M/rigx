using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Partner money: earnings and adjustments lists, and cash payouts (paid against the oldest earnings first).</summary>
public sealed class PartnerLedgerService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<PartnerEarningView>> ListEarningsAsync(Guid? partnerId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await (from item in db.PartnerEarnings.AsNoTracking()
                join partner in db.Partners.AsNoTracking() on item.PartnerId equals partner.Id
                where partnerId == null || item.PartnerId == partnerId
                orderby item.CreatedAtUtc descending
                select new { item, partner.Name }).Take(500).ToListAsync(cancellationToken);
        return rows.Select(row => PartnerViews.EarningView(row.item, row.Name)).ToList();
    }

    public async Task<IReadOnlyList<PartnerAdjustmentView>> ListAdjustmentsAsync(Guid? partnerId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await (from item in db.PartnerAdjustments.AsNoTracking()
                join partner in db.Partners.AsNoTracking() on item.PartnerId equals partner.Id
                where partnerId == null || item.PartnerId == partnerId
                orderby item.CreatedAtUtc descending
                select new { item, partner.Name }).Take(500).ToListAsync(cancellationToken);
        return rows.Select(row => new PartnerAdjustmentView(row.item.Id, row.item.PartnerId, row.Name, row.item.Amount, row.item.AppliedAmount,
            decimal.Round(row.item.Amount - row.item.AppliedAmount, 2), row.item.Status, row.item.Reason, row.item.OrderReturnId,
            row.item.CreatedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<PartnerSettlementView>> ListPayoutsAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var partner = await db.Partners.AsNoTracking().SingleOrDefaultAsync(item => item.Id == partnerId, cancellationToken)
            ?? throw new RetailException(404, "The partner was not found.");
        return (await db.PartnerSettlements.AsNoTracking().Where(item => item.PartnerId == partnerId)
                .OrderByDescending(item => item.CreatedAtUtc).Take(500).ToListAsync(cancellationToken))
            .Select(item => Map(item, partner.Name)).ToList();
    }

    public async Task<PartnerSettlementView> PayoutAsync(Guid partnerId, PartnerPayoutRequest request, Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var amount = decimal.Round(request.Amount, 2);
        if (amount <= 0) throw new RetailException(400, "A positive payout amount is required.");
        var method = OrderRules.Optional(request.Method, 32, "Method") ?? "Cash";
        var reference = OrderRules.Optional(request.Reference, 128, "Reference");
        var note = OrderRules.Optional(request.Note, 500, "Note");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.LockPartnerAsync(partnerId, cancellationToken);
            var partner = await db.Partners.AsNoTracking().SingleOrDefaultAsync(item => item.Id == partnerId, cancellationToken)
                ?? throw new RetailException(404, "The partner was not found.");
            var earnings = await db.PartnerEarnings
                .Where(item => item.PartnerId == partnerId && item.Status == EarningStatus.Outstanding && item.Amount > 0)
                .OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
            var available = earnings.Sum(item => item.Amount - item.PaidAmount - item.OffsetAmount);
            if (amount > available) throw new RetailException(400, "The payout exceeds the cash due after offsets.");
            var remaining = amount;
            foreach (var earning in earnings)
            {
                if (remaining <= 0) break;
                var applied = Math.Min(earning.Amount - earning.PaidAmount - earning.OffsetAmount, remaining);
                earning.PaidAmount += applied; remaining -= applied;
                if (earning.PaidAmount + earning.OffsetAmount >= earning.Amount) earning.Status = EarningStatus.Paid;
            }
            var settlement = new PartnerSettlement
            {
                Id = Guid.CreateVersion7(), PartnerId = partnerId, Amount = amount, Method = method, Reference = reference, Note = note,
                ActorUserAccountId = actorId, CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
            };
            db.PartnerSettlements.Add(settlement);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(settlement, partner.Name);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    private static PartnerSettlementView Map(PartnerSettlement item, string partnerName) => new(item.Id, item.PartnerId, partnerName, item.Amount,
        item.OffsetApplied, item.Method, item.Reference, item.Note, item.CreatedAtUtc);
}
