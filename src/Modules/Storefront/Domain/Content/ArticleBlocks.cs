using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Storefront;

/// <summary>Block kinds of a structured article body (one list per language).</summary>
public static class ArticleBlockType
{
    public const string Heading = "heading";
    public const string Paragraph = "paragraph";
    public const string List = "list";
    public const string Table = "table";
    public const string Image = "image";
    public const string Callout = "callout";
    public const string Product = "product";
    public const string YouTube = "youtube";

    public static readonly IReadOnlyList<string> All = [Heading, Paragraph, List, Table, Image, Callout, Product, YouTube];
}

public static class CalloutVariant
{
    public static readonly IReadOnlyList<string> All = ["info", "tip", "warning"];
}

/// <summary>A run of text inside a paragraph, list item or callout: optionally bold, optionally a link.</summary>
public sealed record ArticleSpan(string? Text, bool Bold = false, string? Href = null);

/// <summary>
/// One block of an article body. Only the fields of its <see cref="Type"/> are kept:
/// heading (Level 2|3, Text) · paragraph (Spans) · list (Ordered, Items = one span list per item) ·
/// table (Rows, first row is the header) · image (ImageKey, Alt, Caption) · callout (Variant, Spans) ·
/// product (ItemCode, a store item RX-xxxx shown as a live card) · youtube (YouTubeId; a YouTube URL is accepted on input).
/// </summary>
public sealed record ArticleBlock(
    string? Type,
    int? Level = null,
    string? Text = null,
    IReadOnlyList<ArticleSpan>? Spans = null,
    bool? Ordered = null,
    IReadOnlyList<IReadOnlyList<ArticleSpan>>? Items = null,
    IReadOnlyList<IReadOnlyList<string>>? Rows = null,
    string? ImageKey = null,
    string? Alt = null,
    string? Caption = null,
    string? Variant = null,
    string? ItemCode = null,
    string? YouTubeId = null);

/// <summary>Article body rules: known block types, plain text only, safe links, size limits.</summary>
public static class ArticleBlockRules
{
    public const int MaxBlocks = 200;
    public const int MaxTextLength = 40_000;
    private const int MaxSpans = 50;
    private const int MaxListItems = 50;
    private const int MaxTableRows = 30;
    private const int MaxTableColumns = 8;

    /// <summary>Validates and trims a body; returns only the fields each block type uses.</summary>
    public static IReadOnlyList<ArticleBlock> Normalize(IReadOnlyList<ArticleBlock>? blocks, string field)
    {
        if (blocks is null) return [];
        if (blocks.Count > MaxBlocks)
            throw new StorefrontException(400, $"{field} has more than {MaxBlocks} blocks.");
        var result = blocks.Select((block, index) => NormalizeBlock(block, $"{field} block {index + 1}")).ToList();
        if (result.Sum(TextLength) > MaxTextLength)
            throw new StorefrontException(400, $"{field} is too long.");
        return result;
    }

    /// <summary>The store item codes the body shows as product cards.</summary>
    public static IEnumerable<string> ItemCodes(IEnumerable<ArticleBlock> blocks) =>
        blocks.Where(block => block.Type == ArticleBlockType.Product && block.ItemCode is not null).Select(block => block.ItemCode!);

    /// <summary>The media library keys the body shows as images.</summary>
    public static IEnumerable<string> ImageKeys(IEnumerable<ArticleBlock> blocks) =>
        blocks.Where(block => block.Type == ArticleBlockType.Image && block.ImageKey is not null).Select(block => block.ImageKey!);

