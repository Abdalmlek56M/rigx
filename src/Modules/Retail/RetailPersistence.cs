using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

public static class UnitStatus
{
    public const string Available = "Available";
    public const string InAssembly = "InAssembly";
    public const string Sold = "Sold";
    public const string PendingInspection = "PendingInspection";
    public const string Scrapped = "Scrapped";
    public const string Returned = "Returned";
}
public static class AssemblyStatus { public const string Draft = "Draft"; public const string Published = "Published"; public const string Sold = "Sold"; public const string Disassembled = "Disassembled"; }
public static class OrderStatus { public const string New = "New"; public const string Confirmed = "Confirmed"; public const string Preparing = "Preparing"; public const string Ready = "Ready"; public const string Delivered = "Delivered"; public const string Completed = "Completed"; public const string Cancelled = "Cancelled"; }
public static class PaymentStatus { public const string Unpaid = "Unpaid"; public const string PendingVerification = "PendingVerification"; public const string Paid = "Paid"; public const string Refunded = "Refunded"; public const string Waived = "Waived"; }
public static class EarningStatus { public const string Outstanding = "Outstanding"; public const string Paid = "Paid"; public const string Reversed = "Reversed"; }
public static class ReturnStatus { public const string PendingInspection = "PendingInspection"; public const string Inspected = "Inspected"; public const string Rejected = "Rejected"; }
public static class AdjustmentStatus { public const string Open = "Open"; public const string Applied = "Applied"; }
public static class LeadSource { public const string Website = "Website"; public const string Phone = "Phone"; public const string Social = "Social"; public const string Manual = "Manual"; }
public static class OrderFulfillment { public const string Delivery = "Delivery"; public const string Pickup = "Pickup"; }
public static class OrderPaymentMethod
{
    public const string CashOnDelivery = "CashOnDelivery";
    public const string BankTransfer = "BankTransfer";
    public const string MobileWalletTransfer = "MobileWalletTransfer";
    public const string ToBeArranged = "ToBeArranged";
}

