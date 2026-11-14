using System.Text.Json;
using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HardwareStore.Modules.Purchasing;

/// <summary>Table mapping for <see cref="PurchaseLine"/>; specs are one jsonb document.</summary>
internal sealed class PurchaseLineConfiguration : IEntityTypeConfiguration<PurchaseLine>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<PurchaseLine> e)
    {
        e.ToTable("purchase_lines", t =>
        {
            t.HasCheckConstraint("ck_purchase_lines_condition", "condition IN ('New','Used')");
            t.HasCheckConstraint("ck_purchase_lines_quantity", "quantity BETWEEN 1 AND 1000");
            t.HasCheckConstraint("ck_purchase_lines_money", "unit_cost >= 0 AND line_total >= 0 AND price >= 0");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        e.Property(x => x.PurchaseId).HasColumnName("purchase_id");
        e.HasIndex(x => x.PurchaseId).HasDatabaseName("ix_purchase_lines_purchase");
        e.Property(x => x.SortOrder).HasColumnName("sort_order");
        e.Property(x => x.CategoryId).HasColumnName("category_id");
        e.Property(x => x.ProductId).HasColumnName("product_id");
        e.Property(x => x.ModelName).HasColumnName("model_name").HasMaxLength(ModelName.MaxLength).IsRequired();
        e.Property(x => x.Condition).HasColumnName("condition").HasMaxLength(8).IsRequired();
        e.Property(x => x.Quantity).HasColumnName("quantity");
        e.Property(x => x.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 2);
        e.Property(x => x.LineTotal).HasColumnName("line_total").HasPrecision(18, 2);
        e.Property(x => x.Price).HasColumnName("price").HasPrecision(18, 2);
        e.Property(x => x.WithBoardExtra).HasColumnName("with_board_extra").HasPrecision(18, 2);
        e.Property(x => x.AloneExtra).HasColumnName("alone_extra").HasPrecision(18, 2);
        e.Property(x => x.SerialNumber).HasColumnName("serial_number").HasMaxLength(PurchaseDraftRules.MaxSerialLength);
        e.Property(x => x.PartnerPercent).HasColumnName("partner_percent").HasPrecision(5, 2);
        e.Property(x => x.PartnerId).HasColumnName("partner_id");
        e.Property(x => x.IsHidden).HasColumnName("is_hidden");
        e.Property(x => x.WarrantyLabel).HasColumnName("warranty_label").HasMaxLength(PurchaseDraftRules.MaxWarrantyLabelLength);
        e.Property(x => x.HasBox).HasColumnName("has_box");
        e.Property(x => x.InternalNote).HasColumnName("internal_note").HasMaxLength(PurchaseDraftRules.MaxInternalNoteLength);
        e.Property(x => x.Specs).HasColumnName("specs").HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'{}'::jsonb")
            .HasConversion(
                new ValueConverter<Dictionary<string, string>, string>(value => JsonSerializer.Serialize(value, Json),
                    text => JsonSerializer.Deserialize<Dictionary<string, string>>(text, Json) ?? new()),
                new ValueComparer<Dictionary<string, string>>(
                    (left, right) => JsonSerializer.Serialize(left, Json) == JsonSerializer.Serialize(right, Json),
                    value => JsonSerializer.Serialize(value, Json).GetHashCode(),
                    value => JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(value, Json), Json)!));
    }
}
