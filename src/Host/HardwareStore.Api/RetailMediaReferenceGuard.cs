using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Protects stock item and build photos from generic catalog media deletion: an image that a stock item or build shows
/// cannot be removed from the media library. Catalog does not depend on Retail, so this guard resolves the references.
/// </summary>
public sealed class RetailMediaReferenceGuard(IDbContextFactory<RetailDbContext> contextFactory)
    : IMediaReferenceGuard
{
    public async Task<IReadOnlyList<MediaReferenceInfo>> FindReferencesAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return [];
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.StockItems.FromSql($"SELECT * FROM stock_items WHERE strpos(photos::text, {storageKey}) > 0")
            .AsNoTracking().OrderBy(item => item.CreatedAtUtc).Take(25)
            .Select(item => new { item.Id, item.Code }).ToListAsync(cancellationToken);
        var builds = await db.Assemblies.FromSql($"SELECT * FROM assemblies WHERE strpos(photos::text, {storageKey}) > 0")
            .AsNoTracking().OrderBy(build => build.CreatedAtUtc).Take(25)
            .Select(build => new { build.Id, build.Code }).ToListAsync(cancellationToken);
        return rows.Select(item => new MediaReferenceInfo("Retail", "StockItem", item.Id, $"Stock item '{item.Code}' uses this image."))
            .Concat(builds.Select(build => new MediaReferenceInfo("Retail", "Assembly", build.Id, $"Build '{build.Code}' uses this image.")))
            .ToList();
    }
}
