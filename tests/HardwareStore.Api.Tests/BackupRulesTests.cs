using HardwareStore.Common.Domain;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Backup schedule, naming and retention rules, and the data reset section rules (no database).</summary>
public sealed class BackupRulesTests
{
    private static readonly TimeZoneInfo Cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute = 0) => new(year, month, day, hour, minute, 0, TimeSpan.Zero);

    [Fact]
    public void A_backup_is_due_from_the_configured_cairo_hour_until_one_succeeds_that_cairo_day()
    {
        // 2026-11-14 is winter time in Cairo (UTC+2): 00:59 UTC is 02:59 local, 01:00 UTC is 03:00 local.
        Assert.False(BackupRules.IsDue(true, 3, Utc(2026, 11, 14, 0, 59), null, Cairo));
        Assert.True(BackupRules.IsDue(true, 3, Utc(2026, 11, 14, 1, 0), null, Cairo));
        Assert.True(BackupRules.IsDue(true, 3, Utc(2026, 11, 14, 20), Utc(2026, 11, 13, 1, 5), Cairo));
        Assert.False(BackupRules.IsDue(true, 3, Utc(2026, 11, 14, 20), Utc(2026, 11, 14, 1, 5), Cairo));
        Assert.False(BackupRules.IsDue(false, 3, Utc(2026, 11, 14, 20), null, Cairo));
    }

    [Fact]
    public void The_day_is_the_cairo_day_not_the_utc_day()
    {
        // 22:30 UTC on the 13th is already 00:30 on the 14th in Cairo, so a backup at 22:10 UTC (00:10 on the 14th) counts for the 14th.
        Assert.False(BackupRules.IsDue(true, 0, Utc(2026, 11, 13, 22, 30), Utc(2026, 11, 13, 22, 10), Cairo));
        Assert.True(BackupRules.IsDue(true, 0, Utc(2026, 11, 13, 22, 30), Utc(2026, 11, 13, 21, 50), Cairo));
    }

    [Fact]
    public void File_names_carry_the_utc_start_time_and_only_those_names_match()
    {
        Assert.Equal("rigx-backup-20261114-013005Z.zip", BackupRules.FileName(Utc(2026, 11, 14, 1, 30).AddSeconds(5)));
        Assert.True(BackupRules.IsBackupFile("rigx-backup-20261114-013005Z.zip"));
        Assert.False(BackupRules.IsBackupFile("rigx-backup-20261114-013005Z.zip.tmp"));
        Assert.False(BackupRules.IsBackupFile("notes.zip"));
        Assert.False(BackupRules.IsBackupFile("rigx-backup-2026-11-14.zip"));
    }

    [Fact]
    public void Retention_expires_only_matching_archives_older_than_the_cutoff()
    {
        var now = Utc(2026, 11, 14, 12);
        var cutoff = BackupRules.RetentionCutoffUtc(now, 14);

        Assert.Equal(Utc(2026, 10, 31, 12), cutoff);
        Assert.True(BackupRules.IsExpired("rigx-backup-20261031-115959Z.zip", cutoff));
        Assert.False(BackupRules.IsExpired("rigx-backup-20261031-120000Z.zip", cutoff));
        Assert.False(BackupRules.IsExpired("rigx-backup-20200101-000000Z.zip.tmp", cutoff));
        Assert.False(BackupRules.IsExpired("holiday-photos-2020.zip", cutoff));
    }

    [Fact]
    public void A_reset_needs_the_confirmation_text_and_known_sections()
    {
        Assert.Equal(400, Assert.Throws<DataResetException>(() => DataResetRules.Validate(["business"], "reset")).StatusCode);
        Assert.Equal(400, Assert.Throws<DataResetException>(() => DataResetRules.Validate(["business"], null)).StatusCode);
        Assert.Equal(400, Assert.Throws<DataResetException>(() => DataResetRules.Validate([], "RESET")).StatusCode);
        Assert.Equal(400, Assert.Throws<DataResetException>(() => DataResetRules.Validate(null, "RESET")).StatusCode);
        Assert.Equal(400, Assert.Throws<DataResetException>(() => DataResetRules.Validate(["business", "everything"], "RESET")).StatusCode);
    }

    [Fact]
    public void Resetting_customers_needs_business_in_the_same_call()
    {
        Assert.Equal(400, Assert.Throws<DataResetException>(() => DataResetRules.Validate(["customers"], "RESET")).StatusCode);
        Assert.Equal(400, Assert.Throws<DataResetException>(() => DataResetRules.Validate(["customers", "content"], "RESET")).StatusCode);

        var chosen = DataResetRules.Validate(["Customers", "business", "business"], "RESET");

        Assert.Equal(["business", "customers"], chosen.Order());
    }
}
