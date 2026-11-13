using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HardwareStore.Modules.Purchasing;

/// <summary>PostgreSQL sequence behind the purchase code (PO-0001).</summary>
internal static class CodeSequence
{
    public const string Purchase = "purchase_code_seq";
}

internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> e)
    {
        e.ToTable("purchases", t =>
        {
            t.HasCheckConstraint("ck_purchases_status", "status IN ('Draft','Received','Cancelled')");
            t.HasCheckConstraint("ck_purchases_kind", "kind IN ('Parts','FullPc')");
            t.HasCheckConstraint("ck_purchases_method", "payment_method IN ('Cash','InstaPay','Wallet','TradeIn','CashAndTradeIn')");
            t.HasCheckConstraint("ck_purchases_money", "total >= 0 AND paid_amount >= 0 AND trade_in_value >= 0 AND remaining >= 0");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        e.Property(x => x.Code).HasColumnName("code").HasMaxLength(16).IsRequired();
        e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_purchases_code");
        e.Property(x => x.SellerId).HasColumnName("seller_id");
        e.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => x.SellerId).HasDatabaseName("ix_purchases_seller");
        e.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(16).IsRequired();
        e.Property(x => x.KeepAsBuild).HasColumnName("keep_as_build");
        e.Property(x => x.BuildName).HasColumnName("build_name").HasMaxLength(PurchaseDraftRules.MaxBuildNameLength);
        e.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
        e.Property(x => x.PurchaseDate).HasColumnName("purchase_date").HasColumnType("date");
        e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(PurchaseDraftRules.MaxNotesLength);
        e.Property(x => x.Total).HasColumnName("total").HasPrecision(18, 2);
        e.Property(x => x.PaymentMethod).HasColumnName("payment_method").HasMaxLength(16).IsRequired();
        e.Property(x => x.PaidAmount).HasColumnName("paid_amount").HasPrecision(18, 2);
        e.Property(x => x.TradeInValue).HasColumnName("trade_in_value").HasPrecision(18, 2);
        e.Property(x => x.TradeInNote).HasColumnName("trade_in_note").HasMaxLength(PurchaseDraftRules.MaxTradeInNoteLength);
        e.Property(x => x.Remaining).HasColumnName("remaining").HasPrecision(18, 2);
        e.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.AssemblyId).HasColumnName("assembly_id");
        e.Property(x => x.CreatedByUserAccountId).HasColumnName("created_by_user_account_id");
        e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
        e.HasIndex(x => new { x.Status, x.CreatedAtUtc }).HasDatabaseName("ix_purchases_status_created");
        e.HasMany(x => x.Lines).WithOne().HasForeignKey(line => line.PurchaseId).OnDelete(DeleteBehavior.Cascade);
    }
}
