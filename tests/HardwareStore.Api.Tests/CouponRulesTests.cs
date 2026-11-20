using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Coupon code format, input checks, when a code works and how much it takes off (roadmap Phase 5, slice K).</summary>
public sealed class CouponRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private static Coupon Coupon(string kind = CouponDiscountKind.Percent, decimal value = 10m, decimal? cap = null, decimal? min = null,
        int? maxUses = null, bool once = true, bool active = true, DateTimeOffset? starts = null, DateTimeOffset? ends = null) =>
        new()
        {
            Code = "AHMED10", SourceName = "Ahmed", DiscountKind = kind, DiscountValue = value, MaxDiscount = cap, MinOrderTotal = min,
            MaxUses = maxUses, OncePerCustomer = once, IsActive = active, StartsAtUtc = starts, EndsAtUtc = ends
        };

    private static CouponInput Input(string? code = "ahmed-10", string? source = "Ahmed – TikTok", string? kind = CouponDiscountKind.Percent,
        decimal value = 10m, decimal? cap = null, decimal? min = null, int? maxUses = null, DateTimeOffset? starts = null,
        DateTimeOffset? ends = null) => new(code, source, kind, value, cap, min, maxUses, true, starts, ends);

    private static string FieldOf(Action action)
    {
        var exception = Assert.Throws<RetailException>(action);
        Assert.Equal(400, exception.StatusCode);
        return Assert.Single(exception.Errors!).Key;
    }

    [Theory]
    [InlineData(" ahmed10 ", "AHMED10")]
    [InlineData("fb-nov", "FB-NOV")]
    [InlineData("ab", null)]
    [InlineData("has space", null)]
    [InlineData("عربي123", null)]
    [InlineData("A1234567890123456789012345678901", null)]
    [InlineData(null, null)]
    public void Codes_are_upper_case_letters_digits_and_dashes(string? value, string? expected) =>
        Assert.Equal(expected, CouponRules.TryCode(value));

    [Fact]
    public void Normalize_keeps_a_good_coupon_and_drops_the_cap_on_an_amount()
    {
        var values = CouponRules.Normalize(Input(kind: CouponDiscountKind.Amount, value: 150m, cap: 50m));
        Assert.Equal(("AHMED-10", "Ahmed – TikTok", CouponDiscountKind.Amount, 150m, (decimal?)null),
            (values.Code, values.SourceName, values.DiscountKind, values.DiscountValue, values.MaxDiscount));
    }

    [Fact]
    public void Normalize_names_the_wrong_field()
    {
        Assert.Equal("code", FieldOf(() => CouponRules.Normalize(Input(code: "x"))));
        Assert.Equal("sourceName", FieldOf(() => CouponRules.Normalize(Input(source: " "))));
        Assert.Equal("discountKind", FieldOf(() => CouponRules.Normalize(Input(kind: "Free"))));
        Assert.Equal("discountValue", FieldOf(() => CouponRules.Normalize(Input(value: 91m))));
        Assert.Equal("discountValue", FieldOf(() => CouponRules.Normalize(Input(value: 0m))));
        Assert.Equal("discountValue", FieldOf(() => CouponRules.Normalize(Input(kind: CouponDiscountKind.Amount, value: 10.005m))));
        Assert.Equal("maxDiscount", FieldOf(() => CouponRules.Normalize(Input(cap: -5m))));
        Assert.Equal("minOrderTotal", FieldOf(() => CouponRules.Normalize(Input(min: 0m))));
        Assert.Equal("maxUses", FieldOf(() => CouponRules.Normalize(Input(maxUses: 0))));
        Assert.Equal("endsAtUtc", FieldOf(() => CouponRules.Normalize(Input(starts: Now, ends: Now))));
    }

    [Fact]
    public void Problem_checks_on_dates_uses_customer_and_minimum()
    {
        Assert.Null(CouponRules.Problem(Coupon(), Now, 100m, 0, 0));
        Assert.Equal(CouponProblem.Inactive, CouponRules.Problem(Coupon(active: false), Now, 100m, 0, 0));
        Assert.Equal(CouponProblem.NotStarted, CouponRules.Problem(Coupon(starts: Now.AddMinutes(1)), Now, 100m, 0, 0));
        Assert.Equal(CouponProblem.Expired, CouponRules.Problem(Coupon(ends: Now), Now, 100m, 0, 0));
        Assert.Equal(CouponProblem.UsedUp, CouponRules.Problem(Coupon(maxUses: 3), Now, 100m, 3, 0));
        Assert.Null(CouponRules.Problem(Coupon(maxUses: 3), Now, 100m, 2, 0));
        Assert.Equal(CouponProblem.AlreadyUsed, CouponRules.Problem(Coupon(), Now, 100m, 1, 1));
        Assert.Null(CouponRules.Problem(Coupon(once: false), Now, 100m, 1, 1));
        Assert.Equal(CouponProblem.BelowMinimum, CouponRules.Problem(Coupon(min: 500m), Now, 499.99m, 0, 0));
        Assert.Null(CouponRules.Problem(Coupon(min: 500m), Now, 500m, 0, 0));
    }

    [Theory]
    [InlineData(CouponDiscountKind.Percent, 10, null, 1234.5, 123.45)]
    [InlineData(CouponDiscountKind.Percent, 10, 100.0, 1234.5, 100)]
    [InlineData(CouponDiscountKind.Percent, 15, null, 0.1, 0.02)]
    [InlineData(CouponDiscountKind.Amount, 150, null, 1000, 150)]
    [InlineData(CouponDiscountKind.Amount, 150, null, 100, 100)]
    [InlineData(CouponDiscountKind.Amount, 150, null, 0, 0)]
    public void Discount_is_the_percent_capped_or_the_amount_never_above_the_order(string kind, double value, double? cap, double total,
        double expected) =>
        Assert.Equal((decimal)expected, CouponRules.Discount(Coupon(kind, (decimal)value, (decimal?)cap), (decimal)total));

    [Fact]
    public void Running_means_on_and_inside_the_dates()
    {
        Assert.True(CouponRules.IsRunning(Coupon(starts: Now.AddDays(-1), ends: Now.AddDays(1)), Now));
        Assert.False(CouponRules.IsRunning(Coupon(active: false), Now));
        Assert.False(CouponRules.IsRunning(Coupon(ends: Now.AddSeconds(-1)), Now));
    }
}
