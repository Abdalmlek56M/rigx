using System.Text;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// The readable product URL part (owner decision 18 Nov 2026): the full model name as written, brand included,
/// Latin lower case, words joined by hyphens ("RTX 3060 12GB PNY" → <c>rtx-3060-12gb-pny</c>). No RX code; a collision
/// only adds <c>-2</c>, <c>-3</c>, …. The 4.7 backfill migration repeats the same rule in SQL.
/// </summary>
public static class StockItemSlug
{
    public const int MaxLength = 180;
    /// <summary>Room left after the base for a "-NNNN" suffix.</summary>
    private const int MaxBaseLength = 160;
    /// <summary>A name with no Latin letters or digits (e.g. Arabic only).</summary>
    public const string Fallback = "item";

    /// <summary>The slug before any collision suffix.</summary>
    public static string Base(string modelName)
    {
        var slug = new StringBuilder(modelName.Length);
        foreach (var character in modelName.ToLowerInvariant())
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9') slug.Append(character);
            else if (slug.Length > 0 && slug[^1] != '-') slug.Append('-');
        }
        var clean = slug.ToString().Trim('-');
        if (clean.Length > MaxBaseLength) clean = clean[..MaxBaseLength].TrimEnd('-');
        return clean.Length == 0 ? Fallback : clean;
    }

    /// <summary>The first free slug for <paramref name="baseSlug"/>: the base itself, then <c>-2</c>, <c>-3</c>, ….</summary>
    public static string FirstFree(string baseSlug, IReadOnlySet<string> taken)
    {
        if (!taken.Contains(baseSlug)) return baseSlug;
        for (var suffix = 2; ; suffix++)
        {
            var candidate = $"{baseSlug}-{suffix}";
            if (!taken.Contains(candidate)) return candidate;
        }
    }

    /// <summary>
    /// The slug for <paramref name="itemId"/>: the first one not used by another item now and not one another item had before
    /// (old slugs stay reserved so an old link never leads to a different product). The item may take back its own old slug.
    /// </summary>
    public static string Choose(string baseSlug, Guid itemId, IEnumerable<string> otherItemsSlugs, IEnumerable<StockItemOldSlug> oldSlugs) =>
        FirstFree(baseSlug, otherItemsSlugs
            .Concat(oldSlugs.Where(old => old.StockItemId != itemId).Select(old => old.Slug))
            .ToHashSet(StringComparer.Ordinal));

    /// <summary>True when <paramref name="slug"/> already belongs to <paramref name="baseSlug"/> (the base or a numbered copy), so a rename keeps it.</summary>
    public static bool BelongsTo(string slug, string baseSlug) =>
        slug == baseSlug || (slug.StartsWith(baseSlug + "-", StringComparison.Ordinal) && IsSuffix(slug[(baseSlug.Length + 1)..]));

    private static bool IsSuffix(string text) => text.Length > 0 && text[0] != '0' && text.All(char.IsAsciiDigit) && int.TryParse(text, out var n) && n >= 2;
}
