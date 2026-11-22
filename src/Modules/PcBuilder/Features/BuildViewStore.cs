using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.PcBuilder;

/// <summary>Records one visitor's view of a shared build (in memory) and saves the waiting views to the build rows in one batch.</summary>
public sealed class BuildViewStore(IDbContextFactory<PcBuilderDbContext> contextFactory, BuildViewCounter counter, TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone)
{
    /// <summary>
    /// Counts the view when the rules allow it. A visitor already counted today costs no database read; a build that does not exist or
    /// cannot be counted changes nothing and says nothing.
    /// </summary>
    public async Task RecordAsync(string token, Guid? viewerId, string visitorKey, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (BuildViewRules.IsRobot(userAgent)) return;
        var today = BusinessDay.Today(timeProvider, businessTimeZone);
        if (counter.Remembers(token, visitorKey, today)) return;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var build = await db.SavedBuilds.AsNoTracking().Where(entry => entry.Token == token)
            .Select(entry => new { entry.Visibility, entry.UserAccountId }).SingleOrDefaultAsync(cancellationToken);
        if (build is not null && BuildViewRules.Counts(build.Visibility, build.UserAccountId, viewerId)) counter.TryCount(token, visitorKey, today);
    }

    /// <summary>Adds the waiting views to the builds; on a database error they are kept for the next run.</summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        var views = counter.Drain();
        if (views.Count == 0) return;
        try
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            foreach (var (token, count) in views)
                await db.SavedBuilds.Where(build => build.Token == token)
                    .ExecuteUpdateAsync(set => set.SetProperty(build => build.ViewCount, build => build.ViewCount + count), cancellationToken);
        }
        catch
        {
            counter.Restore(views);
            throw;
        }
    }
}
