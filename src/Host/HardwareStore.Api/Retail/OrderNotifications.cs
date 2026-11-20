using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;

/// <summary>Puts an order's new status in the customer's notifications bell when the order belongs to an account.</summary>
public static class OrderNotifications
{
    public static Task AddAsync(CustomerNotificationService notifications, OrderView order, CancellationToken cancellationToken) =>
        // "Completed" is internal: the customer already saw "Delivered" for it.
        order.UserAccountId is { } accountId && order.Status != OrderStatus.Completed
            ? notifications.AddOrderStatusAsync(accountId, order.Id, order.Reference, order.Status, cancellationToken)
            : Task.CompletedTask;
}
