using HardwareStore.Common.Domain;
using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Business (Cairo) days: pure conversions, no database.</summary>
public sealed class BusinessDayTests
{
    private static readonly TimeZoneInfo Cairo = RetailSales.BusinessZone;

    [Fact]
    public void A_late_utc_evening_is_already_the_next_cairo_day() =>
        Assert.Equal(new DateOnly(2026, 11, 14), BusinessDay.Of(DateTimeOffset.Parse("2026-11-13T22:30:00Z"), Cairo));

    [Fact]
    public void Today_follows_the_clock_in_the_business_zone() =>
        Assert.Equal(new DateOnly(2026, 11, 14), BusinessDay.Today(new FixedTime(DateTimeOffset.Parse("2026-11-13T22:30:00Z")), Cairo));

    [Fact]
    public void A_winter_day_runs_from_22_00_to_22_00_utc()
    {
        var (from, to) = BusinessDay.Range(new DateOnly(2026, 11, 14), new DateOnly(2026, 11, 14), Cairo);
        Assert.Equal(DateTimeOffset.Parse("2026-11-13T22:00:00Z"), from);
        Assert.Equal(DateTimeOffset.Parse("2026-11-14T22:00:00Z"), to);
        Assert.Equal(TimeSpan.Zero, from.Offset);
    }

    [Fact]
    public void A_summer_day_starts_at_21_00_utc()
    {
        var (from, _) = BusinessDay.Range(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 1), Cairo);
        Assert.Equal(DateTimeOffset.Parse("2026-06-30T21:00:00Z"), from);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
