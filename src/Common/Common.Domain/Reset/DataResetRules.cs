namespace HardwareStore.Common.Domain;

public sealed class DataResetException(int statusCode, string message) : AppException(statusCode, message);

/// <summary>
/// What a module deletes on a data reset, for the sections it owns. Each module runs it in one transaction and
/// touches only explicit tables (never a schema-wide cascade). Returns the number of rows removed.
/// </summary>
public interface IDataResetSource
{
    Task<int> ResetAsync(IReadOnlySet<string> sections, CancellationToken cancellationToken = default);
}

/// <summary>The sections an owner can reset, and the rules for choosing them.</summary>
public static class DataResetRules
{
    public const string Business = "business";
    public const string Content = "content";
    public const string Customers = "customers";
    public const string ConfirmText = "RESET";

    public static readonly IReadOnlyList<string> All = [Business, Content, Customers];

    /// <summary>The distinct, known sections to reset; throws a 400 for an empty or unknown choice, a wrong confirmation, or customers without business.</summary>
    public static IReadOnlySet<string> Validate(IReadOnlyCollection<string>? sections, string? confirm)
    {
        if (!string.Equals(confirm, ConfirmText, StringComparison.Ordinal))
            throw new DataResetException(400, $"Type {ConfirmText} to confirm the reset.");
        if (sections is null or { Count: 0 })
            throw new DataResetException(400, "Choose at least one section to reset.");
        var chosen = sections.Select(section => section?.Trim().ToLowerInvariant() ?? string.Empty).ToHashSet(StringComparer.Ordinal);
        if (chosen.Any(section => !All.Contains(section)))
            throw new DataResetException(400, "Unknown reset section.");
        if (chosen.Contains(Customers) && !chosen.Contains(Business))
            throw new DataResetException(400, "Resetting customers also needs the business section, because orders and warranties point to them.");
        return chosen;
    }
}
