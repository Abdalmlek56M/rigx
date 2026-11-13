namespace HardwareStore.Modules.Retail;

/// <summary>Maps an order to its API view; contact details are masked unless the caller may see them.</summary>
internal static class OrderMapping
{
    public static OrderView ToView(StoreOrder order, bool includeContact = true) => new(order.Id, order.Reference, order.UserAccountId,
        order.CustomerName, includeContact ? order.PhoneNumber : Mask(order.PhoneNumber), includeContact ? order.AddressText : null,
        order.FulfillmentMethod, order.LeadSource, order.Status, order.PaymentStatus, order.PaymentMethod, order.PaymentRequired,
        order.PaymentWaivedAtUtc, order.PaymentWaivedByUserAccountId, order.PaymentWaiverReason, order.Subtotal, order.DeliveryFee,
        order.Total, order.RefundedAmount, order.Notes, order.CreatedAtUtc, order.UpdatedAtUtc, order.RowVersion,
        order.Items.Select(item => new OrderItemView(item.Id, item.ProductId, item.VariantId, item.VariantName,
            item.AssemblyId, item.StockItemId, item.Name, item.Sku, item.ThumbnailStorageKeySnapshot,
            item.Quantity, item.UnitPrice, item.LineTotal, item.DiscountAmount, item.DirectExpenseAmount,
            item.DirectExpenseNote, item.NetProfit,
            item.Components.Select(component => new OrderComponentView(component.Id, component.UnitId,
                component.AllocationPercent, component.RevenueAmount, component.DiscountAmount,
                component.DirectExpenseAmount, component.CostAmount, component.NetProfit)).ToList())).ToList(),
        order.Events.OrderBy(entry => entry.CreatedAtUtc).Select(entry => new OrderEventView(entry.EventType, entry.Notes, entry.ActorUserAccountId, entry.CreatedAtUtc)).ToList());

    private static string Mask(string value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : new string('•', Math.Min(12, Math.Max(4, value.Length)));
}
