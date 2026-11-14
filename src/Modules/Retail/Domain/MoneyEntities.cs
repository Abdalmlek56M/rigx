namespace HardwareStore.Modules.Retail;

/// <summary>A place where store money sits (cash drawer, InstaPay, wallet, bank). Its balance is the opening balance plus its movements.</summary>
public sealed class MoneyAccount
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Kind { get; set; }
    public decimal OpeningBalance { get; set; }
    public DateOnly OpeningDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

/// <summary>Money in (positive) or out (negative) of an account. Append-only: never edited or deleted.</summary>
public sealed class MoneyMovement
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public required string Source { get; set; }
    public Guid SourceId { get; set; }
    /// <summary>Human reference of the source: the order reference or the purchase code.</summary>
    public string? Reference { get; set; }
    public string? Note { get; set; }
    public DateOnly OccurredOn { get; set; }
    public Guid ActorUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
