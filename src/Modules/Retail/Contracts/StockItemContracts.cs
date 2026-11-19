namespace HardwareStore.Modules.Retail;

// Admin (back office). Cost, serial and partner fields are null when the caller lacks the permission.
public sealed record StockUnitView(Guid Id, string Status, string Condition, string? SerialNumber, decimal? AcquisitionCost,
    decimal? PartnerPercentOverride, Guid? PartnerId, Guid? AssemblyId, DateTimeOffset ReceivedAtUtc, long RowVersion);
public sealed record StockItemAdminView(Guid Id, string Code, string Condition, Guid? CategoryId, Guid? BrandId, Guid? ProductId, string ModelName,
    decimal Price, decimal? WithBoardExtra, decimal? AloneExtra, StockItemPrices Prices, string? WarrantyLabel, int? WarrantyMonths,
    bool HasBox, bool IsHidden, bool IsPublished, string? InternalNote, IReadOnlyDictionary<string, string> Specs,
    IReadOnlyList<StockItemPhoto> Photos, int AvailableCount, int DaysInStock, DateTimeOffset CreatedAtUtc, long RowVersion,
    IReadOnlyList<StockUnitView> Units);
public sealed record StockItemQuery(string? Search = null, Guid? CategoryId = null, string? Condition = null, bool? Hidden = null,
    bool? InStock = null, int Page = 1, int PageSize = 50);
/// <summary>Warranty is resolved from the settings list by the caller before it reaches Retail.</summary>
public sealed record StockItemUpdate(string? ModelName, Guid? CategoryId, Guid? BrandId, Guid? ProductId, decimal Price, decimal? WithBoardExtra,
    decimal? AloneExtra, string? WarrantyLabel, int? WarrantyMonths, bool HasBox, bool IsHidden, string? InternalNote,
    IReadOnlyDictionary<string, string>? Specs, long RowVersion);
public sealed record StockVisibilityRequest(IReadOnlyList<Guid> Ids, bool Hidden);

// Storefront (public). Never cost, serial, partner, internal note, or hidden items.
public sealed record StoreItemCard(string Code, string Slug, string ModelName, string Condition, Guid? CategoryId, string? WarrantyLabel,
    StockItemPrices Prices, int Available, string? ThumbnailStorageKey, DateTimeOffset ListedAtUtc);
public sealed record StoreItemDetail(string Code, string Slug, string ModelName, string Condition, Guid? CategoryId, Guid? BrandId, string? WarrantyLabel,
    int? WarrantyMonths, bool HasBox, StockItemPrices Prices, int Available, IReadOnlyList<StockItemPhoto> Photos,
    IReadOnlyDictionary<string, string> Specs);
/// <summary>
/// <paramref name="CategoryIds"/> is the chosen category plus its descendants (resolved from Catalog by the caller).
/// <paramref name="Specs"/> are exact spec values an item must all have (spelled as stored); <paramref name="BrandId"/> is a Catalog brand.
/// </summary>
public sealed record StoreItemQuery(string? Search = null, IReadOnlyList<Guid>? CategoryIds = null, string? Condition = null,
    bool? InStock = null, decimal? MinPrice = null, decimal? MaxPrice = null, string? Sort = null, int Page = 1, int PageSize = 24,
    IReadOnlyDictionary<string, string>? Specs = null, Guid? BrandId = null);
public sealed record StoreConditionCounts(int New, int Used);
public sealed record StoreItemPage(IReadOnlyList<StoreItemCard> Items, int Page, int PageSize, int Total, StoreConditionCounts Counts);
public sealed record ModelNameSuggestion(string ModelName, Guid? CategoryId, Guid? ProductId, int ItemCount);

// Notify-me lookups (any item, including unpublished ones, by code; published and available ones by model).
public sealed record StockModelRef(string ModelName, string ModelKey, Guid? CategoryId);
/// <summary>The cheapest published item of a model that has a unit ready to sell.</summary>
public sealed record AvailableModelItem(string ModelKey, string Code, string ModelName, string Condition);

// PC builder (public). Published items only; never cost, serial or internal note.
public sealed record BuilderItemView(Guid StockItemId, string Code, string ModelName, string Condition, Guid? CategoryId,
    string? WarrantyLabel, StockItemPrices Prices, int Available, string? ThumbnailStorageKey, IReadOnlyDictionary<string, string> Specs);
/// <summary>A part of an existing build and the same model from free stock (null when none is free).</summary>
public sealed record BuildPartMatch(string ModelName, string Condition, Guid? CategoryId, BuilderItemView? Match);
public sealed record BuildMatchView(string Code, string Title, string? Usage, IReadOnlyList<BuildPartMatch> Parts);
