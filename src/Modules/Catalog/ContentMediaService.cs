using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

public sealed record ContentMediaAssetView(
    Guid Id,
    string TitleAr,
    string TitleEn,
    string AltAr,
    string AltEn,
    string? CaptionAr,
    string? CaptionEn,
    IReadOnlyList<string> Tags,
    string StorageKey,
    string ThumbnailStorageKey,
    long FileSizeBytes,
    int Width,
    int Height,
    int ThumbnailWidth,
    int ThumbnailHeight,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion);

public sealed record ContentMediaAssetPage(
    IReadOnlyList<ContentMediaAssetView> Items,
    int Page,
    int PageSize,
    int Total);

public sealed record ContentMediaMetadataInput(
    string? TitleAr,
    string? TitleEn,
    string? AltAr,
    string? AltEn,
    string? CaptionAr,
    string? CaptionEn,
    string? Tags,
    long RowVersion);

public sealed record ContentMediaUsageView(
    string ReferenceType,
    Guid ReferenceId,
    string ReferenceTitle,
    bool IsPublished,
    DateTimeOffset UpdatedAtUtc);

public interface IContentMediaUsageSink
{
    Task RegisterAsync(
        string referenceType,
        Guid referenceId,
        string referenceTitle,
        string? storageKey,
        bool isPublished,
        CancellationToken cancellationToken = default);

    Task ClearAsync(
        string referenceType,
        Guid referenceId,
        CancellationToken cancellationToken = default);
}

