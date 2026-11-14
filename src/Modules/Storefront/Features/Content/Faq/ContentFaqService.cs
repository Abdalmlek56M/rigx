using HardwareStore.Common.Domain;
using HardwareStore.Modules.Catalog;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// FAQ entries: help page list, item-page list (general + category + product scope), admin list with filters,
/// create/update, publish/unpublish.
/// </summary>
public sealed class ContentFaqService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TaxonomyService taxonomy,
    TimeProvider timeProvider)
{
    /// <summary>The help page: published general FAQs, including help-page-only ones.</summary>
    public async Task<IReadOnlyList<FaqView>> ListPublicAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return (await db.ContentFaqs.AsNoTracking()
                .Where(faq => faq.Status == ContentStatus.Published && faq.Scope == FaqScope.General)
                .OrderBy(faq => faq.SortOrder)
                .ThenBy(faq => faq.CreatedAtUtc)
                .ToListAsync(cancellationToken))
            .Select(Map)
            .ToList();
    }

    /// <summary>
    /// The FAQs of an item page: its own, its category path's and the general ones, in that order.
    /// <paramref name="categoryPathIds"/> is the item's category and its parents.
    /// </summary>
    public async Task<IReadOnlyList<FaqView>> ListForItemAsync(
        string itemCode,
        IReadOnlyCollection<Guid> categoryPathIds,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var path = categoryPathIds.ToArray();
        var candidates = await db.ContentFaqs.AsNoTracking()
            .Where(faq => faq.Status == ContentStatus.Published
                && (faq.Scope == FaqScope.General
                    || (faq.Scope == FaqScope.Category && faq.CategoryId != null && path.Contains(faq.CategoryId.Value))
                    || (faq.Scope == FaqScope.Product && faq.ItemCode == itemCode)))
            .ToListAsync(cancellationToken);
        return candidates
            .Where(faq => FaqRules.ShowsOnItem(Target(faq), itemCode, categoryPathIds))
            .OrderBy(faq => FaqRules.ItemPageRank(faq.Scope))
            .ThenBy(faq => faq.SortOrder)
            .ThenBy(faq => faq.CreatedAtUtc)
            .Select(Map)
            .ToList();
    }

    public async Task<IReadOnlyList<FaqView>> ListAdminAsync(
        string? scope = null,
        Guid? categoryId = null,
        string? itemCode = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = db.ContentFaqs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(scope))
        {
            var value = scope.Trim();
            rows = rows.Where(faq => faq.Scope == value);
        }
        if (categoryId is { } category)
            rows = rows.Where(faq => faq.CategoryId == category);
        if (!string.IsNullOrWhiteSpace(itemCode))
        {
            var code = PublicCode.TryNormalize(itemCode, PublicCode.StockItem, out var normalized)
                ? normalized
                : throw new StorefrontException(400, "Use a store item code like RX-0142.");
            rows = rows.Where(faq => faq.ItemCode == code);
        }
        return (await rows
                .OrderBy(faq => faq.SortOrder)
                .ThenBy(faq => faq.CreatedAtUtc)
                .ToListAsync(cancellationToken))
            .Select(Map)
            .ToList();
    }

    public async Task<FaqView> CreateAsync(
        FaqInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = await NormalizeAsync(input, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var faq = new ContentFaq
        {
            Id = Guid.CreateVersion7(),
            QuestionAr = normalized.QuestionAr,
            QuestionEn = normalized.QuestionEn,
            AnswerAr = normalized.AnswerAr,
            AnswerEn = normalized.AnswerEn,
            Status = ContentStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        Apply(faq, normalized);
        db.ContentFaqs.Add(faq);
        await db.SaveChangesAsync(cancellationToken);
        return Map(faq);
    }

    public async Task<FaqView> UpdateAsync(
        Guid id,
        FaqInput input,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        var normalized = await NormalizeAsync(input, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var faq = await db.ContentFaqs.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "FAQ was not found.");
        if (faq.RowVersion != rowVersion)
            throw new StorefrontException(409, "The FAQ was changed by another user.");
        faq.QuestionAr = normalized.QuestionAr;
        faq.QuestionEn = normalized.QuestionEn;
        faq.AnswerAr = normalized.AnswerAr;
        faq.AnswerEn = normalized.AnswerEn;
        Apply(faq, normalized);
        faq.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        faq.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(faq);
    }

    public async Task<FaqView> SetPublishedAsync(
        Guid id,
        bool published,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var faq = await db.ContentFaqs.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "FAQ was not found.");
        if (faq.RowVersion != rowVersion)
            throw new StorefrontException(409, "The FAQ was changed by another user.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        faq.Status = published ? ContentStatus.Published : ContentStatus.Draft;
        if (published)
        {
            if (string.IsNullOrWhiteSpace(faq.AnswerAr) || string.IsNullOrWhiteSpace(faq.AnswerEn))
                throw new StorefrontException(400, "Both answers are required before publishing.");
            faq.PublishedAtUtc ??= now;
        }
        faq.UpdatedAtUtc = now;
        faq.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(faq);
    }

    private async Task<NormalizedFaq> NormalizeAsync(FaqInput input, CancellationToken cancellationToken)
    {
        var target = FaqRules.Target(input.Scope, input.CategoryId, input.ItemCode, input.HelpPageOnly,
            input.ExcludedCategoryIds, input.ExcludedItemCodes);
        if (target.CategoryId is { } categoryId && (await taxonomy.GetCategoryPathIdsAsync(categoryId, cancellationToken)).Count == 0)
            throw new StorefrontException(400, "The FAQ category was not found.");
        return new NormalizedFaq(
            ContentText.Required(input.QuestionAr, 400, "Arabic question"),
            ContentText.Required(input.QuestionEn, 400, "English question"),
            ContentText.Required(input.AnswerAr, 4000, "Arabic answer"),
            ContentText.Required(input.AnswerEn, 4000, "English answer"),
            ContentText.Optional(input.Topic, 64, "Topic"),
            target,
            string.IsNullOrWhiteSpace(input.YouTubeUrl) ? null : YouTubeUrl.ToVideoId(input.YouTubeUrl),
            Math.Clamp(input.SortOrder, -10_000, 10_000));
    }

    private static void Apply(ContentFaq faq, NormalizedFaq normalized)
    {
        faq.Topic = normalized.Topic;
        faq.Scope = normalized.Target.Scope;
        faq.CategoryId = normalized.Target.CategoryId;
        faq.ItemCode = normalized.Target.ItemCode;
        faq.HelpPageOnly = normalized.Target.HelpPageOnly;
        faq.ExcludedCategoryIds = [.. normalized.Target.ExcludedCategoryIds];
        faq.ExcludedItemCodes = [.. normalized.Target.ExcludedItemCodes];
        faq.YouTubeId = normalized.YouTubeId;
        faq.SortOrder = normalized.SortOrder;
    }

    private static FaqTarget Target(ContentFaq faq) => new(faq.Scope, faq.CategoryId, faq.ItemCode, faq.HelpPageOnly,
        faq.ExcludedCategoryIds, faq.ExcludedItemCodes);

    private static FaqView Map(ContentFaq faq) => new(
        faq.Id, faq.QuestionAr, faq.QuestionEn, faq.AnswerAr, faq.AnswerEn,
        faq.Topic, faq.Scope, faq.CategoryId, faq.ItemCode, faq.HelpPageOnly,
        faq.ExcludedCategoryIds, faq.ExcludedItemCodes,
        faq.YouTubeId, faq.YouTubeId is null ? null : YouTubeUrl.EmbedUrl(faq.YouTubeId),
        faq.SortOrder, faq.Status, faq.PublishedAtUtc,
        faq.CreatedAtUtc, faq.UpdatedAtUtc, faq.RowVersion);

    private sealed record NormalizedFaq(
        string QuestionAr,
        string QuestionEn,
        string AnswerAr,
        string AnswerEn,
        string? Topic,
        FaqTarget Target,
        string? YouTubeId,
        int SortOrder);
}
