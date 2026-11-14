/// <summary>The PostgreSQL command-line tools the backup needs, behind an interface so tests can run them inside a container.</summary>
public interface IDatabaseBackupTool
{
    /// <summary>Writes a custom-format dump (no owners) of the whole database to <paramref name="outputPath"/>.</summary>
    Task DumpAsync(string outputPath, CancellationToken cancellationToken = default);

    /// <summary>Restores a dump into the existing, empty database <paramref name="databaseName"/>.</summary>
    Task RestoreAsync(string dumpPath, string databaseName, CancellationToken cancellationToken = default);
}

/// <summary>A backup tool failed; the message is short and never contains the connection password.</summary>
public sealed class BackupToolException(string message) : Exception(message);
