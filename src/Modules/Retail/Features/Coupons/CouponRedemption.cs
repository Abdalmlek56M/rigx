using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Checks a coupon code against an order and puts its discount on the lines; shared by checkout, POS and the "try code" endpoint.</summary>
internal static class CouponRedemption
{
    /// <summary>
    /// The coupon named by <paramref name="code"/> and what it gives on <paramref name="orderTotal"/> (items, before shipping).
    /// Uses count the coupon's orders that are not cancelled; "once per customer" matches the phone number.
    /// </summary>
    public static async Task<(Coupon? Coupon, CouponCheckView View)> CheckAsync(RetailDbContext db, string? code, string? phone,
        decimal orderTotal, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var normalized = CouponRules.TryCode(code);
        var coupon = normalized is null ? null : await db.Coupons.SingleOrDefaultAsync(entry => entry.Code == normalized, cancellationToken);
        if (coupon is null) return (null, new CouponCheckView(normalized, 0m, CouponProblem.NotFound));
        var orders = db.Orders.Where(order => order.CouponId == coupon.Id && order.Status != OrderStatus.Cancelled);
        var uses = await orders.CountAsync(cancellationToken);
        var digits = OrderRules.TryPhone(phone);
        var customerUses = coupon.OncePerCustomer && digits is not null
            ? await orders.CountAsync(order => order.PhoneNumber == digits, cancellationToken)
            : 0;
        var problem = CouponRules.Problem(coupon, now, orderTotal, uses, customerUses);
        return (coupon, new CouponCheckView(coupon.Code, problem is null ? CouponRules.Discount(coupon, orderTotal) : 0m, problem));
    }

    /// <summary>
    /// Applies <paramref name="code"/> to a new order inside the caller's transaction: the coupon row is locked so two orders cannot
    /// take its last use, a code that does not work is a 400 on <c>couponCode</c>, and the discount is spread over the lines.
    /// Call before <see cref="OrderDrafting.NewOrder"/>; then <see cref="Stamp"/> the order.
    /// </summary>
    public static async Task<(Coupon Coupon, decimal Discount)?> RedeemAsync(RetailDbContext db, string? code, string? phone,
        List<OrderItem> items, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var normalized = CouponRules.TryCode(code) ?? throw CouponRules.Rejected(CouponProblem.NotFound);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM coupons WHERE code = {normalized} FOR UPDATE", cancellationToken);
        var (coupon, view) = await CheckAsync(db, normalized, phone, items.Sum(item => item.LineTotal - item.DiscountAmount), now,
            cancellationToken);
        if (view.Problem is not null) throw CouponRules.Rejected(view.Problem);
        OrderDrafting.ApplyOrderDiscount(items, view.Discount);
        return (coupon!, view.Discount);
    }

    public static void Stamp(StoreOrder order, (Coupon Coupon, decimal Discount)? redeemed)
    {
        if (redeemed is not { } used) return;
        order.CouponId = used.Coupon.Id;
        order.CouponCode = used.Coupon.Code;
        order.CouponDiscount = used.Discount;
    }
}
