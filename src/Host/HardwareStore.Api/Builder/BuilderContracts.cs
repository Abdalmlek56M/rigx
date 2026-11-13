using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;

public sealed record BuilderPartInput(string? Code, int Quantity = 1);

public sealed record BuilderSlotView(string Slot, string Name, IReadOnlyList<Guid> CategoryIds);

public sealed record BuilderPartView(string Slot, BuilderItemView Item, int Quantity, decimal UnitPrice, decimal LineTotal, bool IsAvailable);

/// <summary>
/// A checked build. <paramref name="MissingCodes"/> are codes that are no longer published (a saved link keeps working);
/// <paramref name="CartLines"/> is what "add all to cart" and checkout send.
/// </summary>
public sealed record BuilderEvaluation(IReadOnlyList<BuilderPartView> Parts, IReadOnlyList<string> MissingCodes, IReadOnlyList<BuilderIssue> Issues,
    decimal? PowerEstimateWatts, decimal? PsuCapacityWatts, bool UsesBuildPrice, string PriceOption, decimal Total, bool CanOrder,
    IReadOnlyList<OrderLineRequest> CartLines);

public sealed record BuilderOptionView(BuilderItemView Item, IReadOnlyList<BuilderIssue> Issues, bool IsCompatible, bool IsAvailable);
public sealed record BuilderOptionPage(IReadOnlyList<BuilderOptionView> Items, int Page, int PageSize, int Total);

public sealed record UnmatchedPartView(string ModelName, string Condition);
public sealed record BuilderFromBuildView(string Code, string Title, string? Usage, BuilderEvaluation Evaluation, IReadOnlyList<UnmatchedPartView> Unmatched);

public sealed record SavedBuildView(string Token, string? Name, string? Usage, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    long RowVersion, bool IsOwner, BuilderEvaluation Evaluation);
public sealed record SavedBuildSummary(string Token, string? Name, string? Usage, int PartCount, DateTimeOffset UpdatedAtUtc);

public sealed record BuilderOptionsRequest(string? Slot, IReadOnlyList<BuilderPartInput>? Parts, string? Condition = null, string? Q = null, int Page = 1);
public sealed record BuilderCheckRequest(IReadOnlyList<BuilderPartInput>? Parts);
public sealed record BuilderQuoteRequest(string? Name, string? Usage, IReadOnlyList<BuilderPartInput>? Parts);
public sealed record BuilderSaveRequest(string? Name, string? Usage, IReadOnlyList<BuilderPartInput>? Parts);
public sealed record BuilderSaveUpdateRequest(string? Name, string? Usage, IReadOnlyList<BuilderPartInput>? Parts, long RowVersion);
