using System.Globalization;
using System.Text.RegularExpressions;

namespace HardwareStore.Common.Domain;

public sealed class BackupException(int statusCode, string message) : AppException(statusCode, message);

/// <summary>When a daily backup is due, what the archives are called and which of them are past retention.</summary>
public static partial class BackupRules
{
    private const string TimestampFormat = "yyyyMMdd-HHmmss";

    public static string FileName(DateTimeOffset startedAtUtc) =>
        $"rigx-backup-{startedAtUtc.UtcDateTime.ToString(TimestampFormat, CultureInfo.InvariantCulture)}Z.zip";

    /// <summary>True only for names this app writes, so retention never touches other files in a destination.</summary>
    public static bool IsBackupFile(string fileName) => NamePattern().IsMatch(fileName);

    /// <summary>
    /// A scheduled backup is due once the Cairo clock reaches the configured hour and no scheduled backup
    /// has succeeded yet on today's Cairo date.
    /// </summary>
    public static bool IsDue(bool enabled, int hourCairo, DateTimeOffset nowUtc, DateTimeOffset? lastScheduledSuccessUtc, TimeZoneInfo zone)
    {
        if (!enabled) return false;
        var local = TimeZoneInfo.ConvertTime(nowUtc, zone);
        if (local.Hour < hourCairo) return false;
        return lastScheduledSuccessUtc is null || BusinessDay.Of(lastScheduledSuccessUtc.Value, zone) != BusinessDay.Of(nowUtc, zone);
    }

    public static DateTimeOffset RetentionCutoffUtc(DateTimeOffset nowUtc, int retentionDays) => nowUtc.AddDays(-retentionDays);

    /// <summary>A matching archive older than the cutoff; the age comes from the name, since synced folders change file times.</summary>
    public static bool IsExpired(string fileName, DateTimeOffset cutoffUtc)
    {
        var match = NamePattern().Match(fileName);
        if (!match.Success) return false;
        var created = DateTime.ParseExact(match.Groups["stamp"].Value, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        return new DateTimeOffset(created, TimeSpan.Zero) < cutoffUtc;
    }

    [GeneratedRegex(@"^rigx-backup-(?<stamp>\d{8}-\d{6})Z\.zip$", RegexOptions.CultureInvariant)]
    private static partial Regex NamePattern();
}
