using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

internal static class CustomerRequestConfiguration
{
    /// <summary>PostgreSQL sequence behind the request code (REQ-0001); created by the migration.</summary>
    public const string CodeSequence = "customer_request_code_seq";

    public static void Configure(ModelBuilder model)
    {
        model.Entity<CustomerRequest>(entity =>
        {
            entity.ToTable("customer_requests", table =>
            {
                table.HasCheckConstraint("ck_customer_requests_type",
                    "type IN ('Sell','TradeIn','Upgrade','NotifyMe','Support','Contact','Service')");
                table.HasCheckConstraint("ck_customer_requests_status",
                    "status IN ('New','Following','Offered','Accepted','Rejected','Converted','Closed')");
                table.HasCheckConstraint("ck_customer_requests_service_kind",
                    "service_kind IS NULL OR service_kind IN ('Consult','Build','Test','Maintenance','Sourcing','Upgrade')");
            });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.Code).HasColumnName("code").HasMaxLength(16).IsRequired();
            entity.Property(item => item.Type).HasColumnName("type").HasMaxLength(16).IsRequired();
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(item => item.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(item => item.Phone).HasColumnName("phone").HasMaxLength(32).IsRequired();
            entity.Property(item => item.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(item => item.Governorate).HasColumnName("governorate").HasMaxLength(8);
            entity.Property(item => item.Source).HasColumnName("source").HasMaxLength(64);
            entity.Property(item => item.UserAccountId).HasColumnName("user_account_id");
            entity.Property(item => item.Subject).HasColumnName("subject").HasMaxLength(200);
            entity.Property(item => item.Message).HasColumnName("message").HasMaxLength(4000);
            entity.Property(item => item.PartType).HasColumnName("part_type").HasMaxLength(120);
            entity.Property(item => item.ModelName).HasColumnName("model_name").HasMaxLength(200);
            entity.Property(item => item.OwnedFor).HasColumnName("owned_for").HasMaxLength(60);
            entity.Property(item => item.ItemCode).HasColumnName("item_code").HasMaxLength(40);
            entity.Property(item => item.ModelKey).HasColumnName("model_key").HasMaxLength(200);
            entity.Property(item => item.CategoryId).HasColumnName("category_id");
            entity.Property(item => item.AvailableItemCode).HasColumnName("available_item_code").HasMaxLength(40);
            entity.Property(item => item.AvailableAtUtc).HasColumnName("available_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.NotifiedAtUtc).HasColumnName("notified_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.ServiceKind).HasColumnName("service_kind").HasMaxLength(16);
            entity.Property(item => item.PreferredDate).HasColumnName("preferred_date").HasColumnType("date");
            entity.Property(item => item.OfferAmount).HasColumnName("offer_amount").HasColumnType("numeric(12,2)");
            entity.Property(item => item.ExpectedResale).HasColumnName("expected_resale").HasColumnType("numeric(12,2)");
            entity.Property(item => item.InternalNote).HasColumnName("internal_note").HasMaxLength(CustomerRequestRules.MaxNoteLength);
            entity.Property(item => item.PhotoTokenHash).HasColumnName("photo_token_hash").HasMaxLength(64);
            entity.Property(item => item.PhotoTokenExpiresAtUtc).HasColumnName("photo_token_expires_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.Photos).HasColumnName("photos").HasColumnType("text[]").HasDefaultValueSql("'{}'::text[]").IsRequired();
            entity.Property(item => item.PurchaseId).HasColumnName("purchase_id");
            entity.Property(item => item.PurchaseCode).HasColumnName("purchase_code").HasMaxLength(16);
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.ClosedAtUtc).HasColumnName("closed_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(item => item.PhotoTokenHash).HasDatabaseName("ix_customer_requests_photo_token_hash")
                .HasFilter("photo_token_hash IS NOT NULL");
            entity.HasIndex(item => item.Code).IsUnique().HasDatabaseName("ux_customer_requests_code");
            entity.HasIndex(item => new { item.Status, item.CreatedAtUtc }).HasDatabaseName("ix_customer_requests_status_created");
            entity.HasIndex(item => new { item.Type, item.Status }).HasDatabaseName("ix_customer_requests_type_status");
            entity.HasIndex(item => new { item.Type, item.ModelKey }).HasDatabaseName("ix_customer_requests_type_model_key")
                .HasFilter("model_key IS NOT NULL AND status IN ('New','Following')");
            entity.HasIndex(item => new { item.Phone, item.CreatedAtUtc }).HasDatabaseName("ix_customer_requests_phone_created");
        });
    }
}
