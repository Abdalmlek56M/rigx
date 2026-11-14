namespace HardwareStore.Modules.Storefront;

public sealed record ArticleView(
    Guid Id,
    string Slug,
    string TitleAr,
    string TitleEn,
    string SummaryAr,
    string SummaryEn,
    IReadOnlyList<ArticleBlock> BlocksAr,
    IReadOnlyList<ArticleBlock> BlocksEn,
    string? MainImageStorageKey,
    string? CategorySlug,
    IReadOnlyList<string> Tags,
    string Status,
    long ViewCount,
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
    IReadOnlyList<ArticleBlock>? BlocksAr,
    IReadOnlyList<ArticleBlock>? BlocksEn,
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

/// <summary>Publish now, or at <paramref name="PublishAtUtc"/> when it is in the future (scheduled).</summary>
public sealed record ArticlePublishRequest(long RowVersion, DateTimeOffset? PublishAtUtc = null);

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
    string? Topic,
    string Scope,
    Guid? CategoryId,
    string? ItemCode,
    bool HelpPageOnly,
    IReadOnlyList<Guid> ExcludedCategoryIds,
    IReadOnlyList<string> ExcludedItemCodes,
    string? YouTubeId,
    string? YouTubeEmbedUrl,
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
    string? Topic,
    string? Scope,
    Guid? CategoryId,
    string? ItemCode,
    bool HelpPageOnly,
    IReadOnlyList<Guid>? ExcludedCategoryIds,
    IReadOnlyList<string>? ExcludedItemCodes,
    string? YouTubeUrl,
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
