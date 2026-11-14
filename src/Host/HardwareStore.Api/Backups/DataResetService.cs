using System.Text.Json;
using HardwareStore.Common.Domain;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;

/// <summary>What a reset did: the sections, the backup taken first and how many rows went.</summary>
public sealed record DataResetResult(IReadOnlyList<string> Sections, Guid BackupRunId, int RowsDeleted);

/// <summary>
/// The owner data reset. A successful backup (trigger BeforeReset) is taken first; if it cannot be, nothing is deleted.
/// Each module then clears its own tables (one transaction per module), the private photos that belonged to the business data
/// are removed, and the reset is audited. Staff, settings, catalog, audit logs and public-code sequences are never touched.
/// </summary>
public sealed class DataResetService(
    IEnumerable<IDataResetSource> sources,
    BackupService backups,
    ICatalogMediaStorage storage,
    ISecurityAuditWriter audit)
{
    public const string ResetEvent = "System.DataReset";

    public async Task<DataResetResult> ResetAsync(IReadOnlyCollection<string>? requested, string? confirm, Guid actorId, CancellationToken cancellationToken)
    {
        var sections = DataResetRules.Validate(requested, confirm);
        var backup = await backups.RunAsync(BackupTrigger.BeforeReset, actorId, cancellationToken);
        if (backup.Status != BackupStatus.Succeeded)
            throw new DataResetException(409, "The backup before the reset failed, so nothing was deleted.");

        var ordered = DataResetRules.All.Where(sections.Contains).ToList();
        try
        {
            var rows = 0;
            foreach (var source in sources)
                rows += await source.ResetAsync(sections, cancellationToken);
            if (sections.Contains(DataResetRules.Business))
                await DeletePrivatePhotosAsync(cancellationToken);
            await WriteAuditAsync("Succeeded", actorId, ordered, backup.Id, rows, null, cancellationToken);
            return new DataResetResult(ordered, backup.Id, rows);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await WriteAuditAsync("Failed", actorId, ordered, backup.Id, 0, exception.GetType().Name, CancellationToken.None);
            throw;
        }
    }

    /// <summary>Request photos and expense receipts are the only private files, and they belong to the business data.</summary>
    private async Task DeletePrivatePhotosAsync(CancellationToken cancellationToken)
    {
        foreach (var file in await storage.ListFilesAsync(cancellationToken))
            if (CatalogMediaKeys.IsPrivate(file.StorageKey))
                await storage.DeleteStorageKeyAsync(file.StorageKey, cancellationToken);
    }

    private Task WriteAuditAsync(string outcome, Guid actorId, IReadOnlyList<string> sections, Guid backupRunId, int rows, string? error,
        CancellationToken cancellationToken) =>
        audit.WriteAsync(new SecurityAuditEntry(ResetEvent, outcome, "User", actorId,
            SafeMetadata: JsonSerializer.Serialize(new { sections, backupRunId, rows, error })), cancellationToken);
}