public sealed class InventoryUnit
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string? Sku { get; set; }
    public required string Name { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Category { get; set; }
    public required string Condition { get; set; }
    public string? InspectionCondition { get; set; }
    public string? SerialNumber { get; set; }
    public string? WarrantyNote { get; set; }
    public string? SourceName { get; set; }
    public string? SourceType { get; set; }
    public string? PurchaseKind { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string? Description { get; set; }
    public string? ExternalCode { get; set; }
    public string? SpecificationsJson { get; set; }
    public string? ImageStorageKeysJson { get; set; }
    public decimal AcquisitionCost { get; set; }
    public decimal? StandalonePrice { get; set; }
    public decimal? PartnerPercentOverride { get; set; }
    public required string Status { get; set; } = UnitStatus.Available;
    public Guid? AssemblyId { get; set; }
    public Guid? ReceiptId { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class PurchaseReceipt
{
    public Guid Id { get; set; }
    public Guid PurchaseDraftId { get; set; }
    public Guid ReceivedByUserAccountId { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public sealed class Assembly
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? PublicTitleAr { get; set; }
    public string? PublicTitleEn { get; set; }
    public decimal SalePrice { get; set; }
    public required string Status { get; set; } = AssemblyStatus.Draft;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public ICollection<AssemblyUnit> Units { get; set; } = new List<AssemblyUnit>();
}

public sealed class AssemblyUnit
{
    public Guid AssemblyId { get; set; }
    public Guid UnitId { get; set; }
    public decimal AllocationPercent { get; set; }
    public Assembly Assembly { get; set; } = null!;
    public InventoryUnit Unit { get; set; } = null!;
}

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

public sealed class RetailExpense
{
    public Guid Id { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public required string Category { get; set; }
    public required string Classification { get; set; }
    public string? Note { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? UnitId { get; set; }
    public string? AttachmentReference { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed record ExpenseView(Guid Id, DateOnly ExpenseDate, decimal Amount, string Category, string Classification,
    string? Note, Guid? OrderId, Guid? UnitId, string? AttachmentReference, Guid CreatedByUserAccountId,
    DateTimeOffset CreatedAtUtc, long RowVersion);
public sealed record BusinessReportView(DateOnly From, DateOnly To, decimal GrossSales, decimal Discounts,
    decimal Refunds, decimal CostOfGoods, decimal ItemDirectExpenses, decimal AdditionalDirectExpenses,
    decimal NetItemProfit, decimal GeneralOverhead, decimal StoreNetResult, decimal PartnerAccrued,
    decimal PartnerPaid, decimal PartnerRecoverable, int CompletedOrders, int Returns);

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

public sealed record UnitView(Guid Id, Guid? ProductId, string Name, string? Sku, string? Brand, string? Model, string? Category,
    string? Description, string? ExternalCode, string? SpecificationsJson, string Condition, string? InspectionCondition,
    string? SerialNumber, string? SourceName, string? SourceType, string? PurchaseKind, DateOnly? PurchaseDate,
    decimal? AcquisitionCost, decimal? StandalonePrice, decimal? PartnerPercentOverride, string Status, Guid? AssemblyId,
    DateTimeOffset ReceivedAtUtc);
public sealed record AssemblyUnitView(Guid UnitId, string Name, string? Sku, string Condition, decimal? AcquisitionCost,
    decimal? StandalonePrice, decimal AllocationPercent, decimal? AllocatedAmount);
public sealed record AssemblyView(Guid Id, string Name, string? PublicTitleAr, string? PublicTitleEn, decimal SalePrice,
    string Status, IReadOnlyList<AssemblyUnitView> Units, decimal? ComponentCost, decimal? AllocatedTotal, long RowVersion);
public sealed record OrderComponentView(Guid Id, Guid UnitId, decimal AllocationPercent, decimal RevenueAmount, decimal DiscountAmount, decimal DirectExpenseAmount, decimal CostAmount, decimal NetProfit);
public sealed record OrderItemView(Guid Id, Guid? ProductId, Guid? VariantId, string? VariantName, Guid? AssemblyId, string Name, string? Sku, string? ThumbnailStorageKeySnapshot, int Quantity, decimal UnitPrice, decimal LineTotal, decimal DiscountAmount, decimal DirectExpenseAmount, string? DirectExpenseNote, decimal NetProfit, IReadOnlyList<OrderComponentView> Components);
public sealed record OrderEventView(string EventType, string? Notes, Guid? ActorUserAccountId, DateTimeOffset CreatedAtUtc);
public sealed record OrderView(Guid Id, string Reference, Guid? UserAccountId, string CustomerName, string PhoneNumber, string? AddressText, string FulfillmentMethod, string LeadSource, string Status, string PaymentStatus, string PaymentMethod, bool PaymentRequired, DateTimeOffset? PaymentWaivedAtUtc, Guid? PaymentWaivedByUserAccountId, string? PaymentWaiverReason, decimal Subtotal, decimal DeliveryFee, decimal Total, decimal RefundedAmount, string? Notes, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long RowVersion, IReadOnlyList<OrderItemView> Items, IReadOnlyList<OrderEventView> Events);
public sealed record AdminOrderUnitView(Guid UnitId, Guid? ProductId, string Name, string? Sku, string Condition, string? InspectionCondition, string? SerialNumber, string Status);
public sealed record AdminOrderDetailView(OrderView Order, IReadOnlyList<AdminOrderUnitView> Units);
public sealed record PartnerEarningView(Guid Id, Guid OrderId, Guid OrderItemId, Guid? UnitId, string PartnerName, decimal Percent, decimal RevenueAmount, decimal DiscountAmount, decimal DirectExpenseAmount, decimal CostAmount, decimal GrossProfit, decimal Amount, decimal PaidAmount, decimal OffsetAmount, string Status, DateTimeOffset? ReversedAtUtc, DateTimeOffset CreatedAtUtc);
public sealed record PartnerAdjustmentView(Guid Id, string PartnerName, decimal Amount, decimal AppliedAmount, decimal RemainingAmount, string Status, string Reason, Guid? OrderReturnId, DateTimeOffset CreatedAtUtc);
public sealed record PartnerAllocationView(Guid Id, string PartnerName, Guid? UnitId, Guid? ProductId, string? Category, decimal Percent, bool LossSharingEnabled, bool IsActive, long RowVersion);
public sealed record PartnerAllocationEventView(Guid Id, Guid AllocationId, string PartnerName, string Action, Guid ActorUserAccountId, string Reason, string SnapshotJson, DateTimeOffset CreatedAtUtc);
public sealed record PartnerAllocationPreviewView(string Source, IReadOnlyList<PartnerAllocationView> Allocations, decimal TotalPercent, decimal UnallocatedPercent);
public sealed record PartnerSettlementView(Guid Id, string PartnerName, decimal Amount, decimal OffsetApplied, string Method, string? Reference, string? Note, DateTimeOffset CreatedAtUtc);
public sealed record ReturnView(Guid Id, Guid OrderId, Guid OrderItemId, Guid UnitId, decimal RefundAmount, string Reason, string Status, string? InspectionCondition, string? InspectionNotes, Guid? InspectedByUserAccountId, DateTimeOffset? InspectedAtUtc, DateTimeOffset CreatedAtUtc, long RowVersion);
public sealed record PartnerLedgerView(string PartnerName, decimal Outstanding, decimal CashDue, decimal Recoverable);

public sealed class RetailException(int statusCode, string message) : AppException(statusCode, message);

public sealed class RetailDbContext(DbContextOptions<RetailDbContext> options) : DbContext(options)
{
    public DbSet<InventoryUnit> Units => Set<InventoryUnit>();
    public DbSet<PurchaseReceipt> Receipts => Set<PurchaseReceipt>();
    public DbSet<Assembly> Assemblies => Set<Assembly>();
    public DbSet<AssemblyUnit> AssemblyUnits => Set<AssemblyUnit>();
    public DbSet<StoreOrder> Orders => Set<StoreOrder>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemComponent> OrderItemComponents => Set<OrderItemComponent>();
    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>();
    public DbSet<PartnerAgreement> PartnerAgreements => Set<PartnerAgreement>();
    public DbSet<PartnerEarning> PartnerEarnings => Set<PartnerEarning>();
    public DbSet<PartnerSettlement> PartnerSettlements => Set<PartnerSettlement>();
    public DbSet<PartnerAdjustment> PartnerAdjustments => Set<PartnerAdjustment>();
    public DbSet<PartnerUnitAllocation> PartnerUnitAllocations => Set<PartnerUnitAllocation>();
    public DbSet<PartnerAllocationEvent> PartnerAllocationEvents => Set<PartnerAllocationEvent>();
    public DbSet<RetailExpense> Expenses => Set<RetailExpense>();
    public DbSet<OrderReturn> OrderReturns => Set<OrderReturn>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<InventoryUnit>(e =>
        {
            e.ToTable("inventory_units", t => t.HasCheckConstraint("ck_inventory_units_cost", "acquisition_cost >= 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.VariantId).HasColumnName("variant_id");
            e.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(128);
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            e.Property(x => x.Brand).HasColumnName("brand").HasMaxLength(160);
            e.Property(x => x.Model).HasColumnName("model").HasMaxLength(160);
            e.Property(x => x.Category).HasColumnName("category").HasMaxLength(160);
            e.Property(x => x.Condition).HasColumnName("condition").HasMaxLength(16).IsRequired();
            e.Property(x => x.InspectionCondition).HasColumnName("inspection_condition").HasMaxLength(16);
            e.Property(x => x.SerialNumber).HasColumnName("serial_number").HasMaxLength(128);
            e.Property(x => x.WarrantyNote).HasColumnName("warranty_note").HasMaxLength(500);
            e.Property(x => x.SourceName).HasColumnName("source_name").HasMaxLength(200);
            e.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(32);
            e.Property(x => x.PurchaseKind).HasColumnName("purchase_kind").HasMaxLength(32);
            e.Property(x => x.PurchaseDate).HasColumnName("purchase_date").HasColumnType("date");
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(2000);
            e.Property(x => x.ExternalCode).HasColumnName("external_code").HasMaxLength(128);
            e.Property(x => x.SpecificationsJson).HasColumnName("specifications_json");
            e.Property(x => x.ImageStorageKeysJson).HasColumnName("image_storage_keys_json");
            e.Property(x => x.AcquisitionCost).HasColumnName("acquisition_cost").HasPrecision(18, 2);
            e.Property(x => x.StandalonePrice).HasColumnName("standalone_price").HasPrecision(18, 2);
            e.Property(x => x.PartnerPercentOverride).HasColumnName("partner_percent_override").HasPrecision(5, 2);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(24).IsRequired();
            e.Property(x => x.AssemblyId).HasColumnName("assembly_id");
            e.Property(x => x.ReceiptId).HasColumnName("receipt_id");
            e.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(x => new { x.Status, x.ProductId }).HasDatabaseName("ix_inventory_units_status_product");
            e.HasIndex(x => x.SerialNumber).HasDatabaseName("ix_inventory_units_serial");
        });
        model.Entity<PurchaseReceipt>(e =>
        {
            e.ToTable("purchase_receipts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.PurchaseDraftId).HasColumnName("purchase_draft_id");
            e.HasIndex(x => x.PurchaseDraftId).IsUnique().HasDatabaseName("ux_purchase_receipts_draft");
            e.Property(x => x.ReceivedByUserAccountId).HasColumnName("received_by_user_account_id");
            e.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);
        });
        model.Entity<Assembly>(e =>
        {
            e.ToTable("assemblies", t => t.HasCheckConstraint("ck_assemblies_price", "sale_price >= 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.PublicTitleAr).HasColumnName("public_title_ar").HasMaxLength(200);
            e.Property(x => x.PublicTitleEn).HasColumnName("public_title_en").HasMaxLength(200);
            e.Property(x => x.SalePrice).HasColumnName("sale_price").HasPrecision(18, 2);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasMany(x => x.Units).WithOne(x => x.Assembly).HasForeignKey(x => x.AssemblyId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<AssemblyUnit>(e =>
        {
            e.ToTable("assembly_units", t => t.HasCheckConstraint("ck_assembly_units_allocation", "allocation_percent BETWEEN 0 AND 100"));
            e.HasKey(x => new { x.AssemblyId, x.UnitId });
            e.Property(x => x.AllocationPercent).HasColumnName("allocation_percent").HasPrecision(5, 2);
            e.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.UnitId).IsUnique().HasDatabaseName("ux_assembly_units_unit");
        });
        model.Entity<StoreOrder>(e =>
        {
            e.ToTable("store_orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Reference).HasColumnName("reference").HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.Reference).IsUnique().HasDatabaseName("ux_store_orders_reference");
            e.Property(x => x.UserAccountId).HasColumnName("user_account_id");
            e.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
            e.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(32).IsRequired();
            e.Property(x => x.AddressText).HasColumnName("address_text").HasMaxLength(1000);
            e.Property(x => x.FulfillmentMethod).HasColumnName("fulfillment_method").HasMaxLength(32).IsRequired();
            e.Property(x => x.LeadSource).HasColumnName("lead_source").HasMaxLength(16).IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            e.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(24).IsRequired();
            e.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(32).IsRequired();
            e.Property(x => x.PaymentRequired).HasColumnName("payment_required").HasDefaultValue(true);
            e.Property(x => x.PaymentWaivedAtUtc).HasColumnName("payment_waived_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.PaymentWaivedByUserAccountId).HasColumnName("payment_waived_by_user_account_id");
            e.Property(x => x.PaymentWaiverReason).HasColumnName("payment_waiver_reason").HasMaxLength(500);
            e.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
            e.Property(x => x.DeliveryFee).HasColumnName("delivery_fee").HasPrecision(18, 2);
            e.Property(x => x.Total).HasColumnName("total").HasPrecision(18, 2);
            e.Property(x => x.RefundedAmount).HasColumnName("refunded_amount").HasPrecision(18, 2);
            e.Property(x => x.TermsAcceptedAtUtc).HasColumnName("terms_accepted_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.TermsPolicyVersion).HasColumnName("terms_policy_version").HasMaxLength(64);
            e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000);
            e.Property(x => x.InternalNotes).HasColumnName("internal_notes").HasMaxLength(2000);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(x => new { x.Status, x.CreatedAtUtc }).HasDatabaseName("ix_store_orders_status_created");
        });
        model.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.UnitId).HasColumnName("unit_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.VariantId).HasColumnName("variant_id");
            e.Property(x => x.VariantName).HasColumnName("variant_name").HasMaxLength(200);
            e.Property(x => x.ThumbnailStorageKeySnapshot).HasColumnName("thumbnail_storage_key_snapshot").HasMaxLength(512);
            e.Property(x => x.AssemblyId).HasColumnName("assembly_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            e.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(128);
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasColumnName("line_total").HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(18, 2);
            e.Property(x => x.DirectExpenseAmount).HasColumnName("direct_expense_amount").HasPrecision(18, 2);
            e.Property(x => x.DirectExpenseNote).HasColumnName("direct_expense_note").HasMaxLength(500);
            e.Property(x => x.UnitCostSnapshot).HasColumnName("unit_cost_snapshot").HasPrecision(18, 2);
            e.Property(x => x.NetProfit).HasColumnName("net_profit").HasPrecision(18, 2);
            e.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Components).WithOne(x => x.OrderItem).HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<OrderItemComponent>(e =>
        {
            e.ToTable("order_item_components", t => t.HasCheckConstraint("ck_order_item_components_allocation", "allocation_percent BETWEEN 0 AND 100"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderItemId).HasColumnName("order_item_id");
            e.Property(x => x.UnitId).HasColumnName("unit_id");
            e.HasIndex(x => new { x.OrderItemId, x.UnitId }).IsUnique().HasDatabaseName("ux_order_item_components_unit");
            e.Property(x => x.AllocationPercent).HasColumnName("allocation_percent").HasPrecision(5, 2);
            e.Property(x => x.RevenueAmount).HasColumnName("revenue_amount").HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(18, 2);
            e.Property(x => x.DirectExpenseAmount).HasColumnName("direct_expense_amount").HasPrecision(18, 2);
            e.Property(x => x.CostAmount).HasColumnName("cost_amount").HasPrecision(18, 2);
            e.Property(x => x.NetProfit).HasColumnName("net_profit").HasPrecision(18, 2);
        });
        model.Entity<OrderEvent>(e =>
        {
            e.ToTable("order_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(64).IsRequired();
            e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000);
            e.Property(x => x.ActorUserAccountId).HasColumnName("actor_user_account_id");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.HasOne<StoreOrder>().WithMany(x => x.Events).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<PartnerAgreement>(e =>
        {
            e.ToTable("partner_agreements", t => t.HasCheckConstraint("ck_partner_agreements_percent", "default_percent BETWEEN 0 AND 100"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.PartnerName).HasColumnName("partner_name").HasMaxLength(160).IsRequired();
            e.Property(x => x.DefaultPercent).HasColumnName("default_percent").HasPrecision(5, 2);
            e.Property(x => x.LossSharingEnabled).HasColumnName("loss_sharing_enabled").HasDefaultValue(false);
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.EffectiveFrom).HasColumnName("effective_from").HasColumnType("date");
            e.Property(x => x.EffectiveTo).HasColumnName("effective_to").HasColumnType("date");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
        });
        model.Entity<PartnerEarning>(e =>
        {
            e.ToTable("partner_earnings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderItemId).HasColumnName("order_item_id");
            e.Property(x => x.OrderItemComponentId).HasColumnName("order_item_component_id");
            e.Property(x => x.UnitId).HasColumnName("unit_id");
            e.HasIndex(x => new { x.OrderItemId, x.UnitId, x.PartnerName }).IsUnique().HasDatabaseName("ux_partner_earnings_item_unit_partner");
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.PartnerName).HasColumnName("partner_name").HasMaxLength(160).IsRequired();
            e.Property(x => x.Percent).HasColumnName("percent").HasPrecision(5, 2);
            e.Property(x => x.LossSharingEnabled).HasColumnName("loss_sharing_enabled").HasDefaultValue(false);
            e.Property(x => x.RevenueAmount).HasColumnName("revenue_amount").HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasColumnName("discount_amount").HasPrecision(18, 2);
            e.Property(x => x.DirectExpenseAmount).HasColumnName("direct_expense_amount").HasPrecision(18, 2);
            e.Property(x => x.CostAmount).HasColumnName("cost_amount").HasPrecision(18, 2);
            e.Property(x => x.GrossProfit).HasColumnName("gross_profit").HasPrecision(18, 2);
            e.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasColumnName("paid_amount").HasPrecision(18, 2);
            e.Property(x => x.OffsetAmount).HasColumnName("offset_amount").HasPrecision(18, 2);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            e.Property(x => x.ReversedAtUtc).HasColumnName("reversed_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.ReversalReason).HasColumnName("reversal_reason").HasMaxLength(500);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        });
        model.Entity<PartnerSettlement>(e =>
        {
            e.ToTable("partner_settlements");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.PartnerName).HasColumnName("partner_name").HasMaxLength(160).IsRequired();
            e.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
            e.Property(x => x.OffsetApplied).HasColumnName("offset_applied").HasPrecision(18, 2);
            e.Property(x => x.Method).HasColumnName("method").HasMaxLength(32).IsRequired();
            e.Property(x => x.Reference).HasColumnName("reference").HasMaxLength(128);
            e.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
            e.Property(x => x.ActorUserAccountId).HasColumnName("actor_user_account_id");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        });
        model.Entity<PartnerUnitAllocation>(e =>
        {
            e.ToTable("partner_unit_allocations", t => t.HasCheckConstraint("ck_partner_unit_allocations_percent", "percent > 0 AND percent <= 100"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.PartnerName).HasColumnName("partner_name").HasMaxLength(160).IsRequired();
            e.Property(x => x.UnitId).HasColumnName("unit_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Category).HasColumnName("category").HasMaxLength(160);
            e.Property(x => x.Percent).HasColumnName("percent").HasPrecision(5, 2);
            e.Property(x => x.LossSharingEnabled).HasColumnName("loss_sharing_enabled").HasDefaultValue(false);
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(x => new { x.PartnerName, x.UnitId, x.ProductId, x.Category }).IsUnique()
                .HasDatabaseName("ux_partner_unit_allocations_scope");
        });
        model.Entity<PartnerAllocationEvent>(e =>
        {
            e.ToTable("partner_allocation_events");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.AllocationId).HasColumnName("allocation_id");
            e.HasIndex(x => x.AllocationId).HasDatabaseName("ix_partner_allocation_events_allocation");
            e.Property(x => x.PartnerName).HasColumnName("partner_name").HasMaxLength(160).IsRequired();
            e.Property(x => x.Action).HasColumnName("action").HasMaxLength(32).IsRequired();
            e.Property(x => x.ActorUserAccountId).HasColumnName("actor_user_account_id");
            e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
            e.Property(x => x.SnapshotJson).HasColumnName("snapshot_json").IsRequired();
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        });
        model.Entity<RetailExpense>(e =>
        {
            e.ToTable("retail_expenses", t =>
            {
                t.HasCheckConstraint("ck_retail_expenses_amount", "amount > 0");
                t.HasCheckConstraint("ck_retail_expenses_class", "classification IN ('Direct','GeneralOverhead')");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.ExpenseDate).HasColumnName("expense_date").HasColumnType("date");
            e.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
            e.Property(x => x.Category).HasColumnName("category").HasMaxLength(80).IsRequired();
            e.Property(x => x.Classification).HasColumnName("classification").HasMaxLength(20).IsRequired();
            e.Property(x => x.Note).HasColumnName("note").HasMaxLength(1000);
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.UnitId).HasColumnName("unit_id");
            e.Property(x => x.AttachmentReference).HasColumnName("attachment_reference").HasMaxLength(512);
            e.Property(x => x.CreatedByUserAccountId).HasColumnName("created_by_user_account_id");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(x => x.ExpenseDate).HasDatabaseName("ix_retail_expenses_date");
        });
        model.Entity<PartnerAdjustment>(e =>
        {
            e.ToTable("partner_adjustments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.PartnerName).HasColumnName("partner_name").HasMaxLength(160).IsRequired();
            e.Property(x => x.PartnerEarningId).HasColumnName("partner_earning_id");
            e.Property(x => x.OrderReturnId).HasColumnName("order_return_id");
            e.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
            e.Property(x => x.AppliedAmount).HasColumnName("applied_amount").HasPrecision(18, 2);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        });
        model.Entity<OrderReturn>(e =>
        {
            e.ToTable("order_returns", t => t.HasCheckConstraint("ck_order_returns_refund", "refund_amount >= 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.OrderItemId).HasColumnName("order_item_id");
            e.Property(x => x.UnitId).HasColumnName("unit_id");
            e.HasIndex(x => new { x.OrderItemId, x.UnitId }).IsUnique().HasDatabaseName("ux_order_returns_item_unit");
            e.Property(x => x.RefundAmount).HasColumnName("refund_amount").HasPrecision(18, 2);
            e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(24).IsRequired();
            e.Property(x => x.InspectionCondition).HasColumnName("inspection_condition").HasMaxLength(16);
            e.Property(x => x.InspectionNotes).HasColumnName("inspection_notes").HasMaxLength(500);
            e.Property(x => x.InspectedByUserAccountId).HasColumnName("inspected_by_user_account_id");
            e.Property(x => x.InspectedAtUtc).HasColumnName("inspected_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.CreatedByUserAccountId).HasColumnName("created_by_user_account_id");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
        });
    }
}

public sealed class RetailDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<RetailDbContext>
{
    public RetailDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<RetailDbContext>()
            .UseNpgsql("Host=localhost;Database=retail_design;Username=unused;Password=unused").Options);
}
