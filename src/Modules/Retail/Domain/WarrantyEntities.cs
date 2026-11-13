namespace HardwareStore.Modules.Retail;

/// <summary>
/// One sold unit's warranty, issued when its order is delivered. The label and months are the snapshot taken when the
/// stock was claimed, so later edits to the item or build never change a warranty that was already sold.
/// </summary>
public sealed class Warranty
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid UnitId { get; set; }
    public Guid StockItemId { get; set; }
    /// <summary>RX code of the stock item (the code on the RIGX sticker).</summary>
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Condition { get; set; }
    public string? SerialNumber { get; set; }
    /// <summary>BLD code when the unit was sold as part of a build line.</summary>
    public string? BuildCode { get; set; }
    public required string Label { get; set; }
    /// <summary>Null = manufacturer warranty (no end date).</summary>
    public int? Months { get; set; }
    public DateTimeOffset StartsAtUtc { get; set; }
    public DateTimeOffset? EndsAtUtc { get; set; }
    /// <summary>Digits only, copied from the order.</summary>
    public required string PhoneNumber { get; set; }
    public Guid? UserAccountId { get; set; }
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public string? VoidReason { get; set; }
    public Guid? VoidedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}
