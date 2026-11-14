using HardwareStore.Common.Domain;

/// <summary>One trashed record on the trash page, with the day it becomes eligible for automatic purge.</summary>
public sealed record TrashEntry(string Kind, Guid Id, string Label, DateTimeOffset DeletedAtUtc, Guid? DeletedByUserId, DateTimeOffset PurgeAfterUtc);

/// <summary>
/// The trash across modules (the Host orchestrates; each module owns its data through its <see cref="ITrashSource"/>):
/// list, restore, purge now and purge what is past retention.
/// </summary>
public sealed class TrashService(IEnumerable<ITrashSource> sources, TrashAuditor audit, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<TrashEntry>> ListAsync(string? kind, CancellationToken cancellationToken)
    {
        var selected = string.IsNullOrWhiteSpace(kind) ? sources.ToList() : [Source(kind)];
        var entries = new List<TrashEntry>();
        foreach (var source in selected)
            entries.AddRange((await source.ListAsync(cancellationToken)).Select(item => new TrashEntry(
                source.Kind, item.Id, item.Label, item.DeletedAtUtc, item.DeletedByUserId, item.DeletedAtUtc.AddDays(TrashRules.RetentionDays))));
        return [.. entries.OrderByDescending(entry => entry.DeletedAtUtc)];
    }

    public async Task RestoreAsync(string kind, Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        await Source(kind).RestoreAsync(id, cancellationToken);
        await audit.RestoredAsync(kind, id, actorId, cancellationToken);
    }

    public async Task PurgeAsync(string kind, Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        await Source(kind).PurgeAsync(id, cancellationToken);
        await audit.PurgedAsync(kind, id, actorId, cancellationToken);
    }

    /// <summary>Purges every item deleted more than the retention period ago; returns how many went.</summary>
    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        var before = TrashRules.PurgeDueBeforeUtc(timeProvider.GetUtcNow());
        var purged = 0;
        foreach (var source in sources)
            foreach (var id in await source.PurgeExpiredAsync(before, cancellationToken))
            {
                await audit.PurgedAsync(source.Kind, id, null, cancellationToken);
                purged++;
            }
        return purged;
    }

    private ITrashSource Source(string kind) =>
        sources.SingleOrDefault(source => string.Equals(source.Kind, kind, StringComparison.Ordinal))
        ?? throw new TrashException(404, "Unknown trash kind.");
}
