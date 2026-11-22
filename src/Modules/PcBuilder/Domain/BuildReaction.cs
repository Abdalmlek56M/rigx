using System.Text.Json.Serialization;

namespace HardwareStore.Modules.PcBuilder;

/// <summary>A heart keeps the build in the visitor's favourites; a like only says it is good.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BuildReactionKind>))]
public enum BuildReactionKind
{
    Heart,
    Like
}

/// <summary>One signed-in account's heart or like on a build. At most one row per build, account and kind.</summary>
public sealed class BuildReaction
{
    public Guid SavedBuildId { get; set; }
    public Guid UserAccountId { get; set; }
    public BuildReactionKind Kind { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public static class BuildReactionRules
{
    /// <summary>An account keeps at most this many hearted builds (the favourites page lists them all).</summary>
    public const int MaxHearts = 50;

    public static void EnsureRoomForAnotherHeart(int heartedCount)
    {
        if (heartedCount >= MaxHearts) throw new PcBuilderException(409, $"You can heart at most {MaxHearts} builds.");
    }
}
