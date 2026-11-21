using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;

/// <summary>
/// The per-category sale price switches, read from Catalog for Retail (the storefront cards and pages, and the online checkout).
/// Only the categories that switch something off are listed; Retail treats every other category as offering all three prices.
/// </summary>
public sealed class CatalogPriceOptionSource(TaxonomyService taxonomy) : IPriceOptionSource
{
    public async Task<PriceOptionRule> LoadAsync(CancellationToken cancellationToken = default)
    {
        var restricted = (await taxonomy.GetCategoriesAsync(cancellationToken))
            .Where(category => category.PriceOptions is { Alone: false } or { WithBoard: false } or { InBuild: false })
            .ToDictionary(category => category.Id, category => new PriceOptionSet(category.PriceOptions!.Alone, category.PriceOptions.WithBoard, category.PriceOptions.InBuild));
        return new PriceOptionRule(restricted);
    }
}
