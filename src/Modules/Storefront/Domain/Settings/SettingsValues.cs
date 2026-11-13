using System.Text.RegularExpressions;

namespace HardwareStore.Modules.Storefront;

/// <summary>Value rules shared by the settings sections (numbers, links, money, label lists).</summary>
internal static partial class SettingsValues
{
    public static string PhoneDigits(string? value, string label)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (digits.Length is < 8 or > 15)
            throw new StorefrontException(400, $"{label} must contain 8 to 15 digits.");
        return digits;
    }

    public static string? OptionalPhoneDigits(string? value, string label) =>
        string.IsNullOrWhiteSpace(value) ? null : PhoneDigits(value, label);

    public static string HttpUrl(string? value, string label)
    {
        var text = ContentText.Required(value, 512, label);
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new StorefrontException(400, $"{label} must be an absolute http or https URL.");
        return text;
    }

    public static decimal Money(decimal value, string label)
    {
        if (value is < 0 or > 10_000_000m || decimal.Round(value, 2) != value)
            throw new StorefrontException(400, $"{label} must be between 0 and 10,000,000 with at most 2 decimals.");
        return value;
    }

    public static int Range(int value, int min, int max, string label)
    {
        if (value < min || value > max)
            throw new StorefrontException(400, $"{label} must be between {min} and {max}.");
        return value;
    }

    public static string OneOf(string? value, IReadOnlyList<string> allowed, string label)
    {
        if (value is null || !allowed.Contains(value, StringComparer.Ordinal))
            throw new StorefrontException(400, $"{label} must be one of: {string.Join(", ", allowed)}.");
        return value;
    }

    public static string? OptionalEmail(string? value, string label)
    {
        var text = ContentText.Optional(value, 255, label);
        if (text is not null && !System.Net.Mail.MailAddress.TryCreate(text, out _))
            throw new StorefrontException(400, $"{label} must be a valid email address.");
        return text;
    }

    public static string? OptionalDomain(string? value, string label)
    {
        var text = ContentText.Optional(value, 253, label)?.ToLowerInvariant();
        if (text is not null && !DomainPattern().IsMatch(text))
            throw new StorefrontException(400, $"{label} must be a domain name such as rigxhardware.com.");
        return text;
    }

    /// <summary>A non-empty list of unique plain-text labels, in the given order.</summary>
    public static IReadOnlyList<string> Labels(IReadOnlyList<string>? values, int maxCount, string label)
    {
        var labels = (values ?? []).Select(value => ContentText.Required(value, 60, label)).ToArray();
        if (labels.Length is 0 || labels.Length > maxCount)
            throw new StorefrontException(400, $"{label} list must have 1 to {maxCount} entries.");
        EnsureUnique(labels, label);
        return labels;
    }

    public static void EnsureUnique(IEnumerable<string> values, string label)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (values.Any(value => !seen.Add(value)))
            throw new StorefrontException(400, $"{label} entries must be unique.");
    }

    [GeneratedRegex("^(?=.{1,253}$)([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\\.)+[a-z]{2,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex DomainPattern();
}
