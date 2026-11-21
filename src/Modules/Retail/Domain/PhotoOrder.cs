namespace HardwareStore.Modules.Retail;

/// <summary>The order of an item's or a build's photos; the first one is the main photo shown on the storefront.</summary>
public static class PhotoOrder
{
    /// <summary>The photos in the order of <paramref name="storageKeys"/>, which must name every current photo exactly once.</summary>
    public static List<StockItemPhoto> Apply(IReadOnlyList<StockItemPhoto> current, IReadOnlyList<string>? storageKeys)
    {
        if (storageKeys is null || storageKeys.Count != current.Count || storageKeys.Distinct(StringComparer.Ordinal).Count() != current.Count)
            throw new RetailException(400, "The photo order must list every photo of the item exactly once.");
        var byKey = current.ToDictionary(photo => photo.StorageKey, StringComparer.Ordinal);
        if (storageKeys.Any(key => !byKey.ContainsKey(key))) throw new RetailException(400, "The photo order lists a photo that is not on the item.");
        return storageKeys.Select(key => byKey[key]).ToList();
    }
}
