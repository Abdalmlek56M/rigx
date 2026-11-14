using System.Text.Json;
using HardwareStore.Modules.Auditing;

/// <summary>Writes the audit trail of the trash: every delete, restore and purge records what (kind and id) and who.</summary>
public sealed class TrashAuditor(ISecurityAuditWriter writer)
{
    public const string DeletedEvent = "Trash.Deleted";
    public const string RestoredEvent = "Trash.Restored";
    public const string PurgedEvent = "Trash.Purged";

    public Task DeletedAsync(string kind, Guid id, Guid actorId, CancellationToken cancellationToken) =>
        WriteAsync(DeletedEvent, kind, id, actorId, cancellationToken);

    public Task RestoredAsync(string kind, Guid id, Guid actorId, CancellationToken cancellationToken) =>
        WriteAsync(RestoredEvent, kind, id, actorId, cancellationToken);

    /// <summary>A purge by a person, or by the retention job when <paramref name="actorId"/> is null.</summary>
    public Task PurgedAsync(string kind, Guid id, Guid? actorId, CancellationToken cancellationToken) =>
        WriteAsync(PurgedEvent, kind, id, actorId, cancellationToken);

    private Task WriteAsync(string eventType, string kind, Guid id, Guid? actorId, CancellationToken cancellationToken) =>
        writer.WriteAsync(new SecurityAuditEntry(eventType, "Succeeded", actorId is null ? "System" : "User", actorId,
            SafeMetadata: JsonSerializer.Serialize(new { kind, id })), cancellationToken);
}
