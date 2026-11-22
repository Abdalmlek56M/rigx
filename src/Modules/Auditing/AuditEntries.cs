using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Common.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Auditing;

/// <summary>
/// One change of one business row (who, when, from where, which fields before and after). Written only by the audit interceptor
/// inside the saving transaction; the database refuses to update, delete or truncate these rows.
/// </summary>
public sealed class AuditEntry
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public required string ActorKind { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorIp { get; set; }
    public string? ActorUserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public required string Module { get; set; }
    public required string Entity { get; set; }
    public required string EntityId { get; set; }
    public required string Action { get; set; }
    /// <summary>jsonb list of <see cref="AuditFieldChange"/> (read through <see cref="AuditChanges"/>).</summary>
    public required string Changes { get; set; }
    public Guid? RestoredFromEntryId { get; set; }
}

internal static class AuditEntryConfiguration
{
    public static void Configure(ModelBuilder model) =>
        model.Entity<AuditEntry>(entity =>
        {
            entity.ToTable(AuditTable.Name, table => table.HasCheckConstraint("ck_audit_entries_action",
                $"{AuditTable.Action} IN ({string.Join(", ", AuditAction.All.Select(action => $"'{action}'"))})"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName(AuditTable.Id).ValueGeneratedNever();
            entity.Property(item => item.OccurredAtUtc).HasColumnName(AuditTable.OccurredAtUtc).HasColumnType("timestamp with time zone");
            entity.Property(item => item.ActorKind).HasColumnName(AuditTable.ActorKind).HasMaxLength(16).IsRequired();
            entity.Property(item => item.ActorUserId).HasColumnName(AuditTable.ActorUserId);
            entity.Property(item => item.ActorIp).HasColumnName(AuditTable.ActorIp).HasMaxLength(64);
            entity.Property(item => item.ActorUserAgent).HasColumnName(AuditTable.ActorUserAgent).HasMaxLength(AuditRules.MaxUserAgentLength);
            entity.Property(item => item.CorrelationId).HasColumnName(AuditTable.CorrelationId).HasMaxLength(128);
            entity.Property(item => item.Module).HasColumnName(AuditTable.Module).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Entity).HasColumnName(AuditTable.Entity).HasMaxLength(128).IsRequired();
            entity.Property(item => item.EntityId).HasColumnName(AuditTable.EntityId).HasMaxLength(256).IsRequired();
            entity.Property(item => item.Action).HasColumnName(AuditTable.Action).HasMaxLength(16).IsRequired();
            entity.Property(item => item.Changes).HasColumnName(AuditTable.Changes).HasColumnType("jsonb").IsRequired();
            entity.Property(item => item.RestoredFromEntryId).HasColumnName(AuditTable.RestoredFromEntryId);
            entity.HasIndex(item => item.OccurredAtUtc).IsDescending().HasDatabaseName("ix_audit_entries_occurred_at_utc");
            entity.HasIndex(item => new { item.Entity, item.EntityId }).HasDatabaseName("ix_audit_entries_entity");
            entity.HasIndex(item => item.ActorUserId).HasDatabaseName("ix_audit_entries_actor_user_id");
            entity.HasIndex(item => item.Module).HasDatabaseName("ix_audit_entries_module");
        });
}

/// <summary>An audit entry as the log shows it; the actor's name and "can restore" are added by Host.</summary>
public sealed record AuditEntryView(Guid Id, DateTimeOffset OccurredAtUtc, string ActorKind, Guid? ActorUserId, string? ActorIp,
    string? ActorUserAgent, string? CorrelationId, string Module, string Entity, string EntityId, string Action,
    IReadOnlyList<AuditFieldChange> Changes, Guid? RestoredFromEntryId);

public sealed record AuditEntryFilter(Guid? ActorUserId, string? Module, string? Entity, string? EntityId, string? Action,
    DateTimeOffset FromUtc, DateTimeOffset ToUtc, int Page, int PageSize);

/// <summary>A range of the log narrowed by kind only; the month and day tallies read the log this way.</summary>
public sealed record AuditRangeFilter(string? Module, string? Entity, string? Action, DateTimeOffset FromUtc, DateTimeOffset ToUtc);

public sealed record AuditFilterLists(IReadOnlyList<string> Modules, IReadOnlyList<string> Entities, IReadOnlyList<string> Actions);

/// <summary>Reads the business audit log. There is no write or delete here on purpose.</summary>
public sealed class AuditEntryStore(IDbContextFactory<SecurityAuditDbContext> contextFactory)
{
    public async Task<PagedResult<AuditEntryView>> QueryAsync(AuditEntryFilter filter, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = Filtered(db, filter);
        var total = await rows.CountAsync(cancellationToken);
        var page = await rows.OrderByDescending(entry => entry.OccurredAtUtc).ThenByDescending(entry => entry.Id)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<AuditEntryView>([.. page.Select(ToView)], filter.Page, filter.PageSize, total);
    }

