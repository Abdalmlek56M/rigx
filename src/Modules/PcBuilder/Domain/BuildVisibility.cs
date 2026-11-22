using System.Text.Json.Serialization;

namespace HardwareStore.Modules.PcBuilder;

/// <summary>Who may open a saved build: only its owner, anyone with the link (the default), or the public gallery after approval.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BuildVisibility>))]
public enum BuildVisibility
{
    Private,
    Link,
    Public
}

/// <summary>Visibility rules of a saved build.</summary>
public static class BuildVisibilityRules
{
    /// <summary>What a build is when its creator did not choose (and what an anonymous build always is).</summary>
    public const BuildVisibility Default = BuildVisibility.Link;

    /// <summary>A private or public build belongs to an account: a build saved while signed out can only be shared by its link.</summary>
    public static void EnsureAllowed(BuildVisibility visibility, bool ownerSignedIn)
    {
        if (visibility != Default && !ownerSignedIn)
            throw new PcBuilderException(400, "Sign in to keep a build private or make it public.");
    }

    /// <summary>A private build is seen only by its owner; every other build by whoever has its link.</summary>
    public static bool CanView(BuildVisibility visibility, Guid? ownerId, Guid? viewerId) =>
        visibility != BuildVisibility.Private || (ownerId is not null && ownerId == viewerId);
}
