using HardwareStore.Common.Infrastructure.Audit;
using HardwareStore.Common.Infrastructure.Trash;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace HardwareStore.Modules.Catalog;

[JsonConverter(typeof(JsonStringEnumConverter<CatalogProductStatus>))]
public enum CatalogProductStatus
{
    Draft,
    Published
}

[JsonConverter(typeof(JsonStringEnumConverter<CatalogProductCondition>))]
public enum CatalogProductCondition
{
    New,
    Used
}

public sealed class CatalogCategory
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public required string Name { get; set; }
    /// <summary>The English name shown on the English site; the Arabic <see cref="Name"/> is used when it is empty.</summary>
    public string? NameEn { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? IconStorageKey { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Code { get; set; }
    public bool ShowInMenu { get; set; } = true;
    public bool ShowInBuilder { get; set; }
    /// <summary>The sale prices this category offers (see <see cref="CategoryPriceOptions"/>); all on by default.</summary>
    public bool PriceAlone { get; set; } = true;
    public bool PriceWithBoard { get; set; } = true;
    public bool PriceInBuild { get; set; } = true;
    public List<SpecDefinition> SpecDefinitions { get; set; } = [];
    public int RowVersion { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public CatalogCategory? Parent { get; set; }
    public ICollection<CatalogCategory> Children { get; set; } = new List<CatalogCategory>();
    public ICollection<CatalogProduct> Products { get; set; } = new List<CatalogProduct>();
}

public sealed class CatalogBrand
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? LogoStorageKey { get; set; }
    public string? SupportUrl { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Position in the brand lists (admin and storefront); equal positions fall back to the name.</summary>
    public int SortOrder { get; set; }
    public int RowVersion { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<CatalogProduct> Products { get; set; } = new List<CatalogProduct>();
    public ICollection<CatalogBrandCategory> Categories { get; set; } = new List<CatalogBrandCategory>();
}

/// <summary>A suggestion that a brand belongs to a category: pickers list such brands first. It never limits what can be chosen.</summary>
public sealed class CatalogBrandCategory
{
    public Guid BrandId { get; set; }
    public Guid CategoryId { get; set; }
}

public sealed class CatalogProduct
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Sku { get; set; }
    public string? Barcode { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public CatalogProductStatus Status { get; set; } = CatalogProductStatus.Draft;
    public CatalogProductCondition Condition { get; set; } = CatalogProductCondition.New;
    public int RowVersion { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public CatalogCategory Category { get; set; } = null!;
    public CatalogBrand? Brand { get; set; }
    public ICollection<CatalogProductSpecification> Specifications { get; set; } = new List<CatalogProductSpecification>();
    public ICollection<CatalogProductImage> Images { get; set; } = new List<CatalogProductImage>();
}

public sealed class CatalogProductSpecification
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public required string GroupName { get; set; }
    public required string Name { get; set; }
    public required string Value { get; set; }
    public int SortOrder { get; set; }
    public CatalogProduct Product { get; set; } = null!;
}

public sealed class CatalogProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public required string StorageKey { get; set; }
    public required string ThumbnailStorageKey { get; set; }
    public string? OriginalStorageKey { get; set; }
    public required string AltText { get; set; }
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int ThumbnailWidth { get; set; }
    public int ThumbnailHeight { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public CatalogProduct Product { get; set; } = null!;
}

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<CatalogCategory> Categories => Set<CatalogCategory>();
    public DbSet<CatalogBrand> Brands => Set<CatalogBrand>();
    public DbSet<CatalogBrandCategory> BrandCategories => Set<CatalogBrandCategory>();
    public DbSet<CatalogProduct> Products => Set<CatalogProduct>();
    public DbSet<CatalogProductSpecification> ProductSpecifications => Set<CatalogProductSpecification>();
    public DbSet<CatalogProductImage> ProductImages => Set<CatalogProductImage>();
    public DbSet<ContentMediaAsset> ContentMediaAssets => Set<ContentMediaAsset>();
    public DbSet<ContentMediaAssetUsage> ContentMediaAssetUsages => Set<ContentMediaAssetUsage>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<StoreReview> StoreReviews => Set<StoreReview>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        ContentMediaModelConfiguration.Configure(model);
        ReviewModelConfiguration.Configure(model);
        model.Entity<CatalogCategory>(entity =>
        {
            entity.ToTable("categories", table =>
            {
                table.HasCheckConstraint("ck_categories_self_parent", "parent_id IS NULL OR parent_id <> id");
                table.HasCheckConstraint("ck_categories_sort_order", "sort_order >= 0");
                table.HasCheckConstraint("ck_categories_name_not_blank", "length(btrim(name)) > 0");
                table.HasCheckConstraint("ck_categories_slug_format", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
                table.HasCheckConstraint("ck_categories_price_option", "price_alone OR price_with_board OR price_in_build");
            });
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(category => category.ParentId).HasColumnName("parent_id");
            entity.Property(category => category.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
            entity.Property(category => category.NameEn).HasColumnName("name_en").HasMaxLength(160);
            entity.Property(category => category.Slug).HasColumnName("slug").HasMaxLength(160).IsRequired();
            entity.HasIndex(category => category.Slug).IsUnique().HasDatabaseName("ux_categories_slug");
            entity.Property(category => category.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(category => category.IconStorageKey).HasColumnName("icon_storage_key").HasMaxLength(512);
            entity.Property(category => category.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(category => category.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(category => category.Code).HasColumnName("code").HasMaxLength(16);
            entity.HasIndex(category => category.Code).IsUnique().HasDatabaseName("ux_categories_code").HasFilter("code IS NOT NULL");
            entity.Property(category => category.ShowInMenu).HasColumnName("show_in_menu").HasDefaultValue(true);
            entity.Property(category => category.ShowInBuilder).HasColumnName("show_in_builder").HasDefaultValue(false);
            entity.Property(category => category.PriceAlone).HasColumnName("price_alone").HasDefaultValue(true);
            entity.Property(category => category.PriceWithBoard).HasColumnName("price_with_board").HasDefaultValue(true);
            entity.Property(category => category.PriceInBuild).HasColumnName("price_in_build").HasDefaultValue(true);
            CategorySpecDefinitionsMapping.Configure(entity.Property(category => category.SpecDefinitions));
            entity.Property(category => category.RowVersion).HasColumnName("row_version").IsConcurrencyToken().HasDefaultValue(1);
            entity.Property(category => category.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(category => category.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.HasIndex(category => new { category.ParentId, category.SortOrder }).HasDatabaseName("ix_categories_parent_sort");
            entity.HasOne(category => category.Parent).WithMany(category => category.Children)
                .HasForeignKey(category => category.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<CatalogBrand>(entity =>
        {
            entity.ToTable("brands", table =>
            {
                table.HasCheckConstraint("ck_brands_name_not_blank", "length(btrim(name)) > 0");
                table.HasCheckConstraint("ck_brands_slug_format", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
            });
            entity.HasKey(brand => brand.Id);
            entity.Property(brand => brand.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(brand => brand.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
            entity.Property(brand => brand.Slug).HasColumnName("slug").HasMaxLength(160).IsRequired();
            entity.HasIndex(brand => brand.Slug).IsUnique().HasDatabaseName("ux_brands_slug");
            entity.Property(brand => brand.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(brand => brand.LogoStorageKey).HasColumnName("logo_storage_key").HasMaxLength(512);
            entity.Property(brand => brand.SupportUrl).HasColumnName("support_url").HasMaxLength(512);
            entity.Property(brand => brand.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(brand => brand.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(brand => brand.RowVersion).HasColumnName("row_version").IsConcurrencyToken().HasDefaultValue(1);
            entity.Property(brand => brand.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(brand => brand.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
        });

        model.Entity<CatalogBrandCategory>(entity =>
        {
            entity.ToTable("brand_categories");
            entity.HasKey(link => new { link.BrandId, link.CategoryId });
            entity.Property(link => link.BrandId).HasColumnName("brand_id");
            entity.Property(link => link.CategoryId).HasColumnName("category_id");
            entity.HasIndex(link => link.CategoryId).HasDatabaseName("ix_brand_categories_category");
            entity.HasOne<CatalogBrand>().WithMany(brand => brand.Categories).HasForeignKey(link => link.BrandId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CatalogCategory>().WithMany().HasForeignKey(link => link.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<CatalogProduct>(entity =>
        {
            entity.ToTable("products", table =>
            {
                table.HasCheckConstraint("ck_products_base_price", "base_price >= 0");
                table.HasCheckConstraint("ck_products_status", "status IN ('Draft', 'Published')");
                table.HasCheckConstraint("ck_products_condition", "condition IN ('New', 'Used')");
                table.HasCheckConstraint("ck_products_barcode_not_blank", "barcode IS NULL OR length(btrim(barcode)) > 0");
                table.HasCheckConstraint("ck_products_name_not_blank", "length(btrim(name)) > 0");
                table.HasCheckConstraint("ck_products_sku_not_blank", "length(btrim(sku)) > 0");
                table.HasCheckConstraint("ck_products_slug_format", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
            });
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(product => product.CategoryId).HasColumnName("category_id");
            entity.Property(product => product.BrandId).HasColumnName("brand_id");
            entity.Property(product => product.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(product => product.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
            entity.HasIndex(product => product.Slug).IsUnique().HasDatabaseName("ux_products_slug");
            entity.Property(product => product.Sku).HasColumnName("sku").HasMaxLength(128).IsRequired();
            entity.HasIndex(product => product.Sku).IsUnique().HasDatabaseName("ux_products_sku");
            entity.Property(product => product.Barcode).HasColumnName("barcode").HasMaxLength(128);
            entity.HasIndex(product => product.Barcode).IsUnique().HasDatabaseName("ux_products_barcode")
                .HasFilter("barcode IS NOT NULL");
            entity.Property(product => product.ShortDescription).HasColumnName("short_description").HasMaxLength(600);
            entity.Property(product => product.Description).HasColumnName("description").HasMaxLength(8000);
            entity.Property(product => product.BasePrice).HasColumnName("base_price").HasPrecision(18, 4);
            entity.Property(product => product.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).HasDefaultValue(CatalogProductStatus.Draft);
            entity.Property(product => product.Condition).HasColumnName("condition").HasConversion<string>().HasMaxLength(16).HasDefaultValue(CatalogProductCondition.New);
            entity.Property(product => product.RowVersion).HasColumnName("row_version").IsConcurrencyToken().HasDefaultValue(1);
            entity.Property(product => product.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(product => product.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.HasIndex(product => new { product.Status, product.CategoryId }).HasDatabaseName("ix_products_status_category");
            entity.HasIndex(product => new { product.BrandId, product.Status }).HasDatabaseName("ix_products_brand_status");
            entity.HasOne(product => product.Category).WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(product => product.Brand).WithMany(brand => brand.Products)
                .HasForeignKey(product => product.BrandId).OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<CatalogProductSpecification>(entity =>
        {
            entity.ToTable("product_specifications", table =>
            {
                table.HasCheckConstraint("ck_product_specifications_sort_order", "sort_order >= 0");
                table.HasCheckConstraint("ck_product_specifications_group_not_blank", "length(btrim(group_name)) > 0");
                table.HasCheckConstraint("ck_product_specifications_name_not_blank", "length(btrim(name)) > 0");
                table.HasCheckConstraint("ck_product_specifications_value_not_blank", "length(btrim(value)) > 0");
            });
            entity.HasKey(specification => specification.Id);
            entity.Property(specification => specification.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(specification => specification.ProductId).HasColumnName("product_id");
            entity.Property(specification => specification.GroupName).HasColumnName("group_name").HasMaxLength(100).IsRequired();
            entity.Property(specification => specification.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
            entity.Property(specification => specification.Value).HasColumnName("value").HasMaxLength(500).IsRequired();
            entity.Property(specification => specification.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.HasIndex(specification => new { specification.ProductId, specification.GroupName, specification.Name })
                .IsUnique().HasDatabaseName("ux_product_specifications_product_group_name");
            entity.HasIndex(specification => new { specification.ProductId, specification.SortOrder })
                .HasDatabaseName("ix_product_specifications_product_sort");
            entity.HasOne(specification => specification.Product).WithMany(product => product.Specifications)
                .HasForeignKey(specification => specification.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<CatalogProductImage>(entity =>
        {
            entity.ToTable("product_images", table =>
            {
                table.HasCheckConstraint("ck_product_images_file_size", "file_size_bytes > 0 AND file_size_bytes <= 8388608");
                table.HasCheckConstraint("ck_product_images_sort_order", "sort_order >= 0");
                table.HasCheckConstraint("ck_product_images_storage_not_blank", "length(btrim(storage_key)) > 0");
                table.HasCheckConstraint("ck_product_images_alt_not_blank", "length(btrim(alt_text)) > 0");
                table.HasCheckConstraint("ck_product_images_dimensions", "width > 0 AND height > 0 AND thumbnail_width > 0 AND thumbnail_height > 0");
            });
            entity.HasKey(image => image.Id);
            entity.Property(image => image.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(image => image.ProductId).HasColumnName("product_id");
            entity.Property(image => image.StorageKey).HasColumnName("storage_key").HasMaxLength(512).IsRequired();
            entity.HasIndex(image => image.StorageKey).IsUnique().HasDatabaseName("ux_product_images_storage_key");
            entity.Property(image => image.ThumbnailStorageKey).HasColumnName("thumbnail_storage_key").HasMaxLength(512).IsRequired();
            entity.HasIndex(image => image.ThumbnailStorageKey).IsUnique().HasDatabaseName("ux_product_images_thumbnail_storage_key");
            entity.Property(image => image.OriginalStorageKey).HasColumnName("original_storage_key").HasMaxLength(512);
            entity.HasIndex(image => image.OriginalStorageKey).IsUnique().HasFilter("original_storage_key IS NOT NULL").HasDatabaseName("ux_product_images_original_storage_key");
            entity.Property(image => image.AltText).HasColumnName("alt_text").HasMaxLength(300).IsRequired();
            entity.Property(image => image.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(image => image.Width).HasColumnName("width");
            entity.Property(image => image.Height).HasColumnName("height");
            entity.Property(image => image.ThumbnailWidth).HasColumnName("thumbnail_width");
            entity.Property(image => image.ThumbnailHeight).HasColumnName("thumbnail_height");
            entity.Property(image => image.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(image => image.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false);
            entity.Property(image => image.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.HasIndex(image => new { image.ProductId, image.SortOrder }).HasDatabaseName("ix_product_images_product_sort");
            entity.HasIndex(image => image.ProductId).IsUnique().HasFilter("is_primary")
                .HasDatabaseName("ux_product_images_one_primary");
            entity.HasOne(image => image.Product).WithMany(product => product.Images)
                .HasForeignKey(image => image.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        model.ExcludeFromAudit(typeof(ContentMediaAssetUsage));
        model.ApplySoftDelete();
    }
}
