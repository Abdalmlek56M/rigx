namespace HardwareStore.Modules.Purchasing;

/// <summary>Free text: trimmed, empty becomes null (or an error when required), too long is a 400 naming the field.</summary>
internal static class TextRules
{
    public static string? Optional(string? text, int maxLength, string field)
    {
        var clean = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (clean?.Length > maxLength) throw new PurchasingException(400, $"{field} is too long (at most {maxLength} characters).");
        return clean;
    }

    public static string Required(string? text, int maxLength, string field) =>
        Optional(text, maxLength, field) ?? throw new PurchasingException(400, $"{field} is required.");
}
