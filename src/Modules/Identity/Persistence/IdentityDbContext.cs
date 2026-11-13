using HardwareStore.Modules.Identity.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity.Persistence;

/// <summary>Identity schema: people, accounts, sessions, roles/permissions, email codes, outbox, addresses.</summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<PersonCodeCounter> PersonCodeCounters => Set<PersonCodeCounter>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<IdentityEmailOutboxMessage> EmailOutbox => Set<IdentityEmailOutboxMessage>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<IdentityEmailChangeRequest> EmailChangeRequests => Set<IdentityEmailChangeRequest>();
    public DbSet<AccountRestriction> AccountRestrictions => Set<AccountRestriction>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<AuthorizationState> AuthorizationStates => Set<AuthorizationState>();
    public DbSet<EmailCode> EmailCodes => Set<EmailCode>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Person>(e =>
        {
            e.ToTable("persons"); e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.PersonCode).HasColumnName("person_code").HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.PersonCode).IsUnique().HasDatabaseName("ux_persons_person_code");
            e.ToTable(t => t.HasCheckConstraint("ck_persons_person_code_format", "person_code ~ '^PER-[0-9]{6}-[0-9]{5}$'"));
            e.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
            e.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(32);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            e.HasIndex(x => x.PhoneNumber).HasDatabaseName("ix_persons_phone_number");
            e.HasIndex(x => x.Email).HasDatabaseName("ix_persons_email");
            e.Property(x => x.IsCustomer).HasColumnName("is_customer").HasDefaultValue(false);
            e.Property(x => x.IsSupplier).HasColumnName("is_supplier").HasDefaultValue(false);
            e.Property(x => x.IsEmployee).HasColumnName("is_employee").HasDefaultValue(false);
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.HasOne(x => x.UserAccount).WithOne(x => x.Person).HasForeignKey<UserAccount>(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<AuthSession>(e =>
        {
            e.ToTable("auth_sessions", table =>
            {
                table.HasCheckConstraint("ck_auth_sessions_key_hash_length", "octet_length(session_key_hash) = 32");
                table.HasCheckConstraint("ck_auth_sessions_expiry_order", "idle_expires_at_utc <= absolute_expires_at_utc");
                table.HasCheckConstraint("ck_auth_sessions_idle_timeout", "idle_timeout_seconds > 0");
            });
            e.HasKey(x => x.SessionKeyHash);
            e.Property(x => x.SessionKeyHash).HasColumnName("session_key_hash").HasColumnType("bytea");
            e.Property(x => x.PublicId).HasColumnName("public_id").ValueGeneratedNever();
            e.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("ux_auth_sessions_public_id");
            e.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
            e.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
            e.Property(x => x.UserAccountId).HasColumnName("user_account_id");
            e.Property(x => x.ProtectedTicket).HasColumnName("protected_ticket").HasColumnType("bytea").IsRequired();
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.LastActivityAtUtc).HasColumnName("last_activity_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.IdleExpiresAtUtc).HasColumnName("idle_expires_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.AbsoluteExpiresAtUtc).HasColumnName("absolute_expires_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(128);
            e.Property(x => x.IdleTimeoutSeconds).HasColumnName("idle_timeout_seconds");
            e.Property(x => x.AuthorizationVersion).HasColumnName("authorization_version");
            e.Property(x => x.RbacRevision).HasColumnName("rbac_revision");
            e.Property(x => x.RowVersion).HasColumnName("row_version").IsConcurrencyToken().HasDefaultValue(1L);
            e.HasIndex(x => x.UserAccountId).HasDatabaseName("ix_auth_sessions_user_account_id");
            e.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<IdentityEmailOutboxMessage>(e =>
        {
            e.ToTable("identity_email_outbox");
            e.HasKey(row => row.Id);
            e.Property(row => row.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(row => row.UserAccountId).HasColumnName("user_account_id");
            e.Property(row => row.Kind).HasColumnName("kind").HasMaxLength(32).IsRequired();
            e.Property(row => row.ProtectedPayload).HasColumnName("protected_payload").HasColumnType("bytea").IsRequired();
            e.Property(row => row.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            e.Property(row => row.Attempts).HasColumnName("attempts");
            e.Property(row => row.NextAttemptAtUtc).HasColumnName("next_attempt_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.ProcessingAtUtc).HasColumnName("processing_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.ProcessedAtUtc).HasColumnName("processed_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.LastErrorCode).HasColumnName("last_error_code").HasMaxLength(64);
            e.Property(row => row.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.HasIndex(row => new { row.Status, row.NextAttemptAtUtc }).HasDatabaseName("ix_identity_email_outbox_due");
        });

        model.Entity<CustomerAddress>(e =>
        {
            e.ToTable("customer_addresses");
            e.HasKey(row => row.Id);
            e.Property(row => row.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(row => row.UserAccountId).HasColumnName("user_account_id");
            e.Property(row => row.Label).HasColumnName("label").HasMaxLength(32).IsRequired();
            e.Property(row => row.RecipientName).HasColumnName("recipient_name").HasMaxLength(255).IsRequired();
            e.Property(row => row.PhoneNumber).HasColumnName("phone_number").HasMaxLength(32).IsRequired();
            e.Property(row => row.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsRequired();
            e.Property(row => row.CountryCallingCode).HasColumnName("country_calling_code").HasMaxLength(8).IsRequired();
            e.Property(row => row.Governorate).HasColumnName("governorate").HasMaxLength(128).IsRequired();
            e.Property(row => row.City).HasColumnName("city").HasMaxLength(128).IsRequired();
            e.Property(row => row.District).HasColumnName("district").HasMaxLength(128);
            e.Property(row => row.Street).HasColumnName("street").HasMaxLength(255).IsRequired();
            e.Property(row => row.Building).HasColumnName("building").HasMaxLength(64);
            e.Property(row => row.Floor).HasColumnName("floor").HasMaxLength(32);
            e.Property(row => row.Apartment).HasColumnName("apartment").HasMaxLength(32);
            e.Property(row => row.Landmark).HasColumnName("landmark").HasMaxLength(255);
            e.Property(row => row.PostalCode).HasColumnName("postal_code").HasMaxLength(32);
            e.Property(row => row.Instructions).HasColumnName("instructions").HasMaxLength(1000);
            e.Property(row => row.IsDefault).HasColumnName("is_default");
            e.Property(row => row.IsDeleted).HasColumnName("is_deleted");
            e.Property(row => row.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(row => new { row.UserAccountId, row.IsDeleted }).HasDatabaseName("ix_customer_addresses_account");
            e.HasIndex(row => row.UserAccountId).IsUnique()
                .HasFilter("is_default AND NOT is_deleted")
                .HasDatabaseName("ux_customer_addresses_one_default");
            e.HasOne<UserAccount>().WithMany().HasForeignKey(row => row.UserAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<IdentityEmailChangeRequest>(e =>
        {
            e.ToTable("identity_email_change_requests");
            e.HasKey(row => row.Id);
            e.Property(row => row.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(row => row.UserAccountId).HasColumnName("user_account_id");
            e.Property(row => row.NewEmail).HasColumnName("new_email").HasMaxLength(255).IsRequired();
            e.Property(row => row.NormalizedNewEmail).HasColumnName("normalized_new_email").HasMaxLength(255).IsRequired();
            e.Property(row => row.TokenHash).HasColumnName("token_hash").HasColumnType("bytea").IsRequired();
            e.Property(row => row.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.Attempts).HasColumnName("attempts");
            e.Property(row => row.ConsumedAtUtc).HasColumnName("consumed_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(row => new { row.UserAccountId, row.ConsumedAtUtc, row.RevokedAtUtc })
                .HasDatabaseName("ix_identity_email_change_requests_account");
            e.HasIndex(row => row.TokenHash).HasDatabaseName("ix_identity_email_change_requests_token");
            e.HasOne<UserAccount>().WithMany().HasForeignKey(row => row.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<AccountRestriction>(e =>
        {
            e.ToTable("account_restrictions", table =>
            {
                table.HasCheckConstraint("ck_account_restrictions_type", "type IN ('Suspension','Ban')");
                table.HasCheckConstraint("ck_account_restrictions_window",
                    "expires_at_utc IS NULL OR expires_at_utc > effective_at_utc");
            });
            e.HasKey(row => row.Id);
            e.Property(row => row.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(row => row.UserAccountId).HasColumnName("user_account_id");
            e.Property(row => row.Type).HasColumnName("type").HasMaxLength(16).IsRequired();
            e.Property(row => row.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
            e.Property(row => row.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.EffectiveAtUtc).HasColumnName("effective_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.CreatedByUserAccountId).HasColumnName("created_by_user_account_id");
            e.Property(row => row.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.RevokedByUserAccountId).HasColumnName("revoked_by_user_account_id");
            e.Property(row => row.ReinstatementReason).HasColumnName("reinstatement_reason").HasMaxLength(1000);
            e.Property(row => row.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(row => new { row.UserAccountId, row.RevokedAtUtc })
                .HasDatabaseName("ix_account_restrictions_account");
            e.HasOne<UserAccount>().WithMany().HasForeignKey(row => row.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<UserNotification>(e =>
        {
            e.ToTable("user_notifications");
            e.HasKey(row => row.Id);
            e.Property(row => row.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(row => row.UserAccountId).HasColumnName("user_account_id");
            e.Property(row => row.Kind).HasColumnName("kind").HasMaxLength(64).IsRequired();
            e.Property(row => row.TitleKey).HasColumnName("title_key").HasMaxLength(128).IsRequired();
            e.Property(row => row.BodyKey).HasColumnName("body_key").HasMaxLength(128);
            e.Property(row => row.LinkUrl).HasColumnName("link_url").HasMaxLength(512);
            e.Property(row => row.IsRead).HasColumnName("is_read");
            e.Property(row => row.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(row => row.ReadAtUtc).HasColumnName("read_at_utc").HasColumnType("timestamp with time zone");
            e.HasIndex(row => new { row.UserAccountId, row.IsRead, row.CreatedAtUtc })
                .HasDatabaseName("ix_user_notifications_recipient");
            e.HasOne<UserAccount>().WithMany().HasForeignKey(row => row.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<EmailCode>(e =>
        {
            e.ToTable("email_codes", table =>
            {
                table.HasCheckConstraint("ck_email_codes_purpose", "purpose IN ('SignIn', 'StepUp')");
                table.HasCheckConstraint("ck_email_codes_expiry_order", "expires_at_utc > created_at_utc");
                table.HasCheckConstraint("ck_email_codes_attempts", "attempts >= 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(255).IsRequired();
            e.Property(x => x.Purpose).HasColumnName("purpose").HasMaxLength(16).IsRequired();
            e.Property(x => x.ProtectedCode).HasColumnName("protected_code").HasColumnType("bytea").IsRequired();
            e.Property(x => x.Attempts).HasColumnName("attempts").HasDefaultValue(0);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.UsedAtUtc).HasColumnName("used_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamp with time zone");
            e.HasIndex(x => new { x.NormalizedEmail, x.Purpose }).IsUnique()
                .HasFilter("used_at_utc IS NULL AND revoked_at_utc IS NULL")
                .HasDatabaseName("ux_email_codes_active_email_purpose");
            e.HasIndex(x => new { x.NormalizedEmail, x.Purpose, x.CreatedAtUtc })
                .HasDatabaseName("ix_email_codes_email_purpose_created");
        });
        model.Entity<AuthorizationState>(e =>
        {
            e.ToTable("authorization_state", table =>
                table.HasCheckConstraint("ck_authorization_state_singleton", "id = 1"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RbacRevision).HasColumnName("rbac_revision");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.HasData(new AuthorizationState { Id = 1, RbacRevision = 0, UpdatedAtUtc = DateTimeOffset.UnixEpoch });
        });
        model.Entity<UserAccount>(e =>
        {
            e.ToTable("user_accounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.PersonId).HasColumnName("person_id"); e.HasIndex(x => x.PersonId).IsUnique().HasDatabaseName("ux_user_accounts_person_id");
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(128).IsRequired(); e.HasIndex(x => x.Username).IsUnique().HasDatabaseName("ux_user_accounts_username");
            e.Property(x => x.NormalizedUsername).HasColumnName("normalized_username").HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.NormalizedUsername).IsUnique().HasDatabaseName("ux_user_accounts_normalized_username");
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired(); e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ux_user_accounts_email");
            e.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.NormalizedEmail).IsUnique().HasDatabaseName("ux_user_accounts_normalized_email");
            e.Property(x => x.IsEmailVerified).HasColumnName("is_email_verified").HasDefaultValue(false);
            e.Property(x => x.AvatarStorageKey).HasColumnName("avatar_storage_key").HasMaxLength(512);
            e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(x => x.AuthorizationVersion).HasColumnName("authorization_version").HasDefaultValue(0);
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
        });
        model.Entity<Role>(e =>
        {
            e.ToTable("roles"); e.HasKey(x => x.Id); e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired(); e.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_roles_code");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        });
        model.Entity<Permission>(e =>
        {
            e.ToTable("permissions"); e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(128).IsRequired();
            e.HasData(PermissionCatalog.All.OrderBy(code => code, StringComparer.Ordinal).Select(code => new Permission { Code = code }));
        });
        model.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles"); e.HasKey(x => new { x.UserAccountId, x.RoleId });
            e.Property(x => x.UserAccountId).HasColumnName("user_account_id"); e.Property(x => x.RoleId).HasColumnName("role_id");
            e.HasOne(x => x.UserAccount).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<RolePermission>(e =>
        {
            e.ToTable("role_permissions"); e.HasKey(x => new { x.RoleId, x.PermissionCode });
            e.Property(x => x.RoleId).HasColumnName("role_id"); e.Property(x => x.PermissionCode).HasColumnName("permission_code").HasMaxLength(128);
            e.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionCode).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<PersonCodeCounter>(e =>
        {
            e.ToTable("person_code_counters"); e.HasKey(x => x.YearMonth);
            e.Property(x => x.YearMonth).HasColumnName("year_month").HasMaxLength(6).IsFixedLength();
            e.Property(x => x.LastValue).HasColumnName("last_value");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("ck_person_code_counters_last_value", "last_value BETWEEN 1 AND 99999");
                t.HasCheckConstraint("ck_person_code_counters_year_month", "year_month ~ '^[0-9]{6}$'");
            });
        });
        model.Entity<Role>().HasData(new Role { Id = Guid.Parse("11111111-1111-4111-8111-111111111111"), Code = "Customer", Name = "Customer" });
    }
}
