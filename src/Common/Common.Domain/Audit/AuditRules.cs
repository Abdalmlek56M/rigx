using System.Text.Json.Nodes;

namespace HardwareStore.Common.Domain;

public sealed class AuditException(int statusCode, string message, IReadOnlyDictionary<string, string[]>? errors = null)
    : AppException(statusCode, message, errors);

public static class AuditAction
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string SoftDeleted = "SoftDeleted";
    public const string Restored = "Restored";
    public const string ValuesRestored = "ValuesRestored";

    public static readonly IReadOnlyList<string> All = [Created, Updated, Deleted, SoftDeleted, Restored, ValuesRestored];
}

/// <summary>What the business audit log records and how it is read: masking, bookkeeping fields, the action of a save, query limits.</summary>
public static class AuditRules
{
    /// <summary>What a masked field (token hash, secret, national ID) shows instead of its value.</summary>
    public const string MaskedValue = "***";

    public const int MaxUserAgentLength = 256;
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 25;

    /// <summary>Fields the system maintains on every edit; they never appear in a change list.</summary>
    private static readonly HashSet<string> BookkeepingFields = new(StringComparer.Ordinal)
    {
        "RowVersion", "UpdatedAtUtc", "DeletedAtUtc", "DeletedByUserId"
    };

    public static bool IsBookkeeping(string field) => BookkeepingFields.Contains(field);

    public static string? ShortenUserAgent(string? userAgent) =>
        string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Length <= MaxUserAgentLength ? userAgent : userAgent[..MaxUserAgentLength];

    /// <summary>
    /// The action an edit of an existing row is logged as: a trash move or restore, a restore of earlier values,
    /// or a plain update. An update with no real field change is not logged (null).
    /// </summary>
    public static string? ActionForUpdate(DateTimeOffset? deletedBefore, DateTimeOffset? deletedAfter, bool valuesRestore, int changedFields)
    {
        if (deletedBefore is null && deletedAfter is not null) return AuditAction.SoftDeleted;
        if (deletedBefore is not null && deletedAfter is null) return AuditAction.Restored;
        if (changedFields == 0) return null;
        return valuesRestore ? AuditAction.ValuesRestored : AuditAction.Updated;
    }

    /// <summary>Checks the paging and action filter of a list request (400 when out of range).</summary>
    public static void ValidateQuery(int page, int pageSize, string? action)
    {
        if (page < 1) throw new AuditException(400, "Page must be 1 or more.");
        if (pageSize is < 1 or > MaxPageSize) throw new AuditException(400, $"Page size must be between 1 and {MaxPageSize}.");
        if (action is not null && !AuditAction.All.Contains(action))
            throw new AuditException(400, "Unknown audit action.");
    }

    /// <summary>
    /// The changes of an entry that can be put back: only an edit (<see cref="AuditAction.Updated"/>) of a kind on the allowlist,
    /// limited to that kind's restorable fields. Empty means "cannot restore".
    /// </summary>
    public static IReadOnlyList<AuditFieldChange> RestorableChanges(string action, IReadOnlyCollection<string>? restorableFields,
        IReadOnlyList<AuditFieldChange> changes) =>
        action == AuditAction.Updated && restorableFields is not null
            ? [.. changes.Where(change => restorableFields.Contains(change.Field, StringComparer.Ordinal))]
            : [];

    /// <summary>409 listing the fields whose current value is no longer the one this entry saved.</summary>
    public static void EnsureUnchangedSince(IReadOnlyList<AuditFieldChange> restorable, IReadOnlyDictionary<string, JsonNode?> current)
    {
        var changed = restorable.Where(change => !current.TryGetValue(change.Field, out var now) || !AuditChanges.ValuesEqual(now, change.After))
            .Select(change => change.Field).ToArray();
        if (changed.Length > 0)
            throw new AuditException(409, "The record changed since this entry.", new Dictionary<string, string[]> { ["fields"] = changed });
    }
}

/// <summary>
/// One kind of record whose earlier values can be put back from the audit log: an explicit list of simple descriptive
/// fields (never money ledgers, stock state, orders, payments or accounts). A module registers one per allowlisted kind.
/// </summary>
public interface IAuditRestoreTarget
{
    string Module { get; }

    /// <summary>The table name, as the audit entry records it.</summary>
    string Entity { get; }

    IReadOnlyCollection<string> RestorableFields { get; }

    /// <summary>The current value of every restorable field; null when the record no longer exists.</summary>
    Task<IReadOnlyDictionary<string, JsonNode?>?> ReadCurrentAsync(string entityId, CancellationToken cancellationToken = default);

    /// <summary>Writes the values back (and bumps the row version), so the save is logged as a restore.</summary>
    Task ApplyAsync(string entityId, IReadOnlyDictionary<string, JsonNode?> values, CancellationToken cancellationToken = default);
}
