using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Retail;

/// <summary>
/// Public page base URLs: tracking (<c>/t/{token}</c>, <c>Sales:TrackingBaseUrl</c>), invoice (<c>/i/{token}</c>,
/// <c>Sales:InvoiceBaseUrl</c>) and the public warranty search (<c>Sales:WarrantyLookupUrl</c>).
/// </summary>
public sealed record OrderLinks(Uri TrackingBase, Uri InvoiceBase, Uri WarrantyLookupBase)
{
    public static OrderLinks FromConfiguration(IConfiguration configuration) => new(
        Read(configuration, "Sales:TrackingBaseUrl", "http://localhost:3000/t/", slash: true),
        Read(configuration, "Sales:InvoiceBaseUrl", "http://localhost:3000/i/", slash: true),
        Read(configuration, "Sales:WarrantyLookupUrl", "http://localhost:3000/warranty", slash: false));

    private static Uri Read(IConfiguration configuration, string key, string fallback, bool slash)
    {
        var value = configuration[key] ?? fallback;
        if (slash && !value.EndsWith('/')) value += "/";
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException($"{key} must be an absolute http(s) URL.");
        return uri;
    }

    public string TrackingUrl(string token) => new Uri(TrackingBase, Uri.EscapeDataString(token)).ToString();

    public string InvoiceUrl(string token) => new Uri(InvoiceBase, Uri.EscapeDataString(token)).ToString();

    /// <summary>Warranty search page opened with the invoice number (the warranty QR on the invoice).</summary>
    public string WarrantyLookupUrl(string invoiceNumber) => $"{WarrantyLookupBase.ToString().TrimEnd('/')}?q={Uri.EscapeDataString(invoiceNumber)}";
}

/// <summary>Queues the customer's order emails (received, confirmed, shipped, invoiced) on the email outbox when an email is known.</summary>
public static class OrderEmails
{
    public static Task QueueAsync(IdentityEmailOutboxWriter outbox, OrderLinks links, OrderView order, string stage,
        CancellationToken cancellationToken) => EnqueueAsync(outbox, links, order, stage, null, null, cancellationToken);

    /// <summary>Delivered: sends the invoice link (number and public URL of the invoice issued with the delivery).</summary>
    public static async Task QueueInvoiceAsync(IdentityEmailOutboxWriter outbox, OrderLinks links, OrderView order, InvoiceService invoices,
        CancellationToken cancellationToken)
    {
        if (order.CustomerEmail is null) return;
        var invoice = await invoices.FindForOrderAsync(order.Id, cancellationToken);
        if (invoice is null) return;
        await EnqueueAsync(outbox, links, order, OrderEmailMessage.Invoiced, invoice.Value.Number, links.InvoiceUrl(invoice.Value.AccessToken), cancellationToken);
    }

    private static async Task EnqueueAsync(IdentityEmailOutboxWriter outbox, OrderLinks links, OrderView order, string stage,
        string? invoiceNumber, string? invoiceUrl, CancellationToken cancellationToken)
    {
        if (order.CustomerEmail is null) return;
        await outbox.EnqueueAsync(IdentityEmailOutboxWriter.OrderUpdateKind, order.UserAccountId,
            new OrderEmailMessage(order.CustomerEmail, order.CustomerName, order.Reference, stage, links.TrackingUrl(order.TrackingToken),
                order.CourierName, order.WaybillNumber, invoiceNumber, invoiceUrl), cancellationToken);
    }
}
