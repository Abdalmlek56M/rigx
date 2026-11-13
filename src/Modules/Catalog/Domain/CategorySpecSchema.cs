using System.Globalization;
using System.Text.RegularExpressions;

namespace HardwareStore.Modules.Catalog;

/// <summary>Value types a category spec can have (admin "choice / number / yes-no", plus short text).</summary>
public static class SpecType
{
    public const string Choice = "Choice";
    public const string Number = "Number";
    public const string YesNo = "YesNo";
    public const string Text = "Text";

    public static readonly IReadOnlyList<string> All = [Choice, Number, YesNo, Text];
    public const string Yes = "yes";
    public const string No = "no";
}

/// <summary>
/// One spec a category defines (e.g. socket, power W, integrated GPU). <see cref="IsFilter"/> shows it as a storefront
/// filter; <see cref="IsCompatibility"/> marks it for builder compatibility (2.13). Children inherit their parents' specs.
/// </summary>
public sealed record SpecDefinition(string Key, string Name, string Type, IReadOnlyList<string>? Options = null, string? Unit = null,
    bool IsFilter = false, bool IsCompatibility = false);

/// <summary>Category spec schema rules: definition checks, inheritance down the tree, and checking item values.</summary>
public static partial class CategorySpecSchema
{
    public const int MaxDefinitions = 40;
    public const int MaxOptions = 50;

    [GeneratedRegex("^[a-z][a-z0-9_]{0,39}$")]
    private static partial Regex KeyPattern();

    [GeneratedRegex("^[A-Z][A-Z0-9]{1,15}$")]
    private static partial Regex CategoryCodePattern();

    public static bool IsValidKey(string key) => KeyPattern().IsMatch(key);

    /// <summary>Category code such as CPU or GPU: upper-case letters/digits, 2–16 chars; empty means none.</summary>
    public static string? NormalizeCategoryCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var clean = code.Trim().ToUpperInvariant();
        if (!CategoryCodePattern().IsMatch(clean))
            throw new CatalogValidationException("code", "Category code must be 2-16 letters or digits, starting with a letter.");
        return clean;
    }

    public static IReadOnlyList<SpecDefinition> Normalize(IReadOnlyList<SpecDefinition>? definitions)
    {
        var list = definitions ?? [];
        if (list.Count > MaxDefinitions)
            throw new CatalogValidationException("specs", $"A category can define at most {MaxDefinitions} specs.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        return list.Select(definition =>
        {
            var key = definition.Key?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!IsValidKey(key))
                throw new CatalogValidationException("specs", "A spec key must be lower-case letters, digits or '_' (max 40).");
            if (!keys.Add(key)) throw new CatalogValidationException("specs", $"Spec key '{key}' is repeated.");
            var name = definition.Name?.Trim() ?? string.Empty;
            if (name.Length is 0 or > 64) throw new CatalogValidationException("specs", "A spec name needs 1-64 characters.");
            var type = SpecType.All.FirstOrDefault(value => string.Equals(value, definition.Type?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new CatalogValidationException("specs", $"Spec '{key}' has an unknown type.");
            var unit = string.IsNullOrWhiteSpace(definition.Unit) ? null : definition.Unit.Trim();
            if (unit?.Length > 16) throw new CatalogValidationException("specs", "A spec unit is too long.");
            return new SpecDefinition(key, name, type, NormalizeOptions(key, type, definition.Options), unit,
                definition.IsFilter, definition.IsCompatibility);
        }).ToList();
    }

    /// <summary>
    /// The schema a category really has: its ancestors' specs first (root → leaf), a child's spec replacing a parent's spec
    /// with the same key in place.
    /// </summary>
    public static IReadOnlyList<SpecDefinition> Effective(IEnumerable<IReadOnlyList<SpecDefinition>> rootToLeaf)
    {
        var merged = new List<SpecDefinition>();
        foreach (var level in rootToLeaf)
            foreach (var definition in level)
            {
                var index = merged.FindIndex(existing => existing.Key == definition.Key);
                if (index >= 0) merged[index] = definition; else merged.Add(definition);
            }
        return merged;
    }

    /// <summary>
    /// Checks item spec values against the effective schema. Every spec stays optional (business §3); empty values are dropped.
    /// With an empty schema any key is allowed; with a schema, keys must be defined and values must fit the type.
    /// Returns the cleaned values (numbers in invariant form, yes/no lower-case, choices spelled as defined).
    /// </summary>
    public static Dictionary<string, string> CheckValues(IReadOnlyList<SpecDefinition> schema, IReadOnlyDictionary<string, string> values)
    {
        if (schema.Count == 0) return new Dictionary<string, string>(values, StringComparer.Ordinal);
        var byKey = schema.ToDictionary(definition => definition.Key, StringComparer.Ordinal);
        var clean = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, raw) in values)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (!byKey.TryGetValue(key, out var definition))
                throw new CatalogValidationException("specs", $"Spec '{key}' is not defined for this category.");
            clean[key] = CheckValue(definition, raw.Trim());
        }
        return clean;
    }

    private static string CheckValue(SpecDefinition definition, string value)
    {
        switch (definition.Type)
        {
            case SpecType.Number:
                if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                    throw new CatalogValidationException("specs", $"'{definition.Name}' must be a number.");
                return number.ToString(CultureInfo.InvariantCulture);
            case SpecType.YesNo:
                return value.ToLowerInvariant() switch
                {
                    SpecType.Yes or "true" => SpecType.Yes,
                    SpecType.No or "false" => SpecType.No,
                    _ => throw new CatalogValidationException("specs", $"'{definition.Name}' must be yes or no.")
                };
            case SpecType.Choice:
                return definition.Options!.FirstOrDefault(option => string.Equals(option, value, StringComparison.OrdinalIgnoreCase))
                    ?? throw new CatalogValidationException("specs", $"'{value}' is not an option of '{definition.Name}'.");
            default:
                return value;
        }
    }

    private static IReadOnlyList<string>? NormalizeOptions(string key, string type, IReadOnlyList<string>? options)
    {
        var clean = (options ?? []).Where(option => !string.IsNullOrWhiteSpace(option)).Select(option => option.Trim()).ToList();
        if (type != SpecType.Choice)
        {
            if (clean.Count > 0) throw new CatalogValidationException("specs", $"Only choice specs have options ('{key}').");
            return null;
        }
        if (clean.Count is 0 or > MaxOptions)
            throw new CatalogValidationException("specs", $"Choice spec '{key}' needs 1-{MaxOptions} options.");
        if (clean.Any(option => option.Length > 64)) throw new CatalogValidationException("specs", "A spec option is too long.");
        if (clean.Distinct(StringComparer.OrdinalIgnoreCase).Count() != clean.Count)
            throw new CatalogValidationException("specs", $"Choice spec '{key}' repeats an option.");
        return clean;
    }
}
