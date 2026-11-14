namespace HardwareStore.Modules.Retail;

/// <summary>Headline numbers of one period. NetProfit is the store result after expenses and the partners' share.</summary>
public sealed record DashboardKpis(decimal GrossSales, decimal NetProfit, int Orders, decimal AverageOrder);

/// <summary>Change against the previous period in percent (1 decimal); null when the previous value is zero.</summary>
public sealed record DashboardKpiChanges(decimal? GrossSales, decimal? NetProfit, decimal? Orders, decimal? AverageOrder);

public sealed record DashboardAccountBalance(Guid Id, string Name, string Kind, decimal Balance);

/// <summary>State of the shop right now (not tied to the chosen period).</summary>
public sealed record DashboardSnapshot(decimal StockValueAtCost, decimal PartnerDues, decimal CashAvailable,
    IReadOnlyList<DashboardAccountBalance> Accounts);

public sealed record DashboardDay(DateOnly Date, decimal GrossSales, decimal NetItemProfit);

/// <summary>Counts of things waiting for the owner. NewRequests is filled by the host (requests live in Storefront).</summary>
public sealed record DashboardTodo(int NewOrders, int AwaitingPayment, int ExpiredReservations, int PendingReturns,
    int RecurringExpensesDue);

public sealed record DashboardSourceRow(string Label, int Orders, decimal Sales);

/// <summary>Sales of one category: Sales = revenue - discount; Margin = Sales - Cost. CategoryName is filled by the host.</summary>
public sealed record DashboardCategoryRow(Guid? CategoryId, string? CategoryName, decimal Sales, decimal Cost, decimal Margin,
    decimal MarginPercent);

public sealed record DashboardConditionRow(string Condition, decimal Sales, decimal Cost, decimal Margin, decimal MarginPercent);

public sealed record DashboardBuildSales(int FullBuildCount, decimal FullBuildSales, int SplitPartsCount, decimal SplitPartsSales,
    int SingleItemsCount, decimal SingleItemsSales);

public sealed record DashboardAgingUnit(Guid UnitId, string StockItemCode, string Name, string Condition, decimal Cost, int DaysInStock);

public sealed record DashboardStockAging(int Count, decimal CostTotal, IReadOnlyList<DashboardAgingUnit> Oldest);

public sealed record DashboardPartnerProfit(Guid PartnerId, string Name, decimal Earned);

public sealed record DashboardView(DateOnly From, DateOnly To, DateOnly PreviousFrom, DateOnly PreviousTo, DashboardKpis Current,
    DashboardKpis Previous, DashboardKpiChanges Changes, DashboardSnapshot Snapshot, IReadOnlyList<DashboardDay> Days,
    DashboardTodo Todo, IReadOnlyList<DashboardSourceRow> LeadSources, IReadOnlyList<DashboardSourceRow> Usages,
    IReadOnlyList<DashboardCategoryRow> TopCategories, IReadOnlyList<DashboardConditionRow> NewVsUsed, DashboardBuildSales BuildSales,
    decimal? AvgDaysToSellUsed, DashboardStockAging StockAging, IReadOnlyList<DashboardPartnerProfit> PartnerProfitsThisMonth);
