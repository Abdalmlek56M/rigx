using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Auditing;

/// <summary>When an entry happened and who did it; enough to tally a day without reading the changes.</summary>
public sealed record AuditStamp(DateTimeOffset OccurredAtUtc, string ActorKind, Guid? ActorUserId);

/// <summary>One business day of the log: how many changes, by which signed-in users and by which other kinds of actor (system, guest).</summary>
public sealed record AuditDayTally(DateOnly Date, int Count, IReadOnlyList<Guid> ActorUserIds, IReadOnlyList<string> OtherActorKinds);

/// <summary>The month of the log: its first day (Cairo) and how many changes it holds.</summary>
public sealed record AuditMonthTally(DateOnly FirstDay, int Count);

/// <summary>Month and day tallies of the audit log: the log is browsed month, then day, then entries.</summary>
public static class AuditDays
{
    /// <summary>The business days that have entries, newest first, each with its count and distinct actors (a user once, other kinds once).</summary>
    public static IReadOnlyList<AuditDayTally> Tally(IEnumerable<AuditStamp> stamps, TimeZoneInfo zone) =>
        [.. stamps.GroupBy(stamp => BusinessDay.Of(stamp.OccurredAtUtc, zone)).OrderByDescending(day => day.Key)
            .Select(day => new AuditDayTally(day.Key, day.Count(),
                [.. day.Select(stamp => stamp.ActorUserId).OfType<Guid>().Distinct()],
                [.. day.Where(stamp => stamp.ActorUserId is null).Select(stamp => stamp.ActorKind).Distinct().Order(StringComparer.Ordinal)]))];

    /// <summary>The first day of the month named <c>yyyy-MM</c>; 400 for anything else.</summary>
    public static DateOnly ParseMonth(string? month) =>
        month is { Length: 7 } && DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", out var first)
            ? first
            : throw new AuditException(400, "The month must be written yyyy-MM.");

    /// <summary>The first days of every month from the month of <paramref name="earliest"/> to the month of <paramref name="today"/>, newest first.</summary>
    public static IReadOnlyList<DateOnly> MonthsBetween(DateOnly earliest, DateOnly today)
    {
        var months = new List<DateOnly>();
        for (var month = new DateOnly(today.Year, today.Month, 1); month >= new DateOnly(earliest.Year, earliest.Month, 1); month = month.AddMonths(-1))
            months.Add(month);
        return months;
    }
}
