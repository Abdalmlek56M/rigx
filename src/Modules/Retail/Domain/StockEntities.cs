namespace HardwareStore.Modules.Retail;

/// <summary>
/// What the store sells, with its public code <c>RX-xxxx</c> (owner decision D1):
/// a used item is one physical piece (exactly one unit, its own photos);
/// a new item is a lot of identical pieces (one or more units, page shows the available quantity).
/// Cost, serial, partner share and status stay on each <see cref="InventoryUnit"/>.
/// </summary>
public sealed class StockItem
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Condition { get; set; }
    public Guid? CategoryId { get; set; }
    /// <summary>The Catalog brand, when the owner picked one.</summary>
    public Guid? BrandId { get; set; }
    public Guid? ProductId { get; set; }
    public required string ModelName { get; set; }
    public required string ModelKey { get; set; }
    /// <summary>Unique readable URL part, from the model name (<see cref="StockItemSlug"/>).</summary>
    public string Slug { get; set; } = string.Empty;
    /// <summary>Base price = price inside a build.</summary>
    public decimal Price { get; set; }
    public decimal? WithBoardExtra { get; set; }
    public decimal? AloneExtra { get; set; }
    /// <summary>
    /// The ways the owner sells this item online (alone, with a board, inside a build), when he chose them for the item itself; the three
    /// are set together or all null, and null follows the item's category (<see cref="StockItemRules.OwnPriceOptions"/>).
    /// </summary>
    public bool? OffersAlone { get; set; }
    public bool? OffersWithBoard { get; set; }
    public bool? OffersInBuild { get; set; }
    public string? WarrantyLabel { get; set; }
    public int? WarrantyMonths { get; set; }
    public bool HasBox { get; set; }
    /// <summary>The owner left this item's cost out of the stock value (it stays in stock and sellable); see <see cref="StockValueRules"/>.</summary>
    public bool ExcludedFromStockValue { get; set; }
    public bool IsHidden { get; set; }
    public string? InternalNote { get; set; }
    public Dictionary<string, string> Specs { get; set; } = [];
    /// <summary>What the owner wrote about this item for its store page (Arabic), if anything.</summary>
    public string? Description { get; set; }
    /// <summary>The same description in English.</summary>
    public string? DescriptionEn { get; set; }
    public List<StockItemPhoto> Photos { get; set; } = [];
    /// <summary>Database-generated from <see cref="Photos"/> so the storefront can filter published items in SQL.</summary>
    public int PhotoCount { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public ICollection<InventoryUnit> Units { get; set; } = new List<InventoryUnit>();
}

/// <summary>
/// A slug the item had before a rename (owner request 19 Nov 2026: a published link never breaks). It keeps leading to the
/// item and stays reserved, so no other item can ever take it (<see cref="StockItemSlug.Choose"/>).
/// </summary>
public sealed class StockItemOldSlug
{
    public required string Slug { get; init; }
    public Guid StockItemId { get; init; }
}

public sealed record StockItemPhoto(string StorageKey, string ThumbnailStorageKey);

/// <summary>A photo with the id of the stock item or build that shows it.</summary>
public sealed record OwnedPhoto(Guid OwnerId, StockItemPhoto Photo);
