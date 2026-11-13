using System.Globalization;

namespace HardwareStore.Common.Domain;

/// <summary>
/// Public, human-readable codes shown on the site, stickers and documents:
/// stock items <c>RX-0142</c>, builds <c>BLD-07</c>, purchases <c>PO-0031</c>.
/// Numbers come from one database sequence per prefix; the width is a minimum (RX-12345 is valid).
/// </summary>
public static class PublicCode
{
    public const string StockItem = "RX";
    public const string Build = "BLD";
    public const string Purchase = "PO";

    public static string Format(string prefix, long number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        return $"{prefix}-{number.ToString(new string('0', Width(prefix)), CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Reads what a person typed ("rx-142", "RX0142", "rx 142") as a code with <paramref name="prefix"/>.
    /// Returns false when the text is not a code of that kind (so callers can fall back to a name search).
    /// </summary>
    public static bool TryNormalize(string? text, string prefix, out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var compact = string.Concat(text.Where(character => !char.IsWhiteSpace(character) && character != '-'));
        if (!compact.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var digits = compact[prefix.Length..];
        if (digits.Length is 0 or > 9 || !digits.All(char.IsAsciiDigit)) return false;
        var number = long.Parse(digits, CultureInfo.InvariantCulture);
        if (number < 1) return false;
        code = Format(prefix, number);
        return true;
    }

    private static int Width(string prefix) => prefix switch
    {
        StockItem or Purchase => 4,
        Build => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(prefix), prefix, "Unknown public code prefix.")
    };
}
