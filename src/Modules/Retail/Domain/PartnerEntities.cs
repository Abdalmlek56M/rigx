namespace HardwareStore.Modules.Retail;

/// <summary>
/// A partner record (ADM-Partners): capital in, a default percent of net profit and one profit basis —
/// <see cref="PartnerBasis.FinancedParts"/> (earns on the units it finances) or <see cref="PartnerBasis.WholeShop"/>
/// (earns a monthly percent of the store's net profit after expenses and financed shares).
/// </summary>
public sealed class Partner
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public DateOnly JoinDate { get; set; }
    public decimal DefaultPercent { get; set; }
    public required string Basis { get; set; } = PartnerBasis.FinancedParts;
    public bool LossSharingEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

/// <summary>Capital put in or taken out by a partner. Never edited or deleted; the balance is deposits minus withdrawals.</summary>
public sealed class PartnerCapitalEntry
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public required string Kind { get; set; }
    public decimal Amount { get; set; }
    public DateOnly EntryDate { get; set; }
    public string? Method { get; set; }
    public string? Note { get; set; }
    public Guid ActorUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// A partner's share: of one sold unit (order earning, snapshot of revenue/cost/percent) or of one closed month
/// (<see cref="PeriodMonth"/> set, whole-shop basis; <see cref="GrossProfit"/> holds the month base).
/// </summary>
public sealed class PartnerEarning
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public Guid? OrderItemId { get; set; }
    public Guid? OrderItemComponentId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? OrderId { get; set; }
    /// <summary>First day of the closed month for a whole-shop share; null for an order earning.</summary>
    public DateOnly? PeriodMonth { get; set; }
    public decimal Percent { get; set; }
    public bool LossSharingEnabled { get; set; }
    public decimal RevenueAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DirectExpenseAmount { get; set; }
    public decimal CostAmount { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OffsetAmount { get; set; }
    public required string Status { get; set; } = EarningStatus.Outstanding;
    public DateTimeOffset? ReversedAtUtc { get; set; }
    public string? ReversalReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>A payout of profit to a partner (paid against the oldest outstanding earnings).</summary>
public sealed class PartnerSettlement
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public decimal Amount { get; set; }
    public decimal OffsetApplied { get; set; }
    public required string Method { get; set; }
    public string? Reference { get; set; }
    public string? Note { get; set; }
    /// <summary>Set when the settlement is an expense paid from the partner's own money (no account movement).</summary>
    public Guid? ExpenseId { get; set; }
    public Guid ActorUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>A recoverable (negative) partner balance from a return or a shared loss, offset against later earnings.</summary>
public sealed class PartnerAdjustment
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public Guid? PartnerEarningId { get; set; }
    public Guid? OrderReturnId { get; set; }
    public decimal Amount { get; set; }
    public decimal AppliedAmount { get; set; }
    public required string Status { get; set; } = AdjustmentStatus.Open;
    public required string Reason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
