using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Order status workflow. Confirm claims stock, cancel releases it, complete posts partner earnings.
/// </summary>
public sealed class OrderStatusService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<OrderView> UpdateStatusAsync(Guid id, OrderStatusRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var next = request.Status?.Trim();
        if (next is not (OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.Ready or OrderStatus.Delivered or OrderStatus.Cancelled))
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
            var allowed = order.Status switch
            {
                OrderStatus.New => next is OrderStatus.Confirmed or OrderStatus.Cancelled,
                OrderStatus.Confirmed => next is OrderStatus.Preparing or OrderStatus.Cancelled,
                OrderStatus.Preparing => next is OrderStatus.Ready or OrderStatus.Cancelled,
                OrderStatus.Ready => next is OrderStatus.Delivered or OrderStatus.Cancelled,
                OrderStatus.Delivered => next is OrderStatus.Cancelled,
                _ => false
            };
            if (!allowed) throw new RetailException(409, $"The order cannot move from {order.Status} to {next}.");

            if (next == OrderStatus.Confirmed)
            {
                await OrderStockAllocator.ClaimAsync(db, order, cancellationToken);
            }
            if (next == OrderStatus.Cancelled && order.Status is OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.Ready or OrderStatus.Delivered)
            {
                await OrderStockAllocator.ReleaseAsync(db, order, cancellationToken);
            }
            order.Status = next;
            order.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); order.RowVersion++;
            order.Events.Add(new OrderEvent { Id = Guid.CreateVersion7(), EventType = $"Status:{next}", Notes = request.Notes?.Trim(), ActorUserAccountId = actorId, CreatedAtUtc = order.UpdatedAtUtc });
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
            order.Events.Add(new OrderEvent { Id = Guid.CreateVersion7(), EventType = "Status:Completed", Notes = request.Notes?.Trim(), ActorUserAccountId = actorId, CreatedAtUtc = order.UpdatedAtUtc });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OrderMapping.ToView(order);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }
}
