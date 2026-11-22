using HardwareStore.Common.Domain;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>The month and day tallies of the audit log: Cairo days, distinct actors, month parsing and the month list.</summary>
public sealed class AuditDaysTests
{
    private static readonly TimeZoneInfo Cairo = RetailSales.BusinessZone;
    private static readonly Guid Sara = Guid.CreateVersion7();
    private static readonly Guid Omar = Guid.CreateVersion7();

    private static AuditStamp Stamp(string utc, Guid? actor, string kind = "User") => new(DateTimeOffset.Parse(utc), kind, actor);

    [Fact]
    public void Entries_are_counted_by_Cairo_day_newest_first_and_a_late_evening_belongs_to_the_next_day()
    {
        var days = AuditDays.Tally(
        [
            Stamp("2026-03-15T09:00:00Z", Sara), Stamp("2026-03-15T21:59:00Z", Omar), Stamp("2026-03-15T22:30:00Z", Sara), Stamp("2026-03-14T10:00:00Z", Sara)
        ], Cairo);

        Assert.Equal([new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 15), new DateOnly(2026, 3, 14)], days.Select(day => day.Date));
        Assert.Equal([1, 2, 1], days.Select(day => day.Count));
    }

    [Fact]
    public void A_user_is_listed_once_a_day_and_system_or_guest_work_is_listed_by_kind()
    {
        var day = Assert.Single(AuditDays.Tally(
        [
            Stamp("2026-03-15T08:00:00Z", Sara), Stamp("2026-03-15T09:00:00Z", Sara), Stamp("2026-03-15T10:00:00Z", null, "System"),
            Stamp("2026-03-15T11:00:00Z", null, "System"), Stamp("2026-03-15T12:00:00Z", null, "Anonymous")
        ], Cairo));

        Assert.Equal(5, day.Count);
        Assert.Equal([Sara], day.ActorUserIds);
        Assert.Equal(["Anonymous", "System"], day.OtherActorKinds);
    }

    [Fact]
    public void An_empty_log_has_no_days()
    {
        Assert.Empty(AuditDays.Tally([], Cairo));
    }

    [Theory]
    [InlineData("2026-11", 2026, 11)]
    [InlineData("2026-01", 2026, 1)]
    public void A_month_is_read_as_its_first_day(string month, int year, int number)
    {
        Assert.Equal(new DateOnly(year, number, 1), AuditDays.ParseMonth(month));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026-13")]
    [InlineData("2026-1")]
    [InlineData("2026-11-05")]
    [InlineData("november")]
    public void Anything_else_is_a_400(string? month)
    {
        Assert.Equal(400, Assert.Throws<AuditException>(() => AuditDays.ParseMonth(month)).StatusCode);
    }

    [Fact]
    public void The_months_run_from_this_month_back_to_the_month_of_the_first_entry()
    {
        var months = AuditDays.MonthsBetween(new DateOnly(2025, 11, 20), new DateOnly(2026, 2, 3));

        Assert.Equal([new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1), new DateOnly(2025, 12, 1), new DateOnly(2025, 11, 1)], months);
    }
}
