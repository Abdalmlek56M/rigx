namespace HardwareStore.Modules.Retail;

public sealed class RetailExpense
{
    public Guid Id { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public required string Category { get; set; }
    public required string Classification { get; set; }
    public string? Note { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? UnitId { get; set; }
    public string? AttachmentReference { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}
