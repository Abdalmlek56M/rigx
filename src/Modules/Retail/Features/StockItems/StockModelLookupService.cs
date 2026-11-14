using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Model-level stock questions for notify-me: which model an RX code is, and which models can be bought right now.</summary>
public sealed class StockModelLookupService(IDbContextFactory<RetailDbContext> contextFactory)
{
    public async Task<StockModelRef?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.StockItems.AsNoTracking().Where(item => item.Code == code)
            .Select(item => new StockModelRef(item.ModelName, item.ModelKey, item.CategoryId)).SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>For each model key that has a published item with an available unit: its cheapest such item. Models with none are missing.</summary>
    public async Task<IReadOnlyDictionary<string, AvailableModelItem>> FindAvailableAsync(IReadOnlyCollection<string> modelKeys,
        CancellationToken cancellationToken = default)
    {
        if (modelKeys.Count == 0) return new Dictionary<string, AvailableModelItem>();
        var keys = modelKeys.ToArray();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.StockItems.AsNoTracking().Where(StockItemRules.PublishedFilter)
            .Where(item => keys.Contains(item.ModelKey) && item.Units.Any(unit => unit.Status == UnitStatus.Available))
            .OrderBy(item => item.Price).ThenBy(item => item.Code)
            .Select(item => new AvailableModelItem(item.ModelKey, item.Code, item.ModelName)).ToListAsync(cancellationToken);
        return rows.GroupBy(row => row.ModelKey).ToDictionary(group => group.Key, group => group.First());
    }
}
