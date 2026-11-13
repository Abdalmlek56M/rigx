namespace HardwareStore.Modules.Retail;

public sealed record ExpenseCreateRequest(DateOnly? ExpenseDate, decimal Amount, string? Category, string? Classification,
    string? Note, Guid? OrderId, Guid? UnitId, string? AttachmentReference);
public sealed record ExpenseView(Guid Id, DateOnly ExpenseDate, decimal Amount, string Category, string Classification,
    string? Note, Guid? OrderId, Guid? UnitId, string? AttachmentReference, Guid CreatedByUserAccountId,
    DateTimeOffset CreatedAtUtc, long RowVersion);
public sealed record BusinessReportView(DateOnly From, DateOnly To, decimal GrossSales, decimal Discounts,
    decimal Refunds, decimal CostOfGoods, decimal ItemDirectExpenses, decimal AdditionalDirectExpenses,
    decimal NetItemProfit, decimal GeneralOverhead, decimal StoreNetResult, decimal PartnerAccrued,
    decimal PartnerPaid, decimal PartnerRecoverable, int CompletedOrders, int Returns);
