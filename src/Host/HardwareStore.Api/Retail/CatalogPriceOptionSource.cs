using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;

/// <summary>
/// The sale price switches of every category as Retail needs them (the storefront cards and pages, and the online checkout): a child follows
/// its parent (<see cref="CategoryPriceRules"/>). Only the categories that switch something off are listed; Retail treats every other category
/// as offering all three prices.
/// </summary>
public sealed class CatalogPriceOptionSource(TaxonomyService taxonomy) : IPriceOptionSource
{
    public async Task<PriceOptionRule> LoadAsync(CancellationToken cancellationToken = default)
    {
        var effective = CategoryPriceRules.Effective(await taxonomy.GetCategoriesAsync(cancellationToken));
        var restricted = effective
            .Where(entry => entry.Value is { Alone: false } or { WithBoard: false } or { InBuild: false })
            .ToDictionary(entry => entry.Key, entry => new PriceOptionSet(entry.Value.Alone, entry.Value.WithBoard, entry.Value.InBuild));
        return new PriceOptionRule(restricted);
    }
}
