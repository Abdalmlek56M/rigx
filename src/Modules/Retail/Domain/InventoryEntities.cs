namespace HardwareStore.Modules.Retail;

public sealed class InventoryUnit
{
    public Guid Id { get; set; }
    public Guid StockItemId { get; set; }
    /// <summary>Copy of the item's product link; the order flow by product reads it.</summary>
    public Guid? ProductId { get; set; }
    /// <summary>Copy of the item's model name at receipt; the label used by build and order views.</summary>
    public required string Name { get; set; }
    public required string Condition { get; set; }
    public string? InspectionCondition { get; set; }
    public string? SerialNumber { get; set; }
    public string? SourceName { get; set; }
    public string? SourceType { get; set; }
    public string? PurchaseKind { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal AcquisitionCost { get; set; }
    public decimal? PartnerPercentOverride { get; set; }
    /// <summary>The owner left this unit's cost out of the stock value; see <see cref="StockValueRules"/>.</summary>
    public bool ExcludedFromStockValue { get; set; }
    /// <summary>The partner financing this unit (null = the store's own money); its earning is posted when the unit is sold.</summary>
    public Guid? PartnerId { get; set; }
    public required string Status { get; set; } = UnitStatus.Available;
    public Guid? AssemblyId { get; set; }
    public Guid? ReceiptId { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public StockItem StockItem { get; set; } = null!;
}

public sealed class PurchaseReceipt
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public Guid ReceivedByUserAccountId { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public string? Notes { get; set; }
}
