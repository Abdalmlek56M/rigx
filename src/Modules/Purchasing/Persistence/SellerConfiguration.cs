using HardwareStore.Common.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HardwareStore.Modules.Purchasing;

internal sealed class SellerConfiguration : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> e)
    {
        e.ToTable("sellers", t => t.HasCheckConstraint("ck_sellers_kind", "kind IN ('Person','Trader','Company')"));
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        e.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(16).IsRequired();
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(SellerRules.MaxNameLength).IsRequired();
        e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(16).IsRequired();
        e.HasIndex(x => x.Phone).IsUnique().HasDatabaseName("ux_sellers_phone");
        e.Property(x => x.NationalId).HasColumnName("national_id").HasMaxLength(SellerRules.MaxNationalIdLength).AuditMasked();
        e.Property(x => x.Governorate).HasColumnName("governorate").HasMaxLength(8);
        e.Property(x => x.Area).HasColumnName("area").HasMaxLength(SellerRules.MaxAreaLength);
        e.Property(x => x.Source).HasColumnName("source").HasMaxLength(SellerRules.MaxSourceLength);
        e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(SellerRules.MaxNotesLength);
        e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
    }
}
