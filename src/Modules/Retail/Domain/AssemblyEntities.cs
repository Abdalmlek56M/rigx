namespace HardwareStore.Modules.Retail;

/// <summary>
/// A build (<c>BLD-xx</c>) made of stock units. Its warranty applies to every part unless a part overrides it;
/// <see cref="SellMode"/> says whether parts may also be sold one by one (<see cref="AssemblySellMode"/>).
/// </summary>
public sealed class Assembly
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? PublicTitleAr { get; set; }
    public string? PublicTitleEn { get; set; }
    public decimal SalePrice { get; set; }
    public required string Status { get; set; } = AssemblyStatus.Draft;
    /// <summary>One of <see cref="AssemblyUsage"/>; drives the storefront usage filter.</summary>
    public string? Usage { get; set; }
    public string SellMode { get; set; } = AssemblySellMode.Full;
    /// <summary>Null means "per part": every part keeps its own warranty.</summary>
    public string? WarrantyLabel { get; set; }
    public int? WarrantyMonths { get; set; }
    /// <summary>How many identical copies the store can deliver ("لو عندك نفس التجميعة أكتر من مرة").</summary>
    public int Copies { get; set; } = 1;
    /// <summary>"مناسبة لـ" text shown on the build card.</summary>
    public string? SuitableForAr { get; set; }
    public string? SuitableForEn { get; set; }
    public List<StockItemPhoto> Photos { get; set; } = [];
    /// <summary>Parts typed into the build that are not stock units (made to order, round 3): nothing is created or reserved for them.</summary>
    public List<PlannedPart> PlannedParts { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
    public ICollection<AssemblyUnit> Units { get; set; } = new List<AssemblyUnit>();
}

public sealed class AssemblyUnit
{
    public Guid AssemblyId { get; set; }
    public Guid UnitId { get; set; }
    public decimal AllocationPercent { get; set; }
    /// <summary>Price of this part inside this build (used when the build is sold part by part).</summary>
    public decimal PartPrice { get; set; }
    /// <summary>Per-part warranty override; null = the build's warranty (or the item's own when the build has none).</summary>
    public string? WarrantyLabel { get; set; }
    public int? WarrantyMonths { get; set; }
    public Assembly Assembly { get; set; } = null!;
    public InventoryUnit Unit { get; set; } = null!;
}

/// <summary>
/// A part listed in a build without a stock unit ("مش شرط تبقى في المخزون"). <see cref="StockItemId"/> links it to a stock item of the same
/// model when one exists (its code is kept for display). Its price is the part's price inside the build, when known.
/// </summary>
public sealed record PlannedPart(Guid Id, string Name, Guid? CategoryId, Guid? BrandId, decimal? Price, Guid? StockItemId, string? StockItemCode);
