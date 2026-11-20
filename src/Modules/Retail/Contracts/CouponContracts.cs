namespace HardwareStore.Modules.Retail;

/// <summary>Create or edit a coupon. Percent coupons may cap the discount; empty limits mean "no limit".</summary>
public sealed record CouponInput(string? Code, string? SourceName, string? DiscountKind, decimal DiscountValue, decimal? MaxDiscount = null,
    decimal? MinOrderTotal = null, int? MaxUses = null, bool OncePerCustomer = true, DateTimeOffset? StartsAtUtc = null,
    DateTimeOffset? EndsAtUtc = null, bool IsActive = true, long RowVersion = 0);

/// <summary>
/// A coupon for the admin, with its results: orders that used it (not cancelled), how many were completed, and their revenue (items after
/// discounts, no shipping) and discount given (null without the reports permission).
/// </summary>
public sealed record CouponView(Guid Id, string Code, string SourceName, string DiscountKind, decimal DiscountValue, decimal? MaxDiscount,
    decimal? MinOrderTotal, int? MaxUses, bool OncePerCustomer, DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc, bool IsActive,
    bool IsRunning, int Uses, int CompletedUses, decimal? Revenue, decimal? DiscountGiven, DateTimeOffset? LastUsedAtUtc,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long RowVersion);

/// <summary>Checkout and POS "try this code": the discount on <paramref name="OrderTotal"/> (items, before shipping).</summary>
public sealed record CouponCheckRequest(string? Code, decimal OrderTotal, string? Phone = null);

/// <summary>Valid = <see cref="Problem"/> is null; otherwise one of <see cref="CouponProblem"/>.</summary>
public sealed record CouponCheckView(string? Code, decimal Discount, string? Problem);
