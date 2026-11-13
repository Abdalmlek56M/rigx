using HardwareStore.Modules.Catalog;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// Articles: public list/detail (published only), admin list/search, create/update (draft), publish/unpublish.
/// Every write stores a revision snapshot and refreshes the media-usage index (best effort).
/// </summary>
public sealed class ContentArticleService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider,
    IContentMediaUsageSink mediaUsage)
{
    public async Task<ContentPage<ArticleView>> ListPublicAsync(
        int page,
        int pageSize,
        string? category,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = db.ContentArticles.AsNoTracking()
            .Where(article => article.Status == ContentStatus.Published);
        if (!string.IsNullOrWhiteSpace(category))
        {
            var slug = category.Trim();
            rows = rows.Where(article => article.CategorySlug == slug);
        }
        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(article => article.PublishedAtUtc)
            .ThenBy(article => article.SortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new ContentPage<ArticleView>(
            items.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<ArticleView> GetPublicAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var article = await db.ContentArticles.AsNoTracking().SingleOrDefaultAsync(
            item => item.Slug == slug && item.Status == ContentStatus.Published,
            cancellationToken) ?? throw new StorefrontException(404, "Article was not found.");
        return Map(article);
    }

    public async Task<ContentPage<ArticleView>> ListAdminAsync(
        int page,
        int pageSize,
        string? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = db.ContentArticles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var value = status.Trim();
            rows = rows.Where(article => article.Status == value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            rows = rows.Where(article =>
                EF.Functions.ILike(article.TitleAr, pattern) ||
                EF.Functions.ILike(article.TitleEn, pattern) ||
                EF.Functions.ILike(article.Slug, pattern));
        }
        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(article => article.UpdatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new ContentPage<ArticleView>(items.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<ArticleView> GetAdminAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return Map(await FindAsync(db, id, cancellationToken));
    }

    public async Task<IReadOnlyList<ArticleRevisionView>> ListRevisionsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ContentArticleRevisions.AsNoTracking()
            .Where(revision => revision.ArticleId == id)
            .OrderByDescending(revision => revision.CreatedAtUtc)
            .Select(revision => new ArticleRevisionView(
                revision.Id, revision.Reason, revision.CreatedAtUtc, revision.ActorUserAccountId))
            .ToListAsync(cancellationToken);
    }

    public async Task<ArticleView> CreateAsync(
        ArticleInput input,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var normalized = ArticleNormalization.Normalize(input, requireContent: false);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.ContentArticles.AnyAsync(article => article.Slug == normalized.Slug, cancellationToken))
            throw new StorefrontException(409, "An article with this slug already exists.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var article = new ContentArticle
        {
            Id = Guid.CreateVersion7(),
            Slug = normalized.Slug,
            TitleAr = normalized.TitleAr,
            TitleEn = normalized.TitleEn,
            SummaryAr = normalized.SummaryAr,
            SummaryEn = normalized.SummaryEn,
            BodyAr = normalized.BodyAr,
            BodyEn = normalized.BodyEn,
            MainImageStorageKey = normalized.MainImageStorageKey,
            CategorySlug = normalized.CategorySlug,
            TagsCsv = string.Join(",", normalized.Tags),
            Status = ContentStatus.Draft,
            SeoTitleAr = normalized.SeoTitleAr,
            SeoTitleEn = normalized.SeoTitleEn,
            SeoDescriptionAr = normalized.SeoDescriptionAr,
            SeoDescriptionEn = normalized.SeoDescriptionEn,
            CanonicalUrl = normalized.CanonicalUrl,
            SortOrder = normalized.SortOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.ContentArticles.Add(article);
        AddRevision(db, article, actorId, "Created", now);
        await db.SaveChangesAsync(cancellationToken);
        await SyncUsageAsync(article, cancellationToken);
        return Map(article);
    }

    public async Task<ArticleView> UpdateAsync(
        Guid id,
        ArticleInput input,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var normalized = ArticleNormalization.Normalize(input, requireContent: false);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var article = await FindAsync(db, id, cancellationToken);
        if (article.RowVersion != input.RowVersion)
            throw new StorefrontException(409, "The article was changed by another user.");
        if (await db.ContentArticles.AnyAsync(
                other => other.Slug == normalized.Slug && other.Id != id, cancellationToken))
            throw new StorefrontException(409, "An article with this slug already exists.");
        article.Slug = normalized.Slug;
        article.TitleAr = normalized.TitleAr;
        article.TitleEn = normalized.TitleEn;
        article.SummaryAr = normalized.SummaryAr;
        article.SummaryEn = normalized.SummaryEn;
        article.BodyAr = normalized.BodyAr;
        article.BodyEn = normalized.BodyEn;
        article.MainImageStorageKey = normalized.MainImageStorageKey;
        article.CategorySlug = normalized.CategorySlug;
        article.TagsCsv = string.Join(",", normalized.Tags);
        article.SeoTitleAr = normalized.SeoTitleAr;
        article.SeoTitleEn = normalized.SeoTitleEn;
        article.SeoDescriptionAr = normalized.SeoDescriptionAr;
        article.SeoDescriptionEn = normalized.SeoDescriptionEn;
        article.CanonicalUrl = normalized.CanonicalUrl;
        article.SortOrder = normalized.SortOrder;
        article.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        article.RowVersion++;
        AddRevision(db, article, actorId, "Saved", article.UpdatedAtUtc);
        await db.SaveChangesAsync(cancellationToken);
        await SyncUsageAsync(article, cancellationToken);
        return Map(article);
    }

    public async Task<ArticleView> PublishAsync(
        Guid id,
        long rowVersion,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var article = await FindAsync(db, id, cancellationToken);
        if (article.RowVersion != rowVersion)
            throw new StorefrontException(409, "The article was changed by another user.");
        ArticleNormalization.Normalize(ArticleNormalization.ToInput(article), requireContent: true);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        article.Status = ContentStatus.Published;
        article.PublishedAtUtc ??= now;
        article.UpdatedAtUtc = now;
        article.RowVersion++;
        AddRevision(db, article, actorId, "Published", now);
        await db.SaveChangesAsync(cancellationToken);
        await SyncUsageAsync(article, cancellationToken);
        return Map(article);
    }

    public async Task<ArticleView> UnpublishAsync(
        Guid id,
        long rowVersion,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var article = await FindAsync(db, id, cancellationToken);
        if (article.RowVersion != rowVersion)
            throw new StorefrontException(409, "The article was changed by another user.");
        article.Status = ContentStatus.Draft;
        article.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        article.RowVersion++;
        AddRevision(db, article, actorId, "Unpublished", article.UpdatedAtUtc);
        await db.SaveChangesAsync(cancellationToken);
        await SyncUsageAsync(article, cancellationToken);
        return Map(article);
    }

    private async Task SyncUsageAsync(ContentArticle article, CancellationToken cancellationToken)
    {
        try
        {
            await mediaUsage.RegisterAsync(
                "Article",
                article.Id,
                string.IsNullOrWhiteSpace(article.TitleEn) ? article.Slug : article.TitleEn,
                article.MainImageStorageKey,
                article.Status == ContentStatus.Published,
                cancellationToken);
        }
        catch
        {
            // Media usage is advisory metadata for the library delete guard. An
            // article save must not fail because the usage index is unavailable.
        }
    }

    private static async Task<ContentArticle> FindAsync(
        StorefrontDbContext db,
        Guid id,
        CancellationToken cancellationToken) =>
        await db.ContentArticles.SingleOrDefaultAsync(article => article.Id == id, cancellationToken)
        ?? throw new StorefrontException(404, "Article was not found.");

    private static void AddRevision(
        StorefrontDbContext db,
        ContentArticle article,
        Guid actorId,
        string reason,
        DateTimeOffset now)
    {
        db.ContentArticleRevisions.Add(new ContentArticleRevision
        {
            Id = Guid.CreateVersion7(),
            ArticleId = article.Id,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                article.Slug,
                article.TitleAr,
                article.TitleEn,
                article.SummaryAr,
                article.SummaryEn,
                article.BodyAr,
                article.BodyEn,
                article.SeoTitleAr,
                article.SeoTitleEn,
                article.SeoDescriptionAr,
                article.SeoDescriptionEn,
                article.CanonicalUrl,
                article.CategorySlug,
                article.TagsCsv,
                article.Status
            }),
            Reason = reason,
            ActorUserAccountId = actorId,
            CreatedAtUtc = now
        });
    }

    private static ArticleView Map(ContentArticle article) => new(
        article.Id,
        article.Slug,
        article.TitleAr,
        article.TitleEn,
        article.SummaryAr,
        article.SummaryEn,
        article.BodyAr,
        article.BodyEn,
        article.MainImageStorageKey,
        article.CategorySlug,
        article.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries),
        article.Status,
        article.SeoTitleAr,
        article.SeoTitleEn,
        article.SeoDescriptionAr,
        article.SeoDescriptionEn,
        article.CanonicalUrl,
        article.SortOrder,
        article.PublishedAtUtc,
        article.CreatedAtUtc,
        article.UpdatedAtUtc,
        article.RowVersion);
}
