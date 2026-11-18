using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Gives a stock item its unique readable slug (<see cref="StockItemSlug"/>) when it is created or renamed.</summary>
internal static class StockItemSlugs
{
    /// <summary>
    /// Sets <see cref="StockItem.Slug"/> from the model name. A rename that keeps the same base keeps the current slug.
    /// A rename that changes it keeps the old slug as a <see cref="StockItemOldSlug"/> so published links still lead here;
    /// other items' old slugs are never handed out. Items added to <paramref name="db"/> but not saved yet count as taken.
    /// </summary>
    public static async Task AssignAsync(RetailDbContext db, StockItem item, CancellationToken cancellationToken)
    {
        var baseSlug = StockItemSlug.Base(item.ModelName);
        if (StockItemSlug.BelongsTo(item.Slug, baseSlug)) return;
        var prefix = baseSlug + "-";
        var stored = await db.StockItems.AsNoTracking()
            .Where(other => other.Id != item.Id && (other.Slug == baseSlug || other.Slug.StartsWith(prefix)))
            .Select(other => other.Slug).ToListAsync(cancellationToken);
        var pending = db.StockItems.Local.Where(other => other.Id != item.Id).Select(other => other.Slug);
        var oldSlugs = await db.OldSlugs
            .Where(old => old.Slug == baseSlug || old.Slug.StartsWith(prefix)).ToListAsync(cancellationToken);
        var slug = StockItemSlug.Choose(baseSlug, item.Id, stored.Concat(pending), oldSlugs);
        if (oldSlugs.SingleOrDefault(old => old.Slug == slug) is { } reclaimed) db.OldSlugs.Remove(reclaimed);
        if (item.Slug.Length > 0) db.OldSlugs.Add(new StockItemOldSlug { Slug = item.Slug, StockItemId = item.Id });
        item.Slug = slug;
    }
}
