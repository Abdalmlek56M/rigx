using System.Linq.Expressions;
using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Retail;

/// <summary>Customer-facing condition of a stock item: only "جديد" (New) or "مستعمل" (Used).</summary>
public static class StockCondition
{
    public const string New = "New";
    public const string Used = "Used";

    public static string Parse(string? value) => value?.Trim() switch
    {
        New => New,
        Used => Used,
        _ => throw new RetailException(400, "Condition must be New or Used.")
    };
}

/// <summary>Price shown for each way of buying the item (business §3 "سعر حسب طريقة البيع").</summary>
public sealed record StockItemPrices(decimal InBuild, decimal WithBoard, decimal Alone);

/// <summary>Stock item rules: how intake lines become items, context prices, and what may be published.</summary>
public static class StockItemRules
{
    public const int MinPhotosToPublish = 3;
    public const int MaxPhotos = 12;
    public const int MaxSpecs = 40;
    public const int MaxInternalNoteLength = 1000;
    public const int MaxQuantity = 1000;
    public const int MaxSerialLength = 128;

    /// <summary>Shown on the storefront only when not hidden and it has at least three photos.</summary>
    public static bool IsPublished(bool isHidden, int photoCount) => !isHidden && photoCount >= MinPhotosToPublish;

    /// <summary>The same rule as <see cref="IsPublished"/>, for database queries (uses the generated photo count).</summary>
    public static readonly Expression<Func<StockItem, bool>> PublishedFilter = item => !item.IsHidden && item.PhotoCount >= MinPhotosToPublish;

    /// <summary>
    /// How many units each new item gets for an intake line: a used line of 3 pieces is three items
    /// of one unit each (each piece has its own page); a new line of 3 pieces is one lot of three units.
    /// </summary>
    /// <summary>
    /// The item a new piece of the same model copies its data from: the newest one with photos, else the newest one.
    /// <paramref name="newestFirst"/> holds items of one model, newest first.
    /// </summary>
    public static StockItemTemplate PickTemplate(IEnumerable<StockItemTemplate> newestFirst)
    {
        var items = newestFirst.ToList();
        return items.FirstOrDefault(item => item.PhotoCount > 0) ?? items[0];
    }

    public static IReadOnlyList<int> UnitsPerItem(string condition, int quantity)
    {
        if (quantity is < 1 or > MaxQuantity) throw new RetailException(400, $"Quantity must be between 1 and {MaxQuantity}.");
        return condition == StockCondition.Used ? Enumerable.Repeat(1, quantity).ToList() : [quantity];
    }

    /// <summary>Base price is the in-build price; "with board" and "alone" add optional extras.</summary>
    public static StockItemPrices Prices(decimal price, decimal? withBoardExtra, decimal? aloneExtra) =>
        new(price, price + (withBoardExtra ?? 0m), price + (aloneExtra ?? 0m));

    public static void ValidatePrices(decimal price, decimal? withBoardExtra, decimal? aloneExtra)
    {
        if (price < 0 || withBoardExtra < 0 || aloneExtra < 0)
            throw new RetailException(400, "Prices and price extras cannot be negative.");
    }

    public static void ValidateWarranty(string? label, int? months)
    {
        if (label?.Length > 64) throw new RetailException(400, "The warranty label is too long.");
        if (months is < 1 or > 120) throw new RetailException(400, "Warranty months must be between 1 and 120.");
    }

    public static string? CleanNote(string? note)
    {
        var clean = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (clean?.Length > MaxInternalNoteLength) throw new RetailException(400, "The internal note is too long.");
        return clean;
    }

    public static string CleanModelName(string? modelName)
    {
        var clean = string.IsNullOrWhiteSpace(modelName) ? string.Empty : ModelName.Clean(modelName);
        if (clean.Length == 0 || ModelName.Key(clean).Length == 0)
            throw new RetailException(400, "A model name is required.");
        if (clean.Length > ModelName.MaxLength) throw new RetailException(400, "The model name is too long.");
        return clean;
    }

    /// <summary>Specs are optional key/value pairs; empty values are dropped, never required.</summary>
    public static Dictionary<string, string> CleanSpecs(IReadOnlyDictionary<string, string>? specs)
    {
        var clean = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in specs ?? new Dictionary<string, string>())
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) continue;
            var cleanKey = key.Trim();
            var cleanValue = value.Trim();
            if (cleanKey.Length > 64 || cleanValue.Length > 200)
                throw new RetailException(400, "A spec name or value is too long.");
            clean[cleanKey] = cleanValue;
        }
        if (clean.Count > MaxSpecs) throw new RetailException(400, $"An item can have at most {MaxSpecs} specs.");
        return clean;
    }
}
