using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Business report for a date range: sales, refunds, cost of goods, expenses and partner balances.
/// </summary>
public sealed class BusinessReportService(IDbContextFactory<RetailDbContext> contextFactory)
{
    public async Task<BusinessReportView> BuildReportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (to < from) throw new RetailException(400, "Report end date cannot precede its start date.");
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var orders = await db.Orders.AsNoTracking()
            .Include(order => order.Items).ThenInclude(item => item.Components)
            .Where(order => order.Status == OrderStatus.Completed && order.UpdatedAtUtc >= fromUtc && order.UpdatedAtUtc < toUtc)
            .ToListAsync(cancellationToken);
        var expenses = await db.Expenses.AsNoTracking()
            .Where(item => item.ExpenseDate >= from && item.ExpenseDate <= to)
            .ToListAsync(cancellationToken);
        var earnings = await db.PartnerEarnings.AsNoTracking()
            .Where(item => item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc)
            .ToListAsync(cancellationToken);
        var settlements = await db.PartnerSettlements.AsNoTracking()
            .Where(item => item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc)
            .ToListAsync(cancellationToken);
        var openAdjustments = await db.PartnerAdjustments.AsNoTracking()
            .Where(item => item.Status == AdjustmentStatus.Open)
            .ToListAsync(cancellationToken);
        var returns = await db.OrderReturns.AsNoTracking()
            .Where(item => item.Status == ReturnStatus.Inspected && item.InspectedAtUtc >= fromUtc && item.InspectedAtUtc < toUtc)
            .CountAsync(cancellationToken);

        var gross = decimal.Round(orders.Sum(order => order.Total), 2);
        var discounts = decimal.Round(orders.SelectMany(order => order.Items).Sum(item => item.DiscountAmount), 2);
        var refunds = decimal.Round(orders.Sum(order => order.RefundedAmount), 2);
        var cogs = decimal.Round(orders.SelectMany(order => order.Items).SelectMany(item => item.Components).Sum(item => item.CostAmount), 2);
        var itemDirect = decimal.Round(orders.SelectMany(order => order.Items).Sum(item => item.DirectExpenseAmount), 2);
        var netItem = decimal.Round(orders.SelectMany(order => order.Items).SelectMany(item => item.Components).Sum(item => item.NetProfit) - refunds, 2);
        var additionalDirect = decimal.Round(expenses.Where(item => item.Classification == "Direct").Sum(item => item.Amount), 2);
        var overhead = decimal.Round(expenses.Where(item => item.Classification == "GeneralOverhead").Sum(item => item.Amount), 2);
        var accrued = decimal.Round(earnings.Where(item => item.Amount > 0).Sum(item => item.Amount), 2);
        var paid = decimal.Round(settlements.Sum(item => item.Amount), 2);
        var recoverable = Math.Abs(decimal.Round(openAdjustments.Sum(item => item.Amount - item.AppliedAmount), 2));
        return new BusinessReportView(from, to, gross, discounts, refunds, cogs, itemDirect, additionalDirect,
            netItem, overhead, decimal.Round(netItem - additionalDirect - overhead, 2), accrued, paid, recoverable,
            orders.Count, returns);
    }
}
