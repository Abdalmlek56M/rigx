namespace HardwareStore.Modules.Retail;

public sealed class Assembly
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? PublicTitleAr { get; set; }
    public string? PublicTitleEn { get; set; }
    public decimal SalePrice { get; set; }
    public required string Status { get; set; } = AssemblyStatus.Draft;
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
    public Assembly Assembly { get; set; } = null!;
    public InventoryUnit Unit { get; set; } = null!;
}
