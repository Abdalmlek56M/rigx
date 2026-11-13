using System.Text.Json;
using System.Text.RegularExpressions;
using HardwareStore.Modules.Catalog;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Storefront;

public sealed record HomepageLayoutView(string LayoutJson, long RowVersion, DateTimeOffset? PublishedAtUtc);
public sealed record HomepageLayoutUpdateRequest(string? LayoutJson, long RowVersion);

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

internal static partial class ContentText
{
    [GeneratedRegex("<[a-zA-Z!/][^>]*>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTag();

    public static string Required(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new StorefrontException(400, $"{field} is required.");
        var text = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (text.Length > max)
            throw new StorefrontException(400, $"{field} is too long.");
        if (HtmlTag().IsMatch(text))
            throw new StorefrontException(400, $"{field} must be plain text without HTML.");
        if (text.Any(character => char.IsControl(character) && character is not ('\n' or '\r' or '\t')))
            throw new StorefrontException(400, $"{field} contains unsupported control characters.");
        return text;
    }

    public static string? Optional(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var text = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (text.Length > max)
            throw new StorefrontException(400, $"{field} is too long.");
        if (HtmlTag().IsMatch(text))
            throw new StorefrontException(400, $"{field} must be plain text without HTML.");
        if (text.Any(character => char.IsControl(character) && character is not ('\n' or '\r' or '\t')))
            throw new StorefrontException(400, $"{field} contains unsupported control characters.");
        return text;
    }

    public static string Slug(string? value)
    {
        var slug = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (slug.Length is < 2 or > 200 || !SlugPattern().IsMatch(slug))
            throw new StorefrontException(400, "Slug must use lowercase letters, digits and single hyphens.");
        return slug;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();

    public static IReadOnlyList<string> Tags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];
        var tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tag => tag.Normalize(System.Text.NormalizationForm.FormC))
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
        if (tags.Any(tag => tag.Length > 32))
            throw new StorefrontException(400, "Each tag must be 32 characters or fewer.");
        return tags;
    }
}

