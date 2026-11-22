namespace HardwareStore.Common.Domain;

/// <summary>
/// Search text as an ILIKE pattern. The text is matched literally: <c>%</c>, <c>_</c> and the escape character in it are escaped, and the query
/// must name the escape character (<c>EF.Functions.ILike(column, pattern, LikePattern.Escape)</c>), because without it PostgreSQL through
/// Npgsql does not treat the backslash as an escape and a search containing one of them finds nothing.
/// </summary>
public static class LikePattern
{
    public const string Escape = @"\";

    /// <summary>The text with every wildcard and the escape character escaped, with no surrounding wildcards.</summary>
    public static string Escaped(string text) =>
        text.Replace(Escape, Escape + Escape, StringComparison.Ordinal)
            .Replace("%", Escape + "%", StringComparison.Ordinal)
            .Replace("_", Escape + "_", StringComparison.Ordinal);

    /// <summary>A "contains" pattern for the trimmed text.</summary>
    public static string Contains(string text) => "%" + Escaped(text.Trim()) + "%";
}
