using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HardwareStore.Modules.Retail;

/// <summary>Table mapping for <see cref="StockItemOldSlug"/>: one row per retired slug, unique across all items.</summary>
internal sealed class StockItemOldSlugConfiguration : IEntityTypeConfiguration<StockItemOldSlug>
{
    public void Configure(EntityTypeBuilder<StockItemOldSlug> e)
    {
        e.ToTable("stock_item_old_slugs");
        e.HasKey(x => x.Slug).HasName("pk_stock_item_old_slugs");
        e.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(StockItemSlug.MaxLength);
        e.Property(x => x.StockItemId).HasColumnName("stock_item_id");
        e.HasOne<StockItem>().WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Cascade);
        e.HasIndex(x => x.StockItemId).HasDatabaseName("ix_stock_item_old_slugs_stock_item_id");
    }
}

/// <summary>Table mapping for <see cref="StockItem"/>; specs and photos are stored as jsonb documents (<see cref="Document{T}"/>).</summary>
internal sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<StockItem> e)
    {
        e.ToTable("stock_items", t =>
        {
            t.HasCheckConstraint("ck_stock_items_condition", "condition IN ('New','Used')");
            t.HasCheckConstraint("ck_stock_items_price", "price >= 0");
            t.HasCheckConstraint("ck_stock_items_price_options",
                "(offers_alone IS NULL AND offers_with_board IS NULL AND offers_in_build IS NULL) OR " +
                "(offers_alone IS NOT NULL AND offers_with_board IS NOT NULL AND offers_in_build IS NOT NULL AND (offers_alone OR offers_with_board OR offers_in_build))");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        e.Property(x => x.Code).HasColumnName("code").HasMaxLength(16).IsRequired();
        e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_stock_items_code");
        e.Property(x => x.Condition).HasColumnName("condition").HasMaxLength(8).IsRequired();
        e.Property(x => x.CategoryId).HasColumnName("category_id");
        e.Property(x => x.ProductId).HasColumnName("product_id");
        e.Property(x => x.BrandId).HasColumnName("brand_id");
        e.HasIndex(x => x.BrandId).HasDatabaseName("ix_stock_items_brand_id");
        e.Property(x => x.ModelName).HasColumnName("model_name").HasMaxLength(160).IsRequired();
        e.Property(x => x.ModelKey).HasColumnName("model_key").HasMaxLength(160).IsRequired();
        e.HasIndex(x => x.ModelKey).HasDatabaseName("ix_stock_items_model_key");
        e.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(StockItemSlug.MaxLength).IsRequired();
        e.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("ux_stock_items_slug");
        e.Property(x => x.Price).HasColumnName("price").HasPrecision(18, 2);
        e.Property(x => x.WithBoardExtra).HasColumnName("with_board_extra").HasPrecision(18, 2);
        e.Property(x => x.AloneExtra).HasColumnName("alone_extra").HasPrecision(18, 2);
        e.Property(x => x.OffersAlone).HasColumnName("offers_alone");
        e.Property(x => x.OffersWithBoard).HasColumnName("offers_with_board");
        e.Property(x => x.OffersInBuild).HasColumnName("offers_in_build");
        e.Property(x => x.WarrantyLabel).HasColumnName("warranty_label").HasMaxLength(64);
        e.Property(x => x.WarrantyMonths).HasColumnName("warranty_months");
        e.Property(x => x.HasBox).HasColumnName("has_box");
        e.Property(x => x.IsHidden).HasColumnName("is_hidden");
        e.Property(x => x.InternalNote).HasColumnName("internal_note").HasMaxLength(1000);
        Document(e.Property(x => x.Specs).HasColumnName("specs"), "'{}'::jsonb");
        Document(e.Property(x => x.Photos).HasColumnName("photos"), "'[]'::jsonb");
        e.Property(x => x.PhotoCount).HasColumnName("photo_count").HasComputedColumnSql("jsonb_array_length(photos)", stored: true);
        e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
        e.HasIndex(x => new { x.IsHidden, x.Condition, x.CategoryId }).HasDatabaseName("ix_stock_items_listing");
    }

    /// <summary>Maps a property to a jsonb document (also used for build photos).</summary>
    internal static void Document<T>(PropertyBuilder<T> property, string defaultSql) where T : class, new()
    {
        property.HasColumnType("jsonb").IsRequired().HasDefaultValueSql(defaultSql)
            .HasConversion(
                new ValueConverter<T, string>(value => JsonSerializer.Serialize(value, Json),
                    text => JsonSerializer.Deserialize<T>(text, Json) ?? new T()),
                new ValueComparer<T>(
                    (left, right) => JsonSerializer.Serialize(left, Json) == JsonSerializer.Serialize(right, Json),
                    value => JsonSerializer.Serialize(value, Json).GetHashCode(),
                    value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Json), Json)!));
    }
}
