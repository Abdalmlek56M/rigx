using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Storefront;

/// <summary>
/// Article publishing: an article is live when it is Published and its publish time has come.
/// Published with a future time = "Scheduled" (no job needed; the public queries compare with now).
/// </summary>
public static class ArticleSchedule
{
    public const string Scheduled = "Scheduled";
    public static readonly TimeSpan MaxAhead = TimeSpan.FromDays(366);

    /// <summary>
    /// Publish time for a publish request: the requested future time; otherwise keep the first publish time
    /// when it already passed (re-publishing keeps the article's date), else now.
    /// </summary>
    public static DateTimeOffset PublishAt(DateTimeOffset? current, DateTimeOffset? requested, DateTimeOffset now)
    {
        if (requested is { } at)
        {
            var utc = at.ToUniversalTime();
            if (utc <= now) throw new StorefrontException(400, "The publish time must be in the future.");
            if (utc - now > MaxAhead) throw new StorefrontException(400, "An article can be scheduled up to a year ahead.");
            return utc;
        }
        return current is { } first && first <= now ? first : now;
    }

    /// <summary>Status shown to staff: Draft, Scheduled or Published.</summary>
    public static string DisplayStatus(string status, DateTimeOffset? publishedAtUtc, DateTimeOffset now) =>
        status == ContentStatus.Published && publishedAtUtc > now ? Scheduled : status;
}

/// <summary>Where an FAQ answer shows.</summary>
public static class FaqScope
{
    /// <summary>Help page, and every product page unless help-page-only or excluded.</summary>
    public const string General = "General";
    /// <summary>Product pages of items in the category or its sub-categories, unless excluded.</summary>
    public const string Category = "Category";
    /// <summary>The page of one store item (RX code).</summary>
    public const string Product = "Product";

    public static readonly IReadOnlyList<string> All = [General, Category, Product];
}

public sealed record FaqTarget(string Scope, Guid? CategoryId, string? ItemCode, bool HelpPageOnly,
    IReadOnlyList<Guid> ExcludedCategoryIds, IReadOnlyList<string> ExcludedItemCodes);

public static class FaqRules
{
    private const int MaxExclusions = 50;

    /// <summary>Checks the scope fields: a category FAQ needs a category, a product FAQ an item code; exclusions only for wider scopes.</summary>
    public static FaqTarget Target(string? scope, Guid? categoryId, string? itemCode, bool helpPageOnly,
        IReadOnlyList<Guid>? excludedCategoryIds, IReadOnlyList<string>? excludedItemCodes)
    {
        var value = FaqScope.All.FirstOrDefault(item => string.Equals(item, scope?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new StorefrontException(400, "FAQ scope must be General, Category or Product.");
        var categories = (excludedCategoryIds ?? []).Where(id => id != Guid.Empty).Distinct().ToList();
        var codes = (excludedItemCodes ?? []).Select(ItemCode).Distinct().ToList();
        if (categories.Count + codes.Count > MaxExclusions)
            throw new StorefrontException(400, $"An FAQ can exclude at most {MaxExclusions} places.");
        return value switch
        {
            FaqScope.General => new FaqTarget(value, null, null, helpPageOnly, categories, codes),
            FaqScope.Category => new FaqTarget(value,
                categoryId is { } id && id != Guid.Empty ? id : throw new StorefrontException(400, "Choose the FAQ category."),
                null, false, categories.Where(id => id != categoryId).ToList(), codes),
            _ => new FaqTarget(value, null, ItemCode(itemCode), false, [], [])
        };
    }

    /// <summary>
    /// Whether an FAQ shows on an item page. <paramref name="itemCategoryPath"/> is the item's category and its parents.
    /// </summary>
    public static bool ShowsOnItem(FaqTarget faq, string itemCode, IReadOnlyCollection<Guid> itemCategoryPath)
    {
        if (faq.ExcludedItemCodes.Contains(itemCode) || faq.ExcludedCategoryIds.Any(itemCategoryPath.Contains)) return false;
        return faq.Scope switch
        {
            FaqScope.General => !faq.HelpPageOnly,
            FaqScope.Category => faq.CategoryId is { } id && itemCategoryPath.Contains(id),
            _ => faq.ItemCode == itemCode
        };
    }

    /// <summary>Order on an item page: the item's own answers, then its category's, then general ones.</summary>
    public static int ItemPageRank(string scope) => scope switch
    {
        FaqScope.Product => 0,
        FaqScope.Category => 1,
        _ => 2
    };

    private static string ItemCode(string? value) =>
        PublicCode.TryNormalize(value, PublicCode.StockItem, out var code)
            ? code
            : throw new StorefrontException(400, "Use a store item code like RX-0142.");
}
