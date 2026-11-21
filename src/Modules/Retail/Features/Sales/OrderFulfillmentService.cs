using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Shipping details (courier, waybill), the stock reservation of a confirmed order and what happens when it ends.</summary>
public sealed class OrderFulfillmentService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<OrderView> SetShippingAsync(Guid id, OrderShippingRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await LoadAsync(db, id, request.RowVersion, cancellationToken);
        if (order.FulfillmentMethod != OrderFulfillment.Delivery) throw new RetailException(409, "Only a delivery order has shipping details.");
        order.CourierName = OrderRules.Optional(request.CourierName, 120, "Courier name");
        order.WaybillNumber = OrderRules.Optional(request.WaybillNumber, 80, "Waybill number");
        Touch(order, "Shipping", $"{order.CourierName} {order.WaybillNumber}".Trim(), actorId);
        await db.SaveChangesAsync(cancellationToken);
        return OrderMapping.ToView(order);
    }

    /// <summary>
    /// Records that the store could not reach the customer (timeline event <c>Contact:Unreachable</c>); the caller then emails the
    /// customer and the admin page opens WhatsApp. Only an order still open can be marked.
    /// </summary>
    public async Task<OrderView> MarkCustomerUnreachableAsync(Guid id, OrderContactRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await LoadAsync(db, id, request.RowVersion, cancellationToken);
        Touch(order, "Contact:Unreachable", null, actorId);
        await db.SaveChangesAsync(cancellationToken);
        return OrderMapping.ToView(order);
    }

    /// <summary>Restarts the hold for <c>Hours</c> from now (when given) and sets what happens when it ends.</summary>
    public async Task<OrderView> SetReservationAsync(Guid id, OrderReservationRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await LoadAsync(db, id, request.RowVersion, cancellationToken);
        if (!OrderRules.HoldsReservation(order.Status)) throw new RetailException(409, "Only a confirmed order that has not shipped holds stock.");
        if (request.Hours is int hours)
        {
            OrderRules.ValidateReservationHours(hours);
            order.ReservedUntilUtc = timeProvider.GetUtcNow().ToUniversalTime().AddHours(hours);
            order.ReservationExpiredAtUtc = null;
        }
        order.ReservationAutoRelease = request.AutoRelease;
        Touch(order, "Reservation", $"{order.ReservedUntilUtc:u} · {(order.ReservationAutoRelease ? "release" : "notify")}", actorId);
        await db.SaveChangesAsync(cancellationToken);
        return OrderMapping.ToView(order);
    }

    /// <summary>
    /// Ends due reservations: an order set to auto-release is cancelled and its stock returns; any other order is only
    /// flagged (<c>ReservationExpiredAtUtc</c>) so the admin sees it. Returns how many orders were handled.
    /// </summary>
    public async Task<int> ProcessExpiredReservationsAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var dueIds = await db.Orders.AsNoTracking()
            .Where(order => order.ReservedUntilUtc != null && order.ReservedUntilUtc <= now && order.ReservationExpiredAtUtc == null &&
                (order.Status == OrderStatus.Confirmed || order.Status == OrderStatus.Preparing))
            .OrderBy(order => order.ReservedUntilUtc).Select(order => order.Id).Take(50).ToListAsync(cancellationToken);
        foreach (var id in dueIds) await ExpireAsync(id, now, cancellationToken);
        return dueIds.Count;
    }

    private async Task ExpireAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.LockOrderAsync(id, cancellationToken);
            var order = await db.Orders.WithDetails().SingleAsync(item => item.Id == id, cancellationToken);
            if (!OrderRules.HoldsReservation(order.Status) || order.ReservedUntilUtc is null || order.ReservedUntilUtc > now) return;
            if (order.ReservationAutoRelease)
            {
                await OrderStockAllocator.ReleaseAsync(db, order, cancellationToken);
                order.Status = OrderStatus.Cancelled;
                order.ReservedUntilUtc = null;
                order.Events.Add(OrderMapping.Event("ReservationExpired:Released", null, null, now));
            }
            else
            {
                order.ReservationExpiredAtUtc = now;
                order.Events.Add(OrderMapping.Event("ReservationExpired", null, null, now));
            }
            order.UpdatedAtUtc = now; order.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    private static async Task<StoreOrder> LoadAsync(RetailDbContext db, Guid id, long rowVersion, CancellationToken cancellationToken)
    {
        var order = await db.Orders.WithDetails().SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RetailException(404, "Order was not found.");
        if (order.RowVersion != rowVersion) throw new RetailException(409, "The order was changed by another user.");
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed) throw new RetailException(409, "The order is closed.");
        return order;
    }

    private void Touch(StoreOrder order, string eventType, string? notes, Guid actorId)
    {
        order.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        order.RowVersion++;
        order.Events.Add(OrderMapping.Event(eventType, notes, actorId, order.UpdatedAtUtc));
    }
}
