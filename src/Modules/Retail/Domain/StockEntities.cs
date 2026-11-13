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
    /// <summary>Partner allocations scoped by category compare against this text (the category id).</summary>
    public string? PartnerCategoryScope => CategoryId?.ToString();
    public Guid? ProductId { get; set; }
    public required string ModelName { get; set; }
    public required string ModelKey { get; set; }
    /// <summary>Base price = price inside a build.</summary>
    public decimal Price { get; set; }
    public decimal? WithBoardExtra { get; set; }
    public decimal? AloneExtra { get; set; }
    public string? WarrantyLabel { get; set; }
    public int? WarrantyMonths { get; set; }
    public bool HasBox { get; set; }
    public bool IsHidden { get; set; }
    public string? InternalNote { get; set; }
    public Dictionary<string, string> Specs { get; set; } = [];
    public List<StockItemPhoto> Photos { get; set; } = [];
    /// <summary>Database-generated from <see cref="Photos"/> so the storefront can filter published items in SQL.</summary>
    public int PhotoCount { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public ICollection<InventoryUnit> Units { get; set; } = new List<InventoryUnit>();
}

public sealed record StockItemPhoto(string StorageKey, string ThumbnailStorageKey);
