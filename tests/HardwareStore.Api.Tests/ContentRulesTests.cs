using HardwareStore.Modules.Storefront;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class ArticleBlockRulesTests
{
    private static ArticleSpan Span(string text, bool bold = false, string? href = null) => new(text, bold, href);

    private static ArticleBlock Paragraph(params ArticleSpan[] spans) => new(ArticleBlockType.Paragraph, Spans: spans);

    private static ArticleBlock Normalize(ArticleBlock block) => Assert.Single(ArticleBlockRules.Normalize([block], "Body"));

    [Fact]
    public void Every_block_type_keeps_only_its_own_fields()
    {
        var heading = Normalize(new ArticleBlock(ArticleBlockType.Heading, Level: 2, Text: "  Title ", Alt: "ignored"));
        Assert.Equal(("Title", 2, null), (heading.Text, heading.Level, heading.Alt));

        var paragraph = Normalize(Paragraph(Span("Hello "), Span("world", bold: true, href: "https://example.com/x")));
        Assert.Equal(["Hello ", "world"], paragraph.Spans!.Select(span => span.Text));
        Assert.True(paragraph.Spans![1].Bold);

        var list = Normalize(new ArticleBlock(ArticleBlockType.List, Items: [[Span("one")], [Span("two")]]));
        Assert.Equal(false, list.Ordered);
        Assert.Equal(2, list.Items!.Count);

        var table = Normalize(new ArticleBlock(ArticleBlockType.Table, Rows: [["A", "B"], ["1", "2"]]));
        Assert.Equal(2, table.Rows!.Count);

        var image = Normalize(new ArticleBlock(ArticleBlockType.Image, ImageKey: "content/a.webp", Alt: "A card", Caption: " "));
        Assert.Equal(("content/a.webp", "A card", null), (image.ImageKey, image.Alt, image.Caption));

        var callout = Normalize(new ArticleBlock(ArticleBlockType.Callout, Variant: "TIP", Spans: [Span("note")]));
        Assert.Equal("tip", callout.Variant);
        Assert.Equal("info", Normalize(new ArticleBlock(ArticleBlockType.Callout, Spans: [Span("note")])).Variant);
    }

    [Fact]
    public void Unknown_block_types_and_bad_heading_levels_are_rejected()
    {
        Assert.Throws<StorefrontException>(() => Normalize(new ArticleBlock("script", Text: "x")));
        Assert.Throws<StorefrontException>(() => Normalize(new ArticleBlock(null, Text: "x")));
        Assert.Throws<StorefrontException>(() => Normalize(new ArticleBlock(ArticleBlockType.Heading, Level: 1, Text: "x")));
        Assert.Throws<StorefrontException>(() => Normalize(new ArticleBlock(ArticleBlockType.Heading, Level: 4, Text: "x")));
        Assert.Throws<StorefrontException>(() => Normalize(new ArticleBlock(ArticleBlockType.Callout, Variant: "danger", Spans: [Span("x")])));
    }

    [Fact]
    public void Text_must_be_plain()
    {
        Assert.Throws<StorefrontException>(() => Normalize(Paragraph(Span("<img src=x onerror=alert(1)>"))));
        Assert.Throws<StorefrontException>(() => Normalize(Paragraph()));
    }

    [Fact]
    public void Tables_need_the_same_number_of_columns_in_every_row()
    {
        Assert.Throws<StorefrontException>(() => Normalize(new ArticleBlock(ArticleBlockType.Table, Rows: [["A", "B"], ["1"]])));
        Assert.Throws<StorefrontException>(() => Normalize(new ArticleBlock(ArticleBlockType.Table, Rows: [["A", "B"]])));
        Assert.Throws<StorefrontException>(() => Normalize(new ArticleBlock(ArticleBlockType.Table, Rows: [["A"], []])));
    }

    [Theory]
    [InlineData("/products/rx-0001")]
    [InlineData("https://example.com/a")]
    [InlineData("mailto:shop@example.com")]
    [InlineData("tel:+201000000000")]
    public void Safe_links_are_kept(string href) =>
        Assert.Equal(href, Normalize(Paragraph(Span("link", href: href))).Spans![0].Href);

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.example.com")]
    [InlineData("data:text/html,x")]
    [InlineData("ftp://example.com/file")]
    public void Unsafe_links_are_rejected(string href) =>
        Assert.Throws<StorefrontException>(() => Normalize(Paragraph(Span("link", href: href))));

    [Fact]
    public void Product_codes_are_normalized_and_bad_codes_rejected()
    {
        Assert.Equal("RX-0142", Normalize(new ArticleBlock(ArticleBlockType.Product, ItemCode: "rx 142")).ItemCode);
        Assert.Throws<StorefrontException>(() => Normalize(new ArticleBlock(ArticleBlockType.Product, ItemCode: "ABC")));
    }

    [Fact]
    public void YouTube_urls_become_video_ids()
    {
        Assert.Equal("dQw4w9WgXcQ",
            Normalize(new ArticleBlock(ArticleBlockType.YouTube, YouTubeId: "https://youtu.be/dQw4w9WgXcQ")).YouTubeId);
        Assert.Throws<StorefrontException>(() => Normalize(new ArticleBlock(ArticleBlockType.YouTube, YouTubeId: "https://example.com/v")));
    }

    [Fact]
    public void Codes_and_image_keys_are_collected_from_the_body()
    {
        var body = ArticleBlockRules.Normalize(
        [
            new ArticleBlock(ArticleBlockType.Product, ItemCode: "RX-0001"),
            new ArticleBlock(ArticleBlockType.Image, ImageKey: "content/a.webp", Alt: "A"),
            Paragraph(Span("x"))
        ], "Body");
        Assert.Equal(["RX-0001"], ArticleBlockRules.ItemCodes(body));
        Assert.Equal(["content/a.webp"], ArticleBlockRules.ImageKeys(body));
    }

    [Fact]
    public void A_body_has_a_block_limit()
    {
        var blocks = Enumerable.Repeat(Paragraph(Span("x")), ArticleBlockRules.MaxBlocks + 1).ToList();
        Assert.Throws<StorefrontException>(() => ArticleBlockRules.Normalize(blocks, "Body"));
        Assert.Empty(ArticleBlockRules.Normalize(null, "Body"));
    }
}

