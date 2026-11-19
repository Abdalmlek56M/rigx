namespace HardwareStore.Modules.Retail;

/// <summary>One successful opening-stock import; the file hash stops the same workbook from being imported twice.</summary>
public sealed class StockImport
{
    public Guid Id { get; init; }
    public required string Sha256 { get; init; }
    public required string FileName { get; init; }
    public int RowCount { get; init; }
    public int ItemCount { get; init; }
    public Guid ImportedByUserAccountId { get; init; }
    public DateTimeOffset ImportedAtUtc { get; init; }
}
