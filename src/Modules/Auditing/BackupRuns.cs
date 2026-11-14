using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Auditing;

public static class BackupTrigger
{
    public const string Scheduled = "Scheduled";
    public const string Manual = "Manual";
    public const string BeforeReset = "BeforeReset";
}

public static class BackupStatus
{
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}

/// <summary>One backup attempt (what ran, where it went, whether a restore drill has proved it). Rows are kept, never deleted.</summary>
public sealed class BackupRun
{
    public Guid Id { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset FinishedAtUtc { get; set; }
    public required string Trigger { get; set; }
    public required string Status { get; set; }
    public string? FileName { get; set; }
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public int DestinationsWritten { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public bool? VerifyPassed { get; set; }
    public int? VerifyTables { get; set; }
    public long? VerifyRows { get; set; }
    public string? VerifyError { get; set; }
}

/// <summary>The backup history. Lives with the audit records because it is append-mostly operational evidence.</summary>
public sealed class BackupRunStore(IDbContextFactory<SecurityAuditDbContext> contextFactory)
{
    private const int MaxListed = 100;

    public async Task AddAsync(BackupRun run, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.BackupRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<BackupRun?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.BackupRuns.AsNoTracking().SingleOrDefaultAsync(run => run.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<BackupRun>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.BackupRuns.AsNoTracking().OrderByDescending(run => run.StartedAtUtc).Take(MaxListed).ToListAsync(cancellationToken);
    }

    public async Task<DateTimeOffset?> LastSuccessStartedAtAsync(string trigger, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.BackupRuns.AsNoTracking().Where(run => run.Trigger == trigger && run.Status == BackupStatus.Succeeded)
            .OrderByDescending(run => run.StartedAtUtc).Select(run => (DateTimeOffset?)run.StartedAtUtc).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task RecordVerificationAsync(Guid id, DateTimeOffset at, bool passed, int? tables, long? rows, string? error,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await db.BackupRuns.Where(run => run.Id == id).ExecuteUpdateAsync(update => update
            .SetProperty(run => run.VerifiedAtUtc, at).SetProperty(run => run.VerifyPassed, passed)
            .SetProperty(run => run.VerifyTables, tables).SetProperty(run => run.VerifyRows, rows)
            .SetProperty(run => run.VerifyError, error), cancellationToken);
    }
}
