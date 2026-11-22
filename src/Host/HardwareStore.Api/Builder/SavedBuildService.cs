using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;

/// <summary>
/// Saved builds: parts are checked at save time (unpublished codes are refused) and re-checked live on every read. A build made by a
/// signed-in account shows that account's first name ("assembled by ..."); an anonymous build shows none. A private build opens for its owner only.
/// </summary>
public sealed class SavedBuildService(SavedBuildStore store, BuildReactionStore reactions, BuilderService builder, BuildAuthors authors)
{
    public async Task<SavedBuildView> SaveAsync(Guid? userId, string? name, string? usage, IReadOnlyList<BuilderPartInput>? parts, BuildVisibility? visibility,
        CancellationToken cancellationToken)
    {
        var (parsedUsage, selection, evaluation) = await ValidateAsync(usage, parts, cancellationToken);
        var build = await store.CreateAsync(userId, name, parsedUsage, selection, visibility, cancellationToken);
        return await ToViewAsync(build, userId, evaluation, cancellationToken);
    }

    public async Task<SavedBuildView> GetAsync(string token, Guid? userId, CancellationToken cancellationToken)
    {
        var build = await store.GetByTokenAsync(token, userId, cancellationToken);
        return await ToViewAsync(build, userId, await builder.CheckAsync(BuilderPartInput.From(build.Parts), cancellationToken), cancellationToken);
    }

    public async Task<SavedBuildView> UpdateAsync(string token, Guid userId, string? name, string? usage, IReadOnlyList<BuilderPartInput>? parts,
        BuildVisibility? visibility, long rowVersion, CancellationToken cancellationToken)
    {
        var (parsedUsage, selection, evaluation) = await ValidateAsync(usage, parts, cancellationToken);
        var build = await store.UpdateAsync(token, userId, name, parsedUsage, selection, visibility, rowVersion, cancellationToken);
        return await ToViewAsync(build, userId, evaluation, cancellationToken);
    }

    /// <summary>The owner's "private / by link / public" choice for a build they already saved.</summary>
    public async Task<SavedBuildSummary> SetVisibilityAsync(string token, Guid userId, BuildVisibility visibility, CancellationToken cancellationToken) =>
        Summary(await store.SetVisibilityAsync(token, userId, visibility, cancellationToken));

    public Task DeleteAsync(string token, Guid userId, CancellationToken cancellationToken) => store.DeleteAsync(token, userId, cancellationToken);

    public async Task<IReadOnlyList<SavedBuildSummary>> ListMineAsync(Guid userId, CancellationToken cancellationToken) =>
        (await store.ListMineAsync(userId, cancellationToken)).Select(Summary).ToList();

    private static SavedBuildSummary Summary(SavedBuild build) =>
        new(build.Token, build.Name, build.Usage, build.Visibility, build.Moderation?.Status, build.Parts.Count, build.UpdatedAtUtc);

    /// <summary>A conflict may be saved; a part that is not a published item may not.</summary>
    private async Task<(string? Usage, IReadOnlyList<SavedBuildPart> Selection, BuilderEvaluation Evaluation)> ValidateAsync(string? usage,
        IReadOnlyList<BuilderPartInput>? parts, CancellationToken cancellationToken)
    {
        var parsedUsage = AssemblyUsage.Parse(usage);
        var evaluation = await builder.CheckAsync(parts, cancellationToken);
        if (evaluation.MissingCodes.Count > 0) throw new PcBuilderException(400, "A part is not available in the store any more.");
        var selection = evaluation.Parts.Select(part => new SavedBuildPart(part.Item.Code, part.Quantity)).ToList();
        return (parsedUsage, selection, evaluation);
    }

    private async Task<SavedBuildView> ToViewAsync(SavedBuild build, Guid? viewerId, BuilderEvaluation evaluation, CancellationToken cancellationToken)
    {
        var isOwner = viewerId is not null && build.UserAccountId == viewerId;
        var summary = (await reactions.SummariesAsync([build.Id], viewerId, cancellationToken))[build.Id];
        return new SavedBuildView(build.Token, build.Name, await authors.FirstNameAsync(build.UserAccountId, cancellationToken), build.Usage, build.Visibility,
            isOwner ? build.Moderation?.Status : null, build.CreatedAtUtc, build.UpdatedAtUtc, build.RowVersion, isOwner, summary, evaluation);
    }
}
