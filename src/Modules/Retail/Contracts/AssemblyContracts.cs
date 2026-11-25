namespace HardwareStore.Modules.Retail;

// Admin requests. Warranty labels are resolved from the settings list by the caller (Host) before they reach Retail.
public sealed record AssemblyCreateRequest(string? Name, IReadOnlyList<Guid>? UnitIds);
public sealed record AssemblyUpdateRequest(string? Name, string? PublicTitleAr, string? PublicTitleEn, decimal SalePrice, long RowVersion,
    string? Usage = null, string? SellMode = null, string? WarrantyLabel = null, int? WarrantyMonths = null, int Copies = 1,
    string? SuitableForAr = null, string? SuitableForEn = null);
/// <summary>Adds <paramref name="Count"/> identical builds (each with its own new parts) made from an existing build.</summary>
public sealed record AssemblyCopyRequest(int Count);
public sealed record AssemblyAllocationRequest(long RowVersion, IReadOnlyList<AssemblyAllocationInput>? Allocations);
public sealed record AssemblyAllocationInput(Guid UnitId, decimal Percent);
/// <summary>Per-part edits: price inside the build, warranty override (null label = use the build's), financing partner and partner % override.</summary>
public sealed record AssemblyPartUpdateRequest(decimal PartPrice, string? WarrantyLabel, int? WarrantyMonths,
    decimal? PartnerPercentOverride, Guid? PartnerId, long RowVersion);
/// <summary>Adds an available stock unit to the build.</summary>
public sealed record AssemblyAddPartRequest(Guid UnitId, long RowVersion);
/// <summary>Adds a part that is not in stock yet: it becomes a new single-piece stock item inside the build.</summary>
public sealed record AssemblyNewPartRequest(ReceiptLine Part, long RowVersion);
/// <summary>One typed part of a build; <paramref name="Id"/> is null for a new row.</summary>
public sealed record PlannedPartInput(Guid? Id, string? Name, Guid? CategoryId, Guid? BrandId, decimal? Price, Guid? StockItemId);
/// <summary>Replaces the build's whole list of typed parts.</summary>
public sealed record AssemblyPlannedPartsRequest(IReadOnlyList<PlannedPartInput>? Parts, long RowVersion);
/// <summary>Without a target the part goes back to stock as available; with a target it moves into that build.</summary>
public sealed record AssemblyRemovePartRequest(long RowVersion, Guid? TargetAssemblyId = null, long? TargetRowVersion = null);
/// <summary>
/// Replaces a part with an available unit, or with a part of another build (<paramref name="OtherRowVersion"/>):
/// then the two parts change places.
/// </summary>
public sealed record AssemblyReplacePartRequest(Guid ReplacementUnitId, long RowVersion, long? OtherRowVersion = null);

/// <summary><paramref name="Usage"/>, <paramref name="MinPrice"/> and <paramref name="MaxPrice"/> are the storefront filters (usage + budget).</summary>
public sealed record AssemblyStoreQuery(string? Usage = null, decimal? MinPrice = null, decimal? MaxPrice = null);

// Storefront (public): no cost, partner, serial or internal data.
public sealed record AssemblyPublicView(Guid Id, string Code, string? TitleAr, string? TitleEn, decimal SalePrice, string? Usage,
    string SellMode, int Copies, string? WarrantyLabel, string? SuitableForAr, string? SuitableForEn, IReadOnlyList<StockItemPhoto> Photos,
    IReadOnlyList<PublicUnitView> Units);
/// <summary><paramref name="PartPrice"/> is shown only when the build may be sold part by part.</summary>
public sealed record PublicUnitView(Guid? ProductId, Guid? CategoryId, string Name, string StockItemCode, string Condition,
    string? WarrantyLabel, decimal? PartPrice);

// Admin views. Cost, partner and price split fields are null without the cost permission.
/// <summary><paramref name="StandalonePrice"/> is the stock item's price when sold alone (admin only).</summary>
public sealed record AssemblyUnitView(Guid UnitId, Guid? CategoryId, string Name, string StockItemCode, string Condition,
    decimal? AcquisitionCost, decimal? StandalonePrice, decimal PartPrice, string? WarrantyLabel, int? WarrantyMonths,
    bool WarrantyOverridden, decimal? PartnerPercentOverride, Guid? PartnerId, decimal AllocationPercent, decimal? AllocatedAmount);
public sealed record AssemblyView(Guid Id, string Code, string Name, string? PublicTitleAr, string? PublicTitleEn, decimal SalePrice,
    string Status, string? Usage, string SellMode, string? WarrantyLabel, int? WarrantyMonths, int Copies, string? SuitableForAr,
    string? SuitableForEn, IReadOnlyList<StockItemPhoto> Photos, IReadOnlyList<AssemblyUnitView> Units, decimal? ComponentCost,
    decimal PartsTotal, decimal? AllocatedTotal, long RowVersion, IReadOnlyList<PlannedPart> PlannedParts);
