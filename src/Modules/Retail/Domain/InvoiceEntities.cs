namespace HardwareStore.Modules.Retail;

/// <summary>
/// The invoice of a delivered order (one per order). Issued in the same transaction as the warranties, so the invoice date
/// is the warranty start. The public link <c>/i/{token}</c> opens it with <see cref="AccessToken"/>.
/// </summary>
public sealed class Invoice
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    /// <summary>Prefix-year-sequence, for example INV-2026-0001.</summary>
    public required string Number { get; set; }
    public int Year { get; set; }
    public int Sequence { get; set; }
    /// <summary>Unguessable token of the public invoice link.</summary>
    public required string AccessToken { get; set; }
    /// <summary>Name of the staff member who delivered the order, copied when the invoice was issued.</summary>
    public string? SellerName { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Last invoice sequence used in a year; bumped under the row lock so numbers have no gaps.</summary>
public sealed class InvoiceCounter
{
    public int Year { get; set; }
    public int LastNumber { get; set; }
}
