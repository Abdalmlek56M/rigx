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
    public DbSet<OrderPayment> OrderPayments => Set<OrderPayment>();
    public DbSet<BlacklistEntry> Blacklist => Set<BlacklistEntry>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<PartnerCapitalEntry> PartnerCapitalEntries => Set<PartnerCapitalEntry>();
    public DbSet<PartnerEarning> PartnerEarnings => Set<PartnerEarning>();
    public DbSet<PartnerSettlement> PartnerSettlements => Set<PartnerSettlement>();
    public DbSet<PartnerAdjustment> PartnerAdjustments => Set<PartnerAdjustment>();
    public DbSet<RetailExpense> Expenses => Set<RetailExpense>();
    public DbSet<OrderReturn> OrderReturns => Set<OrderReturn>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceCounter> InvoiceCounters => Set<InvoiceCounter>();

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
            e.Property(x => x.PartnerId).HasColumnName("partner_id");
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(24).IsRequired();
            e.Property(x => x.AssemblyId).HasColumnName("assembly_id");
            e.Property(x => x.ReceiptId).HasColumnName("receipt_id");
            e.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(x => new { x.Status, x.ProductId }).HasDatabaseName("ix_inventory_units_status_product");
            e.HasIndex(x => x.SerialNumber).HasDatabaseName("ix_inventory_units_serial");
            e.HasIndex(x => x.StockItemId).HasDatabaseName("ix_inventory_units_stock_item");
            e.HasIndex(x => x.PartnerId).HasDatabaseName("ix_inventory_units_partner");
            e.HasOne<Partner>().WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
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
            e.Property(x => x.TrackingToken).HasColumnName("tracking_token").HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.TrackingToken).IsUnique().HasDatabaseName("ux_store_orders_tracking_token");
            e.Property(x => x.Channel).HasColumnName("channel").HasMaxLength(16).IsRequired();
            e.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired();
            e.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.PhoneNumber).HasDatabaseName("ix_store_orders_phone");
            e.Property(x => x.PhoneHasWhatsApp).HasColumnName("phone_has_whatsapp");
            e.Property(x => x.CustomerEmail).HasColumnName("customer_email").HasMaxLength(256);
            e.Property(x => x.Governorate).HasColumnName("governorate").HasMaxLength(8);
            e.Property(x => x.Area).HasColumnName("area").HasMaxLength(120);
            e.Property(x => x.Street).HasColumnName("street").HasMaxLength(200);
            e.Property(x => x.Building).HasColumnName("building").HasMaxLength(120);
            e.Property(x => x.Landmark).HasColumnName("landmark").HasMaxLength(200);
            e.Property(x => x.FulfillmentMethod).HasColumnName("fulfillment_method").HasMaxLength(32).IsRequired();
            e.Property(x => x.LeadSource).HasColumnName("lead_source").HasMaxLength(60);
            e.Property(x => x.Usage).HasColumnName("usage").HasMaxLength(16);
            e.Property(x => x.UsageNote).HasColumnName("usage_note").HasMaxLength(200);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            e.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasMaxLength(24).IsRequired();
            e.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(32).IsRequired();
            e.Property(x => x.PaymentRequired).HasColumnName("payment_required").HasDefaultValue(true);
            e.Property(x => x.PaymentWaivedAtUtc).HasColumnName("payment_waived_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.PaymentWaivedByUserAccountId).HasColumnName("payment_waived_by_user_account_id");
            e.Property(x => x.PaymentWaiverReason).HasColumnName("payment_waiver_reason").HasMaxLength(500);
            e.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
            e.Property(x => x.DeliveryFee).HasColumnName("delivery_fee").HasPrecision(18, 2);
            e.Property(x => x.DeliveryTime).HasColumnName("delivery_time").HasMaxLength(60);
            e.Property(x => x.Total).HasColumnName("total").HasPrecision(18, 2);
            e.Property(x => x.UpfrontAmount).HasColumnName("upfront_amount").HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasColumnName("paid_amount").HasPrecision(18, 2);
            e.Property(x => x.RefundedAmount).HasColumnName("refunded_amount").HasPrecision(18, 2);
            e.Property(x => x.IsBlacklisted).HasColumnName("is_blacklisted");
            e.Property(x => x.ReservedUntilUtc).HasColumnName("reserved_until_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.ReservationAutoRelease).HasColumnName("reservation_auto_release");
            e.Property(x => x.ReservationExpiredAtUtc).HasColumnName("reservation_expired_at_utc").HasColumnType("timestamp with time zone");
            e.HasIndex(x => x.ReservedUntilUtc).HasDatabaseName("ix_store_orders_reserved_until")
                .HasFilter("reserved_until_utc IS NOT NULL AND reservation_expired_at_utc IS NULL");
            e.Property(x => x.CourierName).HasColumnName("courier_name").HasMaxLength(120);
            e.Property(x => x.WaybillNumber).HasColumnName("waybill_number").HasMaxLength(80);
            e.Property(x => x.SatisfactionScore).HasColumnName("satisfaction_score");
            e.Property(x => x.SatisfactionComment).HasColumnName("satisfaction_comment").HasMaxLength(500);
            e.Property(x => x.RatedAtUtc).HasColumnName("rated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.TermsAcceptedAtUtc).HasColumnName("terms_accepted_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.TermsPolicyVersion).HasColumnName("terms_policy_version").HasMaxLength(64);
            e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000);
            e.Property(x => x.InternalNotes).HasColumnName("internal_notes").HasMaxLength(2000);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(x => new { x.Status, x.CreatedAtUtc }).HasDatabaseName("ix_store_orders_status_created");
            e.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<OrderPayment>(e =>
        {
            e.ToTable("order_payments", t => t.HasCheckConstraint("ck_order_payments_amount", "amount > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.Method).HasColumnName("method").HasMaxLength(32).IsRequired();
            e.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
            e.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
            e.Property(x => x.ActorUserAccountId).HasColumnName("actor_user_account_id");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        });
        model.Entity<BlacklistEntry>(e =>
        {
            e.ToTable("blacklist_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            e.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(32);
            e.HasIndex(x => x.PhoneNumber).HasDatabaseName("ix_blacklist_entries_phone");
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(256);
            e.HasIndex(x => x.Email).HasDatabaseName("ix_blacklist_entries_email");
            e.Property(x => x.AddressKey).HasColumnName("address_key").HasMaxLength(700);
            e.HasIndex(x => x.AddressKey).HasDatabaseName("ix_blacklist_entries_address");
            e.Property(x => x.AddressText).HasColumnName("address_text").HasMaxLength(700);
            e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
            e.Property(x => x.CreatedByUserAccountId).HasColumnName("created_by_user_account_id");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        });
        model.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.Property(x => x.UnitId).HasColumnName("unit_id");
            e.Property(x => x.ThumbnailStorageKeySnapshot).HasColumnName("thumbnail_storage_key_snapshot").HasMaxLength(512);
            e.Property(x => x.AssemblyId).HasColumnName("assembly_id");
            e.Property(x => x.StockItemId).HasColumnName("stock_item_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(128);
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
            e.Property(x => x.WarrantyLabel).HasColumnName("warranty_label").HasMaxLength(64);
            e.Property(x => x.WarrantyMonths).HasColumnName("warranty_months");
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
        model.Entity<Partner>(e =>
        {
            e.ToTable("partners", t => t.HasCheckConstraint("ck_partners_percent", "default_percent BETWEEN 0 AND 100"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(PartnerRules.NameMaxLength).IsRequired();
            e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ux_partners_name");
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(11);
            e.Property(x => x.JoinDate).HasColumnName("join_date").HasColumnType("date");
            e.Property(x => x.DefaultPercent).HasColumnName("default_percent").HasPrecision(5, 2);
            e.Property(x => x.Basis).HasColumnName("basis").HasMaxLength(16).IsRequired();
            e.Property(x => x.LossSharingEnabled).HasColumnName("loss_sharing_enabled").HasDefaultValue(false);
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(PartnerRules.NotesMaxLength);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
        });
        model.Entity<PartnerCapitalEntry>(e =>
        {
            e.ToTable("partner_capital_entries", t =>
            {
                t.HasCheckConstraint("ck_partner_capital_entries_kind", "kind IN ('Deposit','Withdrawal')");
                t.HasCheckConstraint("ck_partner_capital_entries_amount", "amount > 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.PartnerId).HasColumnName("partner_id");
            e.HasIndex(x => x.PartnerId).HasDatabaseName("ix_partner_capital_entries_partner");
            e.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(16).IsRequired();
            e.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
            e.Property(x => x.EntryDate).HasColumnName("entry_date").HasColumnType("date");
            e.Property(x => x.Method).HasColumnName("method").HasMaxLength(32);
            e.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
            e.Property(x => x.ActorUserAccountId).HasColumnName("actor_user_account_id");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.HasOne<Partner>().WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<PartnerEarning>(e =>
        {
            e.ToTable("partner_earnings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderItemId).HasColumnName("order_item_id");
            e.Property(x => x.OrderItemComponentId).HasColumnName("order_item_component_id");
            e.Property(x => x.UnitId).HasColumnName("unit_id");
            e.Property(x => x.PartnerId).HasColumnName("partner_id");
            e.Property(x => x.PeriodMonth).HasColumnName("period_month").HasColumnType("date");
            e.HasIndex(x => new { x.OrderItemId, x.UnitId, x.PartnerId }).IsUnique().HasDatabaseName("ux_partner_earnings_item_unit_partner");
            e.HasIndex(x => new { x.PartnerId, x.PeriodMonth }).IsUnique().HasFilter("period_month IS NOT NULL")
                .HasDatabaseName("ux_partner_earnings_partner_month");
            e.HasIndex(x => x.PartnerId).HasDatabaseName("ix_partner_earnings_partner");
            e.HasOne<Partner>().WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.OrderId).HasColumnName("order_id");
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
            e.Property(x => x.PartnerId).HasColumnName("partner_id");
            e.HasIndex(x => x.PartnerId).HasDatabaseName("ix_partner_settlements_partner");
            e.HasOne<Partner>().WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
            e.Property(x => x.OffsetApplied).HasColumnName("offset_applied").HasPrecision(18, 2);
            e.Property(x => x.Method).HasColumnName("method").HasMaxLength(32).IsRequired();
            e.Property(x => x.Reference).HasColumnName("reference").HasMaxLength(128);
            e.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
            e.Property(x => x.ActorUserAccountId).HasColumnName("actor_user_account_id");
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
            e.Property(x => x.PartnerId).HasColumnName("partner_id");
            e.HasIndex(x => x.PartnerId).HasDatabaseName("ix_partner_adjustments_partner");
            e.HasOne<Partner>().WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
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
        model.Entity<Warranty>(e =>
        {
            e.ToTable("warranties");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.HasIndex(x => x.OrderId).HasDatabaseName("ix_warranties_order");
            e.Property(x => x.OrderItemId).HasColumnName("order_item_id");
            e.Property(x => x.UnitId).HasColumnName("unit_id");
            e.HasIndex(x => new { x.OrderItemId, x.UnitId }).IsUnique().HasDatabaseName("ux_warranties_item_unit");
            e.Property(x => x.StockItemId).HasColumnName("stock_item_id");
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(16).IsRequired();
            e.HasIndex(x => x.Code).HasDatabaseName("ix_warranties_code");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            e.Property(x => x.Condition).HasColumnName("condition").HasMaxLength(16).IsRequired();
            e.Property(x => x.SerialNumber).HasColumnName("serial_number").HasMaxLength(128);
            e.HasIndex(x => x.SerialNumber).HasDatabaseName("ix_warranties_serial");
            e.Property(x => x.BuildCode).HasColumnName("build_code").HasMaxLength(16);
            e.Property(x => x.Label).HasColumnName("label").HasMaxLength(64).IsRequired();
            e.Property(x => x.Months).HasColumnName("months");
            e.Property(x => x.StartsAtUtc).HasColumnName("starts_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.EndsAtUtc).HasColumnName("ends_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.PhoneNumber).HasDatabaseName("ix_warranties_phone");
            e.Property(x => x.UserAccountId).HasColumnName("user_account_id");
            e.HasIndex(x => x.UserAccountId).HasDatabaseName("ix_warranties_user");
            e.Property(x => x.VoidedAtUtc).HasColumnName("voided_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.VoidReason).HasColumnName("void_reason").HasMaxLength(500);
            e.Property(x => x.VoidedByUserAccountId).HasColumnName("voided_by_user_account_id");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
        });
        model.Entity<Invoice>(e =>
        {
            e.ToTable("invoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.OrderId).HasColumnName("order_id");
            e.HasIndex(x => x.OrderId).IsUnique().HasDatabaseName("ux_invoices_order");
            e.Property(x => x.Number).HasColumnName("number").HasMaxLength(24).IsRequired();
            e.HasIndex(x => x.Number).IsUnique().HasDatabaseName("ux_invoices_number");
            e.Property(x => x.Year).HasColumnName("year");
            e.Property(x => x.Sequence).HasColumnName("sequence");
            e.Property(x => x.AccessToken).HasColumnName("access_token").HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.AccessToken).IsUnique().HasDatabaseName("ux_invoices_token");
            e.Property(x => x.SellerName).HasColumnName("seller_name").HasMaxLength(200);
            e.Property(x => x.IssuedAtUtc).HasColumnName("issued_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        });
        model.Entity<InvoiceCounter>(e =>
        {
            e.ToTable("invoice_counters");
            e.HasKey(x => x.Year);
            e.Property(x => x.Year).HasColumnName("year").ValueGeneratedNever();
            e.Property(x => x.LastNumber).HasColumnName("last_number");
        });
    }
}

public sealed class RetailDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<RetailDbContext>
{
    public RetailDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<RetailDbContext>()
            .UseNpgsql("Host=localhost;Database=retail_design;Username=unused;Password=unused").Options);
}
