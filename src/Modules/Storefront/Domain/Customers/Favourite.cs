using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.Storefront;

/// <summary>A stock item (RX code) or a build (BLD code) a signed-in customer marked with the heart. One row per account and code.</summary>
public sealed class Favourite
{
    public Guid UserAccountId { get; set; }
    public required string ItemCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Favourites are stock item codes (RX) and build codes (BLD); an account keeps at most <see cref="Max"/> of them together.</summary>
public static class FavouriteRules
{
    public const int Max = 200;

    /// <summary>The RX or BLD code the text names, or false when it is neither.</summary>
    public static bool TryCode(string? text, out string code) =>
        PublicCode.TryNormalize(text, PublicCode.StockItem, out code) || PublicCode.TryNormalize(text, PublicCode.Build, out code);

    /// <summary>The RX or BLD code the text names, or a 400 when it is neither.</summary>
    public static string Code(string? text) => TryCode(text, out var code)
        ? code
        : throw new StorefrontException(400, "A stock item or build code (RX-... or BLD-...) is required.");

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
            if (TryCode(text, out var code) && !saved.Contains(code) && !codes.Contains(code)) codes.Add(code);
        }
        return codes;
    }
}
