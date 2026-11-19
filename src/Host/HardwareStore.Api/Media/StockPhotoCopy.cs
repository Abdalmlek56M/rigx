using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;

/// <summary>
/// "نسخ من منتج موجود": gives new stock items their own copies of another item's photos, so removing a photo from one never
/// touches the other. A copy is rendered again from the kept original with the watermark settings of now (like a fresh upload);
/// a photo without an original is copied from its stored image as is, since that image already carries any watermark.
/// </summary>
public sealed class StockPhotoCopyService(StockItemAdminService items, ICatalogMediaStorage storage, MediaWatermarkSource watermarkSource,
    ILogger<StockPhotoCopyService> logger)
{
    public async Task CopyAsync(Guid sourceItemId, IReadOnlyList<Guid> targetItemIds, CancellationToken cancellationToken)
    {
        var photos = await items.FindPhotosAsync(sourceItemId, cancellationToken);
        if (photos.Count == 0 || targetItemIds.Count == 0) return;
        var watermark = await watermarkSource.ForPhotosAsync(cancellationToken);
        foreach (var targetId in targetItemIds)
            foreach (var photo in photos)
                await CopyOneAsync(photo, targetId, watermark, cancellationToken);
    }

    private async Task CopyOneAsync(StockItemPhoto photo, Guid targetId, ImageWatermark? watermark, CancellationToken cancellationToken)
    {
        var originalKey = CatalogMediaKeys.OriginalKeyFor(photo.StorageKey);
        var fromOriginal = storage.Exists(originalKey);
        var sourceKey = fromOriginal ? originalKey : photo.StorageKey;
        if (!storage.Exists(sourceKey)) return;
        CatalogStoredMedia? stored = null;
        try
        {
            await using var content = storage.OpenRead(sourceKey);
            stored = await storage.SaveImageAsync(targetId, Guid.CreateVersion7(), new CatalogImageUpload(content, content.Length, null, null),
                fromOriginal ? watermark : null, cancellationToken);
            await items.AddPhotoAsync(targetId, new StockItemPhoto(stored.StorageKey, stored.ThumbnailStorageKey), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The stock is already received; a photo that cannot be copied is logged and left for the owner to add by hand.
            logger.LogWarning(exception, "Copying photo {StorageKey} to stock item {ItemId} failed.", photo.StorageKey, targetId);
            if (stored is not null) await storage.DeleteAsync(stored.StorageKey, CancellationToken.None);
        }
    }
}
