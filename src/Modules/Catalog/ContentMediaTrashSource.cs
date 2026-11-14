using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Trash;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

/// <summary>Trashed media-library assets. Purging also deletes the image files (full, thumbnail and original).</summary>
public sealed class ContentMediaTrashSource(IDbContextFactory<CatalogDbContext> contextFactory, ICatalogMediaStorage storage)
    : SoftDeleteTrashSource<CatalogDbContext, ContentMediaAsset>(contextFactory)
{
    public const string TrashKind = "media-asset";

    public override string Kind => TrashKind;

    protected override DbSet<ContentMediaAsset> Set(CatalogDbContext db) => db.ContentMediaAssets;

    protected override string Label(ContentMediaAsset item) => string.IsNullOrWhiteSpace(item.TitleAr) ? item.TitleEn : item.TitleAr;

    protected override AppException NotFound() => new ContentMediaNotFoundException();

    protected override Task BeforePurgeAsync(ContentMediaAsset item, CancellationToken cancellationToken) =>
        storage.DeleteAsync(item.StorageKey, cancellationToken);
}
