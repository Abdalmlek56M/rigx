using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;

/// <summary>Answers the Storefront's notify-me questions from Retail stock (Storefront never references Retail).</summary>
public sealed class RetailStockModelDirectory(StockModelLookupService lookup) : IStockModelDirectory
{
    public async Task<StockModel?> FindByItemCodeAsync(string code, CancellationToken cancellationToken = default) =>
        await lookup.FindByCodeAsync(code, cancellationToken) is { } model ? new StockModel(model.ModelName, model.ModelKey, model.CategoryId) : null;

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<AvailableStockItem>>> FindAvailableAsync(IReadOnlyCollection<string> modelKeys,
        CancellationToken cancellationToken = default) =>
        (await lookup.FindAvailableAsync(modelKeys, cancellationToken)).ToDictionary(entry => entry.Key,
            entry => (IReadOnlyList<AvailableStockItem>)entry.Value.Select(item => new AvailableStockItem(item.Code, item.ModelName, item.Condition)).ToList());
}
