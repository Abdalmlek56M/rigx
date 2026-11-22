using HardwareStore.Common.Domain.Paging;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.PcBuilder;

/// <summary>
/// The builds customers chose to show: the public gallery (public and approved), a visitor's hearted builds, and the admin's
/// moderation queue and decision.
/// </summary>
public sealed class CommunityBuildStore(IDbContextFactory<PcBuilderDbContext> contextFactory, TimeProvider timeProvider)
{
    /// <summary>The gallery, newest first.</summary>
    public async Task<PagedResult<SavedBuild>> ListGalleryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var request = PageRequest.Of(page, pageSize);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var listed = db.SavedBuilds.AsNoTracking().Where(build => build.Visibility == BuildVisibility.Public
            && build.Moderation != null && build.Moderation.Status == BuildModerationStatus.Approved);
        return await PageAsync(listed.OrderByDescending(build => build.UpdatedAtUtc).ThenByDescending(build => build.Id), request, cancellationToken);
    }

    /// <summary>The builds the account hearted that it can still open, most recently hearted first.</summary>
    public async Task<IReadOnlyList<SavedBuild>> ListHeartedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await (from reaction in db.BuildReactions.AsNoTracking()
                      where reaction.UserAccountId == userId && reaction.Kind == BuildReactionKind.Heart
                      join build in db.SavedBuilds.AsNoTracking() on reaction.SavedBuildId equals build.Id
                      where build.Visibility != BuildVisibility.Private || build.UserAccountId == userId
                      orderby reaction.CreatedAtUtc descending, build.Id
                      select build).ToListAsync(cancellationToken);
    }

    /// <summary>Public builds with this status: the queue (oldest request first) or the decided ones (latest decision first).</summary>
    public async Task<PagedResult<SavedBuild>> ListForModerationAsync(BuildModerationStatus status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var request = PageRequest.Of(page, pageSize);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var matching = db.SavedBuilds.AsNoTracking()
            .Where(build => build.Visibility == BuildVisibility.Public && build.Moderation != null && build.Moderation.Status == status);
        var ordered = status == BuildModerationStatus.Pending
            ? matching.OrderBy(build => build.Moderation!.SubmittedAtUtc).ThenBy(build => build.Id)
            : matching.OrderByDescending(build => build.Moderation!.DecidedAtUtc).ThenBy(build => build.Id);
        return await PageAsync(ordered, request, cancellationToken);
    }

    /// <summary>Approves or rejects a public build; the deciding account and the time go on its moderation record (which the audit log keeps).</summary>
    public async Task<SavedBuild> DecideAsync(string token, BuildModerationStatus decision, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var build = await db.SavedBuilds.Include(entry => entry.Moderation).SingleOrDefaultAsync(entry => entry.Token == token, cancellationToken)
            ?? throw SavedBuildStore.NotFound();
        BuildModerationRules.EnsureCanDecide(build.Visibility, decision);
        var moderation = build.Moderation ?? throw SavedBuildStore.NotFound();
        moderation.Status = decision;
        moderation.DecidedByUserId = actorId;
        moderation.DecidedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        await db.SaveChangesAsync(cancellationToken);
        return build;
    }

    private static async Task<PagedResult<SavedBuild>> PageAsync(IQueryable<SavedBuild> query, PageRequest request, CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Include(build => build.Moderation).Skip(request.Skip).Take(request.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<SavedBuild>(items, request.Page, request.PageSize, total);
    }
}
