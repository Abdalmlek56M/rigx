using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Storefront;

/// <summary>
/// The watermark to draw on a new upload, from the store settings: null when it is off or no logo was uploaded.
/// The logo is the watermark logo, else the store logo.
/// </summary>
public sealed class MediaWatermarkSource(StoreSettingsService settings, ICatalogMediaStorage storage)
{
    public Task<ImageWatermark?> ForPhotosAsync(CancellationToken cancellationToken) => LoadAsync(articles: false, cancellationToken);

    /// <summary>Articles are watermarked only when the owner asked for it (<c>ApplyToArticles</c>).</summary>
    public Task<ImageWatermark?> ForArticlesAsync(CancellationToken cancellationToken) => LoadAsync(articles: true, cancellationToken);

    private async Task<ImageWatermark?> LoadAsync(bool articles, CancellationToken cancellationToken)
    {
        var view = await settings.GetAdminAsync(cancellationToken);
        var watermark = view.Watermark;
        if (!watermark.Enabled || (articles && !watermark.ApplyToArticles)) return null;
        var key = view.Logos.WatermarkSourceKey;
        if (key is null || !storage.Exists(key)) return null;
        await using var stream = storage.OpenRead(key);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return new ImageWatermark(buffer.ToArray(), watermark.Position, watermark.OpacityPercent, watermark.SizePercent);
    }
}
