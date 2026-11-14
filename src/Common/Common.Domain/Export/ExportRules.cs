namespace HardwareStore.Common.Domain;

public sealed class ExportException(int statusCode, string message) : AppException(statusCode, message);

/// <summary>Limits and defaults every export shares: the date range and the largest file.</summary>
public static class ExportRules
{
    public const int MaxRows = 50_000;
    public const int DefaultDays = 30;

    /// <summary>The business-day range asked for; it defaults to the last <see cref="DefaultDays"/> days up to today.</summary>
    public static (DateOnly From, DateOnly To) Range(DateOnly? from, DateOnly? to, DateOnly today)
    {
        var end = to ?? today;
        var start = from ?? end.AddDays(-DefaultDays);
        if (end < start) throw new ExportException(400, "The end date cannot precede the start date.");
        return (start, end);
    }

    /// <summary>Services read at most this many rows, so one more than <see cref="MaxRows"/> means "too many".</summary>
    public const int ReadLimit = MaxRows + 1;

    public static void EnsureWithinLimit(int rowCount)
    {
        if (rowCount > MaxRows)
            throw new ExportException(409, $"The export has more than {MaxRows} rows. Narrow the date range.");
    }
}
