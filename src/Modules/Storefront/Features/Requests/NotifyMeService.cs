using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>Notify-me over the stock: ends the wait of requests whose model came back, and reports which models people ask for.</summary>
public sealed class NotifyMeService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    IStockModelDirectory stock,
    TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone)
{
    /// <summary>
    /// Gives each waiting request whose model now has a published available item that item's code. One conditional update per request,
    /// so running again (or two runs at once) matches each request once; only newly matched requests are returned.
    /// </summary>
    public async Task<IReadOnlyList<BackInStockMatch>> MatchBackInStockAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var waiting = await db.CustomerRequests.AsNoTracking().Where(NotifyMeRules.OpenFilter).Where(item => item.AvailableItemCode == null)
            .Select(item => new { item.Id, ModelKey = item.ModelKey!, item.Condition, item.Name, item.Email, item.UserAccountId }).ToListAsync(cancellationToken);
        if (waiting.Count == 0) return [];
        var available = await stock.FindAvailableAsync(waiting.Select(item => item.ModelKey).Distinct().ToList(), cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var matches = new List<BackInStockMatch>();
        foreach (var request in waiting)
        {
            if (NotifyMeRules.Pick(available.GetValueOrDefault(request.ModelKey), request.Condition) is not { } item) continue;
            var changed = await db.CustomerRequests.Where(NotifyMeRules.OpenFilter).Where(row => row.Id == request.Id && row.AvailableItemCode == null)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(row => row.AvailableItemCode, item.Code)
                    .SetProperty(row => row.AvailableAtUtc, (DateTimeOffset?)now)
                    .SetProperty(row => row.UpdatedAtUtc, now)
                    .SetProperty(row => row.RowVersion, row => row.RowVersion + 1), cancellationToken);
            if (changed == 1) matches.Add(new BackInStockMatch(request.Name, request.Email, request.UserAccountId, item.Code, item.ModelName));
        }
        return matches;
    }

    /// <summary>Notify-me requests created in the Cairo days <paramref name="from"/>..<paramref name="to"/>, grouped by model (most asked first).</summary>
    public async Task<IReadOnlyList<NotifyMeDemandRow>> DemandAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (to < from) throw new StorefrontException(400, "The start date is after the end date.");
        var (fromUtc, toUtc) = BusinessDay.Range(from, to, businessTimeZone);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entries = await db.CustomerRequests.AsNoTracking()
            .Where(item => item.Type == CustomerRequestType.NotifyMe && item.ModelKey != null && item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc)
            .Select(item => new NotifyMeEntry(item.ModelKey!, item.ModelName ?? item.ModelKey!, item.CategoryId, item.Status, item.AvailableItemCode,
                item.NotifiedAtUtc, item.CreatedAtUtc)).ToListAsync(cancellationToken);
        var available = await stock.FindAvailableAsync(entries.Select(entry => entry.ModelKey).Distinct().ToList(), cancellationToken);
        return NotifyMeRules.Demand(entries, available.Keys.ToHashSet(StringComparer.Ordinal));
    }
}
