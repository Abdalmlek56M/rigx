using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>
/// Product gallery: upload (first image becomes primary), edit metadata, delete (blocked while historical
/// records reference the file; the next image is promoted to primary).
/// </summary>
public sealed class ProductImageService(
    IDbContextFactory<CatalogDbContext> contextFactory,
    ICatalogMediaStorage mediaStorage,
    TimeProvider timeProvider,
    IEnumerable<IMediaReferenceGuard> referenceGuards)
{
    public async Task<ProductImageView> AddImageAsync(
        Guid productId,
        string? altText,
        bool isPrimary,
        CatalogImageUpload image,
        CancellationToken cancellationToken = default)
    {
        var normalizedAltText = CatalogValidation.Required(altText, "altText", 300);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Products.AnyAsync(product => product.Id == productId, cancellationToken))
            throw new CatalogNotFoundException("Product was not found.");

        var imageId = Guid.CreateVersion7();
        var stored = await mediaStorage.SaveImageAsync(
            productId, imageId, image, cancellationToken: cancellationToken);
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var hasPrimary = await db.ProductImages.AnyAsync(
                item => item.ProductId == productId && item.IsPrimary, cancellationToken);
            var makePrimary = isPrimary || !hasPrimary;
            if (makePrimary)
            {
                await db.ProductImages
                    .Where(item => item.ProductId == productId && item.IsPrimary)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsPrimary, false), cancellationToken);
            }
            var sortOrder = await db.ProductImages
                .Where(item => item.ProductId == productId)
                .Select(item => (int?)item.SortOrder)
                .MaxAsync(cancellationToken) ?? -1;
            var imageEntity = new CatalogProductImage
            {
                Id = imageId,
                ProductId = productId,
                StorageKey = stored.StorageKey,
                ThumbnailStorageKey = stored.ThumbnailStorageKey,
                OriginalStorageKey = stored.OriginalStorageKey,
                AltText = normalizedAltText,
                FileSizeBytes = stored.FileSizeBytes,
                Width = stored.Width,
                Height = stored.Height,
                ThumbnailWidth = stored.ThumbnailWidth,
                ThumbnailHeight = stored.ThumbnailHeight,
                SortOrder = sortOrder + 1,
                IsPrimary = makePrimary,
                CreatedAtUtc = UtcNow()
            };
            db.ProductImages.Add(imageEntity);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ProductImageView(
                imageEntity.Id, imageEntity.StorageKey, imageEntity.ThumbnailStorageKey,
                imageEntity.AltText, imageEntity.SortOrder, imageEntity.IsPrimary,
                imageEntity.Width, imageEntity.Height,
                imageEntity.ThumbnailWidth, imageEntity.ThumbnailHeight);
        }
        catch
        {
            await mediaStorage.DeleteAsync(stored.StorageKey, CancellationToken.None);
            throw;
        }
    }

    public async Task<ProductImageView> UpdateImageAsync(
        Guid productId,
        Guid imageId,
        string? altText,
        int sortOrder,
        bool isPrimary,
        CancellationToken cancellationToken = default)
    {
        if (sortOrder < 0)
            throw new CatalogValidationException("sortOrder", "Sort order must not be negative.");
        var normalizedAltText = CatalogValidation.Required(altText, "altText", 300);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var image = await db.ProductImages.SingleOrDefaultAsync(
            item => item.Id == imageId && item.ProductId == productId, cancellationToken)
            ?? throw new CatalogNotFoundException("Product image was not found.");
        if (isPrimary)
        {
            await db.ProductImages
                .Where(item => item.ProductId == productId && item.IsPrimary && item.Id != imageId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsPrimary, false), cancellationToken);
        }
        image.AltText = normalizedAltText;
        image.SortOrder = sortOrder;
        image.IsPrimary = isPrimary;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new ProductImageView(
            image.Id, image.StorageKey, image.ThumbnailStorageKey,
            image.AltText, image.SortOrder, image.IsPrimary,
            image.Width, image.Height, image.ThumbnailWidth, image.ThumbnailHeight);
    }

    public async Task DeleteImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var image = await db.ProductImages.SingleOrDefaultAsync(
            item => item.Id == imageId && item.ProductId == productId, cancellationToken)
            ?? throw new CatalogNotFoundException("Product image was not found.");
        foreach (var guard in referenceGuards)
        {
            var references = await guard.FindReferencesAsync(image.StorageKey, cancellationToken);
            if (references.Count > 0)
                throw new CatalogValidationException("image",
                    "This image is kept by historical inventory or operational records and cannot be deleted.");
        }
        var wasPrimary = image.IsPrimary;
        db.ProductImages.Remove(image);
        await db.SaveChangesAsync(cancellationToken);
        await mediaStorage.DeleteAsync(image.StorageKey, cancellationToken);
        if (wasPrimary)
        {
            var replacement = await db.ProductImages
                .Where(item => item.ProductId == productId)
                .OrderBy(item => item.SortOrder).ThenBy(item => item.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (replacement is not null)
            {
                replacement.IsPrimary = true;
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private DateTimeOffset UtcNow() => timeProvider.GetUtcNow().ToUniversalTime();
}
