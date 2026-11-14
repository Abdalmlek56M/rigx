using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Trash;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>Trashed articles. An article whose slug was taken meanwhile cannot come back (409).</summary>
public sealed class ArticleTrashSource(IDbContextFactory<StorefrontDbContext> contextFactory, ContentArticleService articles)
    : SoftDeleteTrashSource<StorefrontDbContext, ContentArticle>(contextFactory)
{
    public const string TrashKind = "article";

    public override string Kind => TrashKind;

    protected override DbSet<ContentArticle> Set(StorefrontDbContext db) => db.ContentArticles;

    protected override string Label(ContentArticle item) => string.IsNullOrWhiteSpace(item.TitleAr) ? item.Slug : item.TitleAr;

    protected override AppException NotFound() => new StorefrontException(404, "Article was not found in the trash.");

    protected override async Task EnsureCanRestoreAsync(StorefrontDbContext db, ContentArticle item, CancellationToken cancellationToken)
    {
        if (await db.ContentArticles.AnyAsync(other => other.Slug == item.Slug && other.Id != item.Id, cancellationToken))
            throw new StorefrontException(409, "Another article uses this slug now. Rename or delete it first.");
    }

    protected override Task AfterRestoreAsync(ContentArticle item, CancellationToken cancellationToken) =>
        articles.SyncUsageAsync(item, cancellationToken);
}
