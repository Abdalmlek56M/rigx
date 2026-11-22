using HardwareStore.Modules.Identity;
using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;

/// <summary>
/// Saved builds: parts are checked at save time (unpublished codes are refused) and re-checked live on every read. A build made by a
/// signed-in account shows that account's first name ("assembled by ..."); an anonymous build shows none.
/// </summary>
public sealed class SavedBuildService(SavedBuildStore store, BuilderService builder, IdentityProfileService profiles)
{
    public async Task<SavedBuildView> SaveAsync(Guid? userId, string? name, string? usage, IReadOnlyList<BuilderPartInput>? parts,
        CancellationToken cancellationToken)
    {
        var (parsedUsage, selection, evaluation) = await ValidateAsync(usage, parts, cancellationToken);
        var build = await store.CreateAsync(userId, name, parsedUsage, selection, cancellationToken);
        return ToView(build, userId, await AuthorNameAsync(build.UserAccountId, cancellationToken), evaluation);
    }

    public async Task<SavedBuildView> GetAsync(string token, Guid? userId, CancellationToken cancellationToken)
    {
        var build = await store.GetByTokenAsync(token, cancellationToken);
        return ToView(build, userId, await AuthorNameAsync(build.UserAccountId, cancellationToken), await builder.CheckAsync(Inputs(build.Parts), cancellationToken));
    }

    public async Task<SavedBuildView> UpdateAsync(string token, Guid userId, string? name, string? usage, IReadOnlyList<BuilderPartInput>? parts,
        long rowVersion, CancellationToken cancellationToken)
    {
        var (parsedUsage, selection, evaluation) = await ValidateAsync(usage, parts, cancellationToken);
        var build = await store.UpdateAsync(token, userId, name, parsedUsage, selection, rowVersion, cancellationToken);
        return ToView(build, userId, await AuthorNameAsync(build.UserAccountId, cancellationToken), evaluation);
    }

    public Task DeleteAsync(string token, Guid userId, CancellationToken cancellationToken) => store.DeleteAsync(token, userId, cancellationToken);

    public async Task<IReadOnlyList<SavedBuildSummary>> ListMineAsync(Guid userId, CancellationToken cancellationToken) =>
        (await store.ListMineAsync(userId, cancellationToken))
        .Select(build => new SavedBuildSummary(build.Token, build.Name, build.Usage, build.Parts.Count, build.UpdatedAtUtc)).ToList();

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

    /// <summary>The first word of the creator's full name (only that much is public), or null for an anonymous build.</summary>
    private async Task<string?> AuthorNameAsync(Guid? ownerId, CancellationToken cancellationToken)
    {
        if (ownerId is null) return null;
        var fullName = await profiles.GetFullNameAsync(ownerId.Value, cancellationToken);
        return fullName?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
    }

    private static SavedBuildView ToView(SavedBuild build, Guid? userId, string? authorName, BuilderEvaluation evaluation) =>
        new(build.Token, build.Name, authorName, build.Usage, build.CreatedAtUtc, build.UpdatedAtUtc, build.RowVersion,
            userId is not null && build.UserAccountId == userId, evaluation);

    private static List<BuilderPartInput> Inputs(IEnumerable<SavedBuildPart> parts) =>
        parts.Select(part => new BuilderPartInput(part.Code, part.Quantity)).ToList();
}
