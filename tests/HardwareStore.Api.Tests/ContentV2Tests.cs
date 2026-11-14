using System.Text.Json;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class ContentV2PostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly DateTimeOffset Start = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Article_blocks_round_trip_and_publishing_needs_both_bodies()
    {
        var services = ContentServices.Create(await fixture.CreateDatabaseAsync(), TimeProvider.System);
        var blocks = new[]
        {
            new ArticleBlock(ArticleBlockType.Heading, Level: 2, Text: "Intro"),
            new ArticleBlock(ArticleBlockType.Paragraph, Spans: [new ArticleSpan("See "), new ArticleSpan("this", Bold: true, Href: "/help")]),
            new ArticleBlock(ArticleBlockType.Table, Rows: [["A", "B"], ["1", "2"]]),
            new ArticleBlock(ArticleBlockType.Product, ItemCode: "rx 142"),
            new ArticleBlock(ArticleBlockType.YouTube, YouTubeId: "https://youtu.be/dQw4w9WgXcQ")
        };
        var created = await services.Articles.CreateAsync(
            ContentArticles.Input("blocks") with { BlocksAr = blocks, BlocksEn = [] }, Guid.NewGuid());

        var loaded = await services.Articles.GetAdminAsync(created.Id);

        Assert.Equal(5, loaded.BlocksAr.Count);
        Assert.Equal("RX-0142", loaded.BlocksAr[3].ItemCode);
        Assert.Equal("dQw4w9WgXcQ", loaded.BlocksAr[4].YouTubeId);
        Assert.Equal([["A", "B"], ["1", "2"]], loaded.BlocksAr[2].Rows!);
        Assert.Equal(("this", true, "/help"), (loaded.BlocksAr[1].Spans![1].Text, loaded.BlocksAr[1].Spans![1].Bold, loaded.BlocksAr[1].Spans![1].Href));
        Assert.Empty(loaded.BlocksEn);
        var error = await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Articles.PublishAsync(created.Id, new ArticlePublishRequest(created.RowVersion), Guid.NewGuid()));
        Assert.Equal(400, error.StatusCode);
    }

    [Fact]
    public async Task A_scheduled_article_is_hidden_until_its_time_and_admin_sees_it_as_scheduled()
    {
        var clock = new MutableTimeProvider(Start);
        var services = ContentServices.Create(await fixture.CreateDatabaseAsync(), clock);
        var created = await services.Articles.CreateAsync(ContentArticles.Input("later"), Guid.NewGuid());

        var scheduled = await services.Articles.PublishAsync(
            created.Id, new ArticlePublishRequest(created.RowVersion, Start.AddDays(2)), Guid.NewGuid());

        Assert.Equal(ArticleSchedule.Scheduled, scheduled.Status);
        Assert.Equal(Start.AddDays(2), scheduled.PublishedAtUtc);
        Assert.Empty((await services.Articles.ListPublicAsync(1, 10, null)).Items);
        await Assert.ThrowsAsync<StorefrontException>(() => services.Articles.GetPublicAsync("later"));
        await Assert.ThrowsAsync<StorefrontException>(() => services.Articles.RecordViewAsync("later"));
        Assert.Single((await services.Articles.ListAdminAsync(1, 10, "Scheduled", null)).Items);
        Assert.Empty((await services.Articles.ListAdminAsync(1, 10, "Published", null)).Items);
        Assert.Contains(await services.Articles.ListRevisionsAsync(created.Id), revision => revision.Reason == "Scheduled");

        clock.Advance(TimeSpan.FromDays(3));

        Assert.Equal(ContentStatus.Published, (await services.Articles.GetPublicAsync("later")).Status);
        Assert.Single((await services.Articles.ListPublicAsync(1, 10, null)).Items);
        Assert.Empty((await services.Articles.ListAdminAsync(1, 10, "Scheduled", null)).Items);
        Assert.Single((await services.Articles.ListAdminAsync(1, 10, "Published", null)).Items);
    }

    [Fact]
    public async Task The_view_counter_counts_only_live_articles()
    {
        var services = ContentServices.Create(await fixture.CreateDatabaseAsync(), TimeProvider.System);
        var created = await services.Articles.CreateAsync(ContentArticles.Input("counted"), Guid.NewGuid());
        await Assert.ThrowsAsync<StorefrontException>(() => services.Articles.RecordViewAsync("counted"));
        await Assert.ThrowsAsync<StorefrontException>(() => services.Articles.RecordViewAsync("missing"));
        await services.Articles.PublishAsync(created.Id, new ArticlePublishRequest(created.RowVersion), Guid.NewGuid());

        await services.Articles.RecordViewAsync("counted");
        await services.Articles.RecordViewAsync("counted");

        Assert.Equal(2, (await services.Articles.GetAdminAsync(created.Id)).ViewCount);
    }

    [Fact]
    public async Task A_product_block_resolves_to_the_published_item_card_and_hidden_items_are_missing()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var services = ContentServices.Create(database, TimeProvider.System);
        var shown = await retail.PublishedItemAsync("Shown GPU", 1, 1500m);
        var hidden = await retail.PublishedItemAsync("Hidden GPU", 1, 900m);
        await retail.StockItems.SetVisibilityAsync(new StockVisibilityRequest([hidden.Id], true));
        var body = new[]
        {
            new ArticleBlock(ArticleBlockType.Product, ItemCode: shown.Code),
            new ArticleBlock(ArticleBlockType.Product, ItemCode: hidden.Code),
            new ArticleBlock(ArticleBlockType.Product, ItemCode: "RX-9999")
        };
        var created = await services.Articles.CreateAsync(ContentArticles.Input("with-cards") with { BlocksEn = body }, Guid.NewGuid());
        await services.Articles.PublishAsync(created.Id, new ArticlePublishRequest(created.RowVersion), Guid.NewGuid());

        var article = await services.Articles.GetPublicAsync("with-cards");
        var cards = await retail.Store.GetCardsAsync(
            ArticleBlockRules.ItemCodes(article.BlocksAr.Concat(article.BlocksEn)).Distinct().ToList());

        var card = Assert.Single(cards);
        Assert.Equal(("Shown GPU", shown.Code), (card.ModelName, card.Code));
        Assert.Empty(await retail.Store.GetCardsAsync([]));
    }

    [Fact]
    public async Task Item_faqs_are_product_then_category_path_then_general_without_help_only_or_excluded()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var services = ContentServices.Create(database, TimeProvider.System);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var parent = await services.Taxonomy.SaveCategoryAsync(null, new CategoryInput("Parts", $"parts-{suffix}", null, null, null, 0, true, null));
        var child = await services.Taxonomy.SaveCategoryAsync(null, new CategoryInput("Cards", $"cards-{suffix}", parent.Id, null, null, 0, true, null));
        var other = await services.Taxonomy.SaveCategoryAsync(null, new CategoryInput("Cases", $"cases-{suffix}", null, null, null, 0, true, null));
        var item = await retail.PublishedItemAsync("Card GPU", 1, 1500m);
        item = await retail.StockItems.GetAsync(item.Id, false, false);
        await retail.StockItems.UpdateAsync(item.Id, new StockItemUpdate(item.ModelName, child.Id, null, item.Price, null, null, null, null,
            false, false, null, null, item.RowVersion));
        Assert.Equal([child.Id, parent.Id], await services.Taxonomy.GetCategoryPathIdsAsync(child.Id));

        async Task<FaqView> PublishedAsync(FaqInput input)
        {
            var faq = await services.Faqs.CreateAsync(input);
            return await services.Faqs.SetPublishedAsync(faq.Id, true, faq.RowVersion);
        }
        await PublishedAsync(ContentFaqs.General("general", "general"));
        await PublishedAsync(ContentFaqs.General("help only", "help only", helpPageOnly: true));
        await PublishedAsync(ContentFaqs.General("excluded here", "excluded here", excludedItemCodes: [item.Code]));
        await PublishedAsync(ContentFaqs.ForCategory("parent category", parent.Id));
        await PublishedAsync(ContentFaqs.ForCategory("other category", other.Id));
        await PublishedAsync(ContentFaqs.ForItem("this item", item.Code));
        await PublishedAsync(ContentFaqs.ForItem("another item", "RX-9999"));
        await services.Faqs.CreateAsync(ContentFaqs.ForItem("draft item faq", item.Code));

        var path = await services.Taxonomy.GetCategoryPathIdsAsync(child.Id);
        var onItem = await services.Faqs.ListForItemAsync(item.Code, path);
        var onHelpPage = await services.Faqs.ListPublicAsync();

        Assert.Equal(["this item", "parent category", "general"], onItem.Select(faq => faq.QuestionEn));
        Assert.Equal(["general", "help only", "excluded here"], onHelpPage.Select(faq => faq.QuestionEn));
    }

    [Fact]
    public async Task A_category_faq_with_an_unknown_category_and_a_youtube_answer()
    {
        var services = ContentServices.Create(await fixture.CreateDatabaseAsync(), TimeProvider.System);

        var error = await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Faqs.CreateAsync(ContentFaqs.ForCategory("nowhere", Guid.NewGuid())));
        Assert.Equal(400, error.StatusCode);

        var video = await services.Faqs.CreateAsync(ContentFaqs.General("how", "how") with { YouTubeUrl = "https://youtu.be/dQw4w9WgXcQ", Topic = "Setup" });
        Assert.Equal(("dQw4w9WgXcQ", "https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", "Setup"),
            (video.YouTubeId, video.YouTubeEmbedUrl, video.Topic));
        Assert.Equal(400, (await Assert.ThrowsAsync<StorefrontException>(() =>
            services.Faqs.CreateAsync(ContentFaqs.General("bad", "bad") with { YouTubeUrl = "https://example.com/x" }))).StatusCode);
        Assert.Single(await services.Faqs.ListAdminAsync(scope: FaqScope.General, itemCode: null), faq => faq.Id == video.Id);
    }

    [Fact]
    public async Task Old_text_bodies_become_paragraph_blocks_and_the_video_links_table_is_dropped()
    {
        var name = $"content_v2_migration_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(fixture.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{name}\"";
            await create.ExecuteNonQueryAsync();
        }
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = name }.ConnectionString;
        await using var storefront = new StorefrontDbContext(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);
        await storefront.GetService<IMigrator>().MigrateAsync("20261113205404_M30_CustomerRequests");
        var id = Guid.NewGuid();
        await storefront.Database.ExecuteSqlAsync($"""
            INSERT INTO content_articles (id, slug, title_ar, title_en, summary_ar, summary_en, body_ar, body_en, tags_csv, status, created_at_utc, updated_at_utc)
            VALUES ({id}, 'old', 't', 't', 's', 's', E'First\r\nline\r\n\r\n\r\n  Second  \n\nThird', '', '', 'Draft', now(), now());
            """);

        await storefront.Database.MigrateAsync();

        var json = await storefront.Database.SqlQuery<string>($"SELECT blocks_ar::text AS \"Value\" FROM content_articles WHERE id = {id}").SingleAsync();
        var blocks = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.Equal(["First\nline", "Second", "Third"], blocks.Select(block => block.GetProperty("spans")[0].GetProperty("text").GetString()));
        Assert.All(blocks, block => Assert.Equal(ArticleBlockType.Paragraph, block.GetProperty("type").GetString()));
        var english = await storefront.Database.SqlQuery<string>($"SELECT blocks_en::text AS \"Value\" FROM content_articles WHERE id = {id}").SingleAsync();
        Assert.Equal("[]", english);
        var oldTable = await storefront.Database.SqlQueryRaw<string?>("SELECT to_regclass('article_video_references')::text AS \"Value\"").SingleAsync();
        Assert.Null(oldTable);
    }
}
