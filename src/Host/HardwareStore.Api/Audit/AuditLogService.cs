using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Common.Infrastructure.Audit;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;

/// <summary>One entry of the business audit log as the admin screen shows it.</summary>
public sealed record AuditEntryResponse(Guid Id, DateTimeOffset OccurredAtUtc, string ActorKind, Guid? ActorUserId, string? ActorName,
    string? ActorIp, string? Device, string? CorrelationId, string Module, string Entity, string EntityId, string Action,
    IReadOnlyList<AuditFieldChange> Changes, Guid? RestoredFromEntryId, bool CanRestore,
    IReadOnlyDictionary<Guid, string> LinkedNames);

/// <summary>A month of the log (<c>yyyy-MM</c>) and how many changes it holds.</summary>
public sealed record AuditMonthResponse(string Month, int Count);

/// <summary>A day of the log with its count and who made the changes: user names, then the other kinds of actor (System, Anonymous).</summary>
public sealed record AuditDayResponse(DateOnly Date, int Count, IReadOnlyList<string> ActorNames, IReadOnlyList<string> OtherActors);

/// <summary>
/// The business audit log across modules: the Auditing module owns the rows, Identity names the actors and each module
/// registers the kinds whose earlier values can be put back (<see cref="IAuditRestoreTarget"/>); Host composes them.
/// </summary>
public sealed class AuditLogService(AuditEntryStore store, UserDisplayNames names, CatalogNames catalogNames, IEnumerable<IAuditRestoreTarget> targets,
    TimeProvider timeProvider, TimeZoneInfo businessZone)
{
    /// <summary>A log older than this many months is read through the entries list and its date range.</summary>
    private const int MaxMonths = 60;

    private static readonly IReadOnlyDictionary<Guid, string> EmptyNames = new Dictionary<Guid, string>();

    public async Task<PagedResult<AuditEntryResponse>> QueryAsync(Guid? actorUserId, string? module, string? entity, string? entityId, string? action,
        DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken cancellationToken)
    {
        AuditRules.ValidateQuery(page, pageSize, action);
        var (start, end) = ExportRules.Range(from, to, BusinessDay.Today(timeProvider, businessZone));
        var (fromUtc, toUtc) = BusinessDay.Range(start, end, businessZone);
        var result = await store.QueryAsync(new AuditEntryFilter(actorUserId, module, entity, entityId, action, fromUtc, toUtc, page, pageSize), cancellationToken);
        return new PagedResult<AuditEntryResponse>(await ToResponsesAsync(result.Items, cancellationToken), result.Page, result.PageSize, result.Total);
    }

    /// <summary>Every month from the first entry to this month that holds changes (newest first, at most <see cref="MaxMonths"/>), narrowed by kind.</summary>
    public async Task<IReadOnlyList<AuditMonthResponse>> MonthsAsync(string? module, string? entity, string? action, CancellationToken cancellationToken)
    {
        AuditRules.ValidateQuery(1, 1, action);
        if (await store.EarliestAsync(cancellationToken) is not { } earliest) return [];
        var months = new List<AuditMonthResponse>();
        foreach (var first in AuditDays.MonthsBetween(BusinessDay.Of(earliest, businessZone), BusinessDay.Today(timeProvider, businessZone)).Take(MaxMonths))
        {
            var (fromUtc, toUtc) = BusinessDay.Range(first, first.AddMonths(1).AddDays(-1), businessZone);
            var count = await store.CountAsync(new AuditRangeFilter(module, entity, action, fromUtc, toUtc), cancellationToken);
            if (count > 0) months.Add(new AuditMonthResponse($"{first:yyyy-MM}", count));
        }
        return months;
    }

    /// <summary>The days of one month that hold changes, newest first, with the count and the people behind them.</summary>
    public async Task<IReadOnlyList<AuditDayResponse>> DaysAsync(string? month, string? module, string? entity, string? action, CancellationToken cancellationToken)
    {
        AuditRules.ValidateQuery(1, 1, action);
        var first = AuditDays.ParseMonth(month);
        var (fromUtc, toUtc) = BusinessDay.Range(first, first.AddMonths(1).AddDays(-1), businessZone);
        var days = AuditDays.Tally(await store.StampsAsync(new AuditRangeFilter(module, entity, action, fromUtc, toUtc), cancellationToken), businessZone);
        var actorNames = await names.NamesAsync([.. days.SelectMany(day => day.ActorUserIds).Distinct()], cancellationToken);
        return [.. days.Select(day => new AuditDayResponse(day.Date, day.Count,
            [.. day.ActorUserIds.Select(id => actorNames.GetValueOrDefault(id)).OfType<string>().Order(StringComparer.CurrentCulture)], day.OtherActorKinds))];
    }

    public async Task<AuditEntryResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entry = await store.FindAsync(id, cancellationToken) ?? throw new AuditException(404, "Audit entry not found.");
        return (await ToResponsesAsync([entry], cancellationToken)).Single() with { LinkedNames = await LinkedNamesAsync(entry.Changes, cancellationToken) };
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

    /// <summary>The names of the brands, categories, products and users the changes point to, so the detail reads "Brand: Intel" and not an id.</summary>
    private async Task<IReadOnlyDictionary<Guid, string>> LinkedNamesAsync(IReadOnlyList<AuditFieldChange> changes, CancellationToken cancellationToken)
    {
        var ids = AuditChanges.LinkedIds(changes);
        var found = new Dictionary<Guid, string>(await catalogNames.NamesAsync(ids, cancellationToken));
        foreach (var (id, name) in await names.NamesAsync([.. ids.Where(id => !found.ContainsKey(id))], cancellationToken)) found[id] = name;
        return found;
    }

    private IAuditRestoreTarget? TargetOf(AuditEntryView entry) =>
        targets.SingleOrDefault(target => target.Module == entry.Module && target.Entity == entry.Entity);

    private async Task<IReadOnlyList<AuditEntryResponse>> ToResponsesAsync(IReadOnlyList<AuditEntryView> entries, CancellationToken cancellationToken)
    {
        var actorNames = await names.NamesAsync([.. entries.Select(entry => entry.ActorUserId).OfType<Guid>().Distinct()], cancellationToken);
        return [.. entries.Select(entry => new AuditEntryResponse(entry.Id, entry.OccurredAtUtc, entry.ActorKind, entry.ActorUserId,
            entry.ActorUserId is { } actor ? actorNames.GetValueOrDefault(actor) : null, entry.ActorIp, entry.ActorUserAgent, entry.CorrelationId,
            entry.Module, entry.Entity, entry.EntityId, entry.Action, entry.Changes, entry.RestoredFromEntryId,
            AuditRules.RestorableChanges(entry.Action, TargetOf(entry)?.RestorableFields, entry.Changes).Count > 0, EmptyNames))];
    }
}
