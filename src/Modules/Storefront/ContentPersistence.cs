using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

public static class ContentStatus
{
    public const string Draft = "Draft";
    public const string Review = "Review";
    public const string Published = "Published";
    public const string Archived = "Archived";
}

public static class ContentPolicyKind
{
    public static readonly IReadOnlyList<string> All =
        ["Shipping", "Returns", "Exchanges", "Warranty", "Terms"];
}

public sealed class HomepageLayout
{
    public Guid Id { get; set; }
    public required string Status { get; set; }
    public required string LayoutJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public Guid? UpdatedByUserAccountId { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class ContentArticle
{
    public Guid Id { get; set; }
    public required string Slug { get; set; }
    public required string TitleAr { get; set; }
    public required string TitleEn { get; set; }
    public required string SummaryAr { get; set; }
    public required string SummaryEn { get; set; }
    public required string BodyAr { get; set; }
    public required string BodyEn { get; set; }
    public string? MainImageStorageKey { get; set; }
    public string? CategorySlug { get; set; }
    public string TagsCsv { get; set; } = "";
    public required string Status { get; set; } = ContentStatus.Draft;
    public string? SeoTitleAr { get; set; }
    public string? SeoTitleEn { get; set; }
    public string? SeoDescriptionAr { get; set; }
    public string? SeoDescriptionEn { get; set; }
    public string? CanonicalUrl { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class ContentArticleRevision
{
    public Guid Id { get; set; }
    public Guid ArticleId { get; set; }
    public required string SnapshotJson { get; set; }
    public string Reason { get; set; } = "Saved";
    public Guid? ActorUserAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ContentFaq
{
    public Guid Id { get; set; }
    public required string QuestionAr { get; set; }
    public required string QuestionEn { get; set; }
    public required string AnswerAr { get; set; }
    public required string AnswerEn { get; set; }
    public string? Category { get; set; }
    public int SortOrder { get; set; }
    public required string Status { get; set; } = ContentStatus.Draft;
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class ContentPolicy
{
    public Guid Id { get; set; }
    public required string Kind { get; set; }
    public required string TitleAr { get; set; }
    public required string TitleEn { get; set; }
    public required string BodyAr { get; set; }
    public required string BodyEn { get; set; }
    public required string Status { get; set; } = ContentStatus.Draft;
    public int VersionNumber { get; set; } = 1;
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed class ContentBrandReference
{
    public Guid Id { get; set; }
}

public static class ContentModelConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<HomepageLayout>(entity =>
        {
            entity.ToTable("homepage_layouts");
            entity.HasKey(layout => layout.Id);
            entity.Property(layout => layout.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(layout => layout.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(layout => layout.LayoutJson).HasColumnName("layout_json").HasColumnType("jsonb").IsRequired();
            entity.Property(layout => layout.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(layout => layout.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(layout => layout.PublishedAtUtc).HasColumnName("published_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(layout => layout.UpdatedByUserAccountId).HasColumnName("updated_by_user_account_id");
            entity.Property(layout => layout.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(layout => layout.Status).IsUnique().HasDatabaseName("ux_homepage_layouts_status");
        });

        model.Entity<ContentArticle>(entity =>
        {
            entity.ToTable("content_articles");
            entity.HasKey(article => article.Id);
            entity.Property(article => article.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(article => article.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
            entity.HasIndex(article => article.Slug).IsUnique().HasDatabaseName("ux_content_articles_slug");
            entity.Property(article => article.TitleAr).HasColumnName("title_ar").HasMaxLength(200).IsRequired();
            entity.Property(article => article.TitleEn).HasColumnName("title_en").HasMaxLength(200).IsRequired();
            entity.Property(article => article.SummaryAr).HasColumnName("summary_ar").HasMaxLength(600).IsRequired();
            entity.Property(article => article.SummaryEn).HasColumnName("summary_en").HasMaxLength(600).IsRequired();
            entity.Property(article => article.BodyAr).HasColumnName("body_ar").HasMaxLength(20000).IsRequired();
            entity.Property(article => article.BodyEn).HasColumnName("body_en").HasMaxLength(20000).IsRequired();
            entity.Property(article => article.MainImageStorageKey).HasColumnName("main_image_storage_key").HasMaxLength(512);
            entity.Property(article => article.CategorySlug).HasColumnName("category_slug").HasMaxLength(160);
            entity.Property(article => article.TagsCsv).HasColumnName("tags_csv").HasMaxLength(512).IsRequired();
            entity.Property(article => article.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(article => article.SeoTitleAr).HasColumnName("seo_title_ar").HasMaxLength(200);
            entity.Property(article => article.SeoTitleEn).HasColumnName("seo_title_en").HasMaxLength(200);
            entity.Property(article => article.SeoDescriptionAr).HasColumnName("seo_description_ar").HasMaxLength(320);
            entity.Property(article => article.SeoDescriptionEn).HasColumnName("seo_description_en").HasMaxLength(320);
            entity.Property(article => article.CanonicalUrl).HasColumnName("canonical_url").HasMaxLength(512);
            entity.Property(article => article.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(article => article.PublishedAtUtc).HasColumnName("published_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(article => article.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(article => article.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(article => article.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(article => new { article.Status, article.PublishedAtUtc })
                .HasDatabaseName("ix_content_articles_status_published");
        });

        model.Entity<ContentArticleRevision>(entity =>
        {
            entity.ToTable("content_article_revisions");
            entity.HasKey(revision => revision.Id);
            entity.Property(revision => revision.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(revision => revision.ArticleId).HasColumnName("article_id");
            entity.Property(revision => revision.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("jsonb").IsRequired();
            entity.Property(revision => revision.Reason).HasColumnName("reason").HasMaxLength(32).IsRequired();
            entity.Property(revision => revision.ActorUserAccountId).HasColumnName("actor_user_account_id");
            entity.Property(revision => revision.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.HasIndex(revision => revision.ArticleId).HasDatabaseName("ix_content_article_revisions_article");
            entity.HasOne<ContentArticle>().WithMany().HasForeignKey(revision => revision.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<ContentFaq>(entity =>
        {
            entity.ToTable("content_faqs");
            entity.HasKey(faq => faq.Id);
            entity.Property(faq => faq.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(faq => faq.QuestionAr).HasColumnName("question_ar").HasMaxLength(400).IsRequired();
            entity.Property(faq => faq.QuestionEn).HasColumnName("question_en").HasMaxLength(400).IsRequired();
            entity.Property(faq => faq.AnswerAr).HasColumnName("answer_ar").HasMaxLength(4000).IsRequired();
            entity.Property(faq => faq.AnswerEn).HasColumnName("answer_en").HasMaxLength(4000).IsRequired();
            entity.Property(faq => faq.Category).HasColumnName("category").HasMaxLength(64);
            entity.Property(faq => faq.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            entity.Property(faq => faq.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(faq => faq.PublishedAtUtc).HasColumnName("published_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(faq => faq.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(faq => faq.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(faq => faq.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(faq => new { faq.Status, faq.SortOrder }).HasDatabaseName("ix_content_faqs_status_sort");
        });

        model.Entity<ContentPolicy>(entity =>
        {
            entity.ToTable("content_policies");
            entity.HasKey(policy => policy.Id);
            entity.Property(policy => policy.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(policy => policy.Kind).HasColumnName("kind").HasMaxLength(32).IsRequired();
            entity.Property(policy => policy.TitleAr).HasColumnName("title_ar").HasMaxLength(200).IsRequired();
            entity.Property(policy => policy.TitleEn).HasColumnName("title_en").HasMaxLength(200).IsRequired();
            entity.Property(policy => policy.BodyAr).HasColumnName("body_ar").HasMaxLength(40000).IsRequired();
            entity.Property(policy => policy.BodyEn).HasColumnName("body_en").HasMaxLength(40000).IsRequired();
            entity.Property(policy => policy.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(policy => policy.VersionNumber).HasColumnName("version_number");
            entity.Property(policy => policy.PublishedAtUtc).HasColumnName("published_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(policy => policy.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(policy => policy.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            entity.Property(policy => policy.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            entity.HasIndex(policy => new { policy.Kind, policy.VersionNumber }).IsUnique()
                .HasDatabaseName("ux_content_policies_kind_version");
        });

        model.Entity<ContentBrandReference>(entity =>
        {
            entity.ToTable("brands", table => table.ExcludeFromMigrations());
            entity.HasKey(brand => brand.Id);
            entity.Property(brand => brand.Id).HasColumnName("id").ValueGeneratedNever();
        });
    }
}
