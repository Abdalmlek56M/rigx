using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.PcBuilder;

public static class PcBuildStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
}

public static class PcSlotKey
{
    public static readonly IReadOnlyList<string> All =
        ["CPU", "Motherboard", "RAM", "GPU", "Storage", "PSU", "Case", "Cooling", "Monitor", "Peripherals"];
}

public sealed class PcBuildTemplate
{
    public Guid Id { get; set; }
    public required string TitleAr { get; set; }
    public required string TitleEn { get; set; }
    public string? SummaryAr { get; set; }
    public string? SummaryEn { get; set; }
    public required string Status { get; set; } = PcBuildStatus.Draft;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public ICollection<PcBuildSlot> Slots { get; set; } = new List<PcBuildSlot>();
}

public sealed class PcBuildSlot
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public required string SlotKey { get; set; }
    public required string LabelAr { get; set; }
    public required string LabelEn { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public PcBuildTemplate Template { get; set; } = null!;
    public ICollection<PcBuildSlotOption> Options { get; set; } = new List<PcBuildSlotOption>();
}

public sealed class PcBuildSlotOption
{
    public Guid Id { get; set; }
    public Guid SlotId { get; set; }
    public Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public required string ProductSlug { get; set; }
    public required string Sku { get; set; }
    public decimal Price { get; set; }
    public string? CategorySlug { get; set; }
    public string? PrimaryImageKey { get; set; }
    public string? SpecificationsJson { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public PcBuildSlot Slot { get; set; } = null!;
}

public sealed record PcSlotOptionView(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string Sku,
    decimal Price,
    string? CategorySlug,
    string? PrimaryImageKey,
    IReadOnlyList<KeyValuePair<string, string>> Specifications,
    bool IsDefault);

public sealed record PcSlotView(Guid Id, string SlotKey, string LabelAr, string LabelEn, bool IsRequired, int SortOrder, IReadOnlyList<PcSlotOptionView> Options);

public sealed record PcBuildView(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string? SummaryAr,
    string? SummaryEn,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion,
    IReadOnlyList<PcSlotView> Slots);

public sealed record PcSlotInput(string? SlotKey, string? LabelAr, string? LabelEn, bool IsRequired, IReadOnlyList<PcSlotOptionInput>? Options);
public sealed record PcSlotOptionInput(Guid ProductId, bool IsDefault);
public sealed record PcBuildInput(string? TitleAr, string? TitleEn, string? SummaryAr, string? SummaryEn, IReadOnlyList<PcSlotInput>? Slots);
public sealed record PcBuildUpdateRequest(string? TitleAr, string? TitleEn, string? SummaryAr, string? SummaryEn, IReadOnlyList<PcSlotInput>? Slots, long RowVersion);
public sealed record PcBuildCompatibilityRequest(IReadOnlyDictionary<string, Guid>? Selections);
public sealed record PcCompatibilityMessage(string Level, string Code, string SlotKey, string? OtherSlotKey);
public sealed record PcCompatibilityResult(string Status, decimal TotalPrice, IReadOnlyList<PcCompatibilityMessage> Messages);

public sealed class PcBuildException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class PcBuilderDbContext(DbContextOptions<PcBuilderDbContext> options) : DbContext(options)
{
    public DbSet<PcBuildTemplate> Templates => Set<PcBuildTemplate>();
    public DbSet<PcBuildSlot> Slots => Set<PcBuildSlot>();
    public DbSet<PcBuildSlotOption> Options => Set<PcBuildSlotOption>();
    public DbSet<PcCatalogProduct> Products => Set<PcCatalogProduct>();
    public DbSet<PcCatalogProductImage> ProductImages => Set<PcCatalogProductImage>();
    public DbSet<PcCatalogSpecification> Specifications => Set<PcCatalogSpecification>();
    public DbSet<PcCatalogCategory> Categories => Set<PcCatalogCategory>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<PcCatalogProduct>(e =>
        {
            e.ToTable("products", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Slug).HasColumnName("slug");
            e.Property(x => x.Sku).HasColumnName("sku");
            e.Property(x => x.BasePrice).HasColumnName("base_price");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CategoryId).HasColumnName("category_id");
        });
        model.Entity<PcCatalogProductImage>(e =>
        {
            e.ToTable("product_images", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.ThumbnailStorageKey).HasColumnName("thumbnail_storage_key");
            e.Property(x => x.IsPrimary).HasColumnName("is_primary");
        });
        model.Entity<PcCatalogSpecification>(e =>
        {
            e.ToTable("product_specifications", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.GroupName).HasColumnName("group_name");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Value).HasColumnName("value");
        });
        model.Entity<PcCatalogCategory>(e =>
        {
            e.ToTable("categories", t => t.ExcludeFromMigrations());
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Slug).HasColumnName("slug");
        });

        model.Entity<PcBuildTemplate>(e =>
        {
            e.ToTable("pc_build_templates", t =>
            {
                t.HasCheckConstraint("ck_pc_build_templates_status", "status IN ('Draft','Published')");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.TitleAr).HasColumnName("title_ar").HasMaxLength(200).IsRequired();
            e.Property(x => x.TitleEn).HasColumnName("title_en").HasMaxLength(200).IsRequired();
            e.Property(x => x.SummaryAr).HasColumnName("summary_ar").HasMaxLength(1000);
            e.Property(x => x.SummaryEn).HasColumnName("summary_en").HasMaxLength(1000);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasMany(x => x.Slots).WithOne(x => x.Template).HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Status).HasDatabaseName("ix_pc_build_templates_status");
        });
        model.Entity<PcBuildSlot>(e =>
        {
            e.ToTable("pc_build_slots");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.TemplateId).HasColumnName("template_id");
            e.Property(x => x.SlotKey).HasColumnName("slot_key").HasMaxLength(32).IsRequired();
            e.Property(x => x.LabelAr).HasColumnName("label_ar").HasMaxLength(120).IsRequired();
            e.Property(x => x.LabelEn).HasColumnName("label_en").HasMaxLength(120).IsRequired();
            e.Property(x => x.IsRequired).HasColumnName("is_required");
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.HasIndex(x => new { x.TemplateId, x.SlotKey }).IsUnique().HasDatabaseName("ux_pc_build_slots_template_key");
            e.HasMany(x => x.Options).WithOne(x => x.Slot).HasForeignKey(x => x.SlotId).OnDelete(DeleteBehavior.Cascade);
        });
        model.Entity<PcBuildSlotOption>(e =>
        {
            e.ToTable("pc_build_slot_options");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.SlotId).HasColumnName("slot_id");
            e.Property(x => x.ProductId).HasColumnName("product_id");
            e.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(255).IsRequired();
            e.Property(x => x.ProductSlug).HasColumnName("product_slug").HasMaxLength(200).IsRequired();
            e.Property(x => x.Sku).HasColumnName("sku").HasMaxLength(128).IsRequired();
            e.Property(x => x.Price).HasColumnName("price").HasPrecision(18, 2);
            e.Property(x => x.CategorySlug).HasColumnName("category_slug").HasMaxLength(160);
            e.Property(x => x.PrimaryImageKey).HasColumnName("primary_image_key").HasMaxLength(512);
            e.Property(x => x.SpecificationsJson).HasColumnName("specifications_json").HasColumnType("jsonb");
            e.Property(x => x.IsDefault).HasColumnName("is_default");
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.HasIndex(x => new { x.SlotId, x.ProductId }).IsUnique().HasDatabaseName("ux_pc_build_slot_options_product");
        });
    }
}

public sealed class PcCatalogProduct
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class PcCatalogProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ThumbnailStorageKey { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public sealed class PcCatalogSpecification
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class PcCatalogCategory
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
}
