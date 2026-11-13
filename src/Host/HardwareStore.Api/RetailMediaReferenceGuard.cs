using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Protects historical inventory-unit image snapshots from generic catalog media deletion.
/// The unit stores the catalog image keys as a JSON snapshot; this guard resolves the
/// referencing units without making Catalog depend on Retail.
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
        var rows = await db.Units.AsNoTracking()
            .Where(unit => unit.ImageStorageKeysJson != null && unit.ImageStorageKeysJson.Contains(storageKey))
            .OrderBy(unit => unit.ReceivedAtUtc)
            .Take(25)
            .Select(unit => new { unit.Id, unit.Name, unit.Status })
            .ToListAsync(cancellationToken);
        return rows.Select(unit => new MediaReferenceInfo(
            "Retail", "InventoryUnit", unit.Id,
            $"Inventory unit '{unit.Name}' ({unit.Status}) keeps this image as a historical purchase snapshot."))
            .ToList();
    }
}