public sealed class HomepageContentService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public const string DefaultLayoutJson =
        "{\"version\":1,\"sections\":[" +
        "{\"id\":\"hero\",\"visible\":true},{\"id\":\"categories\",\"visible\":true}," +
        "{\"id\":\"brands\",\"visible\":true},{\"id\":\"offers\",\"visible\":true}," +
        "{\"id\":\"discovery\",\"visible\":true}," +
        "{\"id\":\"guide\",\"visible\":true},{\"id\":\"latest\",\"visible\":true}," +
        "{\"id\":\"closing\",\"visible\":true}]," +
        "\"rails\":{" +
        "\"brands\":{\"title\":\"\",\"lead\":\"\",\"contentType\":\"brands\",\"limit\":10,\"itemOrder\":[],\"hiddenItemIds\":[]}," +
        "\"categories\":{\"title\":\"\",\"lead\":\"\",\"contentType\":\"categories\",\"limit\":10,\"itemOrder\":[],\"hiddenItemIds\":[]}}}";

    public async Task<HomepageLayoutView> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var published = await db.HomepageLayouts.AsNoTracking()
            .SingleOrDefaultAsync(layout => layout.Status == ContentStatus.Published, cancellationToken);
        return published is null
            ? new HomepageLayoutView(DefaultLayoutJson, 0, null)
            : new HomepageLayoutView(published.LayoutJson, published.RowVersion, published.PublishedAtUtc);
    }

    public async Task<HomepageLayoutView> GetDraftAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var draft = await db.HomepageLayouts.AsNoTracking()
            .SingleOrDefaultAsync(layout => layout.Status == ContentStatus.Draft, cancellationToken);
        return draft is null
            ? new HomepageLayoutView(DefaultLayoutJson, 0, null)
            : new HomepageLayoutView(draft.LayoutJson, draft.RowVersion, null);
    }

    public async Task<HomepageLayoutView> SaveDraftAsync(
        HomepageLayoutUpdateRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var layoutJson = NormalizeLayout(request.LayoutJson, validateReferences: false);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var draft = await db.HomepageLayouts
            .SingleOrDefaultAsync(layout => layout.Status == ContentStatus.Draft, cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        if (draft is null)
        {
            if (request.RowVersion != 0)
                throw new StorefrontException(409, "The homepage draft was changed by another user.");
            draft = new HomepageLayout
            {
                Id = Guid.CreateVersion7(),
                Status = ContentStatus.Draft,
                LayoutJson = layoutJson,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                UpdatedByUserAccountId = actorId
            };
            db.HomepageLayouts.Add(draft);
        }
        else
        {
            if (draft.RowVersion != request.RowVersion)
                throw new StorefrontException(409, "The homepage draft was changed by another user.");
            draft.LayoutJson = layoutJson;
            draft.UpdatedAtUtc = now;
            draft.UpdatedByUserAccountId = actorId;
            draft.RowVersion++;
        }
        await db.SaveChangesAsync(cancellationToken);
        return new HomepageLayoutView(draft.LayoutJson, draft.RowVersion, null);
    }

    public async Task<HomepageLayoutView> PublishAsync(
        long rowVersion,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var draft = await db.HomepageLayouts
                .SingleOrDefaultAsync(layout => layout.Status == ContentStatus.Draft, cancellationToken)
                ?? throw new StorefrontException(400, "There is no homepage draft to publish.");
            if (draft.RowVersion != rowVersion)
                throw new StorefrontException(409, "The homepage draft was changed by another user.");
            var layoutJson = NormalizeLayout(draft.LayoutJson, validateReferences: true);
            await ValidateReferencesAsync(db, layoutJson, cancellationToken);

            var now = timeProvider.GetUtcNow().ToUniversalTime();
            draft.LayoutJson = layoutJson;
            draft.RowVersion++;
            draft.UpdatedAtUtc = now;
            draft.UpdatedByUserAccountId = actorId;

            var published = await db.HomepageLayouts
                .SingleOrDefaultAsync(layout => layout.Status == ContentStatus.Published, cancellationToken);
            if (published is null)
            {
                published = new HomepageLayout
                {
                    Id = Guid.CreateVersion7(),
                    Status = ContentStatus.Published,
                    LayoutJson = layoutJson,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    PublishedAtUtc = now,
                    UpdatedByUserAccountId = actorId
                };
                db.HomepageLayouts.Add(published);
            }
            else
            {
                published.LayoutJson = layoutJson;
                published.UpdatedAtUtc = now;
                published.PublishedAtUtc = now;
                published.UpdatedByUserAccountId = actorId;
                published.RowVersion++;
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new HomepageLayoutView(published.LayoutJson, draft.RowVersion, published.PublishedAtUtc);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static string NormalizeLayout(string? raw, bool validateReferences)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new StorefrontException(400, "A homepage layout is required.");
        if (raw.Length > 100_000)
            throw new StorefrontException(400, "The homepage layout is too large.");
        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new StorefrontException(400, "The homepage layout must be a JSON object.");
            if (!root.TryGetProperty("version", out var version) || version.GetInt32() != 1)
                throw new StorefrontException(400, "Unsupported homepage layout version.");
            var sections = NormalizeSections(root);
            var rails = NormalizeRails(root);
            return JsonSerializer.Serialize(new { version = 1, sections, rails });
        }
        catch (JsonException)
        {
            throw new StorefrontException(400, "The homepage layout is not valid JSON.");
        }
    }

    private static List<object> NormalizeSections(JsonElement root)
    {
        string[] allowed = ["hero", "categories", "brands", "offers", "discovery", "guide", "latest", "closing"];
        if (!root.TryGetProperty("sections", out var sections) || sections.ValueKind != JsonValueKind.Array)
            throw new StorefrontException(400, "Homepage sections are required.");
        var result = new List<object>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in sections.EnumerateArray())
        {
            var id = section.GetProperty("id").GetString() ?? string.Empty;
            if (!allowed.Contains(id, StringComparer.Ordinal) || !seen.Add(id))
                throw new StorefrontException(400, "The homepage layout contains an unknown or duplicate section.");
            if (!section.TryGetProperty("visible", out var visible) ||
                visible.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new StorefrontException(400, "Each homepage section needs a visible flag.");
            result.Add(new { id, visible = visible.GetBoolean() });
        }
        if (result.Count == 0)
            throw new StorefrontException(400, "At least one homepage section is required.");
        return result;
    }

    private static Dictionary<string, object> NormalizeRails(JsonElement root)
    {
        if (!root.TryGetProperty("rails", out var rails) || rails.ValueKind != JsonValueKind.Object)
            throw new StorefrontException(400, "Homepage rails are required.");
        return new Dictionary<string, object>
        {
            ["brands"] = NormalizeRail(rails, "brands"),
            ["categories"] = NormalizeRail(rails, "categories")
        };
    }

    private static object NormalizeRail(JsonElement rails, string id)
    {
        if (!rails.TryGetProperty(id, out var rail) || rail.ValueKind != JsonValueKind.Object)
            throw new StorefrontException(400, $"Homepage rail '{id}' is required.");
        var contentType = rail.TryGetProperty("contentType", out var type) ? type.GetString() : "brands";
        if (contentType is not ("brands" or "categories" or "products"))
            throw new StorefrontException(400, "A homepage rail has an unsupported content type.");
        var limit = rail.TryGetProperty("limit", out var limitElement) && limitElement.TryGetInt32(out var parsed)
            ? Math.Clamp(parsed, 1, 24)
            : 10;
        return new
        {
            title = ContentText.Optional(ReadString(rail, "title"), 120, "Rail title") ?? "",
            lead = ContentText.Optional(ReadString(rail, "lead"), 300, "Rail lead") ?? "",
            contentType,
            limit,
            itemOrder = ReadIds(rail, "itemOrder"),
            hiddenItemIds = ReadIds(rail, "hiddenItemIds")
        };
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string[] ReadIds(JsonElement rail, string property)
    {
        if (!rail.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            return [];
        var ids = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            var id = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            if (id is null || !Guid.TryParse(id, out _))
                throw new StorefrontException(400, "Homepage rail references must be valid identifiers.");
            if (ids.Count >= 64)
                throw new StorefrontException(400, "A homepage rail has too many references.");
            ids.Add(id);
        }
        return ids.ToArray();
    }

    private static async Task ValidateReferencesAsync(
        StorefrontDbContext db,
        string layoutJson,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(layoutJson);
        var rails = document.RootElement.GetProperty("rails");
        var brandIds = new HashSet<Guid>();
        var categoryIds = new HashSet<Guid>();
        var productIds = new HashSet<Guid>();
        foreach (var railId in new[] { "brands", "categories" })
        {
            var rail = rails.GetProperty(railId);
            var contentType = rail.GetProperty("contentType").GetString();
            var target = contentType switch
            {
                "brands" => brandIds,
                "categories" => categoryIds,
                "products" => productIds,
                _ => brandIds
            };
            foreach (var property in new[] { "itemOrder", "hiddenItemIds" })
            {
                foreach (var item in rail.GetProperty(property).EnumerateArray())
                {
                    if (Guid.TryParse(item.GetString(), out var id))
                        target.Add(id);
                }
            }
        }

        if (brandIds.Count > 0)
        {
            var found = await db.ContentBrands.AsNoTracking()
                .Where(brand => brandIds.Contains(brand.Id))
                .CountAsync(cancellationToken);
            if (found != brandIds.Count)
                throw new StorefrontException(400, "The homepage layout references a brand that does not exist.");
        }
        if (categoryIds.Count > 0)
        {
            var found = await db.WishlistCategories.AsNoTracking()
                .Where(category => categoryIds.Contains(category.Id))
                .CountAsync(cancellationToken);
            if (found != categoryIds.Count)
                throw new StorefrontException(400, "The homepage layout references a category that does not exist.");
        }
        if (productIds.Count > 0)
        {
            var found = await db.WishlistProducts.AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .CountAsync(cancellationToken);
            if (found != productIds.Count)
                throw new StorefrontException(400, "The homepage layout references a product that does not exist.");
        }
    }
}

public sealed class ContentArticleService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider,
    IContentMediaUsageSink mediaUsage)
{
    public async Task<ContentPage<ArticleView>> ListPublicAsync(
        int page,
        int pageSize,
        string? category,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = db.ContentArticles.AsNoTracking()
            .Where(article => article.Status == ContentStatus.Published);
        if (!string.IsNullOrWhiteSpace(category))
        {
            var slug = category.Trim();
            rows = rows.Where(article => article.CategorySlug == slug);
        }
        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(article => article.PublishedAtUtc)
            .ThenBy(article => article.SortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new ContentPage<ArticleView>(
            items.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<ArticleView> GetPublicAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var article = await db.ContentArticles.AsNoTracking().SingleOrDefaultAsync(
            item => item.Slug == slug && item.Status == ContentStatus.Published,
            cancellationToken) ?? throw new StorefrontException(404, "Article was not found.");
        return Map(article);
    }

    public async Task<ContentPage<ArticleView>> ListAdminAsync(
        int page,
        int pageSize,
        string? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = db.ContentArticles.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var value = status.Trim();
            rows = rows.Where(article => article.Status == value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            rows = rows.Where(article =>
                EF.Functions.ILike(article.TitleAr, pattern) ||
                EF.Functions.ILike(article.TitleEn, pattern) ||
                EF.Functions.ILike(article.Slug, pattern));
        }
        var total = await rows.CountAsync(cancellationToken);
        var items = await rows
            .OrderByDescending(article => article.UpdatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new ContentPage<ArticleView>(items.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<ArticleView> GetAdminAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return Map(await FindAsync(db, id, cancellationToken));
    }

    public async Task<IReadOnlyList<ArticleRevisionView>> ListRevisionsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ContentArticleRevisions.AsNoTracking()
            .Where(revision => revision.ArticleId == id)
            .OrderByDescending(revision => revision.CreatedAtUtc)
            .Select(revision => new ArticleRevisionView(
                revision.Id, revision.Reason, revision.CreatedAtUtc, revision.ActorUserAccountId))
            .ToListAsync(cancellationToken);
    }

    public async Task<ArticleView> CreateAsync(
        ArticleInput input,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input, requireContent: false);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.ContentArticles.AnyAsync(article => article.Slug == normalized.Slug, cancellationToken))
            throw new StorefrontException(409, "An article with this slug already exists.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var article = new ContentArticle
        {
            Id = Guid.CreateVersion7(),
            Slug = normalized.Slug,
            TitleAr = normalized.TitleAr,
            TitleEn = normalized.TitleEn,
            SummaryAr = normalized.SummaryAr,
            SummaryEn = normalized.SummaryEn,
            BodyAr = normalized.BodyAr,
            BodyEn = normalized.BodyEn,
            MainImageStorageKey = normalized.MainImageStorageKey,
            CategorySlug = normalized.CategorySlug,
            TagsCsv = string.Join(",", normalized.Tags),
            Status = ContentStatus.Draft,
            SeoTitleAr = normalized.SeoTitleAr,
            SeoTitleEn = normalized.SeoTitleEn,
            SeoDescriptionAr = normalized.SeoDescriptionAr,
            SeoDescriptionEn = normalized.SeoDescriptionEn,
            CanonicalUrl = normalized.CanonicalUrl,
            SortOrder = normalized.SortOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.ContentArticles.Add(article);
        AddRevision(db, article, actorId, "Created", now);
        await db.SaveChangesAsync(cancellationToken);
        await SyncUsageAsync(article, cancellationToken);
        return Map(article);
    }

    public async Task<ArticleView> UpdateAsync(
        Guid id,
        ArticleInput input,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input, requireContent: false);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var article = await FindAsync(db, id, cancellationToken);
        if (article.RowVersion != input.RowVersion)
            throw new StorefrontException(409, "The article was changed by another user.");
        if (await db.ContentArticles.AnyAsync(
                other => other.Slug == normalized.Slug && other.Id != id, cancellationToken))
            throw new StorefrontException(409, "An article with this slug already exists.");
        article.Slug = normalized.Slug;
        article.TitleAr = normalized.TitleAr;
        article.TitleEn = normalized.TitleEn;
        article.SummaryAr = normalized.SummaryAr;
        article.SummaryEn = normalized.SummaryEn;
        article.BodyAr = normalized.BodyAr;
        article.BodyEn = normalized.BodyEn;
        article.MainImageStorageKey = normalized.MainImageStorageKey;
        article.CategorySlug = normalized.CategorySlug;
        article.TagsCsv = string.Join(",", normalized.Tags);
        article.SeoTitleAr = normalized.SeoTitleAr;
        article.SeoTitleEn = normalized.SeoTitleEn;
        article.SeoDescriptionAr = normalized.SeoDescriptionAr;
        article.SeoDescriptionEn = normalized.SeoDescriptionEn;
        article.CanonicalUrl = normalized.CanonicalUrl;
        article.SortOrder = normalized.SortOrder;
        article.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        article.RowVersion++;
        AddRevision(db, article, actorId, "Saved", article.UpdatedAtUtc);
        await db.SaveChangesAsync(cancellationToken);
        await SyncUsageAsync(article, cancellationToken);
        return Map(article);
    }

    public async Task<ArticleView> PublishAsync(
        Guid id,
        long rowVersion,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var article = await FindAsync(db, id, cancellationToken);
        if (article.RowVersion != rowVersion)
            throw new StorefrontException(409, "The article was changed by another user.");
        Normalize(MapInput(article), requireContent: true);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        article.Status = ContentStatus.Published;
        article.PublishedAtUtc ??= now;
        article.UpdatedAtUtc = now;
        article.RowVersion++;
        AddRevision(db, article, actorId, "Published", now);
        await db.SaveChangesAsync(cancellationToken);
        await SyncUsageAsync(article, cancellationToken);
        return Map(article);
    }

    public async Task<ArticleView> UnpublishAsync(
        Guid id,
        long rowVersion,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var article = await FindAsync(db, id, cancellationToken);
        if (article.RowVersion != rowVersion)
            throw new StorefrontException(409, "The article was changed by another user.");
        article.Status = ContentStatus.Draft;
        article.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        article.RowVersion++;
        AddRevision(db, article, actorId, "Unpublished", article.UpdatedAtUtc);
        await db.SaveChangesAsync(cancellationToken);
        await SyncUsageAsync(article, cancellationToken);
        return Map(article);
    }

    private async Task SyncUsageAsync(ContentArticle article, CancellationToken cancellationToken)
    {
        try
        {
            await mediaUsage.RegisterAsync(
                "Article",
                article.Id,
                string.IsNullOrWhiteSpace(article.TitleEn) ? article.Slug : article.TitleEn,
                article.MainImageStorageKey,
                article.Status == ContentStatus.Published,
                cancellationToken);
        }
        catch
        {
            // Media usage is advisory metadata for the library delete guard. An
            // article save must not fail because the usage index is unavailable.
        }
    }

    private static async Task<ContentArticle> FindAsync(
        StorefrontDbContext db,
        Guid id,
        CancellationToken cancellationToken) =>
        await db.ContentArticles.SingleOrDefaultAsync(article => article.Id == id, cancellationToken)
        ?? throw new StorefrontException(404, "Article was not found.");

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

    private static void AddRevision(
        StorefrontDbContext db,
        ContentArticle article,
        Guid actorId,
        string reason,
        DateTimeOffset now)
    {
        db.ContentArticleRevisions.Add(new ContentArticleRevision
        {
            Id = Guid.CreateVersion7(),
            ArticleId = article.Id,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                article.Slug,
                article.TitleAr,
                article.TitleEn,
                article.SummaryAr,
                article.SummaryEn,
                article.BodyAr,
                article.BodyEn,
                article.SeoTitleAr,
                article.SeoTitleEn,
                article.SeoDescriptionAr,
                article.SeoDescriptionEn,
                article.CanonicalUrl,
                article.CategorySlug,
                article.TagsCsv,
                article.Status
            }),
            Reason = reason,
            ActorUserAccountId = actorId,
            CreatedAtUtc = now
        });
    }

    private static ArticleInput MapInput(ContentArticle article) =>
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

    private static NormalizedArticle Normalize(ArticleInput input, bool requireContent)
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

    private static ArticleView Map(ContentArticle article) => new(
        article.Id,
        article.Slug,
        article.TitleAr,
        article.TitleEn,
        article.SummaryAr,
        article.SummaryEn,
        article.BodyAr,
        article.BodyEn,
        article.MainImageStorageKey,
        article.CategorySlug,
        article.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries),
        article.Status,
        article.SeoTitleAr,
        article.SeoTitleEn,
        article.SeoDescriptionAr,
        article.SeoDescriptionEn,
        article.CanonicalUrl,
        article.SortOrder,
        article.PublishedAtUtc,
        article.CreatedAtUtc,
        article.UpdatedAtUtc,
        article.RowVersion);

    private sealed record NormalizedArticle(
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
}

