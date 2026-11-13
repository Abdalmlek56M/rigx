using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Shareable price quote for the website: prices the lines like an online order (published items and builds at the shown price)
/// without creating anything. It carries no invoice number and no customer data.
/// </summary>
public sealed class QuoteService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<QuoteView> QuoteAsync(QuoteRequest request, BuildPriceRule? buildPrice, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = await OrderDrafting.PriceLinesAsync(db, request.Items, OrderChannel.Online, buildPrice, cancellationToken);
        var stockIds = items.Where(item => item.AssemblyId is null).Select(item => item.StockItemId!.Value).Distinct().ToList();
        var conditions = await db.StockItems.AsNoTracking().Where(item => stockIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.Condition, cancellationToken);
        var buildIds = items.Where(item => item.AssemblyId is not null).Select(item => item.AssemblyId!.Value).ToList();
        var builds = await db.Assemblies.AsNoTracking().WithUnits().Where(a => buildIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, cancellationToken);
        var lines = items.Select(item => item.AssemblyId is Guid buildId
            ? Line(item, null, builds[buildId].Units.OrderBy(link => link.Unit.StockItem.Code).Select(Part).ToList())
            : Line(item, conditions[item.StockItemId!.Value], [])).ToList();
        var single = buildIds.Count == 1 && items.Count == 1 ? builds[buildIds[0]] : null;
        return new QuoteView(single is null ? null : items[0].Name, single?.Usage, lines, lines.Sum(line => line.LineTotal), timeProvider.GetUtcNow());
    }

    private static QuoteLineView Line(OrderItem item, string? condition, IReadOnlyList<QuotePartView> parts) =>
        new(item.Code, item.Name, condition, item.Quantity, item.UnitPrice, item.LineTotal, parts);

    private static QuotePartView Part(AssemblyUnit link) => new(link.Unit.StockItem.Code, link.Unit.Name, link.Unit.Condition, link.PartPrice);
}
