using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class TrackingRulesTests
{
    private static readonly DateTimeOffset Delivered = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("01012345678", "010••••••78")]
    [InlineData("+20 101 234 5678", "201•••••••78")]
    [InlineData("0101", "••••")]
    [InlineData("", "••••")]
    public void The_phone_is_masked_but_still_recognisable(string phone, string expected) => Assert.Equal(expected, TrackingRules.MaskPhone(phone));

    [Fact]
    public void The_masked_phone_never_holds_the_whole_number()
    {
        var masked = TrackingRules.MaskPhone("01234567890");
        Assert.DoesNotContain("01234567890", masked);
        Assert.Equal(11, masked.Length);
    }

    [Fact]
    public void Details_stay_for_the_window_after_delivery_and_then_only_the_status_shows()
    {
        Assert.False(TrackingRules.DetailsExpired(Delivered, Delivered.AddDays(89), 90));
        Assert.True(TrackingRules.DetailsExpired(Delivered, Delivered.AddDays(90), 90));
        Assert.True(TrackingRules.DetailsExpired(Delivered, Delivered.AddDays(400), 90));
    }

    [Fact]
    public void An_order_that_is_not_delivered_or_a_window_set_to_never_does_not_expire()
    {
        Assert.False(TrackingRules.DetailsExpired(null, Delivered.AddDays(400), 90));
        Assert.False(TrackingRules.DetailsExpired(Delivered, Delivered.AddDays(4000), null));
    }

    [Fact]
    public void The_window_is_ninety_days_unless_the_owner_changes_it() => Assert.Equal(90, TrackingRules.DetailsVisibleDays);
}