public sealed class ContentMediaService(
    IDbContextFactory<CatalogDbContext> contextFactory,
    ICatalogMediaStorage storage,
    TimeProvider timeProvider,
    IEnumerable<IMediaReferenceGuard> referenceGuards) : IContentMediaUsageSink
{
    public async Task<ContentMediaAssetPage> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 60);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = db.ContentMediaAssets.AsNoTracking().Where(asset => !asset.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            rows = rows.Where(asset =>
                EF.Functions.ILike(asset.TitleAr, pattern) ||
                EF.Functions.ILike(asset.TitleEn, pattern) ||
                EF.Functions.ILike(asset.AltAr, pattern) ||
                EF.Functions.ILike(asset.AltEn, pattern) ||
                EF.Functions.ILike(asset.TagsCsv, pattern));
        }
        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(asset => asset.UpdatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new ContentMediaAssetPage(items.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<ContentMediaAssetView> UploadAsync(
        CatalogImageUpload upload,
        ContentMediaMetadataInput metadata,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(metadata);
        var id = Guid.CreateVersion7();
        var stored = await storage.SaveContentImageAsync(id, upload, replace: false, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var asset = new ContentMediaAsset
        {
            Id = id,
            TitleAr = normalized.TitleAr,
            TitleEn = normalized.TitleEn,
            AltAr = normalized.AltAr,
            AltEn = normalized.AltEn,
            CaptionAr = normalized.CaptionAr,
            CaptionEn = normalized.CaptionEn,
            TagsCsv = string.Join(",", normalized.Tags),
            StorageKey = stored.StorageKey,
            ThumbnailStorageKey = stored.ThumbnailStorageKey,
            OriginalStorageKey = stored.OriginalStorageKey,
            FileSizeBytes = stored.FileSizeBytes,
            Width = stored.Width,
            Height = stored.Height,
            ThumbnailWidth = stored.ThumbnailWidth,
            ThumbnailHeight = stored.ThumbnailHeight,
            UploadedByUserAccountId = actorId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.ContentMediaAssets.Add(asset);
        await db.SaveChangesAsync(cancellationToken);
        return Map(asset);
    }

    public async Task<ContentMediaAssetView> UpdateMetadataAsync(
        Guid id,
        ContentMediaMetadataInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var asset = await FindAsync(db, id, cancellationToken);
        if (asset.RowVersion != input.RowVersion)
            throw new CatalogValidationException("asset", "The media asset was changed by another user.");
        asset.TitleAr = normalized.TitleAr;
        asset.TitleEn = normalized.TitleEn;
        asset.AltAr = normalized.AltAr;
        asset.AltEn = normalized.AltEn;
        asset.CaptionAr = normalized.CaptionAr;
        asset.CaptionEn = normalized.CaptionEn;
        asset.TagsCsv = string.Join(",", normalized.Tags);
        asset.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        asset.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(asset);
    }

    public async Task<ContentMediaAssetView> ReplaceImageAsync(
        Guid id,
        CatalogImageUpload upload,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using (var db = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            var existing = await FindAsync(db, id, cancellationToken);
            if (existing.RowVersion != rowVersion)
                throw new CatalogValidationException("asset", "The media asset was changed by another user.");
        }
        var stored = await storage.SaveContentImageAsync(id, upload, replace: true, cancellationToken);
        await using var updateDb = await contextFactory.CreateDbContextAsync(cancellationToken);
        var asset = await FindAsync(updateDb, id, cancellationToken);
        asset.FileSizeBytes = stored.FileSizeBytes;
        asset.Width = stored.Width;
        asset.Height = stored.Height;
        asset.ThumbnailWidth = stored.ThumbnailWidth;
        asset.ThumbnailHeight = stored.ThumbnailHeight;
        asset.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        asset.RowVersion++;
        await updateDb.SaveChangesAsync(cancellationToken);
        return Map(asset);
    }

    public async Task DeleteAsync(
        Guid id,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var asset = await FindAsync(db, id, cancellationToken);
        if (asset.RowVersion != rowVersion)
            throw new CatalogValidationException("asset", "The media asset was changed by another user.");
        if (await db.ContentMediaAssetUsages.AnyAsync(
                usage => usage.AssetId == id && usage.IsPublished, cancellationToken))
            throw new ContentMediaInUseException();
        var references = await FindGuardReferencesAsync(asset.StorageKey, cancellationToken);
        if (references.Count > 0)
            throw new ContentMediaInUseException(
                $"The media asset is held by historical or operational records: {string.Join("; ", references.Take(3).Select(Describe))}.");
        asset.IsDeleted = true;
        asset.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        asset.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContentMediaUsageView>> ListUsagesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var asset = await FindAsync(db, id, cancellationToken);
        var usages = await db.ContentMediaAssetUsages.AsNoTracking()
            .Where(usage => usage.AssetId == id)
            .OrderByDescending(usage => usage.IsPublished)
            .ThenBy(usage => usage.ReferenceType)
            .Select(usage => new ContentMediaUsageView(
                usage.ReferenceType,
                usage.ReferenceId,
                usage.ReferenceTitle,
                usage.IsPublished,
                usage.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
        var guardViews = (await FindGuardReferencesAsync(asset.StorageKey, cancellationToken))
            .Select(reference => new ContentMediaUsageView(
                $"{reference.Module}.{reference.EntityType}",
                reference.EntityId,
                reference.Description,
                true,
                timeProvider.GetUtcNow().ToUniversalTime()))
            .ToList();
        return [.. usages, .. guardViews];
    }

    public async Task RegisterAsync(
        string referenceType,
        Guid referenceId,
        string referenceTitle,
        string? storageKey,
        bool isPublished,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.ContentMediaAssetUsages
                .Where(usage => usage.ReferenceType == referenceType && usage.ReferenceId == referenceId)
                .ExecuteDeleteAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(storageKey))
            {
                var assetId = await db.ContentMediaAssets.AsNoTracking()
                    .Where(asset => asset.StorageKey == storageKey && !asset.IsDeleted)
                    .Select(asset => (Guid?)asset.Id)
                    .SingleOrDefaultAsync(cancellationToken);
                if (assetId is not null)
                {
                    db.ContentMediaAssetUsages.Add(new ContentMediaAssetUsage
                    {
                        Id = Guid.CreateVersion7(),
                        AssetId = assetId.Value,
                        ReferenceType = referenceType,
                        ReferenceId = referenceId,
                        ReferenceTitle = referenceTitle.Length <= 220
                            ? referenceTitle
                            : referenceTitle[..220],
                        IsPublished = isPublished,
                        UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
                    });
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task ClearAsync(
        string referenceType,
        Guid referenceId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.ContentMediaAssetUsages
            .Where(usage => usage.ReferenceType == referenceType && usage.ReferenceId == referenceId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task<ContentMediaAsset> FindAsync(
        CatalogDbContext db,
        Guid id,
        CancellationToken cancellationToken) =>
        await db.ContentMediaAssets.SingleOrDefaultAsync(
            asset => asset.Id == id && !asset.IsDeleted, cancellationToken)
        ?? throw new ContentMediaNotFoundException();

    private async Task<IReadOnlyList<MediaReferenceInfo>> FindGuardReferencesAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        var references = new List<MediaReferenceInfo>();
        foreach (var guard in referenceGuards)
            references.AddRange(await guard.FindReferencesAsync(storageKey, cancellationToken));
        return references;
    }

    private static string Describe(MediaReferenceInfo reference) =>
        $"{reference.Module}/{reference.EntityType} #{reference.EntityId:N} {reference.Description}";

    private static NormalizedMedia Normalize(ContentMediaMetadataInput input) => new(
        MediaText.Required(input.TitleAr, 200, "Arabic title", fallback: input.TitleEn),
        MediaText.Required(input.TitleEn, 200, "English title", fallback: input.TitleAr),
        MediaText.Required(input.AltAr, 300, "Arabic alt text", fallback: input.AltEn),
        MediaText.Required(input.AltEn, 300, "English alt text", fallback: input.AltAr),
        MediaText.Optional(input.CaptionAr, 500),
        MediaText.Optional(input.CaptionEn, 500),
        MediaText.Tags(input.Tags));

    private static ContentMediaAssetView Map(ContentMediaAsset asset) => new(
        asset.Id,
        asset.TitleAr,
        asset.TitleEn,
        asset.AltAr,
        asset.AltEn,
        asset.CaptionAr,
        asset.CaptionEn,
        asset.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries),
        asset.StorageKey,
        asset.ThumbnailStorageKey,
        asset.FileSizeBytes,
        asset.Width,
        asset.Height,
        asset.ThumbnailWidth,
        asset.ThumbnailHeight,
        asset.CreatedAtUtc,
        asset.UpdatedAtUtc,
        asset.RowVersion);

    private sealed record NormalizedMedia(
        string TitleAr,
        string TitleEn,
        string AltAr,
        string AltEn,
        string? CaptionAr,
        string? CaptionEn,
        IReadOnlyList<string> Tags);
}

public sealed class ContentMediaNotFoundException()
    : Exception("The media asset was not found.");

public sealed class ContentMediaInUseException(string message =
        "The media asset is used by published content and cannot be deleted.")
    : Exception(message);

internal static class MediaText
{
    public static string Required(string? value, int max, string field, string? fallback = null)
    {
        var effective = string.IsNullOrWhiteSpace(value) ? fallback : value;
        if (string.IsNullOrWhiteSpace(effective))
            throw new CatalogValidationException(field, $"{field} is required.");
        return Optional(effective, max) ?? throw new CatalogValidationException(field, $"{field} is required.");
    }

    public static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var text = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (text.Length > max)
            throw new CatalogValidationException("media", "A media field is too long.");
        if (text.Contains('<') || text.Contains('>'))
            throw new CatalogValidationException("media", "Media text must be plain text without HTML.");
        return text;
    }

    public static IReadOnlyList<string> Tags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];
        var tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
        if (tags.Any(tag => tag.Length > 32))
            throw new CatalogValidationException("media", "Each tag must be 32 characters or fewer.");
        return tags;
    }
}
