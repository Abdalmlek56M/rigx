using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HardwareStore.Modules.Retail;

internal sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> e)
    {
        e.ToTable("coupons", t =>
        {
            t.HasCheckConstraint("ck_coupons_kind", "discount_kind IN ('Percent','Amount')");
            t.HasCheckConstraint("ck_coupons_value", "discount_value > 0 AND (discount_kind <> 'Percent' OR discount_value <= 90)");
            t.HasCheckConstraint("ck_coupons_window", "ends_at_utc IS NULL OR starts_at_utc IS NULL OR ends_at_utc > starts_at_utc");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        e.Property(x => x.Code).HasColumnName("code").HasMaxLength(CouponRules.MaxCodeLength).IsRequired();
        e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_coupons_code");
        e.Property(x => x.SourceName).HasColumnName("source_name").HasMaxLength(100).IsRequired();
        e.Property(x => x.DiscountKind).HasColumnName("discount_kind").HasMaxLength(16).IsRequired();
        e.Property(x => x.DiscountValue).HasColumnName("discount_value").HasPrecision(18, 2);
        e.Property(x => x.MaxDiscount).HasColumnName("max_discount").HasPrecision(18, 2);
        e.Property(x => x.MinOrderTotal).HasColumnName("min_order_total").HasPrecision(18, 2);
        e.Property(x => x.MaxUses).HasColumnName("max_uses");
        e.Property(x => x.OncePerCustomer).HasColumnName("once_per_customer");
        e.Property(x => x.StartsAtUtc).HasColumnName("starts_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.EndsAtUtc).HasColumnName("ends_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.IsActive).HasColumnName("is_active");
        e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
    }
}
