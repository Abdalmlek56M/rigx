namespace HardwareStore.Modules.Retail;

/// <summary>Create (no RowVersion) or update (RowVersion required) a money account; the kind is fixed at creation.</summary>
public sealed record MoneyAccountRequest(string? Name, string? Kind, decimal OpeningBalance, DateOnly? OpeningDate = null,
    bool IsActive = true, int SortOrder = 0, long? RowVersion = null);
public sealed record MoneyAccountView(Guid Id, string Name, string Kind, decimal OpeningBalance, DateOnly OpeningDate, bool IsActive,
    int SortOrder, decimal Balance, long RowVersion);
public sealed record MoneyMovementView(Guid Id, Guid AccountId, decimal Amount, string Source, Guid SourceId, string? Reference,
    string? Note, DateOnly OccurredOn, Guid ActorUserAccountId, DateTimeOffset CreatedAtUtc);
public sealed record MoneyTransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount, DateOnly? Date = null, string? Note = null);
