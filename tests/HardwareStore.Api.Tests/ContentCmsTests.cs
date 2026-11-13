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
    public async Task Homepage_draft_publish_and_reference_validation()
    {
        var database = await fixture.CreateDatabaseAsync();
        var category = await SeedCategoryAsync(database, "graphics-cards");
        var services = ContentServices.Create(database, TimeProvider.System);

        var initial = await services.Homepage.GetPublishedAsync();
        Assert.Contains("\"version\":1", initial.LayoutJson);

        var draft = await services.Homepage.GetDraftAsync();
        var saved = await services.Homepage.SaveDraftAsync(
            new HomepageLayoutUpdateRequest(Layout(category.Id, "hero"), draft.RowVersion),
            Guid.NewGuid());
        Assert.Equal(1, saved.RowVersion);

        var published = await services.Homepage.PublishAsync(saved.RowVersion, Guid.NewGuid());
        Assert.NotNull(published.PublishedAtUtc);
        var publicLayout = await services.Homepage.GetPublishedAsync();
        Assert.Contains(category.Id.ToString(), publicLayout.LayoutJson, StringComparison.OrdinalIgnoreCase);

        // Editing the draft after publishing never changes the public layout.
        var draft2 = await services.Homepage.GetDraftAsync();
        var save2 = await services.Homepage.SaveDraftAsync(
            new HomepageLayoutUpdateRequest(Layout(category.Id, "brands"), draft2.RowVersion),
            Guid.NewGuid());
        Assert.Contains("\"categories\"", save2.LayoutJson);
        Assert.Contains("hero", (await services.Homepage.GetPublishedAsync()).LayoutJson);

        var invalidDraft = await services.Homepage.SaveDraftAsync(
            new HomepageLayoutUpdateRequest(Layout(Guid.NewGuid(), "hero"), save2.RowVersion),
            Guid.NewGuid());
        await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Homepage.PublishAsync(invalidDraft.RowVersion, Guid.NewGuid()));

        await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Homepage.SaveDraftAsync(
                new HomepageLayoutUpdateRequest("<script>alert(1)</script>", 0),
                Guid.NewGuid()));
    }

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

    private static string Layout(Guid categoryId, string firstSection) =>
        "{\"version\":1,\"sections\":[" +
        $"{{\"id\":\"{firstSection}\",\"visible\":true}}," +
        "{\"id\":\"categories\",\"visible\":true}]," +
        "\"rails\":{" +
        "\"brands\":{\"title\":\"\",\"lead\":\"\",\"contentType\":\"brands\",\"limit\":10,\"itemOrder\":[],\"hiddenItemIds\":[]}," +
        "\"categories\":{\"title\":\"Categories\",\"lead\":\"\",\"contentType\":\"categories\",\"limit\":8," +
        $"\"itemOrder\":[\"{categoryId}\"],\"hiddenItemIds\":[]}}}}}}";

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

    private static async Task<CatalogCategory> SeedCategoryAsync(
        OwnerBootstrapDatabase database,
        string slug)
    {
        await using var catalog = database.CreateCatalogContext();
        var category = new CatalogCategory
        {
            Id = Guid.CreateVersion7(),
            Name = "Graphics Cards",
            Slug = slug,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
        catalog.Categories.Add(category);
        await catalog.SaveChangesAsync();
        return category;
    }

    private sealed record ContentServices(
        HomepageContentService Homepage,
        ContentArticleService Articles,
        ContentFaqService Faqs,
        ContentPolicyService Policies)
    {
        public static ContentServices Create(OwnerBootstrapDatabase database, TimeProvider clock)
        {
            var factory = new StorefrontFactory(database.ConnectionString);
            return new ContentServices(
                new HomepageContentService(factory, clock),
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
