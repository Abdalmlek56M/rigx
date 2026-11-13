using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

public sealed class RetailDbContext(DbContextOptions<RetailDbContext> options) : DbContext(options)
{
    public DbSet<StockItem> StockItems => Set<StockItem>();
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
        model.ApplyConfiguration(new StockItemConfiguration());
        model.Entity<InventoryUnit>(e =>
        {
            e.ToTable("inventory_units", t => t.HasCheckConstraint("ck_inventory_units_cost", "acquisition_cost >= 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.StockItemId).HasColumnName("stock_item_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            e.Property(x => x.Condition).HasColumnName("condition").HasMaxLength(16).IsRequired();
            e.Property(x => x.InspectionCondition).HasColumnName("inspection_condition").HasMaxLength(16);
            e.Property(x => x.SerialNumber).HasColumnName("serial_number").HasMaxLength(128);
            e.Property(x => x.SourceName).HasColumnName("source_name").HasMaxLength(200);
            e.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(32);
            e.Property(x => x.PurchaseKind).HasColumnName("purchase_kind").HasMaxLength(32);
            e.Property(x => x.PurchaseDate).HasColumnName("purchase_date").HasColumnType("date");
            e.Property(x => x.AcquisitionCost).HasColumnName("acquisition_cost").HasPrecision(18, 2);
            e.Property(x => x.PartnerPercentOverride).HasColumnName("partner_percent_override").HasPrecision(5, 2);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(24).IsRequired();
            e.Property(x => x.AssemblyId).HasColumnName("assembly_id");
            e.Property(x => x.ReceiptId).HasColumnName("receipt_id");
            e.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(x => new { x.Status, x.ProductId }).HasDatabaseName("ix_inventory_units_status_product");
            e.HasIndex(x => x.SerialNumber).HasDatabaseName("ix_inventory_units_serial");
            e.HasIndex(x => x.StockItemId).HasDatabaseName("ix_inventory_units_stock_item");
            e.HasOne(x => x.StockItem).WithMany(x => x.Units).HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<PurchaseReceipt>(e =>
        {
            e.ToTable("purchase_receipts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.PurchaseId).HasColumnName("purchase_draft_id");
            e.HasIndex(x => x.PurchaseId).IsUnique().HasDatabaseName("ux_purchase_receipts_draft");
            e.Property(x => x.ReceivedByUserAccountId).HasColumnName("received_by_user_account_id");
            e.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);
        });
        model.Entity<Assembly>(e =>
        {
            e.ToTable("assemblies", t =>
            {
                t.HasCheckConstraint("ck_assemblies_price", "sale_price >= 0");
                t.HasCheckConstraint("ck_assemblies_copies", "copies >= 1");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(16).IsRequired();
            e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_assemblies_code");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.PublicTitleAr).HasColumnName("public_title_ar").HasMaxLength(200);
            e.Property(x => x.PublicTitleEn).HasColumnName("public_title_en").HasMaxLength(200);
            e.Property(x => x.SalePrice).HasColumnName("sale_price").HasPrecision(18, 2);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            e.Property(x => x.Usage).HasColumnName("usage").HasMaxLength(16);
            e.Property(x => x.SellMode).HasColumnName("sell_mode").HasMaxLength(16).IsRequired().HasDefaultValue(AssemblySellMode.Full);
            e.Property(x => x.WarrantyLabel).HasColumnName("warranty_label").HasMaxLength(64);
            e.Property(x => x.WarrantyMonths).HasColumnName("warranty_months");
            e.Property(x => x.Copies).HasColumnName("copies").HasDefaultValue(1);
            e.Property(x => x.SuitableForAr).HasColumnName("suitable_for_ar").HasMaxLength(AssemblyRules.MaxSuitableForLength);
            e.Property(x => x.SuitableForEn).HasColumnName("suitable_for_en").HasMaxLength(AssemblyRules.MaxSuitableForLength);
            StockItemConfiguration.Document(e.Property(x => x.Photos).HasColumnName("photos"), "'[]'::jsonb");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(x => new { x.Status, x.Usage }).HasDatabaseName("ix_assemblies_listing");
            e.HasMany(x => x.Units).WithOne(x => x.Assembly).HasForeignKey(x => x.AssemblyId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<AssemblyUnit>(e =>
        {
            e.ToTable("assembly_units", t =>
            {
                t.HasCheckConstraint("ck_assembly_units_allocation", "allocation_percent BETWEEN 0 AND 100");
                t.HasCheckConstraint("ck_assembly_units_part_price", "part_price >= 0");
            });
            e.HasKey(x => new { x.AssemblyId, x.UnitId });
            e.Property(x => x.AllocationPercent).HasColumnName("allocation_percent").HasPrecision(5, 2);
            e.Property(x => x.PartPrice).HasColumnName("part_price").HasPrecision(18, 2);
            e.Property(x => x.WarrantyLabel).HasColumnName("warranty_label").HasMaxLength(64);
            e.Property(x => x.WarrantyMonths).HasColumnName("warranty_months");
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
            e.Property(x => x.ThumbnailStorageKeySnapshot).HasColumnName("thumbnail_storage_key_snapshot").HasMaxLength(512);
            e.Property(x => x.AssemblyId).HasColumnName("assembly_id");
            e.Property(x => x.StockItemId).HasColumnName("stock_item_id");
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
