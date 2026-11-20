using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Coupon admin (list with results, create, edit, delete while unused) and the public "try this code" check.</summary>
public sealed class CouponService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    /// <summary>Every coupon with its uses and, with <paramref name="includeMoney"/>, revenue (items, no shipping) and discount given; cancelled orders do not count.</summary>
    public async Task<IReadOnlyList<CouponView>> ListAsync(bool includeMoney, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var coupons = await db.Coupons.AsNoTracking().OrderByDescending(coupon => coupon.CreatedAtUtc).ToListAsync(cancellationToken);
        var stats = await Stats(db).ToDictionaryAsync(row => row.CouponId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        return coupons.Select(coupon => ToView(coupon, stats.GetValueOrDefault(coupon.Id), now, includeMoney)).ToList();
    }

    public async Task<CouponView> CreateAsync(CouponInput input, CancellationToken cancellationToken = default)
    {
        var values = CouponRules.Normalize(input);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCodeFreeAsync(db, values.Code, null, cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var coupon = new Coupon { Id = Guid.CreateVersion7(), Code = values.Code, SourceName = values.SourceName, DiscountKind = values.DiscountKind, CreatedAtUtc = now };
        Apply(coupon, values, now);
        db.Coupons.Add(coupon);
        await db.SaveChangesAsync(cancellationToken);
        return ToView(coupon, null, now);
    }

    /// <summary>Edits a coupon; the code itself is fixed once an order used it, so the report keeps matching what was shared.</summary>
    public async Task<CouponView> UpdateAsync(Guid id, CouponInput input, CancellationToken cancellationToken = default)
    {
        var values = CouponRules.Normalize(input);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var coupon = await db.Coupons.SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken)
            ?? throw new RetailException(404, "Coupon was not found.");
        if (coupon.RowVersion != input.RowVersion) throw new RetailException(409, "The coupon was changed by someone else. Reload it.");
        if (values.Code != coupon.Code)
        {
            if (await db.Orders.AnyAsync(order => order.CouponId == id, cancellationToken))
                throw CouponRules.Field("code", "This code was already used on orders, so it cannot be renamed. Make a new coupon instead.");
            await EnsureCodeFreeAsync(db, values.Code, id, cancellationToken);
        }
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        Apply(coupon, values, now);
        coupon.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return ToView(coupon, await Stats(db, id).SingleOrDefaultAsync(cancellationToken), now);
    }

    /// <summary>Only a coupon no order used can be deleted; a used one is turned off instead, so its results stay.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var coupon = await db.Coupons.SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken)
            ?? throw new RetailException(404, "Coupon was not found.");
        if (await db.Orders.AnyAsync(order => order.CouponId == id, cancellationToken))
            throw new RetailException(409, "This coupon was used on orders. Turn it off instead of deleting it.");
        db.Coupons.Remove(coupon);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>"Try this code" at checkout or in POS; nothing is saved and the real discount is worked out again with the order.</summary>
    public async Task<CouponCheckView> CheckAsync(CouponCheckRequest request, CancellationToken cancellationToken = default)
    {
        if (request.OrderTotal < 0) throw new RetailException(400, "The order total must be zero or more.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var (_, view) = await CouponRedemption.CheckAsync(db, request.Code, request.Phone, request.OrderTotal,
            timeProvider.GetUtcNow().ToUniversalTime(), cancellationToken);
        return view;
    }

    private sealed record CouponStats(Guid CouponId, int Uses, int CompletedUses, decimal Revenue, decimal DiscountGiven, DateTimeOffset? LastUsedAtUtc);

    /// <summary>Results per coupon (all coupons, or only <paramref name="couponId"/>).</summary>
    private static IQueryable<CouponStats> Stats(RetailDbContext db, Guid? couponId = null) => db.Orders.AsNoTracking()
        .Where(order => order.CouponId != null && order.Status != OrderStatus.Cancelled && (couponId == null || order.CouponId == couponId))
        .GroupBy(order => order.CouponId!.Value)
        .Select(group => new CouponStats(group.Key, group.Count(), group.Count(order => order.Status == OrderStatus.Completed),
            group.Sum(order => order.Subtotal), group.Sum(order => order.CouponDiscount), group.Max(order => (DateTimeOffset?)order.CreatedAtUtc)));

    private static async Task EnsureCodeFreeAsync(RetailDbContext db, string code, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await db.Coupons.AnyAsync(entry => entry.Code == code && entry.Id != exceptId, cancellationToken))
            throw CouponRules.Field("code", "Another coupon already has this code.");
    }

    private static void Apply(Coupon coupon, CouponValues values, DateTimeOffset now)
    {
        coupon.Code = values.Code;
        coupon.SourceName = values.SourceName;
        coupon.DiscountKind = values.DiscountKind;
        coupon.DiscountValue = values.DiscountValue;
        coupon.MaxDiscount = values.MaxDiscount;
        coupon.MinOrderTotal = values.MinOrderTotal;
        coupon.MaxUses = values.MaxUses;
        coupon.OncePerCustomer = values.OncePerCustomer;
        coupon.StartsAtUtc = values.StartsAtUtc;
        coupon.EndsAtUtc = values.EndsAtUtc;
        coupon.IsActive = values.IsActive;
        coupon.UpdatedAtUtc = now;
    }

    private static CouponView ToView(Coupon coupon, CouponStats? stats, DateTimeOffset now, bool includeMoney = false) => new(coupon.Id, coupon.Code, coupon.SourceName,
        coupon.DiscountKind, coupon.DiscountValue, coupon.MaxDiscount, coupon.MinOrderTotal, coupon.MaxUses, coupon.OncePerCustomer,
        coupon.StartsAtUtc, coupon.EndsAtUtc, coupon.IsActive, CouponRules.IsRunning(coupon, now), stats?.Uses ?? 0, stats?.CompletedUses ?? 0,
        includeMoney ? stats?.Revenue ?? 0m : null, includeMoney ? stats?.DiscountGiven ?? 0m : null, stats?.LastUsedAtUtc, coupon.CreatedAtUtc, coupon.UpdatedAtUtc, coupon.RowVersion);
}
