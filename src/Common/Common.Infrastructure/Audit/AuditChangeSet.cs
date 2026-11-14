using System.Globalization;
using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HardwareStore.Common.Infrastructure.Audit;

/// <summary>One row of the audit log waiting for its save to finish (keys of new rows exist only afterwards).</summary>
public sealed class PendingAudit
{
    private readonly EntityEntry entry;
    private IReadOnlyList<AuditFieldChange>? changes;
    private string? entityId;

    internal PendingAudit(EntityEntry entry, string action, IReadOnlyList<AuditFieldChange>? changes, string? entityId)
    {
        this.entry = entry;
        Action = action;
        this.changes = changes;
        this.entityId = entityId;
    }

    public string Action { get; }
    public string Entity => entry.Metadata.GetTableName()!;
    public IReadOnlyList<AuditFieldChange> Changes => changes ?? AuditChangeSet.AfterOnly(entry);
    public string EntityId => entityId ?? AuditChangeSet.KeyText(entry);
}

/// <summary>Reads what a save is about to do (per field, before and after) from the change tracker.</summary>
public static class AuditChangeSet
{
    public static IReadOnlyList<PendingAudit> Collect(DbContext context, bool valuesRestore)
    {
        context.ChangeTracker.DetectChanges();
        var pending = new List<PendingAudit>();
        foreach (var entry in context.ChangeTracker.Entries().Where(Audited))
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    pending.Add(new PendingAudit(entry, AuditAction.Created, null, null));
                    break;
                case EntityState.Deleted:
                    pending.Add(new PendingAudit(entry, AuditAction.Deleted, BeforeOnly(entry), KeyText(entry)));
                    break;
                case EntityState.Modified:
                    var changes = Modified(entry);
                    var action = AuditRules.ActionForUpdate(Deleted(entry, original: true), Deleted(entry, original: false), valuesRestore, changes.Count);
                    if (action is not null) pending.Add(new PendingAudit(entry, action, changes, KeyText(entry)));
                    break;
            }
        }
        return pending;
    }

    internal static IReadOnlyList<AuditFieldChange> AfterOnly(EntityEntry entry) =>
        [.. Fields(entry).Where(property => property.CurrentValue is not null)
            .Select(property => new AuditFieldChange(property.Metadata.Name, null, AuditValues.ToJson(property.Metadata, property.CurrentValue)))];

    private static IReadOnlyList<AuditFieldChange> BeforeOnly(EntityEntry entry) =>
        [.. Fields(entry).Where(property => property.OriginalValue is not null)
            .Select(property => new AuditFieldChange(property.Metadata.Name, AuditValues.ToJson(property.Metadata, property.OriginalValue), null))];

    private static List<AuditFieldChange> Modified(EntityEntry entry)
    {
        var changes = new List<AuditFieldChange>();
        foreach (var property in Fields(entry).Where(property => property.IsModified))
        {
            var metadata = property.Metadata;
            var same = AuditValues.IsMasked(metadata)
                ? AuditValues.RawEqual(property.OriginalValue, property.CurrentValue)
                : AuditChanges.ValuesEqual(AuditValues.ToJson(metadata, property.OriginalValue), AuditValues.ToJson(metadata, property.CurrentValue));
            if (!same)
                changes.Add(new AuditFieldChange(metadata.Name, AuditValues.ToJson(metadata, property.OriginalValue), AuditValues.ToJson(metadata, property.CurrentValue)));
        }
        return changes;
    }

    /// <summary>The fields worth listing: not the key, not the bookkeeping the system maintains.</summary>
    private static IEnumerable<PropertyEntry> Fields(EntityEntry entry) =>
        entry.Properties.Where(property => !property.Metadata.IsPrimaryKey() && !AuditRules.IsBookkeeping(property.Metadata.Name));

    private static DateTimeOffset? Deleted(EntityEntry entry, bool original) =>
        entry.Entity is ISoftDeletable
            ? (DateTimeOffset?)(original ? entry.Property(nameof(ISoftDeletable.DeletedAtUtc)).OriginalValue : entry.Property(nameof(ISoftDeletable.DeletedAtUtc)).CurrentValue)
            : null;

    private static bool Audited(EntityEntry entry) =>
        entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted &&
        entry.Metadata.GetTableName() is not null &&
        entry.Metadata.FindAnnotation(AuditModel.ExcludedAnnotation) is null;

    /// <summary>The primary key as text; a composite key is joined with '|'.</summary>
    internal static string KeyText(EntityEntry entry) =>
        string.Join('|', entry.Metadata.FindPrimaryKey()!.Properties.Select(key =>
            Convert.ToString(entry.Property(key.Name).CurrentValue, CultureInfo.InvariantCulture)));
}
