using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static partial class RetailEndpoints
{
    /// <summary>Revenue and discount per code are money figures: only with the reports permission.</summary>
    private static async Task<Ok<IReadOnlyList<CouponView>>> ListCouponsAsync(HttpContext context, CouponService coupons, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await coupons.ListAsync(context.HasPermission("Finance.ViewReports"), ct));
    }

    private static async Task<Ok<CouponView>> CreateCouponAsync(CouponInput request, CouponService coupons, CancellationToken ct) =>
        TypedResults.Ok(await coupons.CreateAsync(request, ct));

    private static async Task<Ok<CouponView>> UpdateCouponAsync(Guid id, CouponInput request, CouponService coupons, CancellationToken ct) =>
        TypedResults.Ok(await coupons.UpdateAsync(id, request, ct));

    private static async Task<NoContent> DeleteCouponAsync(Guid id, CouponService coupons, CancellationToken ct)
    {
        await coupons.DeleteAsync(id, ct);
        return TypedResults.NoContent();
    }

    /// <summary>Checkout and POS "apply code" (rate limited, so codes cannot be guessed in bulk).</summary>
    private static async Task<Ok<CouponCheckView>> CheckCouponAsync(CouponCheckRequest request, HttpContext context, CouponService coupons, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await coupons.CheckAsync(request, ct));
    }
}
