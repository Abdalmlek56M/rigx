using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Posts the monthly share of whole-shop partners by hand after the month ended: each active whole-shop partner earns its percent of
/// (store net result after expenses minus what financed partners earned in the month). Once per partner and month.
/// </summary>
public sealed class PartnerMonthCloseService(IDbContextFactory<RetailDbContext> contextFactory, BusinessReportService reports, TimeProvider timeProvider)
{
    public async Task<PartnerMonthCloseView> CloseMonthAsync(PartnerMonthCloseRequest request, TimeZoneInfo businessZone,
        CancellationToken cancellationToken = default)
    {
        var monthStart = PartnerRules.MonthStart(request.Year, request.Month);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        PartnerRules.EnsureMonthEnded(monthStart, DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, businessZone).DateTime));
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var storeNet = (await reports.BuildReportAsync(monthStart, monthEnd, cancellationToken)).StoreNetResult;
        var fromUtc = new DateTimeOffset(monthStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = fromUtc.AddMonths(1);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var financed = decimal.Round(await db.PartnerEarnings.AsNoTracking()
                .Where(item => item.OrderId != null && item.Amount > 0 && item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc)
                .SumAsync(item => item.Amount, cancellationToken), 2);
            var monthBase = PartnerRules.WholeShopBase(storeNet, financed);
            var posted = await db.PartnerEarnings.AsNoTracking().Where(item => item.PeriodMonth == monthStart)
                .Select(item => item.PartnerId).ToListAsync(cancellationToken);
            var partners = await db.Partners.AsNoTracking()
                .Where(item => item.IsActive && item.Basis == PartnerBasis.WholeShop && item.JoinDate <= monthEnd && !posted.Contains(item.Id))
                .OrderBy(item => item.Name).ToListAsync(cancellationToken);
            var added = partners.Select(partner => new PartnerEarning
            {
                Id = Guid.CreateVersion7(), PartnerId = partner.Id, PeriodMonth = monthStart, Percent = partner.DefaultPercent,
                LossSharingEnabled = partner.LossSharingEnabled, GrossProfit = monthBase,
                Amount = PartnerRules.Share(monthBase, partner.DefaultPercent, partner.LossSharingEnabled),
                Status = EarningStatus.Outstanding, CreatedAtUtc = now
            }).ToList();
            try { await PartnerEarningsPosting.SaveAndApplyAsync(db, added, now, cancellationToken); }
            catch (DbUpdateException exception) when (exception.IsUniqueViolation())
            {
                throw new RetailException(409, "This month was already posted for a partner.");
            }
            await transaction.CommitAsync(cancellationToken);
            var names = partners.ToDictionary(partner => partner.Id, partner => partner.Name);
            return new PartnerMonthCloseView(monthStart, storeNet, financed, monthBase,
                added.Select(item => PartnerViews.EarningView(item, names[item.PartnerId])).ToList());
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }
}
