namespace HardwareStore.Modules.Retail;

public sealed record OrderCustomer(string? Name, string? Phone, bool PhoneHasWhatsApp = false, string? Email = null);

/// <summary>
/// One order line: a stock item (quantity, price option "Alone"/"WithBoard") or a build. POS lines may set the sale price,
/// and a split build sale lists the parts it sells (each with its own sale price).
/// </summary>
public sealed record OrderLineRequest(Guid? StockItemId, Guid? AssemblyId, int Quantity = 1, string? PriceOption = null,
    decimal? SalePrice = null, IReadOnlyList<SplitPartRequest>? Parts = null);
public sealed record SplitPartRequest(Guid UnitId, decimal? SalePrice = null);

/// <summary>Website checkout (guest or signed in).</summary>
public sealed record CheckoutRequest(OrderCustomer? Customer, string? FulfillmentMethod, OrderAddress? Address, string? PaymentMethod,
    IReadOnlyList<OrderLineRequest>? Items, string? Notes = null, string? Usage = null, string? UsageNote = null,
    string? LeadSource = null, bool? TermsAccepted = null);

/// <summary>What the store settings decide for a checkout (read in Host): fee, upfront rule, enabled methods, terms version.</summary>
public sealed record CheckoutPolicy(Guid? UserAccountId, string TermsPolicyVersion, decimal DeliveryFee,
    Func<decimal, decimal> UpfrontFor, IReadOnlyCollection<string> PaymentMethods, decimal MinimumOrder);

/// <summary>Reservation defaults from the store settings, applied when an order is confirmed.</summary>
public sealed record ReservationPolicy(int Hours, bool AutoRelease);

public sealed record OrderStatusRequest(string? Status, string? Notes, long RowVersion);
public sealed record CompleteOrderRequest(long RowVersion, string? Notes);
public sealed record PaymentStatusRequest(string? PaymentStatus, string? Notes, long RowVersion);
public sealed record WaivePaymentRequest(string? Reason, long RowVersion);
public sealed record OrderPaymentRequest(string? Method, decimal Amount, string? Note, long RowVersion);
public sealed record OrderShippingRequest(string? CourierName, string? WaybillNumber, long RowVersion);
public sealed record OrderReservationRequest(int? Hours, bool AutoRelease, long RowVersion);
public sealed record OrderRatingRequest(int Score, string? Comment);
public sealed record BlacklistRequest(Guid? OrderId, string? Name, string? Phone, string? Email, OrderAddress? Address, string? Reason);
public sealed record ReturnCreateRequest(Guid OrderId, Guid OrderItemId, Guid UnitId, decimal RefundAmount, string? Reason);
public sealed record ReturnInspectRequest(string? Condition, string? Notes, long RowVersion);
public sealed record ReturnRejectRequest(string? Notes, long RowVersion);

public sealed record OrderComponentView(Guid Id, Guid UnitId, decimal AllocationPercent, decimal RevenueAmount, decimal DiscountAmount, decimal DirectExpenseAmount, decimal CostAmount, decimal NetProfit);
public sealed record OrderItemView(Guid Id, Guid? AssemblyId, Guid? StockItemId, Guid? UnitId, string Name, string? Code, string? ThumbnailStorageKeySnapshot, int Quantity, decimal UnitPrice, decimal LineTotal, decimal DiscountAmount, decimal DirectExpenseAmount, string? DirectExpenseNote, decimal NetProfit, IReadOnlyList<OrderComponentView> Components);
public sealed record OrderEventView(string EventType, string? Notes, Guid? ActorUserAccountId, DateTimeOffset CreatedAtUtc);
public sealed record OrderPaymentView(string Method, decimal Amount, string? Note, DateTimeOffset CreatedAtUtc);
public sealed record OrderView(Guid Id, string Reference, string TrackingToken, Guid? UserAccountId, string Channel, string CustomerName,
    string PhoneNumber, bool PhoneHasWhatsApp, string? CustomerEmail, OrderAddress? Address, string FulfillmentMethod, string? LeadSource,
    string? Usage, string? UsageNote, string Status, string PaymentStatus, string PaymentMethod, bool PaymentRequired,
    DateTimeOffset? PaymentWaivedAtUtc, Guid? PaymentWaivedByUserAccountId, string? PaymentWaiverReason, decimal Subtotal,
    decimal DeliveryFee, decimal Total, decimal UpfrontAmount, decimal PaidAmount, decimal Remaining, decimal RefundedAmount,
    bool IsBlacklisted, DateTimeOffset? ReservedUntilUtc, bool ReservationAutoRelease, DateTimeOffset? ReservationExpiredAtUtc,
    string? CourierName, string? WaybillNumber, int? SatisfactionScore, string? SatisfactionComment, string? Notes,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long RowVersion, IReadOnlyList<OrderItemView> Items,
    IReadOnlyList<OrderPaymentView> Payments, IReadOnlyList<OrderEventView> Events);
public sealed record AdminOrderUnitView(Guid UnitId, Guid? ProductId, string Name, string StockItemCode, string Condition, string? InspectionCondition, string? SerialNumber, string Status);
public sealed record AdminOrderDetailView(OrderView Order, IReadOnlyList<AdminOrderUnitView> Units);
public sealed record ReturnView(Guid Id, Guid OrderId, Guid OrderItemId, Guid UnitId, decimal RefundAmount, string Reason, string Status, string? InspectionCondition, string? InspectionNotes, Guid? InspectedByUserAccountId, DateTimeOffset? InspectedAtUtc, DateTimeOffset CreatedAtUtc, long RowVersion);
public sealed record BlacklistView(Guid Id, string? Name, string? PhoneNumber, string? Email, string? AddressText, string Reason, DateTimeOffset CreatedAtUtc);

/// <summary>Public tracking page (/t/{token}): no login, no customer name or phone.</summary>
public sealed record TrackingView(string Reference, string Status, string FulfillmentMethod, IReadOnlyList<TrackingStep> Timeline,
    string? CourierName, string? WaybillNumber, string? Area, string? Street, IReadOnlyList<TrackingItem> Items, decimal Total,
    decimal Remaining, bool CanRate, int? SatisfactionScore);
public sealed record TrackingStep(string Status, DateTimeOffset At);
public sealed record TrackingItem(string Name, string? Code, int Quantity);

/// <summary>Public "اتباع قبل كده" card: no price, no customer name.</summary>
public sealed record SoldFeedItem(string Kind, string Title, string? Code, string Condition, Guid? CategoryId, string? ThumbnailStorageKey,
    string FulfillmentMethod, string? Governorate, DateTimeOffset SoldAtUtc);
public sealed record SoldFeedPage(IReadOnlyList<SoldFeedItem> Items, int Page, int PageSize);
