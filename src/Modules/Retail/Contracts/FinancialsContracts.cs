namespace HardwareStore.Modules.Retail;

/// <summary>An expense; paid from an account (explicit or the default cash one) or from a partner's own money, never both.</summary>
public sealed record ExpenseCreateRequest(DateOnly? ExpenseDate, decimal Amount, string? Category, string? Classification,
    string? Note, Guid? OrderId, Guid? UnitId, string? AttachmentReference, Guid? AssemblyId = null, Guid? MoneyAccountId = null,
    Guid? PaidByPartnerId = null);
public sealed record ExpenseView(Guid Id, DateOnly ExpenseDate, decimal Amount, string Category, string Classification,
    string? Note, Guid? OrderId, Guid? UnitId, string? AttachmentReference, Guid CreatedByUserAccountId,
    DateTimeOffset CreatedAtUtc, long RowVersion, Guid? AssemblyId, Guid? MoneyAccountId, Guid? PaidByPartnerId, Guid? RecurringExpenseId);
public sealed record ExpenseTypeRequest(string? Name, bool IsActive = true, int SortOrder = 0, long? RowVersion = null);
public sealed record ExpenseTypeView(Guid Id, string Name, bool IsActive, int SortOrder, long RowVersion);
/// <summary>Create (no RowVersion; StartMonth defaults to this month) or update (RowVersion required; StartMonth cannot change).</summary>
public sealed record RecurringExpenseRequest(string? Category, decimal Amount, int DayOfMonth, Guid? MoneyAccountId = null,
    string? Note = null, bool IsActive = true, DateOnly? StartMonth = null, long? RowVersion = null);
public sealed record RecurringExpenseView(Guid Id, string Category, decimal Amount, int DayOfMonth, Guid? MoneyAccountId, string? Note,
    bool IsActive, DateOnly StartMonth, DateOnly? LastPostedMonth, DateTimeOffset CreatedAtUtc, long RowVersion);
/// <summary>Result of posting the due recurring expenses: how many were posted and how many templates are stuck (e.g. balance too low).</summary>
public sealed record RecurringPostResult(int Posted, int Blocked);
public sealed record ExpenseTypeTotal(string Type, decimal Amount, int Count);
/// <summary>Expense totals for a period against the previous period of the same length and the period's gross sales.</summary>
public sealed record ExpenseSummaryView(DateOnly From, DateOnly To, decimal Total, IReadOnlyList<ExpenseTypeTotal> ByType,
    decimal PreviousTotal, decimal? ChangePercent, decimal GrossSales, decimal? PercentOfSales, decimal FixedMonthly);
public sealed record BusinessReportView(DateOnly From, DateOnly To, decimal GrossSales, decimal Discounts,
    decimal Refunds, decimal CostOfGoods, decimal ItemDirectExpenses, decimal AdditionalDirectExpenses,
    decimal NetItemProfit, decimal GeneralOverhead, decimal StoreNetResult, decimal PartnerAccrued,
    decimal PartnerPaid, decimal PartnerRecoverable, int CompletedOrders, int Returns);