public sealed class ContentFaqService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<FaqView>> ListPublicAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return (await db.ContentFaqs.AsNoTracking()
                .Where(faq => faq.Status == ContentStatus.Published)
                .OrderBy(faq => faq.SortOrder)
                .ThenBy(faq => faq.CreatedAtUtc)
                .ToListAsync(cancellationToken))
            .Select(Map)
            .ToList();
    }

    public async Task<IReadOnlyList<FaqView>> ListAdminAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return (await db.ContentFaqs.AsNoTracking()
                .OrderBy(faq => faq.SortOrder)
                .ThenBy(faq => faq.CreatedAtUtc)
                .ToListAsync(cancellationToken))
            .Select(Map)
            .ToList();
    }

    public async Task<FaqView> CreateAsync(
        FaqInput input,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var faq = new ContentFaq
        {
            Id = Guid.CreateVersion7(),
            QuestionAr = normalized.QuestionAr,
            QuestionEn = normalized.QuestionEn,
            AnswerAr = normalized.AnswerAr,
            AnswerEn = normalized.AnswerEn,
            Category = normalized.Category,
            SortOrder = normalized.SortOrder,
            Status = ContentStatus.Draft,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.ContentFaqs.Add(faq);
        await db.SaveChangesAsync(cancellationToken);
        return Map(faq);
    }

    public async Task<FaqView> UpdateAsync(
        Guid id,
        FaqInput input,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(input);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var faq = await db.ContentFaqs.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "FAQ was not found.");
        if (faq.RowVersion != rowVersion)
            throw new StorefrontException(409, "The FAQ was changed by another user.");
        faq.QuestionAr = normalized.QuestionAr;
        faq.QuestionEn = normalized.QuestionEn;
        faq.AnswerAr = normalized.AnswerAr;
        faq.AnswerEn = normalized.AnswerEn;
        faq.Category = normalized.Category;
        faq.SortOrder = normalized.SortOrder;
        faq.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        faq.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(faq);
    }

    public async Task<FaqView> SetPublishedAsync(
        Guid id,
        bool published,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var faq = await db.ContentFaqs.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "FAQ was not found.");
        if (faq.RowVersion != rowVersion)
            throw new StorefrontException(409, "The FAQ was changed by another user.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        faq.Status = published ? ContentStatus.Published : ContentStatus.Draft;
        if (published)
        {
            if (string.IsNullOrWhiteSpace(faq.AnswerAr) || string.IsNullOrWhiteSpace(faq.AnswerEn))
                throw new StorefrontException(400, "Both answers are required before publishing.");
            faq.PublishedAtUtc ??= now;
        }
        faq.UpdatedAtUtc = now;
        faq.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(faq);
    }

    private static NormalizedFaq Normalize(FaqInput input) => new(
        ContentText.Required(input.QuestionAr, 400, "Arabic question"),
        ContentText.Required(input.QuestionEn, 400, "English question"),
        ContentText.Required(input.AnswerAr, 4000, "Arabic answer"),
        ContentText.Required(input.AnswerEn, 4000, "English answer"),
        ContentText.Optional(input.Category, 64, "Category"),
        Math.Clamp(input.SortOrder, -10_000, 10_000));

    private static FaqView Map(ContentFaq faq) => new(
        faq.Id, faq.QuestionAr, faq.QuestionEn, faq.AnswerAr, faq.AnswerEn,
        faq.Category, faq.SortOrder, faq.Status, faq.PublishedAtUtc,
        faq.CreatedAtUtc, faq.UpdatedAtUtc, faq.RowVersion);

    private sealed record NormalizedFaq(
        string QuestionAr,
        string QuestionEn,
        string AnswerAr,
        string AnswerEn,
        string? Category,
        int SortOrder);
}

