using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

/// <summary>FAQ entries: public list (published only), admin list, create/update, publish/unpublish.</summary>
public sealed class ContentFaqService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<FaqView>> ListPublicAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return (await db.ContentFaqs.AsNoTracking()
                .Where(faq => faq.Status == ContentStatus.Published)
                .OrderBy(faq => faq.SortOrder)
                .ThenBy(faq => faq.CreatedAtUtc)
                .ToListAsync(cancellationToken))
            .Select(Map)
            .ToList();
    }

    public async Task<IReadOnlyList<FaqView>> ListAdminAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return (await db.ContentFaqs.AsNoTracking()
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
        var normalized = Normalize(input);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var faq = new ContentFaq
        {
            Id = Guid.CreateVersion7(),
            QuestionAr = normalized.QuestionAr,
            QuestionEn = normalized.QuestionEn,
            AnswerAr = normalized.AnswerAr,
            AnswerEn = normalized.AnswerEn,
            Category = normalized.Category,
            SortOrder = normalized.SortOrder,
            Status = ContentStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
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
        var normalized = Normalize(input);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var faq = await db.ContentFaqs.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "FAQ was not found.");
        if (faq.RowVersion != rowVersion)
            throw new StorefrontException(409, "The FAQ was changed by another user.");
        faq.QuestionAr = normalized.QuestionAr;
        faq.QuestionEn = normalized.QuestionEn;
        faq.AnswerAr = normalized.AnswerAr;
        faq.AnswerEn = normalized.AnswerEn;
        faq.Category = normalized.Category;
        faq.SortOrder = normalized.SortOrder;
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

    private static NormalizedFaq Normalize(FaqInput input) => new(
        ContentText.Required(input.QuestionAr, 400, "Arabic question"),
        ContentText.Required(input.QuestionEn, 400, "English question"),
        ContentText.Required(input.AnswerAr, 4000, "Arabic answer"),
        ContentText.Required(input.AnswerEn, 4000, "English answer"),
        ContentText.Optional(input.Category, 64, "Category"),
        Math.Clamp(input.SortOrder, -10_000, 10_000));

    private static FaqView Map(ContentFaq faq) => new(
        faq.Id, faq.QuestionAr, faq.QuestionEn, faq.AnswerAr, faq.AnswerEn,
        faq.Category, faq.SortOrder, faq.Status, faq.PublishedAtUtc,
        faq.CreatedAtUtc, faq.UpdatedAtUtc, faq.RowVersion);

    private sealed record NormalizedFaq(
        string QuestionAr,
        string QuestionEn,
        string AnswerAr,
        string AnswerEn,
        string? Category,
        int SortOrder);
}
