using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Pays a partner against the oldest outstanding earnings first; shared by cash payouts and expenses paid from partner money.</summary>
internal static class PartnerPayouts
{
    /// <summary>
    /// Locks the partner, applies the amount to the oldest earnings (400 when it is more than the cash due after offsets) and adds the
    /// settlement to the context. The caller must be inside a transaction and saves.
    /// </summary>
    public static async Task<(PartnerSettlement Settlement, string PartnerName)> PayAsync(RetailDbContext db, Guid partnerId, decimal amount,
        string method, string? reference, string? note, Guid? expenseId, Guid actorId, DateTimeOffset now, CancellationToken cancellationToken)
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
            ExpenseId = expenseId, ActorUserAccountId = actorId, CreatedAtUtc = now
        };
        db.PartnerSettlements.Add(settlement);
        return (settlement, partner.Name);
    }
}
