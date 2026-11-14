namespace HardwareStore.Modules.Retail;

/// <summary>
/// An order from the website (guest or signed in) or the store (POS). Stock is claimed at <c>Confirmed</c> and held
/// until <see cref="ReservedUntilUtc"/>; the public tracking page is opened with <see cref="TrackingToken"/>.
/// </summary>
public sealed class StoreOrder
{
    public Guid Id { get; set; }
    public required string Reference { get; set; }
    public required string TrackingToken { get; set; }
    public Guid? UserAccountId { get; set; }
    public string Channel { get; set; } = OrderChannel.Online;
    public required string CustomerName { get; set; }
    /// <summary>Digits only.</summary>
    public required string PhoneNumber { get; set; }
    public bool PhoneHasWhatsApp { get; set; }
    public string? CustomerEmail { get; set; }
    public string? Governorate { get; set; }
    public string? Area { get; set; }
    public string? Street { get; set; }
    public string? Building { get; set; }
    public string? Landmark { get; set; }
    public string FulfillmentMethod { get; set; } = OrderFulfillment.Pickup;
    /// <summary>"عرفتنا منين؟" answer, one of the settings list (owner decision D5).</summary>
    public string? LeadSource { get; set; }
    public string? Usage { get; set; }
    public string? UsageNote { get; set; }
    public required string Status { get; set; } = Retail.OrderStatus.New;
    public required string PaymentStatus { get; set; } = Retail.PaymentStatus.Unpaid;
    public string PaymentMethod { get; set; } = OrderPaymentMethod.CashOnDelivery;
    public bool PaymentRequired { get; set; } = true;
    public DateTimeOffset? PaymentWaivedAtUtc { get; set; }
    public Guid? PaymentWaivedByUserAccountId { get; set; }
    public string? PaymentWaiverReason { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    /// <summary>Delivery time promised by the shipping settings when the order was placed; null for pickup or when none was set.</summary>
    public string? DeliveryTime { get; set; }
    public decimal Total { get; set; }
    /// <summary>Shipping (+ deposit) asked before the order is prepared; zero for pickup.</summary>
    public decimal UpfrontAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    /// <summary>Set when the customer matched the blacklist; confirming then needs full prepayment.</summary>
    public bool IsBlacklisted { get; set; }
    public DateTimeOffset? ReservedUntilUtc { get; set; }
    /// <summary>True = cancel and release the stock when the reservation ends; false = only flag it.</summary>
    public bool ReservationAutoRelease { get; set; }
    public DateTimeOffset? ReservationExpiredAtUtc { get; set; }
    public string? CourierName { get; set; }
    public string? WaybillNumber { get; set; }
    public int? SatisfactionScore { get; set; }
    public string? SatisfactionComment { get; set; }
    public DateTimeOffset? RatedAtUtc { get; set; }
    public DateTimeOffset? TermsAcceptedAtUtc { get; set; }
    public string? TermsPolicyVersion { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderEvent> Events { get; set; } = new List<OrderEvent>();
    public ICollection<OrderPayment> Payments { get; set; } = new List<OrderPayment>();
}

/// <summary>Money received for an order (a POS sale can be split over several methods).</summary>
public sealed class OrderPayment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public required string Method { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    /// <summary>The account the money went into; null for payments recorded before accounts existed.</summary>
    public Guid? MoneyAccountId { get; set; }
    public Guid? ActorUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>A blacklisted customer, matched on new orders by phone, email or address.</summary>
public sealed class BlacklistEntry
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? AddressKey { get; set; }
    public string? AddressText { get; set; }
    public required string Reason { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? UnitId { get; set; }
    public string? ThumbnailStorageKeySnapshot { get; set; }
    public Guid? AssemblyId { get; set; }
    public Guid? StockItemId { get; set; }
    public required string Name { get; set; }
    /// <summary>RX or BLD code of what was sold.</summary>
    public string? Code { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DirectExpenseAmount { get; set; }
    public string? DirectExpenseNote { get; set; }
    public decimal UnitCostSnapshot { get; set; }
    public decimal NetProfit { get; set; }
    public StoreOrder Order { get; set; } = null!;
    public ICollection<OrderItemComponent> Components { get; set; } = new List<OrderItemComponent>();
}

public sealed class OrderItemComponent
{
    public Guid Id { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid UnitId { get; set; }
    public decimal AllocationPercent { get; set; }
    public decimal RevenueAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DirectExpenseAmount { get; set; }
    public decimal CostAmount { get; set; }
    public decimal NetProfit { get; set; }
    /// <summary>Warranty of this unit as sold (taken when stock was claimed); null label = no warranty.</summary>
    public string? WarrantyLabel { get; set; }
    public int? WarrantyMonths { get; set; }
    public OrderItem OrderItem { get; set; } = null!;
}

public sealed class OrderEvent
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public required string EventType { get; set; }
    public string? Notes { get; set; }
    public Guid? ActorUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class OrderReturn
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid UnitId { get; set; }
    public decimal RefundAmount { get; set; }
    public required string Reason { get; set; }
    public required string Status { get; set; } = ReturnStatus.PendingInspection;
    public string? InspectionCondition { get; set; }
    public string? InspectionNotes { get; set; }
    public Guid? InspectedByUserAccountId { get; set; }
    public DateTimeOffset? InspectedAtUtc { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}
