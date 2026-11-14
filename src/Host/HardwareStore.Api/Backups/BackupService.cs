using HardwareStore.Common.Domain;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Storefront;
using Npgsql;

/// <summary>One backup (or restore drill) at a time per app instance.</summary>
public sealed class BackupGate
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}

/// <summary>
/// The daily full backup: dump + photos + manifest in one zip, copied to every destination, recorded in <c>backup_runs</c>;
/// the restore drill that proves an archive can be restored; download of an archive; and the scheduled run with retention.
/// </summary>
public sealed class BackupService(
    BackupOptions options,
    BackupGate gate,
    IDatabaseBackupTool tool,
    BackupDestinations destinations,
    RestoreDrill drill,
    BackupRunStore store,
    StoreSettingsService settings,
    CatalogMediaOptions media,
    BackupAuditor audit,
    TimeProvider timeProvider,
    TimeZoneInfo businessZone,
    ILogger<BackupService> logger)
{
    private const int MaxErrorLength = 300;

    public Task<IReadOnlyList<BackupRun>> ListAsync(CancellationToken cancellationToken) => store.ListAsync(cancellationToken);

    /// <summary>Runs a backup now. A failed attempt is recorded and returned (status Failed); "backups are off" and "already running" are 409s.</summary>
    public async Task<BackupRun> RunAsync(string trigger, Guid? actorId, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        if (!await gate.Semaphore.WaitAsync(0, cancellationToken))
            throw new BackupException(409, "A backup or restore check is already running.");
        try
        {
            var run = await BuildRunAsync(trigger, cancellationToken);
            await store.AddAsync(run, cancellationToken);
            await audit.CreatedAsync(run, actorId, cancellationToken);
            if (run.Status == BackupStatus.Succeeded)
                await ApplyRetentionAsync(cancellationToken);
            return run;
        }
        finally
        {
            gate.Semaphore.Release();
        }
    }

    /// <summary>The job's entry point: runs the scheduled backup when it is due today (Cairo) and returns it, else null.</summary>
    public async Task<BackupRun?> RunScheduledIfDueAsync(CancellationToken cancellationToken)
    {
        if (!options.IsConfigured) return null;
        var schedule = (await settings.GetAdminAsync(cancellationToken)).Backup;
        var last = await store.LastSuccessStartedAtAsync(BackupTrigger.Scheduled, cancellationToken);
        return BackupRules.IsDue(schedule.Enabled, schedule.HourCairo, timeProvider.GetUtcNow(), last, businessZone)
            ? await RunAsync(BackupTrigger.Scheduled, null, cancellationToken)
            : null;
    }

    /// <summary>
    /// Restore drill: checks the archive's SHA-256 against its manifest, restores it into a temporary database (always dropped) and
    /// records the result on the run. A failed check is recorded and returned, not thrown.
    /// </summary>
    public async Task<BackupRun> VerifyAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var run = await store.FindAsync(id, cancellationToken) ?? throw new BackupException(404, "Backup not found.");
        var archive = (run.FileName is null ? null : destinations.Find(run.FileName))
            ?? throw new BackupException(409, "The archive is not in any backup destination.");
        if (!await gate.Semaphore.WaitAsync(0, cancellationToken))
            throw new BackupException(409, "A backup or restore check is already running.");
        try
        {
            var (passed, result, error) = await DrillAsync(archive, cancellationToken);
            await store.RecordVerificationAsync(id, timeProvider.GetUtcNow(), passed, result?.Tables, result?.Rows, error, cancellationToken);
            await audit.VerifiedAsync(id, passed, result?.Tables, result?.Rows, error, actorId, cancellationToken);
            return (await store.FindAsync(id, cancellationToken))!;
        }
        finally
        {
            gate.Semaphore.Release();
        }
    }

    /// <summary>Opens the archive for download (audited). The caller disposes the stream.</summary>
    public async Task<(Stream Content, string FileName)> OpenDownloadAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var run = await store.FindAsync(id, cancellationToken) ?? throw new BackupException(404, "Backup not found.");
        var path = run.FileName is null ? null : destinations.Find(run.FileName);
        if (path is null)
            throw new BackupException(409, "The archive is not in any backup destination.");
        await audit.DownloadedAsync(id, run.FileName!, actorId, cancellationToken);
        return (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), run.FileName!);
    }

    private void EnsureConfigured()
    {
        if (!options.IsConfigured)
            throw new BackupException(409, "Backups are off: no backup destination is configured on the server.");
    }

    private async Task<(bool Passed, DrillResult? Result, string? Error)> DrillAsync(string archivePath, CancellationToken cancellationToken)
    {
        var workDirectory = Directory.CreateTempSubdirectory("rigx-restore-").FullName;
        try
        {
            var (manifest, hashMatches) = await BackupArchive.ExtractDumpAsync(archivePath, Path.Combine(workDirectory, BackupArchive.DumpEntry), cancellationToken);
            if (!hashMatches)
                return (false, null, "The dump does not match the SHA-256 in the manifest.");
            var result = await drill.RunAsync(Path.Combine(workDirectory, BackupArchive.DumpEntry), cancellationToken);
            return result.Tables > 0
                ? (true, result, null)
                : (false, result, $"The restored database is empty (manifest from {manifest.CreatedAtUtc:u}).");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Restore check failed.");
            return (false, null, ShortError(exception));
        }
        finally
        {
            DeleteDirectory(workDirectory);
        }
    }

    private async Task<BackupRun> BuildRunAsync(string trigger, CancellationToken cancellationToken)
    {
        var started = timeProvider.GetUtcNow();
        var fileName = BackupRules.FileName(started);
        var workDirectory = Directory.CreateTempSubdirectory("rigx-backup-").FullName;
        try
        {
            var dumpPath = Path.Combine(workDirectory, BackupArchive.DumpEntry);
            var zipPath = Path.Combine(workDirectory, fileName);
            await tool.DumpAsync(dumpPath, cancellationToken);
            var manifest = await BackupArchive.WriteAsync(zipPath, dumpPath, media.RootPath, started,
                typeof(BackupService).Assembly.GetName().Version?.ToString() ?? "unknown", await DatabaseSizeAsync(cancellationToken),
                await DatabaseMigrator.AppliedMigrationsAsync(options.ConnectionString, cancellationToken), cancellationToken);
            var written = await destinations.PublishAsync(zipPath, fileName, cancellationToken);
            var complete = written == options.Destinations.Count;
            return Record(trigger, started, complete, fileName, new FileInfo(zipPath).Length, manifest.DatabaseDumpSha256, written,
                complete ? null : $"Only {written} of {options.Destinations.Count} destinations were written.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Backup failed.");
            return Record(trigger, started, false, null, 0, null, 0, ShortError(exception));
        }
        finally
        {
            DeleteDirectory(workDirectory);
        }
    }

    private BackupRun Record(string trigger, DateTimeOffset started, bool succeeded, string? fileName, long size, string? sha256, int written, string? error) =>
        new()
        {
            Id = Guid.NewGuid(), StartedAtUtc = started, FinishedAtUtc = timeProvider.GetUtcNow(), Trigger = trigger,
            Status = succeeded ? BackupStatus.Succeeded : BackupStatus.Failed, FileName = fileName, SizeBytes = size, Sha256 = sha256,
            DestinationsWritten = written, Error = error
        };

    private async Task ApplyRetentionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var days = (await settings.GetAdminAsync(cancellationToken)).Backup.RetentionDays;
            destinations.ApplyRetention(BackupRules.RetentionCutoffUtc(timeProvider.GetUtcNow(), days));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Backup retention failed.");
        }
    }

    private async Task<long> DatabaseSizeAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT pg_database_size(current_database())", connection);
        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private static string ShortError(Exception exception)
    {
        var message = exception is BackupToolException ? exception.Message : $"{exception.GetType().Name}: {exception.Message}";
        return message.Length <= MaxErrorLength ? message : message[..MaxErrorLength];
    }

    private static void DeleteDirectory(string path)
    {
        try { Directory.Delete(path, recursive: true); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The temporary folder is cleaned by the OS later; nothing else depends on it.
        }
    }
}
