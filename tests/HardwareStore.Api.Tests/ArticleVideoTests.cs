using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class ArticleVideoTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Article_video_references_are_ordered_public_filtered_and_unpublish_guarded()
    {
        var database = await fixture.CreateDatabaseAsync();
        var articleId = Guid.NewGuid();
        var publishedVideoId = Guid.NewGuid();
        var draftVideoId = Guid.NewGuid();
        await using (var storefront = database.CreateStorefrontContext())
        {
            storefront.ContentArticles.Add(new ContentArticle
            {
                Id = articleId, Slug = $"video-article-{articleId:N}", TitleAr = "مقال", TitleEn = "Article",
                SummaryAr = "ملخص", SummaryEn = "Summary", BodyAr = "نص", BodyEn = "Body",
                Status = ContentStatus.Published, PublishedAtUtc = DateTimeOffset.UtcNow,
                CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow
            });
            await storefront.SaveChangesAsync();
        }
        await using (var catalog = database.CreateCatalogContext())
        {
            catalog.ContentVideos.Add(new ContentVideoAsset
            {
                Id = publishedVideoId, TitleAr = "فيديو", TitleEn = "Published video",
                StorageKey = $"youtube:{publishedVideoId:N}", Provider = "YouTube",
                YouTubeVideoId = "dQw4w9WgXcQ", MimeType = "video/youtube",
                Status = "Published", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow
            });
            catalog.ContentVideos.Add(new ContentVideoAsset
            {
                Id = draftVideoId, TitleAr = "مسودة", TitleEn = "Draft video",
                StorageKey = $"youtube:{draftVideoId:N}", Provider = "YouTube",
                YouTubeVideoId = "dQw4w9WgXcQ", MimeType = "video/youtube",
                Status = "Draft", CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow
            });
            await catalog.SaveChangesAsync();
        }
        var service = new ArticleVideoService(
            new StorefrontFactory(database.ConnectionString), new CatalogFactory(database.ConnectionString));

        var linked = await service.SetAsync(articleId, [draftVideoId, publishedVideoId]);
        Assert.Equal(2, linked.Count);
        Assert.Equal(publishedVideoId, linked[1].Id);
        Assert.Contains("youtube-nocookie.com/embed/dQw4w9WgXcQ", linked[1].EmbedUrl);

        var publicity = await service.ListBySlugAsync($"video-article-{articleId:N}");
        Assert.Single(publicity);
        Assert.Equal(publishedVideoId, publicity[0].Id);

        var blocked = await Assert.ThrowsAsync<StorefrontException>(() => service.GuardVideoUnpublishAsync(publishedVideoId));
        Assert.Equal(409, blocked.StatusCode);
        await using (var storefront = database.CreateStorefrontContext())
        {
            var article = await storefront.ContentArticles.SingleAsync(item => item.Id == articleId);
            article.Status = ContentStatus.Draft;
            await storefront.SaveChangesAsync();
        }
        await service.GuardVideoUnpublishAsync(publishedVideoId);
    }

    private sealed class StorefrontFactory(string connectionString) : IDbContextFactory<StorefrontDbContext>
    {
        public StorefrontDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class CatalogFactory(string connectionString) : IDbContextFactory<CatalogDbContext>
    {
        public CatalogDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString).Options);
    }
}
