using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.PcBuilder;

/// <summary>
/// Saved builds. Anyone may save (the link is the key); a signed-in saver becomes the owner and is the only one who can
/// change or delete it. A build saved while signed out has no owner, so it can never be changed.
/// </summary>
public sealed class SavedBuildStore(IDbContextFactory<PcBuilderDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<SavedBuild> CreateAsync(Guid? userId, string? name, string? usage, IReadOnlyList<SavedBuildPart> parts,
        CancellationToken cancellationToken = default)
    {
        SavedBuildRules.EnsureParts(parts);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (userId is Guid owner)
            SavedBuildRules.EnsureRoomForAnother(await db.SavedBuilds.CountAsync(build => build.UserAccountId == owner, cancellationToken));
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var build = new SavedBuild
        {
            Id = Guid.CreateVersion7(), Token = SavedBuildRules.NewToken(), UserAccountId = userId, Name = SavedBuildRules.CleanName(name),
            Usage = usage, Parts = parts.ToList(), CreatedAtUtc = now, UpdatedAtUtc = now
        };
        db.SavedBuilds.Add(build);
        await db.SaveChangesAsync(cancellationToken);
        return build;
    }

    public async Task<SavedBuild> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.SavedBuilds.AsNoTracking().SingleOrDefaultAsync(build => build.Token == token, cancellationToken) ?? throw NotFound();
    }

    public async Task<SavedBuild> UpdateAsync(string token, Guid userId, string? name, string? usage, IReadOnlyList<SavedBuildPart> parts,
        long rowVersion, CancellationToken cancellationToken = default)
    {
        SavedBuildRules.EnsureParts(parts);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var build = await OwnedAsync(db, token, userId, cancellationToken);
        if (build.RowVersion != rowVersion) throw new PcBuilderException(409, "The build was changed elsewhere. Reload and try again.");
        build.Name = SavedBuildRules.CleanName(name);
        build.Usage = usage;
        build.Parts = parts.ToList();
        build.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
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
        return await db.SavedBuilds.AsNoTracking().Where(build => build.UserAccountId == userId)
            .OrderByDescending(build => build.UpdatedAtUtc).ThenByDescending(build => build.Id).ToListAsync(cancellationToken);
    }

    /// <summary>Moves a merged account's saved builds to the account that was kept; safe to run again.</summary>
    public async Task<int> MoveOwnerAsync(Guid sourceId, Guid targetId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.SavedBuilds.Where(build => build.UserAccountId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(build => build.UserAccountId, targetId), cancellationToken);
    }

    private static async Task<SavedBuild> OwnedAsync(PcBuilderDbContext db, string token, Guid userId, CancellationToken cancellationToken) =>
        await db.SavedBuilds.SingleOrDefaultAsync(build => build.Token == token && build.UserAccountId == userId, cancellationToken)
        ?? throw NotFound();

    private static PcBuilderException NotFound() => new(404, "The saved build was not found.");
}
