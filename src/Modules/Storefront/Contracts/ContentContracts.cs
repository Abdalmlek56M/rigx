namespace HardwareStore.Modules.Storefront;

public sealed record ArticleView(
    Guid Id,
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
    string Status,
    string? SeoTitleAr,
    string? SeoTitleEn,
    string? SeoDescriptionAr,
    string? SeoDescriptionEn,
    string? CanonicalUrl,
    int SortOrder,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion);

public sealed record ArticleInput(
    string? Slug,
    string? TitleAr,
    string? TitleEn,
    string? SummaryAr,
    string? SummaryEn,
    string? BodyAr,
    string? BodyEn,
    string? MainImageStorageKey,
    string? CategorySlug,
    string? Tags,
    string? SeoTitleAr,
    string? SeoTitleEn,
    string? SeoDescriptionAr,
    string? SeoDescriptionEn,
    string? CanonicalUrl,
    int SortOrder,
    long RowVersion = 0);

public sealed record ContentPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed record ArticleRevisionView(
    Guid Id,
    string Reason,
    DateTimeOffset CreatedAtUtc,
    Guid? ActorUserAccountId);

public sealed record FaqView(
    Guid Id,
    string QuestionAr,
    string QuestionEn,
    string AnswerAr,
    string AnswerEn,
    string? Category,
    int SortOrder,
    string Status,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion);

public sealed record FaqInput(
    string? QuestionAr,
    string? QuestionEn,
    string? AnswerAr,
    string? AnswerEn,
    string? Category,
    int SortOrder);

public sealed record PolicyView(
    Guid Id,
    string Kind,
    string TitleAr,
    string TitleEn,
    string BodyAr,
    string BodyEn,
    string Status,
    int VersionNumber,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion);

public sealed record PolicyInput(
    string? TitleAr,
    string? TitleEn,
    string? BodyAr,
    string? BodyEn,
    long RowVersion);
