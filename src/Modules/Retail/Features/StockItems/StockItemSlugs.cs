using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Gives a stock item its unique readable slug (<see cref="StockItemSlug"/>) when it is created or renamed.</summary>
internal static class StockItemSlugs
{
    /// <summary>
    /// Sets <see cref="StockItem.Slug"/> from the model name. A rename that keeps the same base keeps the current slug
    /// (links stay valid). Items added to <paramref name="db"/> but not saved yet count as taken.
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
        item.Slug = StockItemSlug.FirstFree(baseSlug, stored.Concat(pending).ToHashSet(StringComparer.Ordinal));
    }
}