public sealed class ContentPolicyService(
    IDbContextFactory<StorefrontDbContext> contextFactory,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<PolicyView>> ListAdminAsync(
        string? kind,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = db.ContentPolicies.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(kind))
        {
            var value = kind.Trim();
            rows = rows.Where(policy => policy.Kind == value);
        }
        return (await rows
                .OrderBy(policy => policy.Kind)
                .ThenByDescending(policy => policy.VersionNumber)
                .ToListAsync(cancellationToken))
            .Select(Map)
            .ToList();
    }

    public async Task<PolicyView> GetPublicAsync(
        string kind,
        CancellationToken cancellationToken = default)
    {
        if (!ContentPolicyKind.All.Contains(kind, StringComparer.Ordinal))
            throw new StorefrontException(404, "Policy was not found.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var policy = await db.ContentPolicies.AsNoTracking()
            .Where(item => item.Kind == kind && item.Status == ContentStatus.Published)
            .OrderByDescending(item => item.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new StorefrontException(404, "This policy has not been published yet.");
        return Map(policy);
    }

    public async Task<PolicyView> CreateVersionAsync(
        string kind,
        CancellationToken cancellationToken = default)
    {
        if (!ContentPolicyKind.All.Contains(kind, StringComparer.Ordinal))
            throw new StorefrontException(400, "Unsupported policy kind.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var latest = await db.ContentPolicies
            .Where(policy => policy.Kind == kind)
            .OrderByDescending(policy => policy.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is { Status: ContentStatus.Draft })
            throw new StorefrontException(409, "A draft version already exists for this policy.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var policy = new ContentPolicy
        {
            Id = Guid.CreateVersion7(),
            Kind = kind,
            TitleAr = kind,
            TitleEn = kind,
            BodyAr = string.Empty,
            BodyEn = string.Empty,
            Status = ContentStatus.Draft,
            VersionNumber = (latest?.VersionNumber ?? 0) + 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.ContentPolicies.Add(policy);
        await db.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    public async Task<PolicyView> UpdateAsync(
        Guid id,
        PolicyInput input,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var policy = await db.ContentPolicies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "Policy was not found.");
        if (policy.Status is ContentStatus.Published or ContentStatus.Archived)
            throw new StorefrontException(409, "Published policy versions are immutable. Create a new version.");
        if (policy.RowVersion != input.RowVersion)
            throw new StorefrontException(409, "The policy was changed by another user.");
        policy.TitleAr = ContentText.Required(input.TitleAr, 200, "Arabic title");
        policy.TitleEn = ContentText.Required(input.TitleEn, 200, "English title");
        policy.BodyAr = ContentText.Optional(input.BodyAr, 40000, "Arabic body") ?? string.Empty;
        policy.BodyEn = ContentText.Optional(input.BodyEn, 40000, "English body") ?? string.Empty;
        policy.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        policy.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    public async Task<PolicyView> MarkReviewAsync(
        Guid id,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var policy = await db.ContentPolicies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "Policy was not found.");
        if (policy.Status != ContentStatus.Draft)
            throw new StorefrontException(409, "Only a draft policy can move to review.");
        if (policy.RowVersion != rowVersion)
            throw new StorefrontException(409, "The policy was changed by another user.");
        policy.Status = ContentStatus.Review;
        policy.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        policy.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    public async Task<PolicyView> PublishAsync(
        Guid id,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var policy = await db.ContentPolicies.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new StorefrontException(404, "Policy was not found.");
        if (policy.Status is not (ContentStatus.Draft or ContentStatus.Review))
            throw new StorefrontException(409, "Only a draft or reviewed policy can be published.");
        if (policy.RowVersion != rowVersion)
            throw new StorefrontException(409, "The policy was changed by another user.");
        if (string.IsNullOrWhiteSpace(policy.BodyAr) || string.IsNullOrWhiteSpace(policy.BodyEn))
            throw new StorefrontException(400, "Both policy bodies are required before publishing.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var previousPublished = await db.ContentPolicies
            .Where(item => item.Kind == policy.Kind && item.Status == ContentStatus.Published)
            .ToListAsync(cancellationToken);
        foreach (var previous in previousPublished)
        {
            previous.Status = ContentStatus.Archived;
            previous.UpdatedAtUtc = now;
            previous.RowVersion++;
        }
        policy.Status = ContentStatus.Published;
        policy.PublishedAtUtc = now;
        policy.UpdatedAtUtc = now;
        policy.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(policy);
    }

    private static PolicyView Map(ContentPolicy policy) => new(
        policy.Id, policy.Kind, policy.TitleAr, policy.TitleEn, policy.BodyAr, policy.BodyEn,
        policy.Status, policy.VersionNumber, policy.PublishedAtUtc,
        policy.CreatedAtUtc, policy.UpdatedAtUtc, policy.RowVersion);
}
