namespace HardwareStore.Common.Domain;

/// <summary>Business (Cairo) calendar days: a sale at 00:30 local time belongs to that local day, not the previous UTC day.</summary>
public static class BusinessDay
{
    /// <summary>The calendar date of an instant in the business time zone.</summary>
    public static DateOnly Of(DateTimeOffset instant, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);

    public static DateOnly Today(TimeProvider time, TimeZoneInfo zone) => Of(time.GetUtcNow(), zone);

    /// <summary>UTC instants of local midnight of <paramref name="from"/> (inclusive) and of the day after <paramref name="to"/> (exclusive).</summary>
    public static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) Range(DateOnly from, DateOnly to, TimeZoneInfo zone) =>
        (LocalMidnightUtc(from, zone), LocalMidnightUtc(to.AddDays(1), zone));

    private static DateTimeOffset LocalMidnightUtc(DateOnly date, TimeZoneInfo zone) =>
        new(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), zone), TimeSpan.Zero);
}
