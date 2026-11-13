using System.Text;

namespace HardwareStore.Common.Domain;

/// <summary>
/// Free-text hardware model names. The owner types any model; the system matches spellings
/// through one key, so "rx6600", "RX 6600" and "RX-6600" are the same model.
/// </summary>
public static class ModelName
{
    public const int MaxLength = 160;

    /// <summary>Trims and collapses inner whitespace; keeps the owner's spelling and case.</summary>
    public static string Clean(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Matching key: letters and digits only, lower case (Arabic letters are kept).</summary>
    public static string Key(string text)
    {
        var key = new StringBuilder(text.Length);
        foreach (var character in text)
            if (char.IsLetterOrDigit(character)) key.Append(char.ToLowerInvariant(character));
        return key.ToString();
    }
}
