using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Reset;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>
/// The Catalog part of the "content" reset: the media library (assets, where they are used, and their image files, trashed
/// ones included). Products, categories, brands and their photos are not touched.
/// </summary>
public sealed class ContentMediaDataReset(IDbContextFactory<CatalogDbContext> contextFactory, ICatalogMediaStorage storage) : IDataResetSource
{
    public async Task<int> ResetAsync(IReadOnlySet<string> sections, CancellationToken cancellationToken = default)
    {
        if (!sections.Contains(DataResetRules.Content)) return 0;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var keys = await db.ContentMediaAssets.IgnoreQueryFilters().Select(asset => asset.StorageKey).ToListAsync(cancellationToken);
        var deleted = await TableReset.DeleteAllAsync(db, ["content_media_asset_usages", "content_media_assets"], cancellationToken);
        foreach (var key in keys)
            await storage.DeleteAsync(key, cancellationToken);
        return deleted;
    }
}
