using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Money received for an order (one or more methods), payment status changes and explicit payment waivers.
/// </summary>
public sealed class OrderPaymentService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider, TimeZoneInfo businessZone)
{
    /// <summary>Records money received; the order turns Paid once the payments cover its total.</summary>
    public async Task<OrderView> AddPaymentAsync(Guid id, OrderPaymentRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.WithDetails()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new RetailException(404, "Order was not found.");
        if (order.RowVersion != request.RowVersion) throw new RetailException(409, "The order was changed by another user.");
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed || order.PaymentStatus is PaymentStatus.Waived or PaymentStatus.Refunded)
            throw new RetailException(409, "This order cannot take payments.");
        await RecordAsync(db, order, OrderRules.PaymentMethod(request.Method, OrderChannel.Store), request.MoneyAccountId, request.Amount,
            OrderRules.Optional(request.Note, 500, "Note"), actorId, timeProvider.GetUtcNow().ToUniversalTime(), businessZone, cancellationToken);
        order.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return OrderMapping.ToView(order);
    }

    /// <summary>Adds the payment to the order and the money to the account (the chosen one, else the first active account of the method kind).</summary>
    internal static async Task RecordAsync(RetailDbContext db, StoreOrder order, string method, Guid? accountId, decimal amount, string? note,
        Guid actorId, DateTimeOffset now, TimeZoneInfo zone, CancellationToken cancellationToken)
    {
        OrderRules.ValidatePayment(amount);
        if (amount > OrderRules.Remaining(order)) throw new RetailException(400, "The payment is more than what is left to pay.");
        var payment = new OrderPayment
        {
            Id = Guid.CreateVersion7(), OrderId = order.Id, Method = method, Amount = amount, Note = note,
            ActorUserAccountId = actorId, CreatedAtUtc = now
        };
        var entry = new MoneyEntry(MoneySource.Sale, payment.Id, amount, order.Reference, note, BusinessDay.Of(now, zone), actorId, now);
        payment.MoneyAccountId = await MoneyLedger.PostInAsync(db, accountId, MoneyRules.KindForMethod(method), entry, cancellationToken);
        order.Payments.Add(payment);
        order.PaidAmount += amount;
        if (order.PaidAmount >= order.Total) order.PaymentStatus = PaymentStatus.Paid;
        order.UpdatedAtUtc = now;
        order.Events.Add(OrderMapping.Event($"Payment:{method}", $"{amount:0.##}", actorId, now));
    }

    public async Task<OrderView> UpdatePaymentAsync(Guid id, PaymentStatusRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var next = request.PaymentStatus?.Trim();
        if (next is not (PaymentStatus.Unpaid or PaymentStatus.PendingVerification or PaymentStatus.Paid))
            throw new RetailException(400, "Unsupported payment status. Refunds and waivers use their dedicated actions.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.WithDetails()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new RetailException(404, "Order was not found.");
        if (order.RowVersion != request.RowVersion) throw new RetailException(409, "The order was changed by another user.");
        if (order.PaymentStatus == PaymentStatus.Waived) throw new RetailException(409, "Payment was waived for this order.");
        if (order.Status is OrderStatus.Cancelled) throw new RetailException(409, "A cancelled order cannot change payment.");
        // Recorded payments are the truth: a fully paid order cannot be marked unpaid by a stale status form.
        if (next != PaymentStatus.Paid && order.PaidAmount >= order.Total)
            throw new RetailException(409, "The recorded payments already cover the order total.");
        if (next == order.PaymentStatus) return OrderMapping.ToView(order);
        order.PaymentStatus = next;
        order.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); order.RowVersion++;
        order.Events.Add(OrderMapping.Event($"Payment:{next}", request.Notes?.Trim(), actorId, order.UpdatedAtUtc));
        await db.SaveChangesAsync(cancellationToken);
        return OrderMapping.ToView(order);
    }

    public async Task<OrderView> WaivePaymentAsync(Guid id, WaivePaymentRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
            throw new RetailException(400, "A waiver reason of at least 5 characters is required.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.WithDetails()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new RetailException(404, "Order was not found.");
        if (order.RowVersion != request.RowVersion) throw new RetailException(409, "The order was changed by another user.");
        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new RetailException(409, "A closed order cannot change payment.");
        if (order.PaymentStatus == PaymentStatus.Paid)
            throw new RetailException(409, "A paid order cannot be waived.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        order.PaymentRequired = false;
        order.PaymentStatus = PaymentStatus.Waived;
        order.PaymentWaivedAtUtc = now;
        order.PaymentWaivedByUserAccountId = actorId;
        order.PaymentWaiverReason = request.Reason.Trim();
        order.UpdatedAtUtc = now; order.RowVersion++;
        order.Events.Add(OrderMapping.Event("Payment:Waived", request.Reason.Trim(), actorId, now));
        await db.SaveChangesAsync(cancellationToken);
        return OrderMapping.ToView(order);
    }
}
