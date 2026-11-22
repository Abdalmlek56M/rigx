using HardwareStore.Modules.PcBuilder;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class BuildViewRulesTests
{
    private static readonly DateOnly Monday = new(2026, 11, 23);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Googlebot/2.1 (+http://www.google.com/bot.html)")]
    [InlineData("facebookexternalhit/1.1")]
    [InlineData("WhatsApp/2.23.20 A")]
    [InlineData("curl/8.4.0")]
    [InlineData("python-requests/2.31")]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64) HeadlessChrome/120.0 Safari/537.36")]
    public void A_missing_or_automated_user_agent_is_a_robot(string? userAgent) => Assert.True(BuildViewRules.IsRobot(userAgent));

    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0 Safari/537.36")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148 Safari/604.1")]
    public void A_browser_is_a_person(string userAgent) => Assert.False(BuildViewRules.IsRobot(userAgent));

    [Fact]
    public void The_owner_and_a_private_build_never_count()
    {
        var owner = Guid.NewGuid();
        Assert.True(BuildViewRules.Counts(BuildVisibility.Link, owner, null));
        Assert.True(BuildViewRules.Counts(BuildVisibility.Public, owner, Guid.NewGuid()));
        Assert.False(BuildViewRules.Counts(BuildVisibility.Link, owner, owner));
        Assert.False(BuildViewRules.Counts(BuildVisibility.Private, owner, Guid.NewGuid()));
        Assert.True(BuildViewRules.Counts(BuildVisibility.Link, null, null));
    }

    [Fact]
    public void A_visitor_is_the_account_or_a_hash_of_address_and_browser()
    {
        var account = Guid.NewGuid();
        Assert.Equal(account.ToString("N"), BuildViewRules.VisitorKey(account, "10.0.0.1", "Chrome"));
        Assert.Equal(BuildViewRules.VisitorKey(null, "10.0.0.1", "Chrome"), BuildViewRules.VisitorKey(null, "10.0.0.1", "Chrome"));
        Assert.NotEqual(BuildViewRules.VisitorKey(null, "10.0.0.1", "Chrome"), BuildViewRules.VisitorKey(null, "10.0.0.2", "Chrome"));
        Assert.DoesNotContain("10.0.0.1", BuildViewRules.VisitorKey(null, "10.0.0.1", "Chrome"));
    }

    [Fact]
    public void The_counter_counts_a_visitor_once_a_day_and_forgets_at_midnight()
    {
        var counter = new BuildViewCounter();
        Assert.True(counter.TryCount("tok", "a", Monday));
        Assert.False(counter.TryCount("tok", "a", Monday));
        Assert.True(counter.TryCount("tok", "b", Monday));
        Assert.True(counter.TryCount("other", "a", Monday));
        Assert.True(counter.Remembers("tok", "a", Monday));

        Assert.True(counter.TryCount("tok", "a", Monday.AddDays(1)));
        Assert.False(counter.Remembers("other", "a", Monday.AddDays(1)));
    }

    [Fact]
    public void Draining_takes_the_waiting_views_once_and_a_failed_save_puts_them_back()
    {
        var counter = new BuildViewCounter();
        counter.TryCount("tok", "a", Monday);
        counter.TryCount("tok", "b", Monday);
        counter.TryCount("other", "a", Monday);

        var taken = counter.Drain();
        Assert.Equal((2L, 1L), (taken["tok"], taken["other"]));
        Assert.Empty(counter.Drain());

        counter.Restore(taken);
        counter.TryCount("tok", "c", Monday);
        Assert.Equal(3L, counter.Drain()["tok"]);
    }
}