public sealed class ArticleScheduleTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_future_time_is_kept_and_a_missing_time_means_now()
    {
        Assert.Equal(Now.AddDays(3), ArticleSchedule.PublishAt(null, Now.AddDays(3), Now));
        Assert.Equal(Now, ArticleSchedule.PublishAt(null, null, Now));
    }

    [Fact]
    public void A_past_or_too_distant_time_is_rejected()
    {
        Assert.Throws<StorefrontException>(() => ArticleSchedule.PublishAt(null, Now.AddMinutes(-1), Now));
        Assert.Throws<StorefrontException>(() => ArticleSchedule.PublishAt(null, Now, Now));
        Assert.Throws<StorefrontException>(() => ArticleSchedule.PublishAt(null, Now.AddDays(400), Now));
    }

    [Fact]
    public void Publishing_again_keeps_the_first_publish_date()
    {
        var first = Now.AddDays(-10);
        Assert.Equal(first, ArticleSchedule.PublishAt(first, null, Now));
        Assert.Equal(Now, ArticleSchedule.PublishAt(Now.AddDays(2), null, Now));
    }

    [Fact]
    public void Display_status_shows_scheduled_only_for_published_future_articles()
    {
        Assert.Equal(ArticleSchedule.Scheduled, ArticleSchedule.DisplayStatus(ContentStatus.Published, Now.AddHours(1), Now));
        Assert.Equal(ContentStatus.Published, ArticleSchedule.DisplayStatus(ContentStatus.Published, Now.AddHours(-1), Now));
        Assert.Equal(ContentStatus.Draft, ArticleSchedule.DisplayStatus(ContentStatus.Draft, Now.AddHours(1), Now));
    }
}

