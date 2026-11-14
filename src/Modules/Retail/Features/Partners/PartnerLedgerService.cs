using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Partner money: earnings and adjustments lists, and cash payouts (paid against the oldest earnings first, from a money account).</summary>
public sealed class PartnerLedgerService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider, TimeZoneInfo businessZone)
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
        var method = MoneyRules.KindForMethod(request.Method);
        var reference = OrderRules.Optional(request.Reference, 128, "Reference");
        var note = OrderRules.Optional(request.Note, 500, "Note");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (settlement, partnerName) = await PartnerPayouts.PayAsync(db, partnerId, amount, method, reference, note, null, actorId, now, cancellationToken);
            await MoneyLedger.PostOutAsync(db, request.MoneyAccountId, method, new MoneyEntry(MoneySource.PartnerPayout, settlement.Id, amount,
                reference, note, BusinessDay.Of(now, businessZone), actorId, now), cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(settlement, partnerName);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    private static PartnerSettlementView Map(PartnerSettlement item, string partnerName) => new(item.Id, item.PartnerId, partnerName, item.Amount,
        item.OffsetApplied, item.Method, item.Reference, item.Note, item.CreatedAtUtc, item.ExpenseId);
}
