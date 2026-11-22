using System.Collections.Concurrent;

namespace HardwareStore.Modules.PcBuilder;

/// <summary>
/// Counts build views in memory so a page view never writes to the database: it remembers who already viewed a build today
/// and keeps the views not yet saved. <see cref="BuildViewStore"/> saves them in one batch every minute. After a restart a visitor
/// can be counted once more that day; that is accepted for a popularity figure.
/// </summary>
public sealed class BuildViewCounter
{
    /// <summary>The most visitor entries kept for one day; past it new visitors are not counted until the day changes.</summary>
    public const int MaxRemembered = 200_000;

    private readonly ConcurrentDictionary<string, byte> seen = new();
    private readonly ConcurrentDictionary<string, long> pending = new();
    private readonly object dayLock = new();
    private DateOnly day;

    /// <summary>True when the visitor has not viewed this build on <paramref name="today"/> yet (the view is now waiting to be saved).</summary>
    public bool TryCount(string token, string visitorKey, DateOnly today)
    {
        StartDay(today);
        if (seen.Count >= MaxRemembered || !seen.TryAdd(Key(token, visitorKey), 0)) return false;
        pending.AddOrUpdate(token, 1, (_, count) => count + 1);
        return true;
    }

    /// <summary>True when the visitor already has a view recorded for this build today: the caller can skip the database lookup.</summary>
    public bool Remembers(string token, string visitorKey, DateOnly today)
    {
        StartDay(today);
        return seen.ContainsKey(Key(token, visitorKey));
    }

    /// <summary>Takes the views waiting to be saved (token to count) and starts again from zero.</summary>
    public IReadOnlyDictionary<string, long> Drain()
    {
        var taken = new Dictionary<string, long>();
        foreach (var token in pending.Keys)
            if (pending.TryRemove(token, out var count)) taken[token] = count;
        return taken;
    }

    /// <summary>Puts back views that could not be saved, so a database error does not lose them.</summary>
    public void Restore(IReadOnlyDictionary<string, long> views)
    {
        foreach (var (token, count) in views) pending.AddOrUpdate(token, count, (_, current) => current + count);
    }

    private static string Key(string token, string visitorKey) => $"{token}|{visitorKey}";

    private void StartDay(DateOnly today)
    {
        if (day == today) return;
        lock (dayLock)
        {
            if (day == today) return;
            seen.Clear();
            day = today;
        }
    }
}
