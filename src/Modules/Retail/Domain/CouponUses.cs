namespace HardwareStore.Modules.Retail;

/// <summary>The coupon history of a customer: one entry per order that used a coupon (one coupon per order).</summary>
public static class CouponUses
{
    /// <summary>The use of a coupon on an order, or null when the order has no coupon.</summary>
    public static CouponUseView? Of(Guid orderId, string reference, DateTimeOffset createdAtUtc, string status, string? couponCode, decimal couponDiscount) =>
        string.IsNullOrWhiteSpace(couponCode) ? null : new CouponUseView(couponCode, couponDiscount, orderId, reference, createdAtUtc, status);
}
