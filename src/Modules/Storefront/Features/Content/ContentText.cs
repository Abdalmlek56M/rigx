using System.Text.RegularExpressions;

namespace HardwareStore.Modules.Storefront;

/// <summary>Plain-text rules for all CMS content: trimmed, NFC-normalized, no HTML, no control characters.</summary>
internal static partial class ContentText
{
    [GeneratedRegex("<[a-zA-Z!/][^>]*>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTag();

    public static string Required(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new StorefrontException(400, $"{field} is required.");
        var text = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (text.Length > max)
            throw new StorefrontException(400, $"{field} is too long.");
        if (HtmlTag().IsMatch(text))
            throw new StorefrontException(400, $"{field} must be plain text without HTML.");
        if (text.Any(character => char.IsControl(character) && character is not ('\n' or '\r' or '\t')))
            throw new StorefrontException(400, $"{field} contains unsupported control characters.");
        return text;
    }

    public static string? Optional(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var text = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (text.Length > max)
            throw new StorefrontException(400, $"{field} is too long.");
        if (HtmlTag().IsMatch(text))
            throw new StorefrontException(400, $"{field} must be plain text without HTML.");
        if (text.Any(character => char.IsControl(character) && character is not ('\n' or '\r' or '\t')))
            throw new StorefrontException(400, $"{field} contains unsupported control characters.");
        return text;
    }

    public static string Slug(string? value)
    {
        var slug = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (slug.Length is < 2 or > 200 || !SlugPattern().IsMatch(slug))
            throw new StorefrontException(400, "Slug must use lowercase letters, digits and single hyphens.");
        return slug;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();

    public static IReadOnlyList<string> Tags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];
        var tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tag => tag.Normalize(System.Text.NormalizationForm.FormC))
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();
        if (tags.Any(tag => tag.Length > 32))
            throw new StorefrontException(400, "Each tag must be 32 characters or fewer.");
        return tags;
    }
}
