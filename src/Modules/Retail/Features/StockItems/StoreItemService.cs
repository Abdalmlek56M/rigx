using System.Text.Json;
using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Public storefront view of stock items: only published items, never cost, serial or internal notes.</summary>
public sealed class StoreItemService(IDbContextFactory<RetailDbContext> contextFactory, IPriceOptionSource? priceOptions = null)
{
    private const string SortPriceAscending = "price_asc";
    private const string SortPriceDescending = "price_desc";
    private const int MaxPageSize = 60;

    public async Task<StoreItemPage> ListAsync(StoreItemQuery query, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = db.StockItems.AsNoTracking().Where(StockItemRules.PublishedFilter).Matching(query.Search, includeSerials: false);
        if (query.CategoryIds is { Count: > 0 } categoryIds)
            items = items.Where(item => item.CategoryId != null && categoryIds.Contains(item.CategoryId.Value));
        if (query.BrandId is Guid brandId) items = items.Where(item => item.BrandId == brandId);
        if (query.Specs is { Count: > 0 } specs)
        {
            var json = JsonSerializer.Serialize(specs);
            items = items.Where(item => EF.Functions.JsonContains(item.Specs, json));
        }
        if (query.InStock is bool inStock) items = items.Where(item => item.Units.Any(unit => unit.Status == UnitStatus.Available) == inStock);
        if (query.MinPrice is decimal min) items = items.Where(item => item.Price >= min);
        if (query.MaxPrice is decimal max) items = items.Where(item => item.Price <= max);
        var counts = await items.GroupBy(item => item.Condition)
            .Select(group => new ConditionCount(group.Key, group.Count())).ToListAsync(cancellationToken);
        var condition = string.IsNullOrWhiteSpace(query.Condition) ? null : StockCondition.Parse(query.Condition);
        var shown = condition is null ? items : items.Where(item => item.Condition == condition);
        var (page, pageSize) = PageRequest.Of(query.Page, query.PageSize, MaxPageSize);
        var cards = await ToCardsAsync(Sorted(shown, query.Sort).Skip((page - 1) * pageSize).Take(pageSize), cancellationToken);
        var total = condition is null ? counts.Sum(entry => entry.Count) : CountOf(counts, condition);
        return new StoreItemPage(cards, page, pageSize, total,
            new StoreConditionCounts(CountOf(counts, StockCondition.New), CountOf(counts, StockCondition.Used)));
    }

    /// <summary>The cards of the published items with these codes (hidden or unknown codes are simply missing).</summary>
    public async Task<IReadOnlyList<StoreItemCard>> GetCardsAsync(IReadOnlyCollection<string> codes, CancellationToken cancellationToken = default)
    {
        if (codes.Count == 0) return [];
        var wanted = codes.ToArray();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await ToCardsAsync(db.StockItems.AsNoTracking().Where(StockItemRules.PublishedFilter).Where(item => wanted.Contains(item.Code)), cancellationToken);
    }

    /// <summary>
    /// A published item by its slug, a slug it had before a rename, or its old <c>RX-</c> code. The response carries the
    /// current slug; the web app redirects every other key to it.
    /// </summary>
    public async Task<StoreItemDetail> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = db.StockItems.AsNoTracking().Where(StockItemRules.PublishedFilter);
        var slug = key.Trim().ToLowerInvariant();
        // A model written like a code ("RX 6600") has the slug rx-6600, so a key that reads as a code is also tried as a slug; the code wins.
        var isCode = PublicCode.TryNormalize(key, PublicCode.StockItem, out var code);
        items = items.Where(item => (isCode && item.Code == code) || item.Slug == slug
            || db.OldSlugs.Any(old => old.Slug == slug && old.StockItemId == item.Id));
        var row = await items
            .OrderByDescending(item => isCode && item.Code == code)
            .Select(item => new { Item = item, Available = item.Units.Count(unit => unit.Status == UnitStatus.Available) })
            .FirstOrDefaultAsync(cancellationToken) ?? throw ItemNotFound();
        var item = row.Item;
        var rule = await RuleAsync(cancellationToken);
        return new StoreItemDetail(item.Code, item.Slug, item.ModelName, item.Condition, item.CategoryId, item.BrandId, item.WarrantyLabel, item.WarrantyMonths,
            item.HasBox, StockItemRules.Prices(item.Price, item.WithBoardExtra, item.AloneExtra), row.Available, item.Photos, item.Specs,
            rule.ForItem(item.CategoryId, StockItemRules.OwnPriceOptions(item.OffersAlone, item.OffersWithBoard, item.OffersInBuild)).Names,
            item.Description, item.DescriptionEn);
    }

    /// <summary>The per-category price switches (an item's own choice replaces them, <see cref="PriceOptionRule.ForItem"/>) (all on when no source is wired, as in unit tests).</summary>
    private async Task<PriceOptionRule> RuleAsync(CancellationToken cancellationToken) =>
        priceOptions is null ? PriceOptionRule.Open : await priceOptions.LoadAsync(cancellationToken);

    private async Task<List<StoreItemCard>> ToCardsAsync(IQueryable<StockItem> items, CancellationToken cancellationToken)
    {
        var rule = await RuleAsync(cancellationToken);
        var rows = await items.Select(item => new
        {
            item.Code, item.Slug, item.ModelName, item.Condition, item.CategoryId, item.BrandId, item.WarrantyLabel, item.Price, item.WithBoardExtra,
            item.AloneExtra, item.CreatedAtUtc, item.Photos, item.Specs, item.OffersAlone, item.OffersWithBoard, item.OffersInBuild,
            Available = item.Units.Count(unit => unit.Status == UnitStatus.Available)
        }).ToListAsync(cancellationToken);
        return rows.Select(row => new StoreItemCard(row.Code, row.Slug, row.ModelName, row.Condition, row.CategoryId, row.WarrantyLabel,
            StockItemRules.Prices(row.Price, row.WithBoardExtra, row.AloneExtra), row.Available,
            row.Photos.FirstOrDefault()?.ThumbnailStorageKey, row.Photos.Skip(1).FirstOrDefault()?.ThumbnailStorageKey, row.CreatedAtUtc,
            rule.ForItem(row.CategoryId, StockItemRules.OwnPriceOptions(row.OffersAlone, row.OffersWithBoard, row.OffersInBuild)).Names, row.Specs,
            row.BrandId)).ToList();
    }

    /// <summary>Sold-out items stay listed (marked "خلص") but always come after the available ones.</summary>
    private static IQueryable<StockItem> Sorted(IQueryable<StockItem> items, string? sort)
    {
        var availableFirst = items.OrderByDescending(item => item.Units.Any(unit => unit.Status == UnitStatus.Available));
        return sort switch
        {
            SortPriceAscending => availableFirst.ThenBy(item => item.Price).ThenByDescending(item => item.CreatedAtUtc),
            SortPriceDescending => availableFirst.ThenByDescending(item => item.Price).ThenByDescending(item => item.CreatedAtUtc),
            _ => availableFirst.ThenByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id)
        };
    }

    private static int CountOf(List<ConditionCount> counts, string condition) =>
        counts.Where(entry => entry.Condition == condition).Select(entry => entry.Count).FirstOrDefault();

    private sealed record ConditionCount(string Condition, int Count);

    private static RetailException ItemNotFound() => new(404, "The item was not found.");
}
