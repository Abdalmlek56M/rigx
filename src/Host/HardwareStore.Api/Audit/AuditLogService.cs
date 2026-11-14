using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Common.Infrastructure.Audit;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity;

/// <summary>One entry of the business audit log as the admin screen shows it.</summary>
public sealed record AuditEntryResponse(Guid Id, DateTimeOffset OccurredAtUtc, string ActorKind, Guid? ActorUserId, string? ActorName,
    string? ActorIp, string? Device, string? CorrelationId, string Module, string Entity, string EntityId, string Action,
    IReadOnlyList<AuditFieldChange> Changes, Guid? RestoredFromEntryId, bool CanRestore);

/// <summary>
/// The business audit log across modules: the Auditing module owns the rows, Identity names the actors and each module
/// registers the kinds whose earlier values can be put back (<see cref="IAuditRestoreTarget"/>); Host composes them.
/// </summary>
public sealed class AuditLogService(AuditEntryStore store, UserDisplayNames names, IEnumerable<IAuditRestoreTarget> targets,
    TimeProvider timeProvider, TimeZoneInfo businessZone)
{
    public async Task<PagedResult<AuditEntryResponse>> QueryAsync(Guid? actorUserId, string? module, string? entity, string? entityId, string? action,
        DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken cancellationToken)
    {
        AuditRules.ValidateQuery(page, pageSize, action);
        var (start, end) = ExportRules.Range(from, to, BusinessDay.Today(timeProvider, businessZone));
        var (fromUtc, toUtc) = BusinessDay.Range(start, end, businessZone);
        var result = await store.QueryAsync(new AuditEntryFilter(actorUserId, module, entity, entityId, action, fromUtc, toUtc, page, pageSize), cancellationToken);
        return new PagedResult<AuditEntryResponse>(await ToResponsesAsync(result.Items, cancellationToken), result.Page, result.PageSize, result.Total);
    }

    public async Task<AuditEntryResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await store.FindAsync(id, cancellationToken) ?? throw new AuditException(404, "Audit entry not found.");
        return (await ToResponsesAsync([entry], cancellationToken)).Single();
    }

    public Task<AuditFilterLists> FiltersAsync(CancellationToken cancellationToken) => store.FiltersAsync(cancellationToken);

    /// <summary>Puts the entry's earlier values back, but only if nothing changed them since; the save is logged as a restore.</summary>
    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await store.FindAsync(id, cancellationToken) ?? throw new AuditException(404, "Audit entry not found.");
        var target = TargetOf(entry) ?? throw new AuditException(409, "This kind of record cannot be restored.");
        var restorable = AuditRules.RestorableChanges(entry.Action, target.RestorableFields, entry.Changes);
        if (restorable.Count == 0) throw new AuditException(409, "This entry has no field that can be restored.");
        var current = await target.ReadCurrentAsync(entry.EntityId, cancellationToken) ?? throw new AuditException(404, "The record no longer exists.");
        AuditRules.EnsureUnchangedSince(restorable, current);
        using (AuditRestoreScope.Begin(entry.Id))
            await target.ApplyAsync(entry.EntityId, restorable.ToDictionary(change => change.Field, change => change.Before), cancellationToken);
    }

    /// <summary>The range as an export table, newest first (Cairo time), at most <see cref="ExportRules.ReadLimit"/> rows.</summary>
    public async Task<ExportTable> ExportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var (fromUtc, toUtc) = BusinessDay.Range(from, to, businessZone);
        var rows = await ToResponsesAsync(await store.ListAsync(fromUtc, toUtc, ExportRules.ReadLimit, cancellationToken), cancellationToken);
        return new ExportTable(
            ["time", "actor", "actor kind", "ip", "device", "module", "entity", "entity id", "action", "changes"],
            [.. rows.Select(row => (IReadOnlyList<ExportCell>)
            [
                ExportCell.Instant(row.OccurredAtUtc, businessZone), ExportCell.Text(row.ActorName), ExportCell.Text(row.ActorKind), ExportCell.Text(row.ActorIp),
                ExportCell.Text(row.Device), ExportCell.Text(row.Module), ExportCell.Text(row.Entity), ExportCell.Text(row.EntityId), ExportCell.Text(row.Action),
                ExportCell.Text(AuditChanges.Serialize(row.Changes))
            ])]);
    }

    private IAuditRestoreTarget? TargetOf(AuditEntryView entry) =>
        targets.SingleOrDefault(target => target.Module == entry.Module && target.Entity == entry.Entity);

    private async Task<IReadOnlyList<AuditEntryResponse>> ToResponsesAsync(IReadOnlyList<AuditEntryView> entries, CancellationToken cancellationToken)
    {
        var actorNames = await names.NamesAsync([.. entries.Select(entry => entry.ActorUserId).OfType<Guid>().Distinct()], cancellationToken);
        return [.. entries.Select(entry => new AuditEntryResponse(entry.Id, entry.OccurredAtUtc, entry.ActorKind, entry.ActorUserId,
            entry.ActorUserId is { } actor ? actorNames.GetValueOrDefault(actor) : null, entry.ActorIp, entry.ActorUserAgent, entry.CorrelationId,
            entry.Module, entry.Entity, entry.EntityId, entry.Action, entry.Changes, entry.RestoredFromEntryId,
            AuditRules.RestorableChanges(entry.Action, TargetOf(entry)?.RestorableFields, entry.Changes).Count > 0))];
    }
}
