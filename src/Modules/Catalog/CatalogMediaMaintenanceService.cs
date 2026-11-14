using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

public sealed record CatalogMediaAuditView(
    int ReferencedKeyCount,
    int StoredFileCount,
    IReadOnlyList<string> MissingStorageKeys,
    IReadOnlyList<string> OrphanStorageKeys);

public sealed record CatalogMediaCleanupView(
    CatalogMediaAuditView Audit,
    IReadOnlyList<string> DeletedStorageKeys);

public sealed class CatalogMediaMaintenanceService(
    IDbContextFactory<CatalogDbContext> contextFactory,
    ICatalogMediaStorage storage,
    CatalogMediaOptions options,
    TimeProvider timeProvider,
    IEnumerable<IMediaReferenceGuard> referenceGuards)
{
    public async Task<CatalogMediaAuditView> AuditAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var referenced = await GetReferencedKeysAsync(db, cancellationToken);
        var files = await storage.ListFilesAsync(cancellationToken);
        return Evaluate(referenced, files, timeProvider.GetUtcNow(), options.OrphanGracePeriod);
    }

    public async Task<CatalogMediaCleanupView> CleanupOrphansAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var referenced = await GetReferencedKeysAsync(db, cancellationToken);
        var files = await storage.ListFilesAsync(cancellationToken);
        var audit = Evaluate(referenced, files, timeProvider.GetUtcNow(), options.OrphanGracePeriod);
        var deleted = new List<string>();
        foreach (var storageKey in audit.OrphanStorageKeys)
        {
            // The initial scan is only a candidate list. Re-read the live references immediately
            // before each delete so a file referenced after the scan is preserved.
            if (await IsReferencedAsync(db, storageKey, cancellationToken))
                continue;
            await storage.DeleteStorageKeyAsync(storageKey, cancellationToken);
            deleted.Add(storageKey);
        }
        return new CatalogMediaCleanupView(audit, deleted);
    }

    public static CatalogMediaAuditView Evaluate(
        IReadOnlyCollection<string> referencedKeys,
        IReadOnlyCollection<StoredMediaFile> storedFiles,
        DateTimeOffset now,
        TimeSpan orphanGracePeriod)
    {
        var referenced = referencedKeys.ToHashSet(StringComparer.Ordinal);
        var files = storedFiles.ToDictionary(file => file.StorageKey, StringComparer.Ordinal);
        var missing = referenced.Where(key => !files.ContainsKey(key))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var oldestEligibleOrphan = now - orphanGracePeriod;
        var orphans = files.Values
            .Where(file => !referenced.Contains(file.StorageKey) &&
                           file.LastModifiedUtc < oldestEligibleOrphan &&
                           !CatalogMediaKeys.IsPrivate(file.StorageKey) &&
                           !file.StorageKey.StartsWith(CatalogMediaKeys.SettingsPrefix, StringComparison.OrdinalIgnoreCase) &&
                           (file.StorageKey.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                            file.StorageKey.EndsWith(".original", StringComparison.OrdinalIgnoreCase)))
            .Select(file => file.StorageKey)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return new CatalogMediaAuditView(referenced.Count, files.Count, missing, orphans);
    }

    private async Task<bool> IsReferencedAsync(
        CatalogDbContext db,
        string storageKey,
        CancellationToken cancellationToken)
    {
        if (await db.ProductImages.AsNoTracking().AnyAsync(
                image => image.StorageKey == storageKey ||
                         image.ThumbnailStorageKey == storageKey ||
                         image.OriginalStorageKey == storageKey,
                cancellationToken))
            return true;
        if (await db.Categories.AsNoTracking().AnyAsync(
                category => category.IconStorageKey == storageKey,
                cancellationToken))
            return true;
        if (await db.Brands.AsNoTracking().AnyAsync(
                brand => brand.LogoStorageKey == storageKey,
                cancellationToken))
            return true;
        foreach (var guard in referenceGuards)
        {
            if ((await guard.FindReferencesAsync(storageKey, cancellationToken)).Count > 0)
                return true;
        }
        return false;
    }

    private static async Task<HashSet<string>> GetReferencedKeysAsync(
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        var references = await db.ProductImages.AsNoTracking()
            .Select(image => new { image.StorageKey, image.ThumbnailStorageKey, image.OriginalStorageKey })
            .ToListAsync(cancellationToken);
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reference in references)
        {
            result.Add(reference.StorageKey);
            result.Add(reference.ThumbnailStorageKey);
            if (reference.OriginalStorageKey is not null)
                result.Add(reference.OriginalStorageKey);
        }
        foreach (var key in await db.Categories.AsNoTracking()
                     .Where(category => category.IconStorageKey != null)
                     .Select(category => category.IconStorageKey!)
                     .ToListAsync(cancellationToken))
            result.Add(key);
        foreach (var key in await db.Brands.AsNoTracking()
                     .Where(brand => brand.LogoStorageKey != null)
                     .Select(brand => brand.LogoStorageKey!)
                     .ToListAsync(cancellationToken))
            result.Add(key);
        return result;
    }
}
