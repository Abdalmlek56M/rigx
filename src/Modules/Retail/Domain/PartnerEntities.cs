namespace HardwareStore.Modules.Retail;

public sealed class PartnerAgreement
{
    public Guid Id { get; set; }
    public required string PartnerName { get; set; }
    public decimal DefaultPercent { get; set; }
    public bool LossSharingEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class PartnerEarning
{
    public Guid Id { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid? OrderItemComponentId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid OrderId { get; set; }
    public required string PartnerName { get; set; }
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

public sealed class PartnerSettlement
{
    public Guid Id { get; set; }
    public required string PartnerName { get; set; }
    public decimal Amount { get; set; }
    public decimal OffsetApplied { get; set; }
    public required string Method { get; set; }
    public string? Reference { get; set; }
    public string? Note { get; set; }
    public Guid ActorUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class PartnerUnitAllocation
{
    public Guid Id { get; set; }
    public required string PartnerName { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ProductId { get; set; }
    public string? Category { get; set; }
    public decimal Percent { get; set; }
    public bool LossSharingEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class PartnerAllocationEvent
{
    public Guid Id { get; set; }
    public Guid AllocationId { get; set; }
    public required string PartnerName { get; set; }
    public required string Action { get; set; }
    public Guid ActorUserAccountId { get; set; }
    public required string Reason { get; set; }
    public required string SnapshotJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class PartnerAdjustment
{    public Guid Id { get; set; }
    public required string PartnerName { get; set; }
    public Guid? PartnerEarningId { get; set; }
    public Guid? OrderReturnId { get; set; }
    public decimal Amount { get; set; }
    public decimal AppliedAmount { get; set; }
    public required string Status { get; set; } = AdjustmentStatus.Open;
    public required string Reason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
