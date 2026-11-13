using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Purchasing;

public sealed class SupplierProfile
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public required string CompanyName { get; set; }
    public string? TaxIdNumber { get; set; }
    public string? CommercialRegistration { get; set; }
    public string? ContactPersonName { get; set; }
    public string? ContactPersonPhone { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankIban { get; set; }
    public int PaymentTermsDays { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public SupplierPersonReference Person { get; set; } = null!;
}

/// <summary>
/// Read-only projection of the Identity-owned persons table. It is excluded
/// from Purchasing migrations so the party schema remains owned by Identity.
/// </summary>
public sealed class SupplierPersonReference
{
    public Guid Id { get; set; }
    public string PersonCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}

public sealed partial class PurchasingDbContext(DbContextOptions<PurchasingDbContext> options) : DbContext(options)
{
    public DbSet<SupplierProfile> SupplierProfiles => Set<SupplierProfile>();
    public DbSet<PurchaseDraft> PurchaseDrafts => Set<PurchaseDraft>();
    public DbSet<PurchaseDraftItem> PurchaseDraftItems => Set<PurchaseDraftItem>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<SupplierPersonReference>(entity =>
        {
            entity.ToTable("persons", table => table.ExcludeFromMigrations());
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.PersonCode).HasColumnName("person_code");
            entity.Property(item => item.FullName).HasColumnName("full_name");
            entity.Property(item => item.PhoneNumber).HasColumnName("phone_number");
            entity.Property(item => item.Email).HasColumnName("email");
            entity.Property(item => item.IsActive).HasColumnName("is_active");
        });

        model.Entity<SupplierProfile>(entity =>
        {
            entity.ToTable("supplier_profiles", table =>
            {
                table.HasCheckConstraint(
                    "ck_supplier_profiles_payment_terms_days",
                    "payment_terms_days >= 0 AND payment_terms_days <= 3650");
            });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(item => item.PersonId).HasColumnName("person_id");
            entity.HasIndex(item => item.PersonId).IsUnique().HasDatabaseName("ux_supplier_profiles_person_id");
            entity.Property(item => item.CompanyName).HasColumnName("company_name").HasMaxLength(255).IsRequired();
            entity.Property(item => item.TaxIdNumber).HasColumnName("tax_id_number").HasMaxLength(64);
            entity.HasIndex(item => item.TaxIdNumber).IsUnique().HasFilter("tax_id_number IS NOT NULL")
                .HasDatabaseName("ux_supplier_profiles_tax_id_number");
            entity.Property(item => item.CommercialRegistration).HasColumnName("commercial_registration").HasMaxLength(64);
            entity.HasIndex(item => item.CommercialRegistration).IsUnique()
                .HasFilter("commercial_registration IS NOT NULL")
                .HasDatabaseName("ux_supplier_profiles_commercial_registration");
            entity.Property(item => item.ContactPersonName).HasColumnName("contact_person_name").HasMaxLength(255);
            entity.Property(item => item.ContactPersonPhone).HasColumnName("contact_person_phone").HasMaxLength(32);
            entity.Property(item => item.BankName).HasColumnName("bank_name").HasMaxLength(128);
            entity.Property(item => item.BankAccountNumber).HasColumnName("bank_account_number").HasMaxLength(64);
            entity.Property(item => item.BankIban).HasColumnName("bank_iban").HasMaxLength(64);
            entity.Property(item => item.PaymentTermsDays).HasColumnName("payment_terms_days").HasDefaultValue(0);
            entity.Property(item => item.Notes).HasColumnName("notes").HasMaxLength(2000);
            entity.Property(item => item.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(item => item.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasOne(item => item.Person).WithMany()
                .HasForeignKey(item => item.PersonId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => item.IsActive).HasDatabaseName("ix_supplier_profiles_is_active");
        });

        ConfigureSupplierProducts(model);
        PurchaseDraftConfiguration.Configure(model);
    }
}
