namespace HardwareStore.Modules.Retail;

public sealed class RetailExpense
{
    public Guid Id { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Snapshot of the expense type name at the time; renaming the type does not change it.</summary>
    public required string Category { get; set; }
    public required string Classification { get; set; }
    public string? Note { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? AssemblyId { get; set; }
    /// <summary>The account the money left from; null when a partner paid.</summary>
    public Guid? MoneyAccountId { get; set; }
    /// <summary>The partner who paid from his own money (deducted from his dues); null when an account paid.</summary>
    public Guid? PaidByPartnerId { get; set; }
    public Guid? RecurringExpenseId { get; set; }
    /// <summary>Private storage key of the receipt photo; null when none was uploaded.</summary>
    public string? ReceiptStorageKey { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

/// <summary>A named kind of expense ("إيجار"); expenses must use an active one.</summary>
public sealed class ExpenseType
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

/// <summary>A fixed monthly general-overhead expense, posted automatically on its day of the month (business time).</summary>
public sealed class RecurringExpense
{
    public Guid Id { get; set; }
    public required string Category { get; set; }
    public decimal Amount { get; set; }
    public int DayOfMonth { get; set; }
    public Guid? MoneyAccountId { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>First day of the first month to post.</summary>
    public DateOnly StartMonth { get; set; }
    /// <summary>First day of the last month posted; null before the first posting.</summary>
    public DateOnly? LastPostedMonth { get; set; }
    public Guid CreatedByUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}
