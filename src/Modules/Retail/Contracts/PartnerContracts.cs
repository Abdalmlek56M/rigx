namespace HardwareStore.Modules.Retail;

public sealed record PartnerAgreementRequest(string? PartnerName, decimal DefaultPercent, bool IsActive, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool LossSharingEnabled = false);
public sealed record PartnerAllocationRequest(string? PartnerName, Guid? UnitId, Guid? ProductId, string? Category, decimal Percent, bool LossSharingEnabled, bool IsActive, long? RowVersion = null, string? Reason = null);
public sealed record SettlementRequest(string? PartnerName, decimal Amount, string? Method, string? Reference, string? Note);
public sealed record PartnerEarningView(Guid Id, Guid OrderId, Guid OrderItemId, Guid? UnitId, string PartnerName, decimal Percent, decimal RevenueAmount, decimal DiscountAmount, decimal DirectExpenseAmount, decimal CostAmount, decimal GrossProfit, decimal Amount, decimal PaidAmount, decimal OffsetAmount, string Status, DateTimeOffset? ReversedAtUtc, DateTimeOffset CreatedAtUtc);
public sealed record PartnerAdjustmentView(Guid Id, string PartnerName, decimal Amount, decimal AppliedAmount, decimal RemainingAmount, string Status, string Reason, Guid? OrderReturnId, DateTimeOffset CreatedAtUtc);
public sealed record PartnerAllocationView(Guid Id, string PartnerName, Guid? UnitId, Guid? ProductId, string? Category, decimal Percent, bool LossSharingEnabled, bool IsActive, long RowVersion);
public sealed record PartnerAllocationEventView(Guid Id, Guid AllocationId, string PartnerName, string Action, Guid ActorUserAccountId, string Reason, string SnapshotJson, DateTimeOffset CreatedAtUtc);
public sealed record PartnerAllocationPreviewView(string Source, IReadOnlyList<PartnerAllocationView> Allocations, decimal TotalPercent, decimal UnallocatedPercent);
public sealed record PartnerSettlementView(Guid Id, string PartnerName, decimal Amount, decimal OffsetApplied, string Method, string? Reference, string? Note, DateTimeOffset CreatedAtUtc);
public sealed record PartnerLedgerView(string PartnerName, decimal Outstanding, decimal CashDue, decimal Recoverable);
