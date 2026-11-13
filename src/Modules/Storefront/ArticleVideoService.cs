using HardwareStore.Modules.Catalog;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

public sealed record ArticleVideoView(Guid Id, string TitleAr, string TitleEn, string? DescriptionAr,
    string? DescriptionEn, string Provider, string? YouTubeVideoId, string? EmbedUrl, int SortOrder);

public sealed class ArticleVideoService(
    IDbContextFactory<StorefrontDbContext> storefrontFactory,
    IDbContextFactory<CatalogDbContext> catalogFactory)
{
    public async Task<IReadOnlyList<ArticleVideoView>> ListAsync(Guid articleId, bool publishedOnly,
        CancellationToken cancellationToken = default)
    {
        await using var storefront = await storefrontFactory.CreateDbContextAsync(cancellationToken);
        if (!await storefront.ContentArticles.AnyAsync(article => article.Id == articleId, cancellationToken))
            throw new StorefrontException(404, "Article was not found.");
        return await LoadVideosAsync(articleId, publishedOnly, cancellationToken);
    }

    public async Task<IReadOnlyList<ArticleVideoView>> ListBySlugAsync(string? slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) return [];
        await using var storefront = await storefrontFactory.CreateDbContextAsync(cancellationToken);
        var article = await storefront.ContentArticles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Slug == slug && item.Status == ContentStatus.Published, cancellationToken);
        return article is null ? [] : await LoadVideosAsync(article.Id, true, cancellationToken);
    }

    public async Task<IReadOnlyList<ArticleVideoView>> SetAsync(Guid articleId, IReadOnlyList<Guid>? videoIds,
        CancellationToken cancellationToken = default)
    {
        var ids = videoIds?.Distinct().Take(20).ToList() ?? [];
        await using var catalog = await catalogFactory.CreateDbContextAsync(cancellationToken);
        var existing = await catalog.ContentVideos.AsNoTracking()
            .Where(video => ids.Contains(video.Id)).Select(video => video.Id).ToListAsync(cancellationToken);
        if (existing.Count != ids.Count)
            throw new StorefrontException(400, "One or more videos do not exist.");
        await using var storefront = await storefrontFactory.CreateDbContextAsync(cancellationToken);
        if (!await storefront.ContentArticles.AnyAsync(article => article.Id == articleId, cancellationToken))
            throw new StorefrontException(404, "Article was not found.");
        await using var transaction = await storefront.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await storefront.ArticleVideos.Where(item => item.ArticleId == articleId).ExecuteDeleteAsync(cancellationToken);
            for (var index = 0; index < ids.Count; index++)
                storefront.ArticleVideos.Add(new ArticleVideoReference { ArticleId = articleId, VideoId = ids[index], SortOrder = index });
            await storefront.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
        return await LoadVideosAsync(articleId, false, cancellationToken);
    }

    public async Task GuardVideoUnpublishAsync(Guid videoId, CancellationToken cancellationToken = default)
    {
        await using var storefront = await storefrontFactory.CreateDbContextAsync(cancellationToken);
        var referenced = await storefront.ArticleVideos.AsNoTracking()
            .Where(link => link.VideoId == videoId)
            .Join(storefront.ContentArticles.AsNoTracking().Where(article => article.Status == ContentStatus.Published),
                link => link.ArticleId, article => article.Id, (link, article) => article.Id)
            .AnyAsync(cancellationToken);
        if (referenced)
            throw new StorefrontException(409, "This video is referenced by a published article; unpublish or remove the reference first.");
    }

    private async Task<IReadOnlyList<ArticleVideoView>> LoadVideosAsync(Guid articleId, bool publishedOnly,
        CancellationToken cancellationToken)
    {
        await using var storefront = await storefrontFactory.CreateDbContextAsync(cancellationToken);
        var links = await storefront.ArticleVideos.AsNoTracking()
            .Where(item => item.ArticleId == articleId)
            .OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        if (links.Count == 0) return [];
        var ids = links.Select(link => link.VideoId).ToList();
        await using var catalog = await catalogFactory.CreateDbContextAsync(cancellationToken);
        var query = catalog.ContentVideos.AsNoTracking().Where(video => ids.Contains(video.Id));
        if (publishedOnly) query = query.Where(video => video.Status == "Published");
        var videos = await query.ToDictionaryAsync(video => video.Id, cancellationToken);
        return links.Where(link => videos.ContainsKey(link.VideoId)).Select(link =>
        {
            var video = videos[link.VideoId];
            return new ArticleVideoView(video.Id, video.TitleAr, video.TitleEn, video.DescriptionAr, video.DescriptionEn,
                video.Provider, video.YouTubeVideoId,
                video.YouTubeVideoId is null ? null : $"https://www.youtube-nocookie.com/embed/{video.YouTubeVideoId}",
                link.SortOrder);
        }).ToList();
    }
}
