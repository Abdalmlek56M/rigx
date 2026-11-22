using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.PcBuilder;

/// <summary>
/// Saved builds. Anyone may save (the link is the key); a signed-in saver becomes the owner and is the only one who can
/// change or delete it, and the only one who can keep it private or make it public. A build saved while signed out has no owner,
/// so it can never be changed. Making a build public (or changing a public one) puts it in the moderation queue.
/// </summary>
public sealed class SavedBuildStore(IDbContextFactory<PcBuilderDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<SavedBuild> CreateAsync(Guid? userId, string? name, string? usage, IReadOnlyList<SavedBuildPart> parts, BuildVisibility? visibility,
        CancellationToken cancellationToken = default)
    {
        SavedBuildRules.EnsureParts(parts);
        var chosen = visibility ?? BuildVisibilityRules.Default;
        BuildVisibilityRules.EnsureAllowed(chosen, userId is not null);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (userId is Guid owner)
            SavedBuildRules.EnsureRoomForAnother(await db.SavedBuilds.CountAsync(build => build.UserAccountId == owner, cancellationToken));
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var build = new SavedBuild
        {
            Id = Guid.CreateVersion7(), Token = SavedBuildRules.NewToken(), UserAccountId = userId, Name = SavedBuildRules.CleanName(name),
            Usage = usage, Parts = parts.ToList(), Visibility = chosen, CreatedAtUtc = now, UpdatedAtUtc = now
        };
        Moderate(build, BuildModerationRules.AfterOwnerSave(chosen, wasPublic: false, contentChanged: true, current: null), now);
        db.SavedBuilds.Add(build);
        await db.SaveChangesAsync(cancellationToken);
        return build;
    }

    /// <summary>The build behind a link; a private build is "not found" to everyone but its owner.</summary>
    public async Task<SavedBuild> GetByTokenAsync(string token, Guid? viewerId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var build = await db.SavedBuilds.AsNoTracking().Include(entry => entry.Moderation)
            .SingleOrDefaultAsync(entry => entry.Token == token, cancellationToken);
        return build is not null && BuildVisibilityRules.CanView(build.Visibility, build.UserAccountId, viewerId) ? build : throw NotFound();
    }

    /// <summary>Changes the build; a <c>null</c> visibility keeps the current one.</summary>
    public async Task<SavedBuild> UpdateAsync(string token, Guid userId, string? name, string? usage, IReadOnlyList<SavedBuildPart> parts,
        BuildVisibility? visibility, long rowVersion, CancellationToken cancellationToken = default)
    {
        SavedBuildRules.EnsureParts(parts);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var build = await OwnedAsync(db, token, userId, cancellationToken);
        if (build.RowVersion != rowVersion) throw new PcBuilderException(409, "The build was changed elsewhere. Reload and try again.");
        var cleanName = SavedBuildRules.CleanName(name);
        var chosen = visibility ?? build.Visibility;
        var contentChanged = cleanName != build.Name || usage != build.Usage || !parts.SequenceEqual(build.Parts);
        var status = BuildModerationRules.AfterOwnerSave(chosen, build.Visibility == BuildVisibility.Public, contentChanged, build.Moderation?.Status);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        build.Name = cleanName;
        build.Usage = usage;
        build.Parts = parts.ToList();
        build.Visibility = chosen;
        build.UpdatedAtUtc = now;
        build.RowVersion++;
        Moderate(build, status, now);
        await db.SaveChangesAsync(cancellationToken);
        return build;
    }

    /// <summary>Changes only who may open the build (the owner's "private / by link / public" choice); a build made public goes to the moderation queue.</summary>
    public async Task<SavedBuild> SetVisibilityAsync(string token, Guid userId, BuildVisibility visibility, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var build = await OwnedAsync(db, token, userId, cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        Moderate(build, BuildModerationRules.AfterOwnerSave(visibility, build.Visibility == BuildVisibility.Public, contentChanged: false, build.Moderation?.Status), now);
        build.Visibility = visibility;
        build.UpdatedAtUtc = now;
        build.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return build;
    }

    public async Task DeleteAsync(string token, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.SavedBuilds.Remove(await OwnedAsync(db, token, userId, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SavedBuild>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.SavedBuilds.AsNoTracking().Include(build => build.Moderation).Where(build => build.UserAccountId == userId)
            .OrderByDescending(build => build.UpdatedAtUtc).ThenByDescending(build => build.Id).ToListAsync(cancellationToken);
    }

    /// <summary>Moves a merged account's saved builds to the account that was kept; safe to run again.</summary>
    public async Task<int> MoveOwnerAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.SavedBuilds.Where(build => build.UserAccountId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(build => build.UserAccountId, targetId), cancellationToken);
    }

    /// <summary>Puts a new status on the build's moderation record (creating it on first use); a record whose status did not change is left alone.</summary>
    private static void Moderate(SavedBuild build, BuildModerationStatus? status, DateTimeOffset now)
    {
        if (status is null) return;
        if (build.Moderation is null)
        {
            build.Moderation = new BuildModeration { SavedBuildId = build.Id, Status = status.Value, SubmittedAtUtc = now };
            return;
        }
        if (build.Moderation.Status == status) return;
        build.Moderation.Status = status.Value;
        build.Moderation.SubmittedAtUtc = now;
        build.Moderation.DecidedByUserId = null;
        build.Moderation.DecidedAtUtc = null;
    }

    private static async Task<SavedBuild> OwnedAsync(PcBuilderDbContext db, string token, Guid userId, CancellationToken cancellationToken) =>
        await db.SavedBuilds.Include(build => build.Moderation)
            .SingleOrDefaultAsync(build => build.Token == token && build.UserAccountId == userId, cancellationToken)
        ?? throw NotFound();

    internal static PcBuilderException NotFound() => new(404, "The saved build was not found.");
}
