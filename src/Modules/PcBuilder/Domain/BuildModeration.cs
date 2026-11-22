using System.Text.Json.Serialization;

namespace HardwareStore.Modules.PcBuilder;

[JsonConverter(typeof(JsonStringEnumConverter<BuildModerationStatus>))]
public enum BuildModerationStatus
{
    Pending,
    Approved,
    Rejected
}

/// <summary>
/// The admin's decision on a build its owner made public. It is its own audited record (the build itself is not audited), so
/// the audit log shows who approved or rejected which build and when.
/// </summary>
public sealed class BuildModeration
{
    public Guid SavedBuildId { get; set; }
    public BuildModerationStatus Status { get; set; }
    public DateTimeOffset SubmittedAtUtc { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
}

/// <summary>When a public build goes back to the queue and what a decision may be (it is listed in "تجميعات العملاء" when it is public and approved: the gallery query).</summary>
public static class BuildModerationRules
{
    /// <summary>
    /// The status after the owner saves a build (null: no record). Any edit of the name, usage or parts sends a build that has a record, or is
    /// public, back to the queue, so an approval never covers text the admin did not see. Unedited, an approved build stays approved, and a
    /// rejected one stays rejected only while it is saved again still public; a build that is not public keeps its record as it is.
    /// </summary>
    public static BuildModerationStatus? AfterOwnerSave(BuildVisibility visibility, bool wasPublic, bool contentChanged, BuildModerationStatus? current)
    {
        if (contentChanged) return visibility == BuildVisibility.Public || current is not null ? BuildModerationStatus.Pending : null;
        if (visibility != BuildVisibility.Public) return current;
        return current switch
        {
            BuildModerationStatus.Approved => BuildModerationStatus.Approved,
            BuildModerationStatus.Rejected when wasPublic => BuildModerationStatus.Rejected,
            _ => BuildModerationStatus.Pending
        };
    }

    /// <summary>A decision is approve or reject, and only a public build can have one.</summary>
    public static void EnsureCanDecide(BuildVisibility visibility, BuildModerationStatus decision)
    {
        if (decision == BuildModerationStatus.Pending) throw new PcBuilderException(400, "A decision is Approved or Rejected.");
        if (visibility != BuildVisibility.Public) throw new PcBuilderException(409, "Only a public build can be approved or rejected.");
    }
}
