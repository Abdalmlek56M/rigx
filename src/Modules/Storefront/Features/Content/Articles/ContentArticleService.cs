using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Catalog;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// Articles: public list/detail (live only: published and its publish time has come), admin list/search,
/// create/update (draft), publish now or schedule, unpublish, view counter.
/// Every write stores a revision snapshot and refreshes the media-usage index (best effort).
/// </summary>
public sealed class ContentArticleService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider,
    IContentMediaUsageSink mediaUsage)
{
    /// <summary>The reference type articles use in the media-usage index.</summary>
    private const string UsageType = "Article";

    public async Task<PagedResult<ArticleView>> ListPublicAsync(
        int page,
        int pageSize,
        string? category,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PageRequest.Of(page, pageSize, 50);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = Now();
        var rows = db.ContentArticles.AsNoTracking()
            .Where(article => article.Status == ContentStatus.Published && article.PublishedAtUtc <= now);
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
        return new PagedResult<ArticleView>(
            items.Select(item => Map(item, now)).ToList(), page, pageSize, total);
    }

    public async Task<ArticleView> GetPublicAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = Now();
        var article = await db.ContentArticles.AsNoTracking().SingleOrDefaultAsync(
            item => item.Slug == slug && item.Status == ContentStatus.Published && item.PublishedAtUtc <= now,
            cancellationToken) ?? throw new StorefrontException(404, "Article was not found.");
        return Map(article, now);
    }

    /// <summary>Counts one view of a live article (404 when it is not live).</summary>
    public async Task RecordViewAsync(string slug, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = Now();
        var updated = await db.ContentArticles
            .Where(article => article.Slug == slug && article.Status == ContentStatus.Published && article.PublishedAtUtc <= now)
            .ExecuteUpdateAsync(set => set.SetProperty(article => article.ViewCount, article => article.ViewCount + 1), cancellationToken);
        if (updated == 0)
            throw new StorefrontException(404, "Article was not found.");
    }

    public async Task<PagedResult<ArticleView>> ListAdminAsync(
        int page,
        int pageSize,
        string? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PageRequest.Of(page, pageSize);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = Now();
        var rows = db.ContentArticles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var value = status.Trim();
            rows = value switch
            {
                ArticleSchedule.Scheduled => rows.Where(article => article.Status == ContentStatus.Published && article.PublishedAtUtc > now),
                ContentStatus.Published => rows.Where(article => article.Status == ContentStatus.Published && article.PublishedAtUtc <= now),
                _ => rows.Where(article => article.Status == value)
            };
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
        return new PagedResult<ArticleView>(items.Select(item => Map(item, now)).ToList(), page, pageSize, total);
    }

    public async Task<ArticleView> GetAdminAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return Map(await FindAsync(db, id, cancellationToken), Now());
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
        var now = Now();
        var article = new ContentArticle
        {
            Id = Guid.CreateVersion7(),
            Slug = normalized.Slug,
            TitleAr = normalized.TitleAr,
            TitleEn = normalized.TitleEn,
            SummaryAr = normalized.SummaryAr,
            SummaryEn = normalized.SummaryEn,
            BlocksArJson = ArticleBlocksJson.Serialize(normalized.BlocksAr),
            BlocksEnJson = ArticleBlocksJson.Serialize(normalized.BlocksEn),
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
        return Map(article, now);
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
        article.BlocksArJson = ArticleBlocksJson.Serialize(normalized.BlocksAr);
        article.BlocksEnJson = ArticleBlocksJson.Serialize(normalized.BlocksEn);
        article.MainImageStorageKey = normalized.MainImageStorageKey;
        article.CategorySlug = normalized.CategorySlug;
        article.TagsCsv = string.Join(",", normalized.Tags);
        article.SeoTitleAr = normalized.SeoTitleAr;
        article.SeoTitleEn = normalized.SeoTitleEn;
        article.SeoDescriptionAr = normalized.SeoDescriptionAr;
        article.SeoDescriptionEn = normalized.SeoDescriptionEn;
        article.CanonicalUrl = normalized.CanonicalUrl;
        article.SortOrder = normalized.SortOrder;
        article.UpdatedAtUtc = Now();
        article.RowVersion++;
        AddRevision(db, article, actorId, "Saved", article.UpdatedAtUtc);
        await db.SaveChangesAsync(cancellationToken);
        await SyncUsageAsync(article, cancellationToken);
        return Map(article, article.UpdatedAtUtc);
    }

    public async Task<ArticleView> PublishAsync(
        Guid id,
        ArticlePublishRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var article = await FindAsync(db, id, cancellationToken);
        if (article.RowVersion != request.RowVersion)
            throw new StorefrontException(409, "The article was changed by another user.");
        ArticleNormalization.Normalize(ArticleNormalization.ToInput(article), requireContent: true);
        var now = Now();
        article.Status = ContentStatus.Published;
        article.PublishedAtUtc = ArticleSchedule.PublishAt(article.PublishedAtUtc, request.PublishAtUtc, now);
        article.UpdatedAtUtc = now;
        article.RowVersion++;
        AddRevision(db, article, actorId, article.PublishedAtUtc > now ? ArticleSchedule.Scheduled : "Published", now);
        await db.SaveChangesAsync(cancellationToken);
        await SyncUsageAsync(article, cancellationToken);
        return Map(article, now);
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
        article.UpdatedAtUtc = Now();
        article.RowVersion++;
        AddRevision(db, article, actorId, "Unpublished", article.UpdatedAtUtc);
        await db.SaveChangesAsync(cancellationToken);
        await SyncUsageAsync(article, cancellationToken);
        return Map(article, article.UpdatedAtUtc);
    }

    /// <summary>Moves the article to the trash: hidden from the site and the lists, restorable for 30 days.</summary>
    public async Task DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var article = await FindAsync(db, id, cancellationToken);
        TrashRules.MoveToTrash(article, actorId, Now());
        await db.SaveChangesAsync(cancellationToken);
        await ClearUsageAsync(id, cancellationToken);
    }

    /// <summary>Media usage is advisory metadata for the library delete guard; failing to refresh it must not fail the article change.</summary>
    internal async Task ClearUsageAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await mediaUsage.ClearAsync(UsageType, id, cancellationToken);
        }
        catch
        {
            // Advisory only, see above.
        }
    }

    internal async Task SyncUsageAsync(ContentArticle article, CancellationToken cancellationToken)
    {
        try
        {
            await mediaUsage.RegisterAsync(
                UsageType,
                article.Id,
                string.IsNullOrWhiteSpace(article.TitleEn) ? article.Slug : article.TitleEn,
                [.. ImageKeys(article)],
                article.Status == ContentStatus.Published,
                cancellationToken);
        }
        catch
        {
            // Media usage is advisory metadata for the library delete guard. An
            // article save must not fail because the usage index is unavailable.
        }
    }

    /// <summary>The main image and every image block of both bodies.</summary>
    private static IEnumerable<string> ImageKeys(ContentArticle article) =>
        ArticleBlockRules.ImageKeys(ArticleBlocksJson.Deserialize(article.BlocksArJson))
            .Concat(ArticleBlockRules.ImageKeys(ArticleBlocksJson.Deserialize(article.BlocksEnJson)))
            .Concat(string.IsNullOrWhiteSpace(article.MainImageStorageKey) ? [] : [article.MainImageStorageKey])
            .Distinct();

    private DateTimeOffset Now() => timeProvider.GetUtcNow().ToUniversalTime();

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
                BlocksAr = ArticleBlocksJson.Deserialize(article.BlocksArJson),
                BlocksEn = ArticleBlocksJson.Deserialize(article.BlocksEnJson),
                article.SeoTitleAr,
                article.SeoTitleEn,
                article.SeoDescriptionAr,
                article.SeoDescriptionEn,
                article.CanonicalUrl,
                article.CategorySlug,
                article.TagsCsv,
                article.Status
            }, ArticleBlocksJson.Options),
            Reason = reason,
            ActorUserAccountId = actorId,
            CreatedAtUtc = now
        });
    }

    private static ArticleView Map(ContentArticle article, DateTimeOffset now) => new(
        article.Id,
        article.Slug,
        article.TitleAr,
        article.TitleEn,
        article.SummaryAr,
        article.SummaryEn,
        ArticleBlocksJson.Deserialize(article.BlocksArJson),
        ArticleBlocksJson.Deserialize(article.BlocksEnJson),
        article.MainImageStorageKey,
        article.CategorySlug,
        article.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries),
        ArticleSchedule.DisplayStatus(article.Status, article.PublishedAtUtc, now),
        article.ViewCount,
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
