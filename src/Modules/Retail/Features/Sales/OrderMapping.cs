namespace HardwareStore.Modules.Retail;

/// <summary>Maps an order to its API view; contact details are masked unless the caller may see them.</summary>
internal static class OrderMapping
{
    public static OrderView ToView(StoreOrder order, bool includeContact = true) => new(order.Id, order.Reference, order.TrackingToken,
        order.UserAccountId, order.Channel, order.CustomerName, includeContact ? order.PhoneNumber : Mask(order.PhoneNumber),
        order.PhoneHasWhatsApp, includeContact ? order.CustomerEmail : null, includeContact ? AddressOf(order) : null,
        order.FulfillmentMethod, order.LeadSource, order.Usage, order.UsageNote, order.Status, order.PaymentStatus, order.PaymentMethod,
        order.PaymentRequired, order.PaymentWaivedAtUtc, order.PaymentWaivedByUserAccountId, order.PaymentWaiverReason, order.Subtotal,
        order.DeliveryFee, order.Total, order.UpfrontAmount, order.PaidAmount, OrderRules.Remaining(order), order.RefundedAmount,
        order.IsBlacklisted, order.ReservedUntilUtc, order.ReservationAutoRelease, order.ReservationExpiredAtUtc, order.CourierName,
        order.WaybillNumber, order.SatisfactionScore, order.SatisfactionComment, order.Notes, order.CreatedAtUtc, order.UpdatedAtUtc,
        order.RowVersion,
        order.Items.Select(item => new OrderItemView(item.Id, item.AssemblyId, item.StockItemId, item.UnitId, item.Name, item.Code,
            item.ThumbnailStorageKeySnapshot, item.Quantity, item.UnitPrice, item.LineTotal, item.DiscountAmount,
            item.DirectExpenseAmount, item.DirectExpenseNote, item.NetProfit,
            item.Components.Select(component => new OrderComponentView(component.Id, component.UnitId,
                component.AllocationPercent, component.RevenueAmount, component.DiscountAmount,
                component.DirectExpenseAmount, component.CostAmount, component.NetProfit)).ToList())).ToList(),
        order.Payments.OrderBy(payment => payment.CreatedAtUtc)
            .Select(payment => new OrderPaymentView(payment.Method, payment.Amount, payment.Note, payment.CreatedAtUtc)).ToList(),
        order.Events.OrderBy(entry => entry.CreatedAtUtc).Select(entry => new OrderEventView(entry.EventType, entry.Notes, entry.ActorUserAccountId, entry.CreatedAtUtc)).ToList());

    public static OrderAddress? AddressOf(StoreOrder order) => order.Governorate is null
        ? null
        : new OrderAddress(order.Governorate, order.Area, order.Street, order.Building, order.Landmark);

    public static OrderEvent Event(string type, string? notes, Guid? actorId, DateTimeOffset at) =>
        new() { Id = Guid.CreateVersion7(), EventType = type, Notes = notes, ActorUserAccountId = actorId, CreatedAtUtc = at };

    private static string Mask(string value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : new string('•', Math.Min(12, Math.Max(4, value.Length)));
}
