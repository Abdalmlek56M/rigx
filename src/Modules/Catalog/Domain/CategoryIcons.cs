namespace HardwareStore.Modules.Catalog;

/// <summary>
/// The ready icon set the owner picks a category icon from (filters, menus, home). The storefront draws each key; a category
/// without a key falls back to an icon guessed from its slug.
/// </summary>
public static class CategoryIcons
{
    public static readonly IReadOnlyList<string> Keys =
    [
        "cpu", "gpu", "motherboard", "ram", "storage", "power", "cooling", "case", "monitor", "keyboard", "mouse",
        "headset", "gamepad", "laptop", "network", "cable", "accessories"
    ];

    /// <summary>The key in lower case, or null for none; 400 for a key outside the set.</summary>
    public static string? Normalize(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var normalized = key.Trim().ToLowerInvariant();
        return Keys.Contains(normalized)
            ? normalized
            : throw new CatalogValidationException("iconKey", "The category icon is not one of the ready icons.");
    }
}
