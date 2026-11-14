using HardwareStore.Modules.Catalog;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class ContentMediaPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Upload_metadata_replace_and_usage_guarded_delete()
    {
        var database = await fixture.CreateDatabaseAsync();
        var media = new ContentMediaService(
            new CatalogFactory(database.ConnectionString),
            new FakeMediaStorage(),
            TimeProvider.System,
            []);

        var asset = await media.UploadAsync(
            new CatalogImageUpload(Stream.Null, 10, "test.png", "image/png"),
            new ContentMediaMetadataInput("صورة", "Image", "نص بديل", "Alt text", null, null, "hero, gpu", 0),
            Guid.NewGuid());
        Assert.Equal(1200, asset.Width);
        Assert.Equal(new[] { "hero", "gpu" }, asset.Tags);
        var page = await media.ListAsync("gpu", 1, 10);
        Assert.Single(page.Items);

        var updated = await media.UpdateMetadataAsync(
            asset.Id,
            new ContentMediaMetadataInput("صورة", "Image", "نص بديل", "Alt text", "تعليق", "Caption", "hero", asset.RowVersion));
        Assert.Equal("Caption", updated.CaptionEn);
        await Assert.ThrowsAsync<CatalogValidationException>(() =>
            media.UpdateMetadataAsync(
                asset.Id,
                new ContentMediaMetadataInput("x", "x", "x", "x", null, null, null, asset.RowVersion)));

        var replaced = await media.ReplaceImageAsync(
            asset.Id,
            new CatalogImageUpload(Stream.Null, 10, "new.png", "image/png"),
            updated.RowVersion);
        Assert.Equal(updated.RowVersion + 1, replaced.RowVersion);

        await media.RegisterAsync("Article", Guid.NewGuid(), "Published article", [replaced.StorageKey, replaced.StorageKey], true);
        await Assert.ThrowsAsync<ContentMediaInUseException>(() =>
            media.DeleteAsync(asset.Id, replaced.RowVersion));
        Assert.Single(await media.ListUsagesAsync(asset.Id));

        var otherArticle = Guid.NewGuid();
        await media.RegisterAsync("Article", otherArticle, "Draft article", [replaced.StorageKey], false);
        Assert.Equal(2, (await media.ListUsagesAsync(asset.Id)).Count);
        await media.ClearAsync("Article", otherArticle);
        Assert.Single(await media.ListUsagesAsync(asset.Id));

        // Clearing the published reference releases the delete guard.
        await media.ClearAsync("Article", (await media.ListUsagesAsync(asset.Id))[0].ReferenceId);
        await media.DeleteAsync(asset.Id, replaced.RowVersion);
        Assert.Empty((await media.ListAsync(null, 1, 10)).Items);
    }

    [Fact]
    public async Task External_reference_guard_blocks_generic_media_deletion_and_is_listed()
    {
        var database = await fixture.CreateDatabaseAsync();
        var guard = new FakeReferenceGuard();
        var media = new ContentMediaService(
            new CatalogFactory(database.ConnectionString),
            new FakeMediaStorage(),
            TimeProvider.System,
            [guard]);

        var asset = await media.UploadAsync(
            new CatalogImageUpload(Stream.Null, 10, "guarded.png", "image/png"),
            new ContentMediaMetadataInput("محمي", "Guarded", "وصف", "Description", null, null, "guard", 0),
            Guid.NewGuid());

        var usages = await media.ListUsagesAsync(asset.Id);
        var usage = Assert.Single(usages);
        Assert.Equal("Retail.InventoryUnit", usage.ReferenceType);
        Assert.Contains("historical", usage.ReferenceTitle, StringComparison.OrdinalIgnoreCase);

        var error = await Assert.ThrowsAsync<ContentMediaInUseException>(() =>
            media.DeleteAsync(asset.Id, asset.RowVersion));
        Assert.Contains("historical", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeReferenceGuard : IMediaReferenceGuard
    {
        public Task<IReadOnlyList<MediaReferenceInfo>> FindReferencesAsync(
            string storageKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MediaReferenceInfo>>(
            [
                new MediaReferenceInfo("Retail", "InventoryUnit", Guid.NewGuid(),
                    "Inventory unit keeps this image as a historical purchase snapshot.")
            ]);
    }

    private sealed class CatalogFactory(string connectionString)
        : IDbContextFactory<CatalogDbContext>
    {
        public CatalogDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class FakeMediaStorage : ICatalogMediaStorage
    {
        public Task<CatalogStoredMedia> SaveImageAsync(
            Guid productId, Guid imageId, CatalogImageUpload image, ImageWatermark? watermark = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> SaveAssetImageAsync(
            string collection, Guid ownerId, Guid assetId, CatalogImageUpload image,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CatalogStoredMedia> SaveContentImageAsync(
            Guid assetId, CatalogImageUpload image, bool replace, ImageWatermark? watermark = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CatalogStoredMedia(
                $"content/{assetId:N}/{assetId:N}.webp",
                $"content/{assetId:N}/{assetId:N}-thumb.webp",
                $"content/{assetId:N}/{assetId:N}.original",
                1200,
                800,
                400,
                267,
                2048));

        public Task<string> SaveSettingsLogoAsync(
            string kind, CatalogImageUpload image, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> SavePrivateImageAsync(
            string area, Guid ownerId, CatalogImageUpload image, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public bool Exists(string storageKey) => false;

        public Stream OpenRead(string storageKey) => Stream.Null;
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task<IReadOnlyList<StoredMediaFile>> ListFilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StoredMediaFile>>([]);
        public Task DeleteStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
