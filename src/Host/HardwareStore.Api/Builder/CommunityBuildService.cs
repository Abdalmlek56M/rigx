using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.PcBuilder;

/// <summary>
/// Builds customers show to others: the approved public gallery, a visitor's hearted builds and the admin's moderation queue. Every card is
/// priced live from the store's current prices, like a shared build page.
/// </summary>
public sealed class CommunityBuildService(CommunityBuildStore store, BuildReactionStore reactions, BuilderService builder, BuildAuthors authors)
{
    public async Task<PagedResult<CommunityBuildCard>> GalleryAsync(int page, int pageSize, Guid? viewerId, CancellationToken cancellationToken)
    {
        var found = await store.ListGalleryAsync(page, pageSize, cancellationToken);
        return new PagedResult<CommunityBuildCard>(await CardsAsync(found.Items, viewerId, cancellationToken), found.Page, found.PageSize, found.Total);
    }

    public async Task<IReadOnlyList<CommunityBuildCard>> HeartedAsync(Guid userId, CancellationToken cancellationToken) =>
        await CardsAsync(await store.ListHeartedAsync(userId, cancellationToken), userId, cancellationToken);

    public async Task<PagedResult<CustomerBuildModerationView>> ModerationAsync(BuildModerationStatus status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var found = await store.ListForModerationAsync(status, page, pageSize, cancellationToken);
        var cards = await CardsAsync(found.Items, null, cancellationToken);
        return new PagedResult<CustomerBuildModerationView>(found.Items.Zip(cards, ModerationView).ToList(), found.Page, found.PageSize, found.Total);
    }

    public async Task<CustomerBuildModerationView> DecideAsync(string token, BuildModerationStatus decision, Guid actorId, CancellationToken cancellationToken)
    {
        var build = await store.DecideAsync(token, decision, actorId, cancellationToken);
        return ModerationView(build, (await CardsAsync([build], null, cancellationToken))[0]);
    }

    private static CustomerBuildModerationView ModerationView(SavedBuild build, CommunityBuildCard card) =>
        new(card, build.Moderation!.Status, build.Moderation.SubmittedAtUtc, build.Moderation.DecidedAtUtc);

    private async Task<List<CommunityBuildCard>> CardsAsync(IReadOnlyList<SavedBuild> builds, Guid? viewerId, CancellationToken cancellationToken)
    {
        var summaries = await reactions.SummariesAsync([.. builds.Select(build => build.Id)], viewerId, cancellationToken);
        var cards = new List<CommunityBuildCard>(builds.Count);
        foreach (var build in builds)
        {
            var evaluation = await builder.CheckAsync(BuilderPartInput.From(build.Parts), cancellationToken);
            cards.Add(new CommunityBuildCard(build.Token, build.Name, await authors.FirstNameAsync(build.UserAccountId, cancellationToken), build.Usage,
                evaluation.Parts.Count, evaluation.Total, evaluation.CanOrder, [.. evaluation.Parts.Select(part => new CommunityBuildPart(part.Slot, part.Item.ModelName))],
                build.UpdatedAtUtc, summaries[build.Id]));
        }
        return cards;
    }
}
