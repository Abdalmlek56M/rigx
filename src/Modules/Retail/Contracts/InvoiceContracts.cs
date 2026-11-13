namespace HardwareStore.Modules.Retail;

/// <summary>What the store settings and the signed-in staff member decide when an invoice is issued (read in Host).</summary>
public sealed record InvoicePolicy(string NumberPrefix, string? SellerName, TimeZoneInfo TimeZone);

/// <summary>One sold unit on the invoice with its warranty (label falls back to the sold label when no warranty record exists).</summary>
public sealed record InvoicePartView(string Code, string Name, string Condition, string? SerialNumber, string? WarrantyLabel,
    DateTimeOffset? WarrantyEndsAtUtc, string? WarrantyStatus, Guid? CategoryId, string? Category);

/// <summary>
/// One order line. Code is the RX or BLD code of the line (a whole build line carries the BLD code, a stock line or a part
/// sold alone carries the RX code); BuildCode is the BLD code whenever the line belongs to a build.
/// </summary>
public sealed record InvoiceLineView(string? Code, string Name, string? BuildCode, int Quantity, decimal UnitPrice, decimal LineTotal,
    decimal Discount, IReadOnlyList<InvoicePartView> Parts);

public sealed record InvoiceView(string Number, string Kind, string Status, DateTimeOffset IssuedAtUtc, string OrderReference,
    string CustomerName, string PhoneNumber, OrderAddress? Address, string FulfillmentMethod, string PaymentMethod, string? SellerName,
    IReadOnlyList<InvoiceLineView> Lines, decimal LinesTotal, decimal Discount, decimal DeliveryFee, decimal Total, decimal Paid,
    decimal Remaining, decimal Refunded, string AccessToken);

public sealed record InvoiceSummaryView(string Number, string Kind, string Status, DateTimeOffset IssuedAtUtc, string OrderReference,
    decimal Total, string AccessToken);
public sealed record InvoiceAdminPage(IReadOnlyList<InvoiceSummaryView> Items, int Page, int PageSize, int Total);

/// <summary>Shareable price quote request: published items and builds only.</summary>
public sealed record QuoteRequest(IReadOnlyList<OrderLineRequest>? Items);

/// <summary>A part of a quoted build at its price inside the build.</summary>
public sealed record QuotePartView(string Code, string Name, string Condition, decimal Price);

/// <summary>Condition is the stock item's stored value; a build has no single condition, so it is null there.</summary>
public sealed record QuoteLineView(string? Code, string Name, string? Condition, int Quantity, decimal UnitPrice, decimal LineTotal,
    IReadOnlyList<QuotePartView> Parts);

/// <summary>Title and Usage are filled when the quote is exactly one build.</summary>
public sealed record QuoteView(string? Title, string? Usage, IReadOnlyList<QuoteLineView> Lines, decimal Total, DateTimeOffset PricedAtUtc);
