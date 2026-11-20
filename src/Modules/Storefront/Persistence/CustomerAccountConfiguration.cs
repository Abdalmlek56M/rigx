using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>Tables a signed-in customer owns in the storefront: favourites and notifications.</summary>
internal static class CustomerAccountConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<Favourite>(entity =>
        {
            entity.ToTable("favourites");
            entity.HasKey(item => new { item.UserAccountId, item.ItemCode });
            entity.Property(item => item.UserAccountId).HasColumnName("user_account_id");
            entity.Property(item => item.ItemCode).HasColumnName("item_code").HasMaxLength(40);
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        });

        model.Entity<CustomerNotification>(entity =>
        {
            entity.ToTable("customer_notifications", table =>
                table.HasCheckConstraint("ck_customer_notifications_kind", "kind IN ('OrderStatus','BackInStock')"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.UserAccountId).HasColumnName("user_account_id");
            entity.Property(item => item.Kind).HasColumnName("kind").HasMaxLength(16);
            entity.Property(item => item.Subject).HasColumnName("subject").HasMaxLength(CustomerNotificationRules.SubjectMaxLength);
            entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(16);
            entity.Property(item => item.Target).HasColumnName("target").HasMaxLength(64);
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.ReadAtUtc).HasColumnName("read_at_utc").HasColumnType("timestamp with time zone");
            entity.HasIndex(item => new { item.UserAccountId, item.CreatedAtUtc }).HasDatabaseName("ix_customer_notifications_account_created");
        });
    }
}
