namespace HardwareStore.Modules.Retail;

/// <summary>
/// A discount code given to one influencer or ad (<see cref="SourceName"/>), so the owner can see which source brings sales
/// (roadmap Phase 5, slice K). A used coupon is never deleted, only turned off.
/// </summary>
public sealed class Coupon
{
    public Guid Id { get; set; }
    /// <summary>Upper case letters, digits and dashes (<see cref="CouponRules.Code"/>).</summary>
    public required string Code { get; set; }
    /// <summary>Who the code was given to: an influencer, an ad, a page.</summary>
    public required string SourceName { get; set; }
    public required string DiscountKind { get; set; } = CouponDiscountKind.Percent;
    /// <summary>Percent (1–90) or an amount in EGP.</summary>
    public decimal DiscountValue { get; set; }
    /// <summary>Cap on a percent discount, in EGP; null = no cap.</summary>
    public decimal? MaxDiscount { get; set; }
    /// <summary>Smallest order (items, before shipping) the code works on; null = any.</summary>
    public decimal? MinOrderTotal { get; set; }
    /// <summary>Total uses over all customers; null = no limit. Cancelled orders give their use back.</summary>
    public int? MaxUses { get; set; }
    /// <summary>One use per phone number.</summary>
    public bool OncePerCustomer { get; set; } = true;
    public DateTimeOffset? StartsAtUtc { get; set; }
    public DateTimeOffset? EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public static class CouponDiscountKind
{
    public const string Percent = "Percent";
    public const string Amount = "Amount";
}

/// <summary>Why a code does not work; the web shows each one as its own sentence.</summary>
public static class CouponProblem
{
    public const string NotFound = "NotFound";
    public const string Inactive = "Inactive";
    public const string NotStarted = "NotStarted";
    public const string Expired = "Expired";
    public const string BelowMinimum = "BelowMinimum";
    public const string UsedUp = "UsedUp";
    public const string AlreadyUsed = "AlreadyUsed";
}

/// <summary>The edited values of a coupon after <see cref="CouponRules.Normalize"/>.</summary>
public sealed record CouponValues(string Code, string SourceName, string DiscountKind, decimal DiscountValue, decimal? MaxDiscount,
    decimal? MinOrderTotal, int? MaxUses, bool OncePerCustomer, DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc, bool IsActive);

public static class CouponRules
{
    public const int MinCodeLength = 3;
    public const int MaxCodeLength = 30;
    public const decimal MaxPercent = 90m;

    /// <summary>Codes are case-insensitive: stored upper case, letters A–Z, digits and dashes, 3–30 long.</summary>
    public static string? TryCode(string? value)
    {
        var code = value?.Trim().ToUpperInvariant();
        if (code is null || code.Length is < MinCodeLength or > MaxCodeLength) return null;
        return code.All(ch => ch is >= 'A' and <= 'Z' or >= '0' and <= '9' or '-') ? code : null;
    }

    public static CouponValues Normalize(CouponInput input)
    {
        var code = TryCode(input.Code) ?? throw Field("code",
            $"The code is {MinCodeLength}–{MaxCodeLength} letters (A–Z), digits or dashes.");
        var source = OrderRules.Optional(input.SourceName, 100, "Source") ?? throw Field("sourceName", "Write who the code is for.");
        var kind = input.DiscountKind?.Trim() switch
        {
            CouponDiscountKind.Percent => CouponDiscountKind.Percent,
            CouponDiscountKind.Amount => CouponDiscountKind.Amount,
            _ => throw Field("discountKind", "The discount is a percent or an amount.")
        };
        var value = input.DiscountValue;
        if (decimal.Round(value, 2) != value || value <= 0 || (kind == CouponDiscountKind.Percent && value > MaxPercent))
            throw Field("discountValue", kind == CouponDiscountKind.Percent
                ? $"The percent is more than 0 and at most {MaxPercent:0}."
                : "The amount is more than 0 with at most 2 decimals.");
        var maxDiscount = kind == CouponDiscountKind.Percent ? Positive(input.MaxDiscount, "maxDiscount") : null;
        var minOrder = Positive(input.MinOrderTotal, "minOrderTotal");
        if (input.MaxUses is < 1) throw Field("maxUses", "The number of uses is 1 or more, or empty for no limit.");
        if (input.StartsAtUtc is { } start && input.EndsAtUtc is { } end && end <= start)
            throw Field("endsAtUtc", "The end comes after the start.");
        return new CouponValues(code, source, kind, value, maxDiscount, minOrder, input.MaxUses, input.OncePerCustomer,
            input.StartsAtUtc?.ToUniversalTime(), input.EndsAtUtc?.ToUniversalTime(), input.IsActive);
    }

    /// <summary>
    /// Why the coupon cannot be used now on an order of <paramref name="orderTotal"/> (items, before shipping), or null when it can.
    /// <paramref name="uses"/> counts its orders that are not cancelled; <paramref name="customerUses"/> those with the same phone.
    /// </summary>
    public static string? Problem(Coupon coupon, DateTimeOffset now, decimal orderTotal, int uses, int customerUses)
    {
        if (!coupon.IsActive) return CouponProblem.Inactive;
        if (coupon.StartsAtUtc is { } start && now < start) return CouponProblem.NotStarted;
        if (coupon.EndsAtUtc is { } end && now >= end) return CouponProblem.Expired;
        if (coupon.MaxUses is { } max && uses >= max) return CouponProblem.UsedUp;
        if (coupon.OncePerCustomer && customerUses > 0) return CouponProblem.AlreadyUsed;
        if (coupon.MinOrderTotal is { } min && orderTotal < min) return CouponProblem.BelowMinimum;
        return null;
    }

    /// <summary>The discount on <paramref name="orderTotal"/>: the percent (capped) or the amount, never more than the order.</summary>
    public static decimal Discount(Coupon coupon, decimal orderTotal)
    {
        if (orderTotal <= 0) return 0m;
        var discount = coupon.DiscountKind == CouponDiscountKind.Percent
            ? decimal.Round(orderTotal * coupon.DiscountValue / 100m, 2, MidpointRounding.AwayFromZero)
            : coupon.DiscountValue;
        if (coupon.MaxDiscount is { } cap) discount = Math.Min(discount, cap);
        return Math.Min(discount, orderTotal);
    }

    /// <summary>A coupon is "running" while it is on and inside its dates (the admin list badge).</summary>
    public static bool IsRunning(Coupon coupon, DateTimeOffset now) =>
        coupon.IsActive && (coupon.StartsAtUtc is null || now >= coupon.StartsAtUtc) && (coupon.EndsAtUtc is null || now < coupon.EndsAtUtc);

    private static decimal? Positive(decimal? value, string field)
    {
        if (value is null) return null;
        if (value <= 0 || decimal.Round(value.Value, 2) != value) throw Field(field, "Enter an amount more than 0, or leave it empty.");
        return value;
    }

    /// <summary>A 400 with one field error; the message is for staff (the admin form shows it).</summary>
    public static RetailException Field(string field, string message) =>
        new(400, message, new Dictionary<string, string[]> { [field] = [message] });

    /// <summary>A 400 on the order's <c>couponCode</c> field whose message is the <see cref="CouponProblem"/> code, for the web to word.</summary>
    public static RetailException Rejected(string problem) =>
        new(400, "The coupon code cannot be used.", new Dictionary<string, string[]> { ["couponCode"] = [problem] });
}
