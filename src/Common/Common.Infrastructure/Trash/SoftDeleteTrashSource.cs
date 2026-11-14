using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Common.Infrastructure.Trash;

/// <summary>
/// The shared trash mechanics of one soft-deletable entity: list, restore, purge and purge-expired. A module's source
/// supplies the kind, the label, its 404 and, when needed, the checks around restore and the cleanup around purge.
/// </summary>
public abstract class SoftDeleteTrashSource<TContext, TEntity>(IDbContextFactory<TContext> contextFactory)
    : ITrashSource
    where TContext : DbContext
    where TEntity : class, ISoftDeletable
{
    private const int MaxListed = 500;

    public abstract string Kind { get; }

    protected abstract DbSet<TEntity> Set(TContext db);

    protected abstract string Label(TEntity item);

    protected abstract AppException NotFound();

    /// <summary>Throws (for example a 409) when the item cannot come back.</summary>
    protected virtual Task EnsureCanRestoreAsync(TContext db, TEntity item, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task AfterRestoreAsync(TEntity item, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Runs before the row is removed (for example deletes files). Child rows are removed by the database.</summary>
    protected virtual Task BeforePurgeAsync(TEntity item, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<IReadOnlyList<TrashItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await Trashed(db).AsNoTracking().OrderByDescending(item => item.DeletedAtUtc).Take(MaxListed)
            .ToListAsync(cancellationToken);
        return rows.Select(item => new TrashItem(item.Id, Label(item), item.DeletedAtUtc!.Value, item.DeletedByUserId)).ToList();
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await Trashed(db).SingleOrDefaultAsync(row => row.Id == id, cancellationToken) ?? throw NotFound();
        await EnsureCanRestoreAsync(db, item, cancellationToken);
        TrashRules.Restore(item);
        await db.SaveChangesAsync(cancellationToken);
        await AfterRestoreAsync(item, cancellationToken);
    }

    public async Task PurgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await Trashed(db).SingleOrDefaultAsync(row => row.Id == id, cancellationToken) ?? throw NotFound();
        await RemoveAsync(db, item, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> PurgeExpiredAsync(DateTimeOffset before, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var expired = await Trashed(db).Where(item => item.DeletedAtUtc < before).ToListAsync(cancellationToken);
        foreach (var item in expired) await RemoveAsync(db, item, cancellationToken);
        return expired.Select(item => item.Id).ToList();
    }

    private async Task RemoveAsync(TContext db, TEntity item, CancellationToken cancellationToken)
    {
        await BeforePurgeAsync(item, cancellationToken);
        Set(db).Remove(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TEntity> Trashed(TContext db) =>
        Set(db).IgnoreQueryFilters().Where(item => item.DeletedAtUtc != null);
}
