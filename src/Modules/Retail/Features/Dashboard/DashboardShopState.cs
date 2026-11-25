using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Dashboard figures about the shop right now: money, stock, to-do counts and this month's partner earnings.</summary>
internal static class DashboardShopState
{
    private const int AgingDays = 30;
    private const int AgingListSize = 20;
    private static readonly string[] OnTheWay = [OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Shipped, OrderStatus.Delivered];
    private static readonly string[] Finished = [OrderStatus.Cancelled, OrderStatus.Completed, OrderStatus.Delivered];
    private static readonly string[] InStock = [UnitStatus.Available, UnitStatus.InAssembly];

    public static async Task<DashboardSnapshot> SnapshotAsync(RetailDbContext db, CancellationToken cancellationToken)
    {
        var stock = await db.Units.AsNoTracking().Where(unit => InStock.Contains(unit.Status))
            .Where(StockValueRules.Counted).SumAsync(unit => unit.AcquisitionCost, cancellationToken);
        var dues = await db.PartnerEarnings.AsNoTracking().Where(item => item.Status == EarningStatus.Outstanding && item.Amount > 0)
            .SumAsync(item => item.Amount - item.PaidAmount - item.OffsetAmount, cancellationToken);
        var accounts = await db.MoneyAccounts.AsNoTracking().Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder).ThenBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
        var balances = new List<DashboardAccountBalance>();
        foreach (var account in accounts)
            balances.Add(new DashboardAccountBalance(account.Id, account.Name, account.Kind, await MoneyLedger.BalanceAsync(db, account, cancellationToken)));
        return new DashboardSnapshot(decimal.Round(stock, 2), decimal.Round(dues, 2), decimal.Round(balances.Sum(item => item.Balance), 2), balances);
    }

    public static async Task<DashboardTodo> TodoAsync(RetailDbContext db, int recurringExpensesDue, CancellationToken cancellationToken)
    {
        var orders = db.Orders.AsNoTracking();
        return new DashboardTodo(
            await orders.CountAsync(order => order.Status == OrderStatus.New, cancellationToken),
            await orders.CountAsync(order => OnTheWay.Contains(order.Status)
                && (order.PaymentStatus == PaymentStatus.Unpaid || order.PaymentStatus == PaymentStatus.PendingVerification), cancellationToken),
            await orders.CountAsync(order => order.ReservationExpiredAtUtc != null && !Finished.Contains(order.Status), cancellationToken),
            await db.OrderReturns.AsNoTracking().CountAsync(item => item.Status == ReturnStatus.PendingInspection, cancellationToken),
            recurringExpensesDue);
    }

    /// <summary>Units still in stock (or in a build) that were received more than 30 days ago, with the oldest ones listed.</summary>
    public static async Task<DashboardStockAging> StockAgingAsync(RetailDbContext db, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var threshold = now.AddDays(-AgingDays);
        var aged = db.Units.AsNoTracking().Where(unit => InStock.Contains(unit.Status) && unit.ReceivedAtUtc < threshold);
        var count = await aged.CountAsync(cancellationToken);
        var total = await aged.Where(StockValueRules.Counted).SumAsync(unit => unit.AcquisitionCost, cancellationToken);
        var oldest = await aged.OrderBy(unit => unit.ReceivedAtUtc).Take(AgingListSize)
            .Select(unit => new { unit.Id, unit.StockItem.Code, unit.Name, unit.Condition, unit.AcquisitionCost, unit.ReceivedAtUtc })
            .ToListAsync(cancellationToken);
        return new DashboardStockAging(count, decimal.Round(total, 2), oldest.Select(unit => new DashboardAgingUnit(unit.Id, unit.Code,
            unit.Name, unit.Condition, unit.AcquisitionCost, (int)(now - unit.ReceivedAtUtc).TotalDays)).ToList());
    }

    public static async Task<IReadOnlyList<DashboardPartnerProfit>> PartnerProfitsThisMonthAsync(RetailDbContext db, DateTimeOffset now,
        TimeZoneInfo zone, CancellationToken cancellationToken)
    {
        var local = TimeZoneInfo.ConvertTime(now, zone).DateTime;
        var start = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var fromUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(start, zone), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(start.AddMonths(1), zone), TimeSpan.Zero);
        var earned = await db.PartnerEarnings.AsNoTracking()
            .Where(item => item.Amount > 0 && item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc)
            .GroupBy(item => item.PartnerId).Select(group => new { PartnerId = group.Key, Earned = group.Sum(item => item.Amount) })
            .ToListAsync(cancellationToken);
        var ids = earned.Select(item => item.PartnerId).ToList();
        var names = await db.Partners.AsNoTracking().Where(partner => ids.Contains(partner.Id))
            .ToDictionaryAsync(partner => partner.Id, partner => partner.Name, cancellationToken);
        return earned.OrderByDescending(item => item.Earned)
            .Select(item => new DashboardPartnerProfit(item.PartnerId, names.GetValueOrDefault(item.PartnerId, string.Empty), decimal.Round(item.Earned, 2)))
            .ToList();
    }
}
