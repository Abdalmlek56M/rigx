namespace HardwareStore.Common.Domain;

/// <summary>How long deleted items stay restorable, and the moves between live and trashed.</summary>
public static class TrashRules
{
    public const int RetentionDays = 30;

    /// <summary>Items deleted before this instant are past retention and may be purged.</summary>
    public static DateTimeOffset PurgeDueBeforeUtc(DateTimeOffset now) => now.AddDays(-RetentionDays);

    public static void MoveToTrash(ISoftDeletable item, Guid? actorId, DateTimeOffset now)
    {
        item.DeletedAtUtc = now;
        item.DeletedByUserId = actorId;
    }

    public static void Restore(ISoftDeletable item)
    {
        item.DeletedAtUtc = null;
        item.DeletedByUserId = null;
    }
}
