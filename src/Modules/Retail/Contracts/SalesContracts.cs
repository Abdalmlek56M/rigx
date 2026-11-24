namespace HardwareStore.Modules.Retail;

/// <summary>The customer of an order; <paramref name="WhatsAppNumber"/> is used only when the phone is not a WhatsApp number.</summary>
public sealed record OrderCustomer(string? Name, string? Phone, bool PhoneHasWhatsApp = false, string? Email = null, string? WhatsAppNumber = null);

/// <summary>Which of a stock item's prices an order line uses.</summary>
public static class OrderPriceOption
{
    public const string Alone = "Alone";
    public const string WithBoard = "WithBoard";
    public const string InBuild = "InBuild";
}

/// <summary>
/// One order line: a stock item (quantity, price option "Alone"/"WithBoard"/"InBuild") or a build, named by its id or by its
/// public code instead (<c>RX-…</c> item, <c>BLD-…</c> build; the website cart only knows codes). POS lines may set the sale price,
/// and a split build sale lists the parts it sells (each with its own sale price).
/// </summary>
public sealed record OrderLineRequest(Guid? StockItemId, Guid? AssemblyId, int Quantity = 1, string? PriceOption = null,
    decimal? SalePrice = null, IReadOnlyList<SplitPartRequest>? Parts = null, string? Code = null);
public sealed record SplitPartRequest(Guid UnitId, decimal? SalePrice = null);

/// <summary>Website checkout (guest or signed in).</summary>
public sealed record CheckoutRequest(OrderCustomer? Customer, string? FulfillmentMethod, OrderAddress? Address, string? PaymentMethod,
    IReadOnlyList<OrderLineRequest>? Items, string? Notes = null, string? Usage = null, string? UsageNote = null,
    string? LeadSource = null, bool? TermsAccepted = null, string? CouponCode = null);

/// <summary>Shipping fee and promised delivery time for a governorate (from the shipping settings, read in Host).</summary>
public sealed record ShippingRate(decimal Fee, string? DeliveryTime);

/// <summary>
/// Categories (with their descendants) of the builder's processor and motherboard slots, read from Catalog in Host.
/// Online in-build prices need one item from each (<see cref="OrderRules.EnsureBuildPrice"/>).
/// </summary>
public sealed record BuildPriceRule(IReadOnlySet<Guid> CpuCategoryIds, IReadOnlySet<Guid> BoardCategoryIds);

/// <summary>
/// What the store settings decide for a checkout (read in Host): shipping, upfront rule, enabled methods, terms version.
/// <paramref name="ShippingFor"/> takes the order total after discounts (before shipping), so the free-shipping rule is decided from the prices
/// the server worked out, never from anything the browser sent; <paramref name="UpfrontFor"/> takes that total and the fee that was charged.
/// </summary>
public sealed record CheckoutPolicy(Guid? UserAccountId, string TermsPolicyVersion, Func<decimal, ShippingRate> ShippingFor,
    Func<decimal, decimal, decimal> UpfrontFor, IReadOnlyCollection<string> PaymentMethods, decimal MinimumOrder, BuildPriceRule? BuildPrice = null,
    Func<decimal, decimal>? DepositFor = null, PriceOptionRule? PriceOptions = null);

/// <summary>Reservation defaults from the store settings, applied when an order is confirmed.</summary>
public sealed record ReservationPolicy(int Hours, bool AutoRelease);

public sealed record OrderStatusRequest(string? Status, string? Notes, long RowVersion);
public sealed record CompleteOrderRequest(long RowVersion, string? Notes);
public sealed record PaymentStatusRequest(string? PaymentStatus, string? Notes, long RowVersion);
public sealed record WaivePaymentRequest(string? Reason, long RowVersion);
public sealed record WaiveDepositRequest(string? Reason, long RowVersion);
public sealed record OrderPaymentRequest(string? Method, decimal Amount, string? Note, long RowVersion, Guid? MoneyAccountId = null);
/// <summary>"The customer could not be reached": the order's row version only; the store's reaction (email, WhatsApp) follows from the order.</summary>
public sealed record OrderContactRequest(long RowVersion);
public sealed record OrderShippingRequest(string? CourierName, string? WaybillNumber, long RowVersion);
public sealed record OrderReservationRequest(int? Hours, bool AutoRelease, long RowVersion);
public sealed record OrderRatingRequest(int Score, string? Comment);
public sealed record BlacklistRequest(Guid? OrderId, string? Name, string? Phone, string? Email, OrderAddress? Address, string? Reason);
public sealed record ReturnCreateRequest(Guid OrderId, Guid OrderItemId, Guid UnitId, decimal RefundAmount, string? Reason);
/// <summary>RefundAccountId is the account the refund is paid from; without it the first active cash account pays.</summary>
public sealed record ReturnInspectRequest(string? Condition, string? Notes, long RowVersion, Guid? RefundAccountId = null);
public sealed record ReturnRejectRequest(string? Notes, long RowVersion);

