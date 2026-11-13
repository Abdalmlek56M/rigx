using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Purchasing;

public static class PurchaseSourceType
{
    public const string RegisteredSupplier = "RegisteredSupplier";
    public const string Marketplace = "Marketplace";
    public const string IndividualSeller = "IndividualSeller";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All =
        [RegisteredSupplier, Marketplace, IndividualSeller, Other];
}

public static class PurchaseDraftStatus
{
    public const string Draft = "Draft";
    public const string Cancelled = "Cancelled";
    public const string Received = "Received";
}

public sealed class PurchaseDraft
{
    public Guid Id { get; set; }
    public required string SourceType { get; set; }
    public string PurchaseKind { get; set; } = "Component";
    public Guid? SupplierId { get; set; }
    public string? MarketplaceName { get; set; }
    public string? ExternalSellerName { get; set; }
    public string? ExternalSellerContact { get; set; }
    public string? ListingUrl { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string? Notes { get; set; }
    public required string Status { get; set; } = PurchaseDraftStatus.Draft;
    public decimal TotalAmount { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public ICollection<PurchaseDraftItem> Items { get; set; } = new List<PurchaseDraftItem>();
}

public sealed class PurchaseDraftItem
{
    public Guid Id { get; set; }
    public Guid DraftId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public required string ItemName { get; set; }
    public required string Sku { get; set; }
    public string ItemKind { get; set; } = "Component";
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Category { get; set; }
    public string? Condition { get; set; }
    public string? Description { get; set; }
    public string? ExternalCode { get; set; }
    public string? SpecificationsJson { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }
    public PurchaseDraft Draft { get; set; } = null!;
}

public sealed record PurchaseDraftItemView(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    string ItemName,
    string Sku,
    string ItemKind,
    string? Brand,
    string? Model,
    string? Category,
    string? Condition,
    string? Description,
    string? ExternalCode,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record PurchaseDraftView(
    Guid Id,
    string SourceType,
    string PurchaseKind,
    Guid? SupplierId,
    string? SupplierCompanyName,
    string? MarketplaceName,
    string? ExternalSellerName,
    string? ExternalSellerContact,
    string? ListingUrl,
    DateOnly? PurchaseDate,
    string? Notes,
    string Status,
    decimal TotalAmount,
    Guid CreatedByUserAccountId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion,
    IReadOnlyList<PurchaseDraftItemView> Items);

public sealed record PurchaseDraftPage(
    IReadOnlyList<PurchaseDraftView> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record PurchaseDraftItemInput(
    Guid ProductId,
    Guid? VariantId,
    int Quantity,
    decimal UnitPrice,
    string? ItemName = null,
    string? ItemKind = null,
    string? Brand = null,
    string? Model = null,
    string? Category = null,
    string? Condition = null,
    string? Description = null,
    string? ExternalCode = null,
    string? Specifications = null);

public sealed record PurchaseDraftCreateRequest(
    string? SourceType,
    Guid? SupplierId,
    string? MarketplaceName,
    string? ExternalSellerName,
    string? ExternalSellerContact,
    string? ListingUrl,
    DateOnly? PurchaseDate,
    string? Notes,
    IReadOnlyList<PurchaseDraftItemInput>? Items,
    string? PurchaseKind = null);

public sealed record PurchaseDraftUpdateRequest(
    string? SourceType,
    Guid? SupplierId,
    string? MarketplaceName,
    string? ExternalSellerName,
    string? ExternalSellerContact,
    string? ListingUrl,
    DateOnly? PurchaseDate,
    string? Notes,
    IReadOnlyList<PurchaseDraftItemInput>? Items,
    long RowVersion,
    string? PurchaseKind = null);

public static class PurchaseDraftConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<PurchaseDraft>(entity =>
        {
            entity.ToTable("purchase_drafts", table =>
            {
                table.HasCheckConstraint("ck_purchase_drafts_source",
                    "source_type IN ('RegisteredSupplier','Marketplace','IndividualSeller','Other')");
                table.HasCheckConstraint("ck_purchase_drafts_status", "status IN ('Draft','Cancelled','Received')");
                table.HasCheckConstraint("ck_purchase_drafts_kind",
                    "purchase_kind IN ('Component','Components','Prebuilt','Bundle','Other')");
                table.HasCheckConstraint("ck_purchase_drafts_total", "total_amount >= 0");
                table.HasCheckConstraint("ck_purchase_drafts_registered_supplier",
                    "source_type <> 'RegisteredSupplier' OR supplier_id IS NOT NULL");
            });
            entity.HasKey(draft => draft.Id);
            entity.Property(draft => draft.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(draft => draft.SourceType).HasColumnName("source_type").HasMaxLength(32).IsRequired();
            entity.Property(draft => draft.PurchaseKind).HasColumnName("purchase_kind").HasMaxLength(16).IsRequired();
            entity.Property(draft => draft.SupplierId).HasColumnName("supplier_id");
            entity.Property(draft => draft.MarketplaceName).HasColumnName("marketplace_name").HasMaxLength(160);
            entity.Property(draft => draft.ExternalSellerName).HasColumnName("external_seller_name").HasMaxLength(160);
            entity.Property(draft => draft.ExternalSellerContact).HasColumnName("external_seller_contact").HasMaxLength(160);
            entity.Property(draft => draft.ListingUrl).HasColumnName("listing_url").HasMaxLength(512);
            entity.Property(draft => draft.PurchaseDate).HasColumnName("purchase_date").HasColumnType("date");
            entity.Property(draft => draft.Notes).HasColumnName("notes").HasMaxLength(2000);
            entity.Property(draft => draft.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(draft => draft.TotalAmount).HasColumnName("total_amount").HasPrecision(18, 2);
            entity.Property(draft => draft.CreatedByUserAccountId).HasColumnName("created_by_user_account_id");
            entity.Property(draft => draft.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(draft => draft.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(draft => draft.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(draft => new { draft.Status, draft.CreatedAtUtc })
                .HasDatabaseName("ix_purchase_drafts_status_created");
            entity.HasIndex(draft => draft.SourceType).HasDatabaseName("ix_purchase_drafts_source");
            entity.HasOne<SupplierProfile>().WithMany().HasForeignKey(draft => draft.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(draft => draft.Items).WithOne(item => item.Draft)
                .HasForeignKey(item => item.DraftId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<PurchaseDraftItem>(entity =>
        {
            entity.ToTable("purchase_draft_items", table =>
            {
                table.HasCheckConstraint("ck_purchase_draft_items_quantity", "quantity > 0 AND quantity <= 100000");
                table.HasCheckConstraint("ck_purchase_draft_items_price", "unit_price > 0");
                table.HasCheckConstraint("ck_purchase_draft_items_total", "line_total > 0");
            });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.DraftId).HasColumnName("draft_id");
            entity.Property(item => item.ProductId).HasColumnName("product_id");
            entity.Property(item => item.VariantId).HasColumnName("variant_id");
            entity.Property(item => item.ItemName).HasColumnName("item_name").HasMaxLength(255).IsRequired();
            entity.Property(item => item.Sku).HasColumnName("sku").HasMaxLength(128).IsRequired();
            entity.Property(item => item.ItemKind).HasColumnName("item_kind").HasMaxLength(16).IsRequired();
            entity.Property(item => item.Brand).HasColumnName("brand").HasMaxLength(160);
            entity.Property(item => item.Model).HasColumnName("model").HasMaxLength(160);
            entity.Property(item => item.Category).HasColumnName("category").HasMaxLength(160);
            entity.Property(item => item.Condition).HasColumnName("item_condition").HasMaxLength(32);
            entity.Property(item => item.Description).HasColumnName("description").HasMaxLength(4000);
            entity.Property(item => item.ExternalCode).HasColumnName("external_code").HasMaxLength(128);
            entity.Property(item => item.SpecificationsJson).HasColumnName("specifications_json").HasColumnType("jsonb");
            entity.Property(item => item.Quantity).HasColumnName("quantity");
            entity.Property(item => item.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
            entity.Property(item => item.LineTotal).HasColumnName("line_total").HasPrecision(18, 2);
            entity.Property(item => item.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.HasIndex(item => item.DraftId).HasDatabaseName("ix_purchase_draft_items_draft");
        });
    }
}