    private static ArticleBlock NormalizeBlock(ArticleBlock block, string field) => block.Type?.Trim().ToLowerInvariant() switch
    {
        ArticleBlockType.Heading => new ArticleBlock(ArticleBlockType.Heading,
            Level: block.Level is 2 or 3 ? block.Level : throw new StorefrontException(400, $"{field}: heading level must be 2 or 3."),
            Text: ContentText.Required(block.Text, 200, field)),
        ArticleBlockType.Paragraph => new ArticleBlock(ArticleBlockType.Paragraph, Spans: Spans(block.Spans, field)),
        ArticleBlockType.List => new ArticleBlock(ArticleBlockType.List, Ordered: block.Ordered ?? false,
            Items: Items(block.Items, field)),
        ArticleBlockType.Table => new ArticleBlock(ArticleBlockType.Table, Rows: Rows(block.Rows, field)),
        ArticleBlockType.Image => new ArticleBlock(ArticleBlockType.Image,
            ImageKey: ContentText.Required(block.ImageKey, 512, $"{field} image"),
            Alt: ContentText.Required(block.Alt, 200, $"{field} image description"),
            Caption: ContentText.Optional(block.Caption, 300, $"{field} caption")),
        ArticleBlockType.Callout => new ArticleBlock(ArticleBlockType.Callout,
            Variant: Variant(block.Variant, field), Spans: Spans(block.Spans, field)),
        ArticleBlockType.Product => new ArticleBlock(ArticleBlockType.Product,
            ItemCode: PublicCode.TryNormalize(block.ItemCode, PublicCode.StockItem, out var code)
                ? code : throw new StorefrontException(400, $"{field}: product must be a store item code like RX-0142.")),
        ArticleBlockType.YouTube => new ArticleBlock(ArticleBlockType.YouTube, YouTubeId: YouTubeUrl.ToVideoId(block.YouTubeId)),
        _ => throw new StorefrontException(400, $"{field}: unknown block type.")
    };

    private static IReadOnlyList<ArticleSpan> Spans(IReadOnlyList<ArticleSpan>? spans, string field)
    {
        if (spans is null || spans.Count == 0)
            throw new StorefrontException(400, $"{field} needs text.");
        if (spans.Count > MaxSpans)
            throw new StorefrontException(400, $"{field} has too many text parts.");
        return spans.Select(span => new ArticleSpan(ContentText.Inline(span.Text, 4000, field), span.Bold,
            ContentLink.Optional(span.Href, field))).ToList();
    }

    private static IReadOnlyList<IReadOnlyList<ArticleSpan>> Items(IReadOnlyList<IReadOnlyList<ArticleSpan>>? items, string field)
    {
        if (items is null || items.Count == 0)
            throw new StorefrontException(400, $"{field}: a list needs at least one item.");
        if (items.Count > MaxListItems)
            throw new StorefrontException(400, $"{field}: a list can have at most {MaxListItems} items.");
        return items.Select(item => Spans(item, field)).ToList();
    }

    private static IReadOnlyList<IReadOnlyList<string>> Rows(IReadOnlyList<IReadOnlyList<string>>? rows, string field)
    {
        if (rows is null || rows.Count < 2)
            throw new StorefrontException(400, $"{field}: a table needs a header row and at least one row.");
        if (rows.Count > MaxTableRows)
            throw new StorefrontException(400, $"{field}: a table can have at most {MaxTableRows} rows.");
        var columns = rows[0]?.Count ?? 0;
        if (columns is < 1 or > MaxTableColumns || rows.Any(row => row is null || row.Count != columns))
            throw new StorefrontException(400, $"{field}: every table row needs the same 1–{MaxTableColumns} columns.");
        return rows.Select(row => (IReadOnlyList<string>)row.Select(cell => ContentText.Optional(cell, 300, field) ?? "").ToList()).ToList();
    }

    private static string Variant(string? variant, string field)
    {
        var value = variant?.Trim().ToLowerInvariant() ?? "info";
        return CalloutVariant.All.Contains(value) ? value : throw new StorefrontException(400, $"{field}: unknown callout kind.");
    }

    private static int TextLength(ArticleBlock block) =>
        (block.Text?.Length ?? 0) + (block.Caption?.Length ?? 0) + (block.Alt?.Length ?? 0)
        + (block.Spans?.Sum(span => span.Text?.Length ?? 0) ?? 0)
        + (block.Items?.Sum(item => item.Sum(span => span.Text?.Length ?? 0)) ?? 0)
        + (block.Rows?.Sum(row => row.Sum(cell => cell.Length)) ?? 0);
}

/// <summary>Links in content: http(s) URLs, root-relative site paths or WhatsApp/tel/mailto.</summary>
internal static class ContentLink
{
    public static string? Optional(string? href, string field)
    {
        var value = ContentText.Optional(href, 512, $"{field} link");
        if (value is null) return null;
        if (value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal)) return value;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" or "mailto" or "tel")
            return value;
        throw new StorefrontException(400, $"{field}: links must be http(s), mailto, tel or a site path.");
    }
}
