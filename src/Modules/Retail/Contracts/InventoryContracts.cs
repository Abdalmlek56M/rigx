namespace HardwareStore.Modules.Retail;

public sealed record ReceiptLine(Guid? ProductId, Guid? VariantId, string Name, string? Sku, string Condition, string? SerialNumber, decimal AcquisitionCost, int Quantity, decimal? PartnerPercentOverride, string? WarrantyNote, decimal? StandalonePrice = null, string? SourceName = null, string? Brand = null, string? Model = null, string? Category = null, string? Description = null, string? ExternalCode = null, string? SpecificationsJson = null, string? PurchaseKind = null, string? SourceType = null, DateOnly? PurchaseDate = null, string? ImageStorageKeysJson = null);
public sealed record ReceiveRequest(Guid PurchaseDraftId, string? Notes, IReadOnlyList<ReceiptLine> Lines);
public sealed record UnitView(Guid Id, Guid? ProductId, string Name, string? Sku, string? Brand, string? Model, string? Category,
    string? Description, string? ExternalCode, string? SpecificationsJson, string Condition, string? InspectionCondition,
    string? SerialNumber, string? SourceName, string? SourceType, string? PurchaseKind, DateOnly? PurchaseDate,
    decimal? AcquisitionCost, decimal? StandalonePrice, decimal? PartnerPercentOverride, string Status, Guid? AssemblyId,
    DateTimeOffset ReceivedAtUtc);
