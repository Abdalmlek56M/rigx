using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HardwareStore.Modules.Storefront;

/// <summary>One jsonb column per settings section; '{}' reads back as the section defaults.</summary>
internal static class StoreSettingsConfiguration
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static void Configure(ModelBuilder model)
    {
        model.Entity<StoreSettings>(entity =>
        {
            entity.ToTable("store_settings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            Section(entity, item => item.StoreInfo, "store_info");
            Section(entity, item => item.ContactLinks, "contact_links");
            Section(entity, item => item.Shipping, "shipping");
            Section(entity, item => item.Payment, "payment");
            Section(entity, item => item.Watermark, "watermark");
            Section(entity, item => item.Controls, "controls");
            Section(entity, item => item.WhatsAppTemplates, "whatsapp_templates");
            Section(entity, item => item.Reviews, "reviews");
            Section(entity, item => item.Language, "language");
            Section(entity, item => item.Invoice, "invoice");
            Section(entity, item => item.LeadSources, "lead_sources");
            Section(entity, item => item.Sales, "sales");
            Section(entity, item => item.WarrantyOptions, "warranty_options");
            Section(entity, item => item.Logos, "logos");
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
        });
    }

    private static void Section<T>(EntityTypeBuilder<StoreSettings> entity, Expression<Func<StoreSettings, T>> property, string column)
        where T : class =>
        entity.Property(property)
            .HasColumnName(column)
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired()
            .HasConversion(
                value => ToJson(value),
                json => FromJson<T>(json),
                new ValueComparer<T>(
                    (left, right) => ToJson(left) == ToJson(right),
                    value => ToJson(value).GetHashCode(StringComparison.Ordinal),
                    value => FromJson<T>(ToJson(value))));

    private static string ToJson<T>(T? value) => JsonSerializer.Serialize(value, Json);

    private static T FromJson<T>(string json) where T : class =>
        JsonSerializer.Deserialize<T>(json, Json) ?? throw new JsonException($"Empty {typeof(T).Name} settings.");
}
