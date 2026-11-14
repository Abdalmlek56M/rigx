using System.Text.Json;
using HardwareStore.Modules.Auditing;

/// <summary>Audit trail of backups: created (also when it failed), verified by a restore drill, downloaded.</summary>
public sealed class BackupAuditor(ISecurityAuditWriter writer)
{
    public const string CreatedEvent = "System.BackupCreated";
    public const string VerifiedEvent = "System.BackupVerified";
    public const string DownloadedEvent = "System.BackupDownloaded";

    public Task CreatedAsync(BackupRun run, Guid? actorId, CancellationToken cancellationToken) =>
        WriteAsync(CreatedEvent, run.Status, actorId,
            new { runId = run.Id, trigger = run.Trigger, file = run.FileName, run.SizeBytes, destinations = run.DestinationsWritten, error = run.Error },
            cancellationToken);

    public Task VerifiedAsync(Guid runId, bool passed, int? tables, long? rows, string? error, Guid actorId, CancellationToken cancellationToken) =>
        WriteAsync(VerifiedEvent, passed ? "Succeeded" : "Failed", actorId, new { runId, tables, rows, error }, cancellationToken);

    public Task DownloadedAsync(Guid runId, string fileName, Guid actorId, CancellationToken cancellationToken) =>
        WriteAsync(DownloadedEvent, "Succeeded", actorId, new { runId, file = fileName }, cancellationToken);

    private Task WriteAsync(string eventType, string outcome, Guid? actorId, object metadata, CancellationToken cancellationToken) =>
        writer.WriteAsync(new SecurityAuditEntry(eventType, outcome, actorId is null ? "System" : "User", actorId,
            SafeMetadata: JsonSerializer.Serialize(metadata)), cancellationToken);
}
