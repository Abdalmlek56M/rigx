using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Storefront;

/// <summary>A stock item (RX code) a signed-in customer marked with the heart. One row per account and code.</summary>
public sealed class Favourite
{
    public Guid UserAccountId { get; set; }
    public required string ItemCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Favourites are stock item codes; an account keeps at most <see cref="Max"/> of them.</summary>
public static class FavouriteRules
{
    public const int Max = 200;

    /// <summary>The RX code the text names, or a 400 when it is not one.</summary>
    public static string Code(string? text) => PublicCode.TryNormalize(text, PublicCode.StockItem, out var code)
        ? code
        : throw new StorefrontException(400, "A stock item code (RX-...) is required.");

    /// <summary>
    /// The codes of a guest's local list to add to an account that already has <paramref name="saved"/>: valid, distinct,
    /// not saved yet, and only as many as still fit under <see cref="Max"/>.
    /// </summary>
    public static IReadOnlyList<string> NewCodes(IEnumerable<string?> texts, IReadOnlyCollection<string> saved)
    {
        var room = Max - saved.Count;
        var codes = new List<string>();
        foreach (var text in texts)
        {
            if (codes.Count >= room) break;
            if (PublicCode.TryNormalize(text, PublicCode.StockItem, out var code) && !saved.Contains(code) && !codes.Contains(code)) codes.Add(code);
        }
        return codes;
    }
}
