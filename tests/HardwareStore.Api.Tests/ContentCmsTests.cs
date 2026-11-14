using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class ContentCmsPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Articles_draft_publish_unpublish_and_revisions()
    {
        var database = await fixture.CreateDatabaseAsync();
        var services = ContentServices.Create(database, TimeProvider.System);
        var article = await services.Articles.CreateAsync(ContentArticles.Input("first-post"), Guid.NewGuid());
        Assert.Equal(ContentStatus.Draft, article.Status);
        Assert.Empty((await services.Articles.ListPublicAsync(1, 10, null)).Items);

        var updated = await services.Articles.UpdateAsync(
            article.Id,
            ContentArticles.Input("first-post") with { SummaryAr = "ملخص محدث", RowVersion = article.RowVersion },
            Guid.NewGuid());
        Assert.Equal("ملخص محدث", updated.SummaryAr);

        var published = await services.Articles.PublishAsync(updated.Id, new ArticlePublishRequest(updated.RowVersion), Guid.NewGuid());
        Assert.Equal(ContentStatus.Published, published.Status);
        var publicPage = await services.Articles.ListPublicAsync(1, 10, null);
        Assert.Single(publicPage.Items);
        Assert.Equal("first-post", (await services.Articles.GetPublicAsync("first-post")).Slug);

        var unpublish = await services.Articles.UnpublishAsync(
            published.Id, published.RowVersion, Guid.NewGuid());
        Assert.Equal(ContentStatus.Draft, unpublish.Status);
        await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Articles.GetPublicAsync("first-post"));

        var revisions = await services.Articles.ListRevisionsAsync(article.Id);
        Assert.True(revisions.Count >= 4);

        await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Articles.CreateAsync(ContentArticles.Input("first-post"), Guid.NewGuid()));
        await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Articles.CreateAsync(
                ContentArticles.Input("bad-html") with { BlocksEn = [ContentArticles.Paragraph("<img src=x onerror=alert(1)>")] },
                Guid.NewGuid()));
        await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Articles.UpdateAsync(
                article.Id,
                ContentArticles.Input("first-post") with { RowVersion = article.RowVersion - 1 },
                Guid.NewGuid()));
    }

    [Fact]
    public async Task Faqs_and_versioned_policies()
    {
        var database = await fixture.CreateDatabaseAsync();
        var services = ContentServices.Create(database, TimeProvider.System);

        var faq = await services.Faqs.CreateAsync(ContentFaqs.General("هل الشحن متاح؟", "Is shipping available?"));
        Assert.Empty(await services.Faqs.ListPublicAsync());
        var faqPublished = await services.Faqs.SetPublishedAsync(faq.Id, true, faq.RowVersion);
        Assert.Equal(ContentStatus.Published, faqPublished.Status);
        Assert.Single(await services.Faqs.ListPublicAsync());
        await services.Faqs.SetPublishedAsync(faq.Id, false, faqPublished.RowVersion);
        Assert.Empty(await services.Faqs.ListPublicAsync());

        var version1 = await services.Policies.CreateVersionAsync("Shipping");
        Assert.Equal(1, version1.VersionNumber);
        await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Policies.PublishAsync(version1.Id, version1.RowVersion));
        var edited1 = await services.Policies.UpdateAsync(
            version1.Id,
            new PolicyInput("الشحن", "Shipping", "نص تجريبي", "Sample text", version1.RowVersion));
        var review1 = await services.Policies.MarkReviewAsync(edited1.Id, edited1.RowVersion);
        var published1 = await services.Policies.PublishAsync(review1.Id, review1.RowVersion);
        Assert.Equal(ContentStatus.Published, (await services.Policies.GetPublicAsync("Shipping")).Status);

        await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Policies.UpdateAsync(
                published1.Id,
                new PolicyInput("x", "x", "x", "x", published1.RowVersion)));

        var version2 = await services.Policies.CreateVersionAsync("Shipping");
        Assert.Equal(2, version2.VersionNumber);
        var edited2 = await services.Policies.UpdateAsync(
            version2.Id,
            new PolicyInput("الشحن 2", "Shipping 2", "نسخة ثانية", "Second version", version2.RowVersion));
        await services.Policies.PublishAsync(edited2.Id, edited2.RowVersion);

        var publicPolicy = await services.Policies.GetPublicAsync("Shipping");
        Assert.Equal(2, publicPolicy.VersionNumber);
        var all = await services.Policies.ListAdminAsync("Shipping");
        Assert.Contains(all, policy => policy.VersionNumber == 1 && policy.Status == ContentStatus.Archived);
        await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Policies.GetPublicAsync("Unknown"));
    }
}

/// <summary>Article inputs for the content tests: a minimal valid article and one-paragraph bodies.</summary>
internal static class ContentArticles
{
    public static ArticleBlock Paragraph(string text) =>
        new(ArticleBlockType.Paragraph, Spans: [new ArticleSpan(text)]);

    public static ArticleInput Input(string slug) =>
        new(
            slug,
            "عنوان عربي",
            "English title",
            "ملخص",
            "Summary",
            [Paragraph("محتوى المقالة")],
            [Paragraph("Article body")],
            null,
            "graphics-cards",
            "gpu, guide",
            null,
            null,
            null,
            null,
            "/articles/" + slug,
            0);
}

internal static class ContentFaqs
{
    public static FaqInput General(string questionAr, string questionEn, bool helpPageOnly = false,
        IReadOnlyList<string>? excludedItemCodes = null) =>
        new(questionAr, questionEn, "الإجابة", "The answer", null, FaqScope.General, null, null, helpPageOnly, null, excludedItemCodes, null, 0);

    public static FaqInput ForCategory(string question, Guid categoryId) =>
        new(question, question, "الإجابة", "The answer", null, FaqScope.Category, categoryId, null, false, null, null, null, 0);

    public static FaqInput ForItem(string question, string itemCode) =>
        new(question, question, "الإجابة", "The answer", null, FaqScope.Product, null, itemCode, false, null, null, null, 0);
}

internal sealed record ContentServices(
    ContentArticleService Articles,
    ContentFaqService Faqs,
    ContentPolicyService Policies,
    TaxonomyService Taxonomy)
{
    public static ContentServices Create(OwnerBootstrapDatabase database, TimeProvider clock)
    {
        var factory = new StorefrontFactory(database.ConnectionString);
        var taxonomy = new TaxonomyService(new CatalogFactory(database.ConnectionString), clock);
        return new ContentServices(
            new ContentArticleService(factory, clock, new NoopUsageSink()),
            new ContentFaqService(factory, taxonomy, clock),
            new ContentPolicyService(factory, clock),
            taxonomy);
    }

    private sealed class StorefrontFactory(string connectionString)
        : IDbContextFactory<StorefrontDbContext>
    {
        public StorefrontDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class CatalogFactory(string connectionString)
        : IDbContextFactory<CatalogDbContext>
    {
        public CatalogDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class NoopUsageSink : IContentMediaUsageSink
    {
        public Task RegisterAsync(
            string referenceType,
            Guid referenceId,
            string referenceTitle,
            IReadOnlyCollection<string> storageKeys,
            bool isPublished,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(
            string referenceType,
            Guid referenceId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
