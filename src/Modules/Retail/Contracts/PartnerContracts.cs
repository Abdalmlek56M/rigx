namespace HardwareStore.Modules.Retail;

/// <summary>Create (no RowVersion) or update (RowVersion required) a partner card.</summary>
public sealed record PartnerRequest(string? Name, string? Phone, DateOnly? JoinDate, decimal DefaultPercent, string? Basis,
    bool LossSharingEnabled = false, bool IsActive = true, string? Notes = null, long? RowVersion = null);
public sealed record PartnerCapitalRequest(string? Kind, decimal Amount, DateOnly? EntryDate = null, string? Method = null, string? Note = null,
    Guid? MoneyAccountId = null);
public sealed record PartnerPayoutRequest(decimal Amount, string? Method, string? Reference, string? Note, Guid? MoneyAccountId = null);
/// <summary>Sets or clears (PartnerId null) who finances an unsold unit, with an optional percent that overrides the partner default.</summary>
public sealed record UnitPartnerRequest(Guid? PartnerId, decimal? Percent, long RowVersion);
public sealed record PartnerMonthCloseRequest(int Year, int Month);

public sealed record PartnerView(Guid Id, string Name, string? Phone, DateOnly JoinDate, decimal DefaultPercent, string Basis,
    bool LossSharingEnabled, bool IsActive, string? Notes, decimal CapitalBalance, decimal Due, decimal Recoverable,
    int UnsoldFinancedUnits, long RowVersion);
/// <summary>A unit the partner finances: code, model, state, cost and the percent that applies.</summary>
public sealed record FinancedPartView(Guid UnitId, string StockItemCode, string Name, string Status, decimal Cost,
    decimal Percent, bool PercentOverridden, Guid? AssemblyId, DateTimeOffset ReceivedAtUtc);
public sealed record PartnerDetailView(PartnerView Partner, IReadOnlyList<FinancedPartView> FinancedParts);
public sealed record PartnerCapitalEntryView(Guid Id, Guid PartnerId, string Kind, decimal Amount, DateOnly EntryDate,
    string? Method, string? Note, DateTimeOffset CreatedAtUtc);
public sealed record PartnerEarningView(Guid Id, Guid PartnerId, string PartnerName, Guid? OrderId, Guid? OrderItemId, Guid? UnitId,
    DateOnly? PeriodMonth, decimal Percent, decimal RevenueAmount, decimal DiscountAmount, decimal DirectExpenseAmount, decimal CostAmount,
    decimal GrossProfit, decimal Amount, decimal PaidAmount, decimal OffsetAmount, string Status, DateTimeOffset? ReversedAtUtc,
    DateTimeOffset CreatedAtUtc);
public sealed record PartnerAdjustmentView(Guid Id, Guid PartnerId, string PartnerName, decimal Amount, decimal AppliedAmount,
    decimal RemainingAmount, string Status, string Reason, Guid? OrderReturnId, DateTimeOffset CreatedAtUtc);
public sealed record PartnerSettlementView(Guid Id, Guid PartnerId, string PartnerName, decimal Amount, decimal OffsetApplied,
    string Method, string? Reference, string? Note, DateTimeOffset CreatedAtUtc, Guid? ExpenseId = null);
/// <summary>Whole-shop shares posted for a month: the month base and one earning per partner (skipped when already posted).</summary>
public sealed record PartnerMonthCloseView(DateOnly Month, decimal StoreNetResult, decimal FinancedShares, decimal Base,
    IReadOnlyList<PartnerEarningView> Posted);

/// <summary>Statement data for a period (rendered to PDF by the frontend, Phase 4).</summary>
public sealed record PartnerStatementView(PartnerView Partner, DateOnly From, DateOnly To, decimal CapitalOpening, decimal CapitalClosing,
    decimal Earned, decimal Paid, decimal Recovered, IReadOnlyList<PartnerStatementLine> Lines);
/// <summary>One movement: CapitalDeposit, CapitalWithdrawal, Earning, MonthlyShare, Reversal, Payout or Recovery.</summary>
public sealed record PartnerStatementLine(DateTimeOffset At, string Kind, string Description, decimal Amount, string? Reference);

/// <summary>Estimated partner share of a POS sale (financed parts only).</summary>
public sealed record PartnerSharePreview(Guid PartnerId, string PartnerName, decimal Amount);
