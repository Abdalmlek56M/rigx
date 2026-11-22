using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.PcBuilder;

/// <summary>What a visitor sees under a build: the two counts and, for a signed-in visitor, which of them are theirs.</summary>
public sealed record BuildReactionSummary(int Hearts, int Likes, bool Hearted, bool Liked);

/// <summary>
/// Hearts and likes on saved builds. One row per build, account and kind: adding twice or removing what is not there changes nothing,
/// so a double click or a retried request is safe. Only a build the account can open can be reacted to.
/// </summary>
public sealed class BuildReactionStore(IDbContextFactory<PcBuilderDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<BuildReactionSummary> AddAsync(string token, Guid userId, BuildReactionKind kind, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var buildId = await ViewableIdAsync(db, token, userId, cancellationToken);
        var mine = db.BuildReactions.Where(reaction => reaction.UserAccountId == userId);
        if (kind == BuildReactionKind.Heart && !await mine.AnyAsync(reaction => reaction.SavedBuildId == buildId && reaction.Kind == kind, cancellationToken))
            BuildReactionRules.EnsureRoomForAnotherHeart(await mine.CountAsync(reaction => reaction.Kind == BuildReactionKind.Heart, cancellationToken));
        var now = timeProvider.GetUtcNow();
        var name = kind.ToString();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO saved_build_reactions (saved_build_id, user_account_id, kind, created_at_utc) VALUES ({buildId}, {userId}, {name}, {now}) ON CONFLICT DO NOTHING",
            cancellationToken);
        return (await SummariesAsync(db, [buildId], userId, cancellationToken))[buildId];
    }

    public async Task<BuildReactionSummary> RemoveAsync(string token, Guid userId, BuildReactionKind kind, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var buildId = await ViewableIdAsync(db, token, userId, cancellationToken);
        await db.BuildReactions.Where(reaction => reaction.SavedBuildId == buildId && reaction.UserAccountId == userId && reaction.Kind == kind)
            .ExecuteDeleteAsync(cancellationToken);
        return (await SummariesAsync(db, [buildId], userId, cancellationToken))[buildId];
    }

    /// <summary>The summary of each build (zero counts when nobody reacted); <paramref name="viewerId"/> is null for a visitor who is not signed in.</summary>
    public async Task<IReadOnlyDictionary<Guid, BuildReactionSummary>> SummariesAsync(IReadOnlyCollection<Guid> buildIds, Guid? viewerId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await SummariesAsync(db, buildIds, viewerId, cancellationToken);
    }

    /// <summary>Moves a merged account's hearts and likes to the account that was kept (those it already gave are not doubled); safe to run again.</summary>
    public async Task MoveAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO saved_build_reactions (saved_build_id, user_account_id, kind, created_at_utc) SELECT saved_build_id, {targetId}, kind, created_at_utc FROM saved_build_reactions WHERE user_account_id = {sourceId} ON CONFLICT DO NOTHING",
            cancellationToken);
        await db.BuildReactions.Where(reaction => reaction.UserAccountId == sourceId).ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<Guid, BuildReactionSummary>> SummariesAsync(PcBuilderDbContext db, IReadOnlyCollection<Guid> buildIds,
        Guid? viewerId, CancellationToken cancellationToken)
    {
        var rows = await db.BuildReactions.AsNoTracking().Where(reaction => buildIds.Contains(reaction.SavedBuildId))
            .GroupBy(reaction => new { reaction.SavedBuildId, reaction.Kind })
            .Select(group => new { group.Key.SavedBuildId, group.Key.Kind, Count = group.Count() }).ToListAsync(cancellationToken);
        var mine = viewerId is Guid viewer
            ? await db.BuildReactions.AsNoTracking().Where(reaction => reaction.UserAccountId == viewer && buildIds.Contains(reaction.SavedBuildId))
                .Select(reaction => new { reaction.SavedBuildId, reaction.Kind }).ToListAsync(cancellationToken)
            : [];
        return buildIds.ToDictionary(id => id, id => new BuildReactionSummary(
            rows.Where(row => row.SavedBuildId == id && row.Kind == BuildReactionKind.Heart).Sum(row => row.Count),
            rows.Where(row => row.SavedBuildId == id && row.Kind == BuildReactionKind.Like).Sum(row => row.Count),
            mine.Any(entry => entry.SavedBuildId == id && entry.Kind == BuildReactionKind.Heart),
            mine.Any(entry => entry.SavedBuildId == id && entry.Kind == BuildReactionKind.Like)));
    }

    private static async Task<Guid> ViewableIdAsync(PcBuilderDbContext db, string token, Guid userId, CancellationToken cancellationToken)
    {
        var build = await db.SavedBuilds.AsNoTracking().Where(entry => entry.Token == token)
            .Select(entry => new { entry.Id, entry.Visibility, entry.UserAccountId }).SingleOrDefaultAsync(cancellationToken);
        return build is not null && BuildVisibilityRules.CanView(build.Visibility, build.UserAccountId, userId) ? build.Id : throw SavedBuildStore.NotFound();
    }
}
