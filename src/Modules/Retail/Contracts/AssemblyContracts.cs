namespace HardwareStore.Modules.Retail;

public sealed record AssemblyCreateRequest(string? Name, IReadOnlyList<Guid>? UnitIds);
public sealed record AssemblyUpdateRequest(string? Name, string? PublicTitleAr, string? PublicTitleEn, decimal SalePrice, long RowVersion);
public sealed record AssemblyAllocationRequest(long RowVersion, IReadOnlyList<AssemblyAllocationInput>? Allocations);
public sealed record AssemblyAllocationInput(Guid UnitId, decimal Percent);
public sealed record SwapRequest(Guid FromAssemblyId, Guid ToAssemblyId, Guid UnitId, long FromRowVersion, long ToRowVersion);
public sealed record AssemblyPublicView(Guid Id, string Code, string? TitleAr, string? TitleEn, decimal SalePrice, IReadOnlyList<PublicUnitView> Units);
public sealed record PublicUnitView(Guid? ProductId, string Name, string StockItemCode, string Condition);
/// <summary><paramref name="StandalonePrice"/> is the stock item's price when sold alone (admin only).</summary>
public sealed record AssemblyUnitView(Guid UnitId, string Name, string StockItemCode, string Condition, decimal? AcquisitionCost,
    decimal? StandalonePrice, decimal AllocationPercent, decimal? AllocatedAmount);
public sealed record AssemblyView(Guid Id, string Code, string Name, string? PublicTitleAr, string? PublicTitleEn, decimal SalePrice,
    string Status, IReadOnlyList<AssemblyUnitView> Units, decimal? ComponentCost, decimal? AllocatedTotal, long RowVersion);
