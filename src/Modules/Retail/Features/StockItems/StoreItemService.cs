using System.Text.Json;
using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Public storefront view of stock items: only published items, never cost, serial or internal notes.</summary>
public sealed class StoreItemService(IDbContextFactory<RetailDbContext> contextFactory)
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
        if (query.Specs is { Count: > 0 } specs)
        {
            var json = JsonSerializer.Serialize(specs);
            items = items.Where(item => EF.Functions.JsonContains(item.Specs, json));
        }
        if (query.InStock == true) items = items.Where(item => item.Units.Any(unit => unit.Status == UnitStatus.Available));
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

    public async Task<StoreItemDetail> GetAsync(string code, CancellationToken cancellationToken = default)
    {
        if (!PublicCode.TryNormalize(code, PublicCode.StockItem, out var normalized)) throw ItemNotFound();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.StockItems.AsNoTracking().Where(StockItemRules.PublishedFilter).Where(item => item.Code == normalized)
            .Select(item => new { Item = item, Available = item.Units.Count(unit => unit.Status == UnitStatus.Available) })
            .SingleOrDefaultAsync(cancellationToken) ?? throw ItemNotFound();
        var item = row.Item;
        return new StoreItemDetail(item.Code, item.ModelName, item.Condition, item.CategoryId, item.WarrantyLabel, item.WarrantyMonths,
            item.HasBox, StockItemRules.Prices(item.Price, item.WithBoardExtra, item.AloneExtra), row.Available, item.Photos, item.Specs);
    }

    private static async Task<List<StoreItemCard>> ToCardsAsync(IQueryable<StockItem> items, CancellationToken cancellationToken)
    {
        var rows = await items.Select(item => new
        {
            item.Code, item.ModelName, item.Condition, item.CategoryId, item.WarrantyLabel, item.Price, item.WithBoardExtra,
            item.AloneExtra, item.CreatedAtUtc, item.Photos,
            Available = item.Units.Count(unit => unit.Status == UnitStatus.Available)
        }).ToListAsync(cancellationToken);
        return rows.Select(row => new StoreItemCard(row.Code, row.ModelName, row.Condition, row.CategoryId, row.WarrantyLabel,
            StockItemRules.Prices(row.Price, row.WithBoardExtra, row.AloneExtra), row.Available,
            row.Photos.FirstOrDefault()?.ThumbnailStorageKey, row.CreatedAtUtc)).ToList();
    }

    private static IQueryable<StockItem> Sorted(IQueryable<StockItem> items, string? sort) => sort switch
    {
        SortPriceAscending => items.OrderBy(item => item.Price).ThenByDescending(item => item.CreatedAtUtc),
        SortPriceDescending => items.OrderByDescending(item => item.Price).ThenByDescending(item => item.CreatedAtUtc),
        _ => items.OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id)
    };

    private static int CountOf(List<ConditionCount> counts, string condition) =>
        counts.Where(entry => entry.Condition == condition).Select(entry => entry.Count).FirstOrDefault();

    private sealed record ConditionCount(string Condition, int Count);

    private static RetailException ItemNotFound() => new(404, "The item was not found.");
}
