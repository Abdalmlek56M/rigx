namespace HardwareStore.Modules.Retail;

public sealed record OrderLine(Guid? ProductId, Guid? AssemblyId, string Name, string? Sku, int Quantity, decimal UnitPrice, decimal? DiscountAmount = null, decimal? DirectExpenseAmount = null, string? DirectExpenseNote = null, Guid? VariantId = null, string? VariantName = null, string? ThumbnailStorageKeySnapshot = null, Guid? StockItemId = null);
public sealed record OrderCreateRequest(string? CustomerName, string? PhoneNumber, string? AddressText, string? FulfillmentMethod, string? PaymentMethod, string? Notes, IReadOnlyList<OrderLine>? Items, string? LeadSource, bool? PaymentRequired = null, bool? TermsAccepted = null, string? TermsPolicyVersion = null);
public sealed record OrderStatusRequest(string? Status, string? Notes, long RowVersion);
public sealed record CompleteOrderRequest(long RowVersion, string? Notes);
public sealed record PaymentStatusRequest(string? PaymentStatus, string? Notes, long RowVersion);
public sealed record WaivePaymentRequest(string? Reason, long RowVersion);
public sealed record ReturnCreateRequest(Guid OrderId, Guid OrderItemId, Guid UnitId, decimal RefundAmount, string? Reason);
public sealed record ReturnInspectRequest(string? Condition, string? Notes, long RowVersion);
public sealed record ReturnRejectRequest(string? Notes, long RowVersion);
public sealed record OrderComponentView(Guid Id, Guid UnitId, decimal AllocationPercent, decimal RevenueAmount, decimal DiscountAmount, decimal DirectExpenseAmount, decimal CostAmount, decimal NetProfit);
public sealed record OrderItemView(Guid Id, Guid? ProductId, Guid? VariantId, string? VariantName, Guid? AssemblyId, Guid? StockItemId, string Name, string? Sku, string? ThumbnailStorageKeySnapshot, int Quantity, decimal UnitPrice, decimal LineTotal, decimal DiscountAmount, decimal DirectExpenseAmount, string? DirectExpenseNote, decimal NetProfit, IReadOnlyList<OrderComponentView> Components);
public sealed record OrderEventView(string EventType, string? Notes, Guid? ActorUserAccountId, DateTimeOffset CreatedAtUtc);
public sealed record OrderView(Guid Id, string Reference, Guid? UserAccountId, string CustomerName, string PhoneNumber, string? AddressText, string FulfillmentMethod, string LeadSource, string Status, string PaymentStatus, string PaymentMethod, bool PaymentRequired, DateTimeOffset? PaymentWaivedAtUtc, Guid? PaymentWaivedByUserAccountId, string? PaymentWaiverReason, decimal Subtotal, decimal DeliveryFee, decimal Total, decimal RefundedAmount, string? Notes, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long RowVersion, IReadOnlyList<OrderItemView> Items, IReadOnlyList<OrderEventView> Events);
public sealed record AdminOrderUnitView(Guid UnitId, Guid? ProductId, string Name, string StockItemCode, string Condition, string? InspectionCondition, string? SerialNumber, string Status);
public sealed record AdminOrderDetailView(OrderView Order, IReadOnlyList<AdminOrderUnitView> Units);
public sealed record ReturnView(Guid Id, Guid OrderId, Guid OrderItemId, Guid UnitId, decimal RefundAmount, string Reason, string Status, string? InspectionCondition, string? InspectionNotes, Guid? InspectedByUserAccountId, DateTimeOffset? InspectedAtUtc, DateTimeOffset CreatedAtUtc, long RowVersion);
