namespace HardwareStore.Modules.Retail;

/// <summary>
/// One received line. A used line of N pieces becomes N items of one unit; a new line becomes one lot of N units
/// (<see cref="StockItemRules.UnitsPerItem"/>). <paramref name="SerialNumber"/> is kept only for a single piece.
/// </summary>
public sealed record ReceiptLine(string ModelName, string Condition, int Quantity, decimal AcquisitionCost, decimal Price,
    Guid? CategoryId = null, Guid? ProductId = null, decimal? WithBoardExtra = null, decimal? AloneExtra = null,
    string? SerialNumber = null, decimal? PartnerPercentOverride = null, string? WarrantyLabel = null, int? WarrantyMonths = null,
    bool HasBox = false, bool IsHidden = false, string? InternalNote = null, IReadOnlyDictionary<string, string>? Specs = null,
    Guid? PartnerId = null);
public sealed record ReceiptSource(string? SourceName, string? SourceType, string? PurchaseKind, DateOnly? PurchaseDate);
public sealed record ReceiveRequest(Guid PurchaseId, string? Notes, ReceiptSource Source, IReadOnlyList<ReceiptLine> Lines);
public sealed record UnitView(Guid Id, Guid StockItemId, string StockItemCode, Guid? ProductId, string Name, string Condition,
    string? InspectionCondition, string? SerialNumber, string? SourceName, string? SourceType, string? PurchaseKind, DateOnly? PurchaseDate,
    decimal? AcquisitionCost, decimal? PartnerPercentOverride, Guid? PartnerId, string Status, Guid? AssemblyId, DateTimeOffset ReceivedAtUtc, long RowVersion);
/// <summary>Items created by one receipt, in line order (one entry per created item).</summary>
public sealed record ReceiptResult(Guid ReceiptId, IReadOnlyList<StockItemAdminView> Items);
