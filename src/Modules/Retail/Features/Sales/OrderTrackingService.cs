using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Public tracking link (/t/{token}, no login) and the customer's satisfaction score after delivery.</summary>
public sealed class OrderTrackingService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    private const int MaxTokenLength = 32;

    public async Task<TrackingView> GetAsync(string token, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return ToView(await FindAsync(db, token, tracked: false, cancellationToken));
    }

    /// <summary>One rating (1–5) per order, once it is delivered.</summary>
    public async Task<TrackingView> RateAsync(string token, OrderRatingRequest request, CancellationToken cancellationToken = default)
    {
        OrderRules.ValidateRating(request.Score);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await FindAsync(db, token, tracked: true, cancellationToken);
        if (!CanRate(order)) throw new RetailException(409, "The order can be rated once, after it is delivered.");
        order.SatisfactionScore = request.Score;
        order.SatisfactionComment = OrderRules.Optional(request.Comment, 500, "Comment");
        order.RatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        await db.SaveChangesAsync(cancellationToken);
        return ToView(order);
    }

    private static async Task<StoreOrder> FindAsync(RetailDbContext db, string token, bool tracked, CancellationToken cancellationToken)
    {
        var value = token?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > MaxTokenLength) throw NotFound();
        var query = tracked ? db.Orders : db.Orders.AsNoTracking();
        return await query.Include(order => order.Items).Include(order => order.Events).AsSplitQuery()
            .SingleOrDefaultAsync(order => order.TrackingToken == value, cancellationToken) ?? throw NotFound();
    }

    private static bool CanRate(StoreOrder order) =>
        order.SatisfactionScore is null && order.Status is OrderStatus.Delivered or OrderStatus.Completed;

    private static TrackingView ToView(StoreOrder order) =>
        new(order.Reference, OrderMapping.CustomerStatus(order), order.FulfillmentMethod, OrderMapping.CustomerTimeline(order),
            order.CourierName, order.WaybillNumber, order.Area, order.Street,
            order.Items.Select(item => new TrackingItem(item.Name, item.Code, item.Quantity)).ToList(), order.Total,
            OrderRules.Remaining(order), OrderRules.UpfrontDue(order), order.DeliveryTime, CanRate(order), order.SatisfactionScore);

    private static RetailException NotFound() => new(404, "The order was not found.");
}
