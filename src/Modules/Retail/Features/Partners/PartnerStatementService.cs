using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Statement of one partner for a period: capital opening/closing and every movement (earnings, reversals, payouts, recoveries).</summary>
public sealed class PartnerStatementService(IDbContextFactory<RetailDbContext> contextFactory)
{
    public async Task<PartnerStatementView> GetAsync(Guid partnerId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (to < from) throw new RetailException(400, "The statement end date cannot precede its start date.");
        if (to > from.AddYears(1)) throw new RetailException(400, "A statement covers at most one year.");
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var partner = await db.Partners.AsNoTracking().SingleOrDefaultAsync(item => item.Id == partnerId, cancellationToken)
            ?? throw new RetailException(404, "The partner was not found.");
        var capital = await db.PartnerCapitalEntries.AsNoTracking().Where(item => item.PartnerId == partnerId && item.EntryDate <= to)
            .ToListAsync(cancellationToken);
        var earnings = await db.PartnerEarnings.AsNoTracking().Where(item => item.PartnerId == partnerId &&
            ((item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc) || (item.ReversedAtUtc >= fromUtc && item.ReversedAtUtc < toUtc)))
            .ToListAsync(cancellationToken);
        var payouts = await db.PartnerSettlements.AsNoTracking()
            .Where(item => item.PartnerId == partnerId && item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc).ToListAsync(cancellationToken);
        var adjustments = await db.PartnerAdjustments.AsNoTracking()
            .Where(item => item.PartnerId == partnerId && item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc).ToListAsync(cancellationToken);

        var lines = new List<PartnerStatementLine>();
        foreach (var entry in capital.Where(item => item.EntryDate >= from))
            lines.Add(new PartnerStatementLine(new DateTimeOffset(entry.EntryDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                $"Capital{entry.Kind}", entry.Note ?? entry.Method ?? entry.Kind,
                entry.Kind == PartnerCapitalKind.Deposit ? entry.Amount : -entry.Amount, null));
        foreach (var earning in earnings)
        {
            var reference = earning.PeriodMonth is { } month ? $"{month:yyyy-MM}" : earning.OrderId?.ToString();
            if (earning.CreatedAtUtc >= fromUtc && earning.CreatedAtUtc < toUtc)
                lines.Add(new PartnerStatementLine(earning.CreatedAtUtc, earning.PeriodMonth is null ? "Earning" : "MonthlyShare",
                    earning.PeriodMonth is null ? "Share of a sold part" : "Share of the monthly shop profit", earning.Amount, reference));
            if (earning.ReversedAtUtc is { } reversedAt && reversedAt >= fromUtc && reversedAt < toUtc)
                lines.Add(new PartnerStatementLine(reversedAt, "Reversal", earning.ReversalReason ?? "Reversed", -earning.Amount, reference));
        }
        lines.AddRange(payouts.Select(item => new PartnerStatementLine(item.CreatedAtUtc, "Payout", item.Method, -item.Amount, item.Reference)));
        lines.AddRange(adjustments.Select(item => new PartnerStatementLine(item.CreatedAtUtc, "Recovery", item.Reason, item.Amount,
            item.OrderReturnId?.ToString())));
        return new PartnerStatementView(await PartnerViews.BuildOneAsync(db, partner, cancellationToken), from, to,
            PartnerRules.CapitalBalance(capital.Where(item => item.EntryDate < from)), PartnerRules.CapitalBalance(capital),
            decimal.Round(earnings.Where(item => item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc && item.Amount > 0).Sum(item => item.Amount), 2),
            decimal.Round(payouts.Sum(item => item.Amount), 2), decimal.Round(-adjustments.Where(item => item.Amount < 0).Sum(item => item.Amount), 2),
            lines.OrderBy(line => line.At).ToList());
    }
}
