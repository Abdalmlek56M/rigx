using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HardwareStore.Modules.Retail;

internal sealed class MoneyAccountConfiguration : IEntityTypeConfiguration<MoneyAccount>
{
    public void Configure(EntityTypeBuilder<MoneyAccount> e)
    {
        e.ToTable("money_accounts", t =>
        {
            t.HasCheckConstraint("ck_money_accounts_opening", "opening_balance >= 0");
            t.HasCheckConstraint("ck_money_accounts_kind", "kind IN ('Cash','InstaPay','Wallet','BankTransfer')");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(60).IsRequired();
        e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ux_money_accounts_name");
        e.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(16).IsRequired();
        e.Property(x => x.OpeningBalance).HasColumnName("opening_balance").HasPrecision(18, 2);
        e.Property(x => x.OpeningDate).HasColumnName("opening_date").HasColumnType("date");
        e.Property(x => x.IsActive).HasColumnName("is_active");
        e.Property(x => x.SortOrder).HasColumnName("sort_order");
        e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
    }
}

internal sealed class MoneyMovementConfiguration : IEntityTypeConfiguration<MoneyMovement>
{
    public void Configure(EntityTypeBuilder<MoneyMovement> e)
    {
        e.ToTable("money_movements", t => t.HasCheckConstraint("ck_money_movements_amount", "amount <> 0"));
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        e.Property(x => x.AccountId).HasColumnName("account_id");
        e.HasOne<MoneyAccount>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        e.Property(x => x.Source).HasColumnName("source").HasMaxLength(24).IsRequired();
        e.Property(x => x.SourceId).HasColumnName("source_id");
        e.HasIndex(x => new { x.Source, x.SourceId }).IsUnique().HasDatabaseName("ux_money_movements_source");
        e.Property(x => x.Reference).HasColumnName("reference").HasMaxLength(128);
        e.Property(x => x.Note).HasColumnName("note").HasMaxLength(500);
        e.Property(x => x.OccurredOn).HasColumnName("occurred_on").HasColumnType("date");
        e.HasIndex(x => new { x.AccountId, x.OccurredOn }).HasDatabaseName("ix_money_movements_account_date");
        e.Property(x => x.ActorUserAccountId).HasColumnName("actor_user_account_id");
        e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
    }
}

internal sealed class ExpenseTypeConfiguration : IEntityTypeConfiguration<ExpenseType>
{
    public void Configure(EntityTypeBuilder<ExpenseType> e)
    {
        e.ToTable("expense_types");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ux_expense_types_name");
        e.Property(x => x.IsActive).HasColumnName("is_active");
        e.Property(x => x.SortOrder).HasColumnName("sort_order");
        e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
    }
}

internal sealed class RecurringExpenseConfiguration : IEntityTypeConfiguration<RecurringExpense>
{
    public void Configure(EntityTypeBuilder<RecurringExpense> e)
    {
        e.ToTable("recurring_expenses", t =>
        {
            t.HasCheckConstraint("ck_recurring_expenses_amount", "amount > 0");
            t.HasCheckConstraint("ck_recurring_expenses_day", "day_of_month BETWEEN 1 AND 28");
        });
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        e.Property(x => x.Category).HasColumnName("category").HasMaxLength(80).IsRequired();
        e.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        e.Property(x => x.DayOfMonth).HasColumnName("day_of_month");
        e.Property(x => x.MoneyAccountId).HasColumnName("money_account_id");
        e.HasOne<MoneyAccount>().WithMany().HasForeignKey(x => x.MoneyAccountId).OnDelete(DeleteBehavior.Restrict);
        e.Property(x => x.Note).HasColumnName("note").HasMaxLength(1000);
        e.Property(x => x.IsActive).HasColumnName("is_active");
        e.Property(x => x.StartMonth).HasColumnName("start_month").HasColumnType("date");
        e.Property(x => x.LastPostedMonth).HasColumnName("last_posted_month").HasColumnType("date");
        e.Property(x => x.CreatedByUserAccountId).HasColumnName("created_by_user_account_id");
        e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
    }
}
