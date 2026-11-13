using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Retail;

/// <summary>Public tracking page base URL (frontend <c>/t/{token}</c>), from <c>Sales:TrackingBaseUrl</c>.</summary>
public sealed record OrderLinks(Uri TrackingBase)
{
    public static OrderLinks FromConfiguration(IConfiguration configuration)
    {
        var value = configuration["Sales:TrackingBaseUrl"] ?? "http://localhost:3000/t/";
        if (!Uri.TryCreate(value.EndsWith('/') ? value : value + "/", UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("Sales:TrackingBaseUrl must be an absolute http(s) URL.");
        return new OrderLinks(uri);
    }

    public string TrackingUrl(string token) => new Uri(TrackingBase, Uri.EscapeDataString(token)).ToString();
}

/// <summary>Queues the customer's order emails (received, confirmed, shipped) on the email outbox when an email is known.</summary>
public static class OrderEmails
{
    public static async Task QueueAsync(IdentityEmailOutboxWriter outbox, OrderLinks links, OrderView order, string stage,
        CancellationToken cancellationToken)
    {
        if (order.CustomerEmail is null) return;
        await outbox.EnqueueAsync(IdentityEmailOutboxWriter.OrderUpdateKind, order.UserAccountId,
            new OrderEmailMessage(order.CustomerEmail, order.CustomerName, order.Reference, stage, links.TrackingUrl(order.TrackingToken),
                order.CourierName, order.WaybillNumber), cancellationToken);
    }
}
