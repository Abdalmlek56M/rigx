namespace HardwareStore.Modules.Retail;

public sealed class InventoryUnit
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string? Sku { get; set; }
    public required string Name { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Category { get; set; }
    public required string Condition { get; set; }
    public string? InspectionCondition { get; set; }
    public string? SerialNumber { get; set; }
    public string? WarrantyNote { get; set; }
    public string? SourceName { get; set; }
    public string? SourceType { get; set; }
    public string? PurchaseKind { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string? Description { get; set; }
    public string? ExternalCode { get; set; }
    public string? SpecificationsJson { get; set; }
    public string? ImageStorageKeysJson { get; set; }
    public decimal AcquisitionCost { get; set; }
    public decimal? StandalonePrice { get; set; }
    public decimal? PartnerPercentOverride { get; set; }
    public required string Status { get; set; } = UnitStatus.Available;
    public Guid? AssemblyId { get; set; }
    public Guid? ReceiptId { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class PurchaseReceipt
{
    public Guid Id { get; set; }
    public Guid PurchaseDraftId { get; set; }
    public Guid ReceivedByUserAccountId { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public string? Notes { get; set; }
}
