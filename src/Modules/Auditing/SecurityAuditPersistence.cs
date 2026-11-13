using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.Auditing;

public sealed class SecurityAuditEvent
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public required string Outcome { get; set; }
    public required string ActorKind { get; set; }
    public Guid? ActorUserAccountId { get; set; }
    public Guid? SubjectUserAccountId { get; set; }
    public byte[]? SessionKeyHash { get; set; }
    public string? CorrelationId { get; set; }
    public string SafeMetadata { get; set; } = "{}";
}

// A migration-excluded reference keeps Auditing independent of the Identity project.
public sealed class AuditUserAccountReference
{
    public Guid Id { get; set; }
}

public sealed class SecurityAuditDbContext(DbContextOptions<SecurityAuditDbContext> options) : DbContext(options)
{
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<AuditUserAccountReference>(entity =>
        {
            entity.ToTable("user_accounts", table => table.ExcludeFromMigrations());
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Id).HasColumnName("id");
        });

        model.Entity<SecurityAuditEvent>(entity =>
        {
            entity.ToTable("security_audit_events", table =>
            {
                table.HasCheckConstraint("ck_security_audit_event_type", "length(event_type) BETWEEN 1 AND 128");
                table.HasCheckConstraint("ck_security_audit_outcome", "outcome IN ('Succeeded', 'Failed', 'Rejected', 'Recorded')");
                table.HasCheckConstraint("ck_security_audit_actor_kind", "actor_kind IN ('Anonymous', 'User', 'System')");
                table.HasCheckConstraint("ck_security_audit_session_hash_length", "session_key_hash IS NULL OR octet_length(session_key_hash) = 32");
            });
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(audit => audit.EventType).HasColumnName("event_type").HasMaxLength(128).IsRequired();
            entity.Property(audit => audit.OccurredAtUtc).HasColumnName("occurred_at_utc")
                .HasColumnType("timestamp with time zone").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
            entity.Property(audit => audit.Outcome).HasColumnName("outcome").HasMaxLength(16).IsRequired();
            entity.Property(audit => audit.ActorKind).HasColumnName("actor_kind").HasMaxLength(16).IsRequired();
            entity.Property(audit => audit.ActorUserAccountId).HasColumnName("actor_user_account_id");
            entity.Property(audit => audit.SubjectUserAccountId).HasColumnName("subject_user_account_id");
            entity.Property(audit => audit.SessionKeyHash).HasColumnName("session_key_hash").HasColumnType("bytea");
            entity.Property(audit => audit.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
            entity.Property(audit => audit.SafeMetadata).HasColumnName("safe_metadata").HasColumnType("jsonb").IsRequired();
            entity.HasIndex(audit => audit.OccurredAtUtc).HasDatabaseName("ix_security_audit_events_occurred_at_utc");
            entity.HasIndex(audit => audit.ActorUserAccountId).HasDatabaseName("ix_security_audit_events_actor_user_account_id");
            entity.HasIndex(audit => audit.SubjectUserAccountId).HasDatabaseName("ix_security_audit_events_subject_user_account_id");
            entity.HasOne<AuditUserAccountReference>().WithMany().HasForeignKey(audit => audit.ActorUserAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<AuditUserAccountReference>().WithMany().HasForeignKey(audit => audit.SubjectUserAccountId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed record SecurityAuditEntry(
    string EventType,
    string Outcome,
    string ActorKind,
    Guid? ActorUserAccountId = null,
    Guid? SubjectUserAccountId = null,
    byte[]? SessionKeyHash = null,
    string? CorrelationId = null,
    string SafeMetadata = "{}");

public interface ISecurityAuditWriter
{
    Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken = default);
}

public interface ISecurityAuditTransactionWriter : ISecurityAuditWriter
{
    Task WriteAsync(
        SecurityAuditEntry entry,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken = default);
}

/// <summary>Append-only application API. No update/delete methods or entities are exposed.</summary>
public sealed class SecurityAuditWriter(IDbContextFactory<SecurityAuditDbContext> contextFactory)
    : ISecurityAuditWriter, ISecurityAuditTransactionWriter
{
    public async Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await WriteAsync(context, entry, cancellationToken);
    }

    public async Task WriteAsync(
        SecurityAuditEntry entry,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.Database.SetDbConnection(connection);
        await context.Database.UseTransactionAsync(transaction, cancellationToken);
        await WriteAsync(context, entry, cancellationToken);
    }

    private static async Task WriteAsync(
        SecurityAuditDbContext context,
        SecurityAuditEntry entry,
        CancellationToken cancellationToken)
    {
        if (entry.EventType.Length is < 1 or > 128)
            throw new ArgumentOutOfRangeException(nameof(entry), "Event type must be 1-128 characters.");
        if (entry.Outcome is not ("Succeeded" or "Failed" or "Rejected" or "Recorded"))
            throw new ArgumentException("Unsupported audit outcome.", nameof(entry));
        if (entry.ActorKind is not ("Anonymous" or "User" or "System"))
            throw new ArgumentException("Unsupported audit actor kind.", nameof(entry));
        if (entry.SessionKeyHash is { Length: not 32 })
            throw new ArgumentException("Only a 32-byte session-key hash can be recorded.", nameof(entry));

        context.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = entry.EventType,
            Outcome = entry.Outcome,
            ActorKind = entry.ActorKind,
            ActorUserAccountId = entry.ActorUserAccountId,
            SubjectUserAccountId = entry.SubjectUserAccountId,
            SessionKeyHash = entry.SessionKeyHash,
            CorrelationId = entry.CorrelationId,
            SafeMetadata = entry.SafeMetadata
        });
        await context.SaveChangesAsync(cancellationToken);
    }
}

public static class SecurityAuditPersistenceRegistration
{
    public static IServiceCollection AddSecurityAuditPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        services.AddDbContextFactory<SecurityAuditDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<SecurityAuditWriter>();
        services.AddScoped<ISecurityAuditWriter>(provider => provider.GetRequiredService<SecurityAuditWriter>());
        services.AddScoped<ISecurityAuditTransactionWriter>(provider => provider.GetRequiredService<SecurityAuditWriter>());
        services.AddScoped<SecurityAuditQueryService>();
        return services;
    }
}
