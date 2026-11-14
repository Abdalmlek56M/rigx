namespace HardwareStore.Common.Domain;

/// <summary>One trashed record as the trash page shows it.</summary>
public sealed record TrashItem(Guid Id, string Label, DateTimeOffset DeletedAtUtc, Guid? DeletedByUserId);

/// <summary>
/// What a module offers the trash page for one kind of deletable record. The Host lists, restores and purges through it
/// without touching the module's database.
/// </summary>
public interface ITrashSource
{
    /// <summary>The kind name used in the trash routes (for example <c>article</c>).</summary>
    string Kind { get; }

    Task<IReadOnlyList<TrashItem>> ListAsync(CancellationToken cancellationToken = default);

    Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Deletes a trashed record for good (404 when it is not in the trash).</summary>
    Task PurgeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Purges everything deleted before <paramref name="before"/> and returns the purged ids.</summary>
    Task<IReadOnlyList<Guid>> PurgeExpiredAsync(DateTimeOffset before, CancellationToken cancellationToken = default);
}

public sealed class TrashException(int statusCode, string message) : AppException(statusCode, message);
