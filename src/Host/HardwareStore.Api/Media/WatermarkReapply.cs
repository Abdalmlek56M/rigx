using System.Threading.Channels;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;

/// <summary>What one re-apply run did: photos swapped, photos without a kept original, photos that could not be swapped.</summary>
public sealed record WatermarkReapplyResult(int Replaced, int Skipped, int Failed);

/// <summary>
/// Re-renders every stock item and build photo that still has its original with the watermark settings of now (so turning the watermark
/// off and re-applying removes it). New files get new keys because public media is cached as immutable; the old files stay for orders
/// that keep their thumbnail, and the orphan cleanup removes them later.
/// </summary>
public sealed class WatermarkReapplyService(StockItemAdminService items, AssemblyService assemblies, ICatalogMediaStorage storage,
    MediaWatermarkSource watermarkSource, ILogger<WatermarkReapplyService> logger)
{
    public async Task<WatermarkReapplyResult> RunAsync(CancellationToken cancellationToken)
    {
        var watermark = await watermarkSource.ForPhotosAsync(cancellationToken);
        var counts = new int[3];
        foreach (var owned in await items.ListPhotosAsync(cancellationToken))
            counts[(int)await ReapplyAsync(owned, watermark, items.ReplacePhotoAsync, cancellationToken)]++;
        foreach (var owned in await assemblies.ListPhotosAsync(cancellationToken))
            counts[(int)await ReapplyAsync(owned, watermark, assemblies.ReplacePhotoAsync, cancellationToken)]++;
        return new WatermarkReapplyResult(counts[(int)Outcome.Replaced], counts[(int)Outcome.Skipped], counts[(int)Outcome.Failed]);
    }

    private enum Outcome { Replaced, Skipped, Failed }

    private async Task<Outcome> ReapplyAsync(OwnedPhoto owned, ImageWatermark? watermark,
        Func<Guid, string, StockItemPhoto, CancellationToken, Task<bool>> replace, CancellationToken cancellationToken)
    {
        var originalKey = CatalogMediaKeys.OriginalKeyFor(owned.Photo.StorageKey);
        if (!storage.Exists(originalKey)) return Outcome.Skipped;
        CatalogStoredMedia? stored = null;
        try
        {
            await using var original = storage.OpenRead(originalKey);
            stored = await storage.SaveImageAsync(owned.OwnerId, Guid.CreateVersion7(),
                new CatalogImageUpload(original, original.Length, null, null), watermark, cancellationToken);
            if (await replace(owned.OwnerId, owned.Photo.StorageKey, new StockItemPhoto(stored.StorageKey, stored.ThumbnailStorageKey), cancellationToken))
                return Outcome.Replaced;
            await storage.DeleteAsync(stored.StorageKey, cancellationToken);
            return Outcome.Skipped;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Watermark re-apply failed for photo {StorageKey}.", owned.Photo.StorageKey);
            if (stored is not null) await storage.DeleteAsync(stored.StorageKey, CancellationToken.None);
            return Outcome.Failed;
        }
    }
}

/// <summary>
/// Runs <see cref="WatermarkReapplyService"/> in the background. One worker; signals that arrive while a run is queued are merged into
/// it, and a signal during a run schedules exactly one more run. No polling: the worker sleeps on the channel.
/// </summary>
public sealed class WatermarkReapplyJob(IServiceScopeFactory scopeFactory, ILogger<WatermarkReapplyJob> logger) : BackgroundService
{
    private readonly Channel<bool> signals = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });

    public void Signal() => signals.Writer.TryWrite(true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var _ in signals.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var result = await scope.ServiceProvider.GetRequiredService<WatermarkReapplyService>().RunAsync(stoppingToken);
                logger.LogInformation("Watermark re-apply: {Replaced} replaced, {Skipped} skipped, {Failed} failed.",
                    result.Replaced, result.Skipped, result.Failed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Watermark re-apply failed."); }
        }
    }
}
