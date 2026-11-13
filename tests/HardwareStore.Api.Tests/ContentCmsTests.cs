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
        var article = await services.Articles.CreateAsync(Article("first-post"), Guid.NewGuid());
        Assert.Equal(ContentStatus.Draft, article.Status);
        Assert.Empty((await services.Articles.ListPublicAsync(1, 10, null)).Items);

        var updated = await services.Articles.UpdateAsync(
            article.Id,
            Article("first-post") with { SummaryAr = "ملخص محدث", RowVersion = article.RowVersion },
            Guid.NewGuid());
        Assert.Equal("ملخص محدث", updated.SummaryAr);

        var published = await services.Articles.PublishAsync(updated.Id, updated.RowVersion, Guid.NewGuid());
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
            services.Articles.CreateAsync(Article("first-post"), Guid.NewGuid()));
        await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Articles.CreateAsync(
                Article("bad-html") with { BodyEn = "<img src=x onerror=alert(1)>" },
                Guid.NewGuid()));
        await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Articles.UpdateAsync(
                article.Id,
                Article("first-post") with { RowVersion = article.RowVersion - 1 },
                Guid.NewGuid()));
    }

    [Fact]
    public async Task Faqs_and_versioned_policies()
    {
        var database = await fixture.CreateDatabaseAsync();
        var services = ContentServices.Create(database, TimeProvider.System);

        var faq = await services.Faqs.CreateAsync(
            new FaqInput("هل الشحن متاح؟", "Is shipping available?", "الإجابة", "The answer", null, 0));
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

    private static ArticleInput Article(string slug) =>
        new(
            slug,
            "عنوان عربي",
            "English title",
            "ملخص",
            "Summary",
            "محتوى المقالة",
            "Article body",
            null,
            "graphics-cards",
            "gpu, guide",
            null,
            null,
            null,
            null,
            "/articles/" + slug,
            0);

    private sealed record ContentServices(
        ContentArticleService Articles,
        ContentFaqService Faqs,
        ContentPolicyService Policies)
    {
        public static ContentServices Create(OwnerBootstrapDatabase database, TimeProvider clock)
        {
            var factory = new StorefrontFactory(database.ConnectionString);
            return new ContentServices(
                new ContentArticleService(factory, clock, new NoopUsageSink()),
                new ContentFaqService(factory, clock),
                new ContentPolicyService(factory, clock));
        }
    }

    private sealed class StorefrontFactory(string connectionString)
        : IDbContextFactory<StorefrontDbContext>
    {
        public StorefrontDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class NoopUsageSink : HardwareStore.Modules.Catalog.IContentMediaUsageSink
    {
        public Task RegisterAsync(
            string referenceType,
            Guid referenceId,
            string referenceTitle,
            string? storageKey,
            bool isPublished,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(
            string referenceType,
            Guid referenceId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