    /// <summary>Every entry in the range, newest first, at most <paramref name="limit"/> rows (used by the export).</summary>
    public async Task<IReadOnlyList<AuditEntryView>> ListAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, int limit, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.AuditEntries.AsNoTracking().Where(entry => entry.OccurredAtUtc >= fromUtc && entry.OccurredAtUtc < toUtc)
            .OrderByDescending(entry => entry.OccurredAtUtc).ThenByDescending(entry => entry.Id).Take(limit).ToListAsync(cancellationToken);
        return [.. rows.Select(ToView)];
    }

    public async Task<AuditEntryView?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await db.AuditEntries.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entry is null ? null : ToView(entry);
    }

    /// <summary>Who and when for every entry in the range (the changes themselves are not read).</summary>
    public async Task<IReadOnlyList<AuditStamp>> StampsAsync(AuditRangeFilter filter, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Narrow(InRange(db, filter.FromUtc, filter.ToUtc), filter.Module, filter.Entity, filter.Action)
            .Select(entry => new AuditStamp(entry.OccurredAtUtc, entry.ActorKind, entry.ActorUserId)).ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(AuditRangeFilter filter, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await Narrow(InRange(db, filter.FromUtc, filter.ToUtc), filter.Module, filter.Entity, filter.Action).CountAsync(cancellationToken);
    }

    /// <summary>The instant of the oldest entry, or null for an empty log.</summary>
    public async Task<DateTimeOffset?> EarliestAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.AuditEntries.MinAsync(entry => (DateTimeOffset?)entry.OccurredAtUtc, cancellationToken);
    }

    public async Task<AuditFilterLists> FiltersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var modules = await db.AuditEntries.Select(entry => entry.Module).Distinct().OrderBy(name => name).ToListAsync(cancellationToken);
        var entities = await db.AuditEntries.Select(entry => entry.Entity).Distinct().OrderBy(name => name).ToListAsync(cancellationToken);
        return new AuditFilterLists(modules, entities, AuditAction.All);
    }

    private static IQueryable<AuditEntry> InRange(SecurityAuditDbContext db, DateTimeOffset fromUtc, DateTimeOffset toUtc) =>
        db.AuditEntries.AsNoTracking().Where(entry => entry.OccurredAtUtc >= fromUtc && entry.OccurredAtUtc < toUtc);

    private static IQueryable<AuditEntry> Narrow(IQueryable<AuditEntry> rows, string? module, string? entity, string? action)
    {
        if (!string.IsNullOrWhiteSpace(module)) rows = rows.Where(entry => entry.Module == module);
        if (!string.IsNullOrWhiteSpace(entity)) rows = rows.Where(entry => entry.Entity == entity);
        if (!string.IsNullOrWhiteSpace(action)) rows = rows.Where(entry => entry.Action == action);
        return rows;
    }

    private static IQueryable<AuditEntry> Filtered(SecurityAuditDbContext db, AuditEntryFilter filter)
    {
        var rows = Narrow(InRange(db, filter.FromUtc, filter.ToUtc), filter.Module, filter.Entity, filter.Action);
        if (filter.ActorUserId is { } actor) rows = rows.Where(entry => entry.ActorUserId == actor);
        if (!string.IsNullOrWhiteSpace(filter.EntityId)) rows = rows.Where(entry => entry.EntityId == filter.EntityId);
        return rows;
    }

    private static AuditEntryView ToView(AuditEntry entry) => new(entry.Id, entry.OccurredAtUtc, entry.ActorKind, entry.ActorUserId, entry.ActorIp,
        entry.ActorUserAgent, entry.CorrelationId, entry.Module, entry.Entity, entry.EntityId, entry.Action, AuditChanges.Parse(entry.Changes),
        entry.RestoredFromEntryId);
}
