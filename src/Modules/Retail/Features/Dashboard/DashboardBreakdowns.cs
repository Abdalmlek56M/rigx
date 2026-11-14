using HardwareStore.Common.Domain;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Dashboard sales breakdowns over the completed orders of a period.</summary>
internal static class DashboardBreakdowns
{
    private const string Unknown = "Unknown";
    private const int TopCategoryCount = 10;

    private sealed record ComponentRow(Guid? CategoryId, string Condition, decimal Sales, decimal Cost, DateTimeOffset ReceivedAtUtc, DateTimeOffset SoldAtUtc);

    /// <summary>Completed orders whose last update falls in the days (business days, like the business report).</summary>
    public static IQueryable<StoreOrder> CompletedOrders(RetailDbContext db, DateOnly from, DateOnly to, TimeZoneInfo zone)
    {
        var (fromUtc, toUtc) = BusinessDay.Range(from, to, zone);
        return db.Orders.AsNoTracking().Where(order => order.Status == OrderStatus.Completed && order.UpdatedAtUtc >= fromUtc && order.UpdatedAtUtc < toUtc);
    }

    public static async Task<IReadOnlyList<DashboardSourceRow>> BySourceAsync(IQueryable<StoreOrder> orders,
        Expression<Func<StoreOrder, string?>> key, CancellationToken cancellationToken)
    {
        var rows = await orders.GroupBy(key)
            .Select(group => new { Label = group.Key, Orders = group.Count(), Sales = group.Sum(order => order.Total) })
            .ToListAsync(cancellationToken);
        return rows.GroupBy(row => string.IsNullOrWhiteSpace(row.Label) ? Unknown : row.Label.Trim())
            .Select(group => new DashboardSourceRow(group.Key, group.Sum(row => row.Orders), decimal.Round(group.Sum(row => row.Sales), 2)))
            .OrderByDescending(row => row.Sales).ToList();
    }

    /// <summary>Top categories, new-vs-used and the average days a used unit waited before it sold, from the sold components.</summary>
    public static async Task<(IReadOnlyList<DashboardCategoryRow> Categories, IReadOnlyList<DashboardConditionRow> Conditions, decimal? DaysToSellUsed)>
        ByComponentAsync(RetailDbContext db, IQueryable<StoreOrder> orders, CancellationToken cancellationToken)
    {
        var rows = await (from component in orders.SelectMany(order => order.Items).SelectMany(item => item.Components)
                          join unit in db.Units on component.UnitId equals unit.Id
                          select new ComponentRow(unit.StockItem.CategoryId, unit.Condition, component.RevenueAmount - component.DiscountAmount,
                              component.CostAmount, unit.ReceivedAtUtc, component.OrderItem.Order.UpdatedAtUtc)).ToListAsync(cancellationToken);
        var categories = rows.GroupBy(row => row.CategoryId).Select(group => CategoryRow(group.Key, group.ToList()))
            .OrderByDescending(row => row.Sales).Take(TopCategoryCount).ToList();
        var conditions = rows.GroupBy(row => row.Condition).Select(group => ConditionRow(group.Key, group.ToList()))
            .OrderByDescending(row => row.Sales).ToList();
        var used = rows.Where(row => row.Condition == StockCondition.Used).ToList();
        decimal? days = used.Count == 0 ? null : decimal.Round((decimal)used.Average(row => (row.SoldAtUtc - row.ReceivedAtUtc).TotalDays), 1);
        return (categories, conditions, days);
    }

    /// <summary>Whole builds, builds sold part by part, and single items (key 0 = single, 1 = whole build, 2 = split parts).</summary>
    public static async Task<DashboardBuildSales> BuildSalesAsync(IQueryable<StoreOrder> orders, CancellationToken cancellationToken)
    {
        var rows = await orders.SelectMany(order => order.Items)
            .GroupBy(item => item.AssemblyId == null ? 0 : item.UnitId == null ? 1 : 2)
            .Select(group => new { Kind = group.Key, Count = group.Count(), Sales = group.Sum(item => item.LineTotal) })
            .ToListAsync(cancellationToken);
        var full = rows.FirstOrDefault(row => row.Kind == 1);
        var split = rows.FirstOrDefault(row => row.Kind == 2);
        var single = rows.FirstOrDefault(row => row.Kind == 0);
        return new DashboardBuildSales(full?.Count ?? 0, decimal.Round(full?.Sales ?? 0, 2), split?.Count ?? 0, decimal.Round(split?.Sales ?? 0, 2),
            single?.Count ?? 0, decimal.Round(single?.Sales ?? 0, 2));
    }

    private static DashboardCategoryRow CategoryRow(Guid? categoryId, List<ComponentRow> rows)
    {
        var (sales, cost, margin, percent) = Totals(rows);
        return new DashboardCategoryRow(categoryId, null, sales, cost, margin, percent);
    }

    private static DashboardConditionRow ConditionRow(string condition, List<ComponentRow> rows)
    {
        var (sales, cost, margin, percent) = Totals(rows);
        return new DashboardConditionRow(condition, sales, cost, margin, percent);
    }

    private static (decimal Sales, decimal Cost, decimal Margin, decimal Percent) Totals(List<ComponentRow> rows)
    {
        var sales = decimal.Round(rows.Sum(row => row.Sales), 2);
        var cost = decimal.Round(rows.Sum(row => row.Cost), 2);
        var margin = decimal.Round(sales - cost, 2);
        return (sales, cost, margin, sales == 0 ? 0 : decimal.Round(margin / sales * 100, 1));
    }
}
