using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;

public sealed record BuilderPartInput(string? Code, int Quantity = 1)
{
    public static List<BuilderPartInput> From(IEnumerable<SavedBuildPart> parts) => parts.Select(part => new BuilderPartInput(part.Code, part.Quantity)).ToList();
}

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
public sealed record UnmatchedPartView(string ModelName, string Condition);
public sealed record BuilderFromBuildView(string Code, string Title, string? Usage, BuilderEvaluation Evaluation, IReadOnlyList<UnmatchedPartView> Unmatched);

/// <summary>
/// <paramref name="AuthorName"/> is the creator's first name when a signed-in account made the build, else null. <paramref name="Moderation"/> is the
/// gallery decision and is shown to the owner only (null for everyone else, and for a build that was never public).
/// </summary>
public sealed record SavedBuildView(string Token, string? Name, string? AuthorName, string? Usage, BuildVisibility Visibility, BuildModerationStatus? Moderation,
    DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long RowVersion, bool IsOwner, BuildReactionSummary Reactions, BuilderEvaluation Evaluation);
public sealed record SavedBuildSummary(string Token, string? Name, string? Usage, BuildVisibility Visibility, BuildModerationStatus? Moderation, int PartCount,
    DateTimeOffset UpdatedAtUtc);

public sealed record CommunityBuildPart(string Slot, string ModelName);

/// <summary>A build in the customers' gallery, on a favourites list or in the moderation queue; priced live (<paramref name="CanOrder"/> is false when a part is gone or sold out).</summary>
public sealed record CommunityBuildCard(string Token, string? Name, string? AuthorName, string? Usage, int PartCount, decimal Total, bool CanOrder,
    IReadOnlyList<CommunityBuildPart> Parts, DateTimeOffset UpdatedAtUtc, BuildReactionSummary Reactions);

/// <summary>A public build waiting for, or carrying, the admin's decision.</summary>
public sealed record CustomerBuildModerationView(CommunityBuildCard Build, BuildModerationStatus Status, DateTimeOffset SubmittedAtUtc, DateTimeOffset? DecidedAtUtc);
public sealed record BuildDecisionRequest(BuildModerationStatus Decision);

public sealed record BuilderOptionsRequest(string? Slot, IReadOnlyList<BuilderPartInput>? Parts, string? Condition = null, string? Q = null, int Page = 1);
public sealed record BuilderCheckRequest(IReadOnlyList<BuilderPartInput>? Parts);
public sealed record BuilderQuoteRequest(string? Name, string? Usage, IReadOnlyList<BuilderPartInput>? Parts);
/// <summary>A null <paramref name="Visibility"/> is "anyone with the link" when saving and "unchanged" when updating.</summary>
public sealed record BuilderSaveRequest(string? Name, string? Usage, IReadOnlyList<BuilderPartInput>? Parts, BuildVisibility? Visibility = null);
public sealed record BuilderVisibilityRequest(BuildVisibility Visibility);
public sealed record BuilderSaveUpdateRequest(string? Name, string? Usage, IReadOnlyList<BuilderPartInput>? Parts, long RowVersion, BuildVisibility? Visibility = null);