public sealed class FaqRulesTests
{
    private static readonly Guid Parent = Guid.NewGuid();
    private static readonly Guid Child = Guid.NewGuid();

    private static FaqTarget Target(string scope, Guid? categoryId = null, string? itemCode = null, bool helpOnly = false,
        Guid[]? excludedCategories = null, string[]? excludedItems = null) =>
        FaqRules.Target(scope, categoryId, itemCode, helpOnly, excludedCategories, excludedItems);

    [Fact]
    public void Scope_fields_are_required_for_their_scope()
    {
        Assert.Throws<StorefrontException>(() => Target("Everywhere"));
        Assert.Throws<StorefrontException>(() => Target(FaqScope.Category));
        Assert.Throws<StorefrontException>(() => Target(FaqScope.Product));
        Assert.Throws<StorefrontException>(() => Target(FaqScope.Product, itemCode: "nope"));
        Assert.Equal("RX-0007", Target("product", itemCode: "rx 7").ItemCode);
    }

    [Fact]
    public void Narrow_scopes_drop_fields_that_do_not_apply()
    {
        var product = Target(FaqScope.Product, categoryId: Parent, itemCode: "RX-0001", helpOnly: true, excludedItems: ["RX-0002"]);
        Assert.Equal((null, false), (product.CategoryId, product.HelpPageOnly));
        Assert.Empty(product.ExcludedItemCodes);
        var category = Target(FaqScope.Category, Parent, helpOnly: true, excludedCategories: [Parent, Child]);
        Assert.False(category.HelpPageOnly);
        Assert.Equal([Child], category.ExcludedCategoryIds);
    }

    [Fact]
    public void General_faqs_show_everywhere_unless_help_page_only_or_excluded()
    {
        Assert.True(FaqRules.ShowsOnItem(Target(FaqScope.General), "RX-0001", []));
        Assert.False(FaqRules.ShowsOnItem(Target(FaqScope.General, helpOnly: true), "RX-0001", []));
        Assert.False(FaqRules.ShowsOnItem(Target(FaqScope.General, excludedItems: ["RX-0001"]), "RX-0001", []));
        Assert.False(FaqRules.ShowsOnItem(Target(FaqScope.General, excludedCategories: [Parent]), "RX-0001", [Child, Parent]));
        Assert.True(FaqRules.ShowsOnItem(Target(FaqScope.General, excludedCategories: [Parent]), "RX-0001", [Child]));
    }

    [Fact]
    public void Category_faqs_show_for_the_category_and_its_sub_categories()
    {
        var faq = Target(FaqScope.Category, Parent);
        Assert.True(FaqRules.ShowsOnItem(faq, "RX-0001", [Child, Parent]));
        Assert.False(FaqRules.ShowsOnItem(faq, "RX-0001", [Child]));
        Assert.False(FaqRules.ShowsOnItem(Target(FaqScope.Category, Parent, excludedCategories: [Child]), "RX-0001", [Child, Parent]));
        Assert.False(FaqRules.ShowsOnItem(Target(FaqScope.Category, Parent, excludedItems: ["RX-0001"]), "RX-0001", [Parent]));
    }

    [Fact]
    public void Product_faqs_show_only_on_their_item()
    {
        var faq = Target(FaqScope.Product, itemCode: "RX-0001");
        Assert.True(FaqRules.ShowsOnItem(faq, "RX-0001", []));
        Assert.False(FaqRules.ShowsOnItem(faq, "RX-0002", []));
    }

    [Fact]
    public void Item_page_order_is_product_then_category_then_general()
    {
        Assert.True(FaqRules.ItemPageRank(FaqScope.Product) < FaqRules.ItemPageRank(FaqScope.Category));
        Assert.True(FaqRules.ItemPageRank(FaqScope.Category) < FaqRules.ItemPageRank(FaqScope.General));
    }
}
