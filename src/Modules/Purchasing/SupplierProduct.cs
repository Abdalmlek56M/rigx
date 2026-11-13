using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Purchasing;

public sealed class SupplierProduct
{
    public Guid Id { get; set; }
    public Guid SupplierProfileId { get; set; }
    public Guid ProductVariantId { get; set; }
    public required string SupplierSku { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public SupplierProfile Supplier { get; set; } = null!;
    public SupplierVariantReference Variant { get; set; } = null!;
}

/// <summary>Read-only reference to Catalog's product variants.</summary>
public sealed class SupplierVariantReference
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public SupplierCatalogProductReference Product { get; set; } = null!;
}

/// <summary>Read-only reference to Catalog's products.</summary>
public sealed class SupplierCatalogProductReference
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
}

public sealed partial class PurchasingDbContext
{
    public DbSet<SupplierProduct> SupplierProducts => Set<SupplierProduct>();
    public DbSet<SupplierVariantReference> SupplierVariants => Set<SupplierVariantReference>();

    private static void ConfigureSupplierProducts(ModelBuilder model)
    {
        model.Entity<SupplierCatalogProductReference>(entity =>
        {
            entity.ToTable("products", table => table.ExcludeFromMigrations());
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.Slug).HasColumnName("slug");
            entity.Property(item => item.Sku).HasColumnName("sku");
        });

        model.Entity<SupplierVariantReference>(entity =>
        {
            entity.ToTable("product_variants", table => table.ExcludeFromMigrations());
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.ProductId).HasColumnName("product_id");
            entity.Property(item => item.Name).HasColumnName("name");
            entity.Property(item => item.Sku).HasColumnName("sku");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
            entity.HasOne(item => item.Product).WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<SupplierProduct>(entity =>
        {
            entity.ToTable("supplier_products");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.SupplierProfileId).HasColumnName("supplier_profile_id");
            entity.Property(item => item.ProductVariantId).HasColumnName("product_variant_id");
            entity.HasIndex(item => new { item.SupplierProfileId, item.ProductVariantId })
                .IsUnique()
                .HasDatabaseName("ux_supplier_products_supplier_variant");
            entity.Property(item => item.SupplierSku).HasColumnName("supplier_sku").HasMaxLength(128).IsRequired();
            entity.HasIndex(item => new { item.SupplierProfileId, item.SupplierSku })
                .IsUnique()
                .HasDatabaseName("ux_supplier_products_supplier_sku");
            entity.Property(item => item.Notes).HasColumnName("notes").HasMaxLength(2000);
            entity.Property(item => item.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasOne(item => item.Supplier).WithMany()
                .HasForeignKey(item => item.SupplierProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Variant).WithMany()
                .HasForeignKey(item => item.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => item.IsActive).HasDatabaseName("ix_supplier_products_is_active");
        });
    }
}
