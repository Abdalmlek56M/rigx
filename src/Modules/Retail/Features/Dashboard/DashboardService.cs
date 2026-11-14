using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// The owner's dashboard: money KPIs for a period against the previous period of the same length, the shop state now,
/// a 30-day chart, what is waiting for action and the sales breakdowns. Sales use the same definition as the business report
/// (completed orders by their last update).
/// </summary>
public sealed class DashboardService(IDbContextFactory<RetailDbContext> contextFactory, BusinessReportService reports,
    RecurringExpenseService recurring, TimeProvider timeProvider)
{
    private const int ChartDays = 30;

    public async Task<DashboardView> BuildAsync(DateOnly from, DateOnly to, TimeZoneInfo zone, CancellationToken cancellationToken = default)
    {
        if (to < from) throw new RetailException(400, "Dashboard end date cannot precede its start date.");
        var previousTo = from.AddDays(-1);
        var previousFrom = previousTo.AddDays(from.DayNumber - to.DayNumber);
        var current = Kpis(await reports.BuildReportAsync(from, to, cancellationToken));
        var previous = Kpis(await reports.BuildReportAsync(previousFrom, previousTo, cancellationToken));
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var recurringDue = await recurring.CountDueAsync(zone, cancellationToken);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var orders = DashboardBreakdowns.CompletedOrders(db, from, to, zone);
        var (categories, conditions, daysToSellUsed) = await DashboardBreakdowns.ByComponentAsync(db, orders, cancellationToken);
        return new DashboardView(from, to, previousFrom, previousTo, current, previous, Changes(current, previous),
            await DashboardShopState.SnapshotAsync(db, cancellationToken), await DailySeriesAsync(db, to, zone, cancellationToken),
            await DashboardShopState.TodoAsync(db, recurringDue, cancellationToken),
            await DashboardBreakdowns.BySourceAsync(orders, order => order.LeadSource, cancellationToken),
            await DashboardBreakdowns.BySourceAsync(orders, order => order.Usage, cancellationToken),
            categories, conditions, await DashboardBreakdowns.BuildSalesAsync(orders, cancellationToken), daysToSellUsed,
            await DashboardShopState.StockAgingAsync(db, now, cancellationToken),
            await DashboardShopState.PartnerProfitsThisMonthAsync(db, now, zone, cancellationToken));
    }

    private static DashboardKpis Kpis(BusinessReportView report) => new(report.GrossSales,
        decimal.Round(report.StoreNetResult - report.PartnerAccrued, 2), report.CompletedOrders,
        report.CompletedOrders == 0 ? 0 : decimal.Round(report.GrossSales / report.CompletedOrders, 2));

    private static DashboardKpiChanges Changes(DashboardKpis current, DashboardKpis previous) => new(
        Change(current.GrossSales, previous.GrossSales), Change(current.NetProfit, previous.NetProfit),
        Change(current.Orders, previous.Orders), Change(current.AverageOrder, previous.AverageOrder));

    private static decimal? Change(decimal current, decimal previous) =>
        previous == 0 ? null : decimal.Round((current - previous) / Math.Abs(previous) * 100, 1);

    private static async Task<IReadOnlyList<DashboardDay>> DailySeriesAsync(RetailDbContext db, DateOnly to, TimeZoneInfo zone, CancellationToken cancellationToken)
    {
        var first = to.AddDays(1 - ChartDays);
        var orders = DashboardBreakdowns.CompletedOrders(db, first, to, zone);
        var rows = await orders.Select(order => new
        {
            order.UpdatedAtUtc, order.Total,
            Net = order.Items.SelectMany(item => item.Components).Sum(component => component.NetProfit)
        }).ToListAsync(cancellationToken);
        var byDay = rows.GroupBy(row => BusinessDay.Of(row.UpdatedAtUtc, zone)).ToDictionary(group => group.Key);
        return Enumerable.Range(0, ChartDays).Select(offset =>
        {
            var date = first.AddDays(offset);
            return byDay.TryGetValue(date, out var day)
                ? new DashboardDay(date, decimal.Round(day.Sum(row => row.Total), 2), decimal.Round(day.Sum(row => row.Net), 2))
                : new DashboardDay(date, 0, 0);
        }).ToList();
    }
}
