namespace HardwareStore.Common.Domain;

/// <summary>
/// An entity that is deleted to the trash first: it is hidden everywhere (global query filter) and can be restored
/// until <see cref="TrashRules.RetentionDays"/> pass. Never put append-only, money or stock records here.
/// </summary>
public interface ISoftDeletable
{
    Guid Id { get; }
    DateTimeOffset? DeletedAtUtc { get; set; }
    Guid? DeletedByUserId { get; set; }
}
