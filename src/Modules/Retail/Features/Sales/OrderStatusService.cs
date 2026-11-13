using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Order status workflow. Confirm claims stock and starts the reservation, cancel releases it, complete posts partner earnings.
/// </summary>
public sealed class OrderStatusService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<OrderView> UpdateStatusAsync(Guid id, OrderStatusRequest request, Guid actorId, ReservationPolicy reservation, InvoicePolicy invoice,
        CancellationToken cancellationToken = default)
    {
        var next = request.Status?.Trim();
        if (next is not (OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled))
            throw new RetailException(400, next == OrderStatus.Completed
                ? "Completion requires the dedicated completion action and permission."
                : "Unsupported order status.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.LockOrderAsync(id, cancellationToken);
            var order = await db.Orders.WithDetails()
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new RetailException(404, "Order was not found.");
            if (order.RowVersion != request.RowVersion) throw new RetailException(409, "The order was changed by another user.");
            if (order.Status == next) return OrderMapping.ToView(order);
            if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed) throw new RetailException(409, "A closed order cannot change status.");
            if (!OrderRules.CanMove(order, next)) throw new RetailException(409, $"The order cannot move from {order.Status} to {next}.");
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            if (next == OrderStatus.Confirmed)
            {
                OrderRules.EnsureCanConfirm(order);
                await OrderStockAllocator.ClaimAsync(db, order, cancellationToken);
                OrderRules.ValidateReservationHours(reservation.Hours);
                order.ReservedUntilUtc = now.AddHours(reservation.Hours);
                order.ReservationAutoRelease = reservation.AutoRelease;
            }
            if (next == OrderStatus.Delivered)
            {
                await WarrantyRegistry.IssueForOrderAsync(db, order, now, cancellationToken);
                await InvoiceRegistry.IssueForOrderAsync(db, order, invoice, now, cancellationToken);
            }
            if (next == OrderStatus.Cancelled && order.Status == OrderStatus.Delivered)
                await WarrantyRegistry.VoidForOrderAsync(db, order.Id, "Order cancelled", actorId, now, cancellationToken);
            if (next == OrderStatus.Cancelled && order.Status != OrderStatus.New)
                await OrderStockAllocator.ReleaseAsync(db, order, cancellationToken);
            order.Status = next;
            if (!OrderRules.HoldsReservation(next)) order.ReservedUntilUtc = null;
            order.UpdatedAtUtc = now; order.RowVersion++;
            order.Events.Add(OrderMapping.Event($"Status:{next}", OrderRules.Optional(request.Notes, 2000, "Notes"), actorId, now));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OrderMapping.ToView(order);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    public async Task<OrderView> CompleteOrderAsync(Guid id, CompleteOrderRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.LockOrderAsync(id, cancellationToken);
            var order = await db.Orders.WithDetails()
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new RetailException(404, "Order was not found.");
            if (order.RowVersion != request.RowVersion) throw new RetailException(409, "The order was changed by another user.");
            if (order.Status == OrderStatus.Completed) return OrderMapping.ToView(order);
            if (order.Status != OrderStatus.Delivered)
                throw new RetailException(409, "Only a delivered order can be completed.");
            if (order.PaymentRequired && order.PaymentStatus != PaymentStatus.Paid)
                throw new RetailException(409, "Payment must be confirmed before completion.");
            if (!order.PaymentRequired && order.PaymentStatus != PaymentStatus.Waived)
                throw new RetailException(409, "Record an explicit payment waiver before completion.");

            await PartnerEarningsPosting.PostForOrderAsync(db, order, timeProvider, cancellationToken);
            order.Status = OrderStatus.Completed;
            order.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); order.RowVersion++;
            order.Events.Add(OrderMapping.Event("Status:Completed", request.Notes?.Trim(), actorId, order.UpdatedAtUtc));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OrderMapping.ToView(order);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }
}