public sealed record OrderComponentView(Guid Id, Guid UnitId, decimal AllocationPercent, decimal RevenueAmount, decimal DiscountAmount, decimal DirectExpenseAmount, decimal CostAmount, decimal NetProfit);
public sealed record OrderItemView(Guid Id, Guid? AssemblyId, Guid? StockItemId, Guid? UnitId, string Name, string? Code, string? ThumbnailStorageKeySnapshot, int Quantity, decimal UnitPrice, decimal LineTotal, decimal DiscountAmount, decimal DirectExpenseAmount, string? DirectExpenseNote, decimal NetProfit, IReadOnlyList<OrderComponentView> Components);
public sealed record OrderEventView(string EventType, string? Notes, Guid? ActorUserAccountId, DateTimeOffset CreatedAtUtc);
public sealed record OrderPaymentView(string Method, decimal Amount, string? Note, DateTimeOffset CreatedAtUtc);
public sealed record OrderView(Guid Id, string Reference, string TrackingToken, Guid? UserAccountId, string Channel, string CustomerName,
    string PhoneNumber, bool PhoneHasWhatsApp, string? CustomerEmail, OrderAddress? Address, string FulfillmentMethod, string? LeadSource,
    string? Usage, string? UsageNote, string Status, string PaymentStatus, string PaymentMethod, bool PaymentRequired,
    DateTimeOffset? PaymentWaivedAtUtc, Guid? PaymentWaivedByUserAccountId, string? PaymentWaiverReason, decimal Subtotal,
    decimal DeliveryFee, string? DeliveryTime, decimal Total, decimal UpfrontAmount, decimal UpfrontDue, decimal PaidAmount, decimal Remaining,
    decimal RefundedAmount,
    bool IsBlacklisted, DateTimeOffset? ReservedUntilUtc, bool ReservationAutoRelease, DateTimeOffset? ReservationExpiredAtUtc,
    string? CourierName, string? WaybillNumber, int? SatisfactionScore, string? SatisfactionComment, string? Notes,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long RowVersion, IReadOnlyList<OrderItemView> Items,
    IReadOnlyList<OrderPaymentView> Payments, IReadOnlyList<OrderEventView> Events, string? CouponCode = null, decimal CouponDiscount = 0m, string? WhatsAppNumber = null,
    decimal DepositAmount = 0m);
/// <summary>
/// The customer's own order (checkout result, "my orders", "view as customer"): no cost, profit, staff, payment-waiver or blacklist
/// data. Status and timeline are the customer-facing ones ("Completed" shows as "Delivered").
/// </summary>
public sealed record CustomerOrderView(Guid Id, string Reference, string TrackingToken, string Status, string FulfillmentMethod,
    string CustomerName, string PhoneNumber, string? CustomerEmail, OrderAddress? Address, string PaymentMethod, string PaymentStatus,
    decimal Subtotal, decimal DeliveryFee, string? DeliveryTime, decimal Total, decimal UpfrontAmount, decimal UpfrontDue, decimal PaidAmount,
    decimal Remaining, decimal RefundedAmount, string? CourierName, string? WaybillNumber, int? SatisfactionScore, DateTimeOffset CreatedAtUtc,
    IReadOnlyList<CustomerOrderItemView> Items, IReadOnlyList<TrackingStep> Timeline, string? CouponCode = null, decimal CouponDiscount = 0m);
public sealed record CustomerOrderItemView(string Name, string? Code, string? ThumbnailStorageKey, int Quantity, decimal UnitPrice,
    decimal LineTotal, decimal DiscountAmount);
/// <summary>A new website order: the full view (order emails) and the customer's view (the API response).</summary>
public sealed record PlacedOrder(OrderView Order, CustomerOrderView Customer);
public sealed record AdminOrderUnitView(Guid UnitId, Guid? ProductId, string Name, string StockItemCode, string Condition, string? InspectionCondition, string? SerialNumber, string Status);
public sealed record AdminOrderDetailView(OrderView Order, IReadOnlyList<AdminOrderUnitView> Units);
public sealed record ReturnView(Guid Id, Guid OrderId, Guid OrderItemId, Guid UnitId, decimal RefundAmount, string Reason, string Status, string? InspectionCondition, string? InspectionNotes, Guid? InspectedByUserAccountId, DateTimeOffset? InspectedAtUtc, DateTimeOffset CreatedAtUtc, long RowVersion);
public sealed record BlacklistView(Guid Id, string? Name, string? PhoneNumber, string? Email, string? AddressText, string Reason, DateTimeOffset CreatedAtUtc);

/// <summary>
/// Public tracking page (/t/{token}): no login, no customer name, and the phone only masked (<paramref name="MaskedPhone"/>). When
/// <paramref name="DetailsHidden"/> (long after delivery, see <see cref="TrackingRules"/>) only the reference, status and timeline are filled.
/// </summary>
public sealed record TrackingView(string Reference, string Status, string FulfillmentMethod, IReadOnlyList<TrackingStep> Timeline,
    string? CourierName, string? WaybillNumber, string? Area, IReadOnlyList<TrackingItem> Items, decimal Total,
    decimal Remaining, decimal UpfrontDue, string? DeliveryTime, bool CanRate, int? SatisfactionScore, string? MaskedPhone, bool DetailsHidden);
public sealed record TrackingStep(string Status, DateTimeOffset At);
public sealed record TrackingItem(string Name, string? Code, int Quantity);
