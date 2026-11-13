namespace HardwareStore.Modules.Retail;

// Admin (back office). Cost, serial and partner fields are null when the caller lacks the permission.
public sealed record StockUnitView(Guid Id, string Status, string Condition, string? SerialNumber, decimal? AcquisitionCost,
    decimal? PartnerPercentOverride, Guid? AssemblyId, DateTimeOffset ReceivedAtUtc);
public sealed record StockItemAdminView(Guid Id, string Code, string Condition, Guid? CategoryId, Guid? ProductId, string ModelName,
    decimal Price, decimal? WithBoardExtra, decimal? AloneExtra, StockItemPrices Prices, string? WarrantyLabel, int? WarrantyMonths,
    bool HasBox, bool IsHidden, bool IsPublished, string? InternalNote, IReadOnlyDictionary<string, string> Specs,
    IReadOnlyList<StockItemPhoto> Photos, int AvailableCount, int DaysInStock, DateTimeOffset CreatedAtUtc, long RowVersion,
    IReadOnlyList<StockUnitView> Units);
public sealed record StockItemQuery(string? Search = null, Guid? CategoryId = null, string? Condition = null, bool? Hidden = null,
    bool? InStock = null, int Page = 1, int PageSize = 50);
public sealed record StockItemAdminPage(IReadOnlyList<StockItemAdminView> Items, int Page, int PageSize, int Total);
/// <summary>Warranty is resolved from the settings list by the caller before it reaches Retail.</summary>
public sealed record StockItemUpdate(string? ModelName, Guid? CategoryId, Guid? ProductId, decimal Price, decimal? WithBoardExtra,
    decimal? AloneExtra, string? WarrantyLabel, int? WarrantyMonths, bool HasBox, bool IsHidden, string? InternalNote,
    IReadOnlyDictionary<string, string>? Specs, long RowVersion);
public sealed record StockVisibilityRequest(IReadOnlyList<Guid> Ids, bool Hidden);

// Storefront (public). Never cost, serial, partner, internal note, or hidden items.
public sealed record StoreItemCard(string Code, string ModelName, string Condition, Guid? CategoryId, string? WarrantyLabel,
    StockItemPrices Prices, int Available, string? ThumbnailStorageKey, DateTimeOffset ListedAtUtc);
public sealed record StoreItemDetail(string Code, string ModelName, string Condition, Guid? CategoryId, string? WarrantyLabel,
    int? WarrantyMonths, bool HasBox, StockItemPrices Prices, int Available, IReadOnlyList<StockItemPhoto> Photos,
    IReadOnlyDictionary<string, string> Specs);
/// <summary><paramref name="CategoryIds"/> is the chosen category plus its descendants (resolved from Catalog by the caller).</summary>
public sealed record StoreItemQuery(string? Search = null, IReadOnlyList<Guid>? CategoryIds = null, string? Condition = null,
    bool? InStock = null, decimal? MinPrice = null, decimal? MaxPrice = null, string? Sort = null, int Page = 1, int PageSize = 24);
public sealed record StoreConditionCounts(int New, int Used);
public sealed record StoreItemPage(IReadOnlyList<StoreItemCard> Items, int Page, int PageSize, int Total, StoreConditionCounts Counts);
public sealed record ModelNameSuggestion(string ModelName, Guid? CategoryId, Guid? ProductId, int ItemCount);
