using System.Security.Cryptography;

namespace HardwareStore.Modules.PcBuilder;

/// <summary>A part of a saved build: the stock item's public code and the quantity.</summary>
public sealed record SavedBuildPart(string Code, int Quantity);

/// <summary>A build saved from the builder. The token is the share link; only the owner (when signed in) can change or delete it.</summary>
public sealed class SavedBuild
{
    public Guid Id { get; set; }
    public required string Token { get; set; }
    public Guid? UserAccountId { get; set; }
    public string? Name { get; set; }
    public string? Usage { get; set; }
    public BuildVisibility Visibility { get; set; } = BuildVisibilityRules.Default;
    public List<SavedBuildPart> Parts { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;

    /// <summary>The admin's decision, once the build has been made public; null for a build that never was.</summary>
    public BuildModeration? Moderation { get; set; }
}

/// <summary>Saved build rules: name cleaning, the per-account limit and the unguessable share token.</summary>
public static class SavedBuildRules
{
    public const int MaxPerAccount = 50;
    public const int MaxNameLength = 100;
    private const int TokenBytes = 16;

    public static string? CleanName(string? name)
    {
        var text = name?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length > MaxNameLength) throw new PcBuilderException(400, $"The build name must be at most {MaxNameLength} characters.");
        return text;
    }

    public static void EnsureParts(IReadOnlyList<SavedBuildPart> parts)
    {
        if (parts.Count == 0) throw new PcBuilderException(400, "A saved build needs at least one part.");
    }

    public static void EnsureRoomForAnother(int savedCount)
    {
        if (savedCount >= MaxPerAccount) throw new PcBuilderException(409, $"You can keep at most {MaxPerAccount} saved builds.");
    }

    /// <summary>Random URL-safe token (base64url, no padding).</summary>
    public static string NewToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
