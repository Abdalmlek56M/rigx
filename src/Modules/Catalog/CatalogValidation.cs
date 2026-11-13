using System.Text.RegularExpressions;

namespace HardwareStore.Modules.Catalog;

public sealed partial class CatalogValidationException : Exception
{
    public CatalogValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] })
    {
    }

    public CatalogValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Catalog validation failed.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

public sealed class CatalogNotFoundException(string message) : Exception(message);

public sealed class CatalogConflictException(string message) : Exception(message);

public static partial class CatalogValidation
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();

    public static string Required(string? value, string field, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw Field(field, $"{field} is required.");
        if (normalized.Length > maxLength)
            throw Field(field, $"{field} must not exceed {maxLength} characters.");
        return normalized;
    }

    public static string? Optional(string? value, string field, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
            return null;
        if (normalized.Length > maxLength)
            throw Field(field, $"{field} must not exceed {maxLength} characters.");
        return normalized;
    }

    public static string Slug(string? value)
    {
        var normalized = Required(value, "slug", 200)
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormKC);
        if (!SlugPattern().IsMatch(normalized))
            throw Field("slug", "Slug must use lowercase letters, numbers, and single hyphens.");
        return normalized;
    }

    public static decimal NonNegativeMoney(decimal value, string field)
    {
        if (value < 0 || decimal.Round(value, 4) != value)
            throw Field(field, $"{field} must be non-negative with no more than four decimal places.");
        return value;
    }

    private static CatalogValidationException Field(string field, string message) =>
        new(field, message);
}
