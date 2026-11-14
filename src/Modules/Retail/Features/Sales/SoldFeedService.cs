using HardwareStore.Common.Domain.Paging;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Public "اتباع قبل كده" feed: delivered items and builds, newest first, without prices or customer names. The
/// governorate shows only when the store setting allows it.
/// </summary>
public sealed class SoldFeedService(IDbContextFactory<RetailDbContext> contextFactory)
{
    public const string KindItem = "Item";
    public const string KindBuild = "Build";
    private const int PageSize = 24;

    public async Task<PagedResult<SoldFeedItem>> ListAsync(bool showGovernorate, string? kind, Guid? categoryId, int page, CancellationToken cancellationToken = default)
    {
        var paging = PageRequest.Of(page, PageSize);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var lines = db.OrderItems.AsNoTracking()
            .Where(item => item.Order.Status == OrderStatus.Delivered || item.Order.Status == OrderStatus.Completed);
        lines = kind?.Trim() switch
        {
            KindBuild => lines.Where(item => item.AssemblyId != null && item.UnitId == null),
            KindItem => lines.Where(item => item.StockItemId != null),
            _ => lines
        };
        if (categoryId is Guid category)
            lines = lines.Where(item => db.StockItems.Any(stock => stock.Id == item.StockItemId && stock.CategoryId == category));
        var total = await lines.CountAsync(cancellationToken);
        var rows = await lines.OrderByDescending(item => item.Order.UpdatedAtUtc).ThenBy(item => item.Id)
            .Skip(paging.Skip).Take(paging.PageSize)
            .Select(item => new
            {
                Build = item.AssemblyId != null && item.UnitId == null,
                item.Name, item.Code, item.ThumbnailStorageKeySnapshot, item.Order.FulfillmentMethod, item.Order.Governorate,
                SoldAt = item.Order.UpdatedAtUtc,
                Stock = db.StockItems.Where(stock => stock.Id == item.StockItemId)
                    .Select(stock => new { stock.Condition, stock.CategoryId }).FirstOrDefault(),
                AnyUsedPart = item.Components.Any(component =>
                    db.Units.Any(unit => unit.Id == component.UnitId && unit.Condition == StockCondition.Used))
            }).ToListAsync(cancellationToken);
        var items = rows.Select(row => new SoldFeedItem(row.Build ? KindBuild : KindItem, row.Name, row.Code,
            row.Build ? (row.AnyUsedPart ? StockCondition.Used : StockCondition.New) : row.Stock?.Condition ?? StockCondition.Used,
            row.Stock?.CategoryId, row.ThumbnailStorageKeySnapshot, row.FulfillmentMethod,
            showGovernorate && row.FulfillmentMethod == OrderFulfillment.Delivery ? row.Governorate : null, row.SoldAt)).ToList();
        return new PagedResult<SoldFeedItem>(items, paging.Page, paging.PageSize, total);
    }
}
