namespace HardwareStore.Modules.Storefront;

/// <summary>Article input rules (slug, bilingual text limits, tags, canonical URL). Body/summary are required only to publish.</summary>
internal static class ArticleNormalization
{
    public static ArticleInput ToInput(ContentArticle article) =>
        new(
            article.Slug,
            article.TitleAr,
            article.TitleEn,
            article.SummaryAr,
            article.SummaryEn,
            article.BodyAr,
            article.BodyEn,
            article.MainImageStorageKey,
            article.CategorySlug,
            article.TagsCsv,
            article.SeoTitleAr,
            article.SeoTitleEn,
            article.SeoDescriptionAr,
            article.SeoDescriptionEn,
            article.CanonicalUrl,
            article.SortOrder,
            article.RowVersion);

    public static NormalizedArticle Normalize(ArticleInput input, bool requireContent)
    {
        var canonical = ContentText.Optional(input.CanonicalUrl, 512, "Canonical URL");
        if (canonical is not null)
            ValidateCanonical(canonical);
        var tags = ContentText.Tags(input.Tags);
        return new NormalizedArticle(
            ContentText.Slug(input.Slug),
            ContentText.Required(input.TitleAr, 200, "Arabic title"),
            ContentText.Required(input.TitleEn, 200, "English title"),
            requireContent ? ContentText.Required(input.SummaryAr, 600, "Arabic summary") : ContentText.Optional(input.SummaryAr, 600, "Arabic summary") ?? "",
            requireContent ? ContentText.Required(input.SummaryEn, 600, "English summary") : ContentText.Optional(input.SummaryEn, 600, "English summary") ?? "",
            requireContent ? ContentText.Required(input.BodyAr, 20000, "Arabic body") : ContentText.Optional(input.BodyAr, 20000, "Arabic body") ?? "",
            requireContent ? ContentText.Required(input.BodyEn, 20000, "English body") : ContentText.Optional(input.BodyEn, 20000, "English body") ?? "",
            ContentText.Optional(input.MainImageStorageKey, 512, "Main image"),
            ContentText.Optional(input.CategorySlug, 160, "Category"),
            tags,
            ContentText.Optional(input.SeoTitleAr, 200, "Arabic SEO title"),
            ContentText.Optional(input.SeoTitleEn, 200, "English SEO title"),
            ContentText.Optional(input.SeoDescriptionAr, 320, "Arabic SEO description"),
            ContentText.Optional(input.SeoDescriptionEn, 320, "English SEO description"),
            canonical,
            Math.Clamp(input.SortOrder, -10_000, 10_000));
    }

    private static void ValidateCanonical(string canonical)
    {
        if (Uri.TryCreate(canonical, UriKind.Absolute, out var absolute))
        {
            if (absolute.Scheme is not ("http" or "https"))
                throw new StorefrontException(400, "Canonical URL must use http or https.");
            return;
        }
        if (!canonical.StartsWith('/') || canonical.StartsWith("//", StringComparison.Ordinal))
            throw new StorefrontException(400, "Canonical URL must be an absolute URL or a root-relative path.");
    }
}

internal sealed record NormalizedArticle(
    string Slug,
    string TitleAr,
    string TitleEn,
    string SummaryAr,
    string SummaryEn,
    string BodyAr,
    string BodyEn,
    string? MainImageStorageKey,
    string? CategorySlug,
    IReadOnlyList<string> Tags,
    string? SeoTitleAr,
    string? SeoTitleEn,
    string? SeoDescriptionAr,
    string? SeoDescriptionEn,
    string? CanonicalUrl,
    int SortOrder);
