namespace HardwareStore.Modules.Retail;

public sealed class StoreOrder
{
    public Guid Id { get; set; }
    public required string Reference { get; set; }
    public Guid? UserAccountId { get; set; }
    public required string CustomerName { get; set; }
    public required string PhoneNumber { get; set; }
    public string? AddressText { get; set; }
    public string FulfillmentMethod { get; set; } = "Handover";
    public required string LeadSource { get; set; } = Retail.LeadSource.Website;
    public required string Status { get; set; } = Retail.OrderStatus.New;
    public required string PaymentStatus { get; set; } = Retail.PaymentStatus.Unpaid;
    public string PaymentMethod { get; set; } = "CashOnHandover";
    public bool PaymentRequired { get; set; } = true;
    public DateTimeOffset? PaymentWaivedAtUtc { get; set; }
    public Guid? PaymentWaivedByUserAccountId { get; set; }
    public string? PaymentWaiverReason { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTimeOffset? TermsAcceptedAtUtc { get; set; }
    public string? TermsPolicyVersion { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderEvent> Events { get; set; } = new List<OrderEvent>();
}

public sealed class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public string? ThumbnailStorageKeySnapshot { get; set; }
    public Guid? AssemblyId { get; set; }
    public required string Name { get; set; }
    public string? Sku { get; set; }
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
