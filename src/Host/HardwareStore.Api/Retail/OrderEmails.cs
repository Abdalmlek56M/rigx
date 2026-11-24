using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;

/// <summary>
/// Public page base URLs: tracking (<c>/t/{token}</c>, <c>Sales:TrackingBaseUrl</c>), invoice (<c>/i/{token}</c>,
/// <c>Sales:InvoiceBaseUrl</c>), the public warranty search (<c>Sales:WarrantyLookupUrl</c>) and an item page (<c>/products/{code}</c>, <c>Sales:ItemBaseUrl</c>).
/// </summary>
public sealed record OrderLinks(Uri TrackingBase, Uri InvoiceBase, Uri WarrantyLookupBase, Uri ItemBase)
{
    public static OrderLinks FromConfiguration(IConfiguration configuration) => new(
        Read(configuration, "Sales:TrackingBaseUrl", "http://localhost:3000/t/", slash: true),
        Read(configuration, "Sales:InvoiceBaseUrl", "http://localhost:3000/i/", slash: true),
        Read(configuration, "Sales:WarrantyLookupUrl", "http://localhost:3000/warranty", slash: false),
        Read(configuration, "Sales:ItemBaseUrl", "http://localhost:3000/products/", slash: true));

    private static Uri Read(IConfiguration configuration, string key, string fallback, bool slash)
    {
        var value = configuration[key] ?? fallback;
        if (slash && !value.EndsWith('/')) value += "/";
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException($"{key} must be an absolute http(s) URL.");
        return uri;
    }

    public string TrackingUrl(string token) => new Uri(TrackingBase, Uri.EscapeDataString(token)).ToString();

    public string ItemUrl(string code) => new Uri(ItemBase, Uri.EscapeDataString(code)).ToString();

    public string InvoiceUrl(string token) => new Uri(InvoiceBase, Uri.EscapeDataString(token)).ToString();

    /// <summary>Warranty search page opened with the invoice number (the warranty QR on the invoice).</summary>
    public string WarrantyLookupUrl(string invoiceNumber) => $"{WarrantyLookupBase.ToString().TrimEnd('/')}?q={Uri.EscapeDataString(invoiceNumber)}";
}

/// <summary>Queues the customer's order emails (received, confirmed, shipped, invoiced, unreachable) on the email outbox when an email is known.</summary>
public static class OrderEmails
{
    public static Task QueueAsync(IdentityEmailOutboxWriter outbox, OrderLinks links, OrderView order, string stage,
        CancellationToken cancellationToken) => EnqueueAsync(outbox, links, order, stage, null, null, cancellationToken);

    /// <summary>
    /// "Received": the order summary, plus how to pay when the order is paid by transfer, wallet or InstaPay and the store wrote instructions
    /// (<see cref="PaymentSettings.TransferInstructions"/>).
    /// </summary>
    public static Task QueueReceivedAsync(IdentityEmailOutboxWriter outbox, OrderLinks links, OrderView order, PaymentSettings payment,
        CancellationToken cancellationToken) =>
        EnqueueAsync(outbox, links, order, OrderEmailMessage.Received, null, null, cancellationToken,
            PaymentInstructionsFor(order.PaymentMethod, payment));

    /// <summary>The store's transfer instructions when <paramref name="method"/> is paid by transfer; otherwise nothing.</summary>
    public static string? PaymentInstructionsFor(string method, PaymentSettings payment) =>
        method is OrderPaymentMethod.InstaPay or OrderPaymentMethod.Wallet ? payment.TransferInstructions : null;

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
        string? invoiceNumber, string? invoiceUrl, CancellationToken cancellationToken, string? paymentInstructions = null)
    {
        if (order.CustomerEmail is null) return;
        await outbox.EnqueueAsync(IdentityEmailOutboxWriter.OrderUpdateKind, order.UserAccountId,
            new OrderEmailMessage(order.CustomerEmail, order.CustomerName, order.Reference, stage, links.TrackingUrl(order.TrackingToken),
                order.CourierName, order.WaybillNumber, invoiceNumber, invoiceUrl, SummaryOf(order), paymentInstructions), cancellationToken);
    }

    /// <summary>The ordered lines and the totals the customer sees on the order (the same numbers as the tracking and invoice pages).</summary>
    public static OrderEmailSummary SummaryOf(OrderView order) => new(
        order.Items.Select(item => new OrderEmailLine(item.Name, item.Quantity, item.LineTotal)).ToList(),
        order.Subtotal, order.CouponDiscount, order.DeliveryFee, order.Total, order.Remaining);
}
