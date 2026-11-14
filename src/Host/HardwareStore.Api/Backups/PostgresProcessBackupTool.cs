using System.Diagnostics;
using Npgsql;

/// <summary>
/// Runs <c>pg_dump</c> / <c>pg_restore</c> as child processes: an argument list (no shell), the password only in the
/// <c>PGPASSWORD</c> environment variable, a timeout, and stderr captured and truncated.
/// </summary>
public sealed class PostgresProcessBackupTool(BackupOptions options) : IDatabaseBackupTool
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(30);
    private const int MaxErrorLength = 300;

    public Task DumpAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnectionStringBuilder(options.ConnectionString);
        return RunAsync(options.PgDumpPath, connection, ["--format=custom", "--no-owner", $"--file={outputPath}", .. Target(connection, connection.Database)], cancellationToken);
    }

    public Task RestoreAsync(string dumpPath, string databaseName, CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnectionStringBuilder(options.ConnectionString);
        return RunAsync(options.PgRestorePath, connection, ["--no-owner", "--no-acl", "--exit-on-error", .. Target(connection, databaseName), dumpPath], cancellationToken);
    }

    private static string[] Target(NpgsqlConnectionStringBuilder connection, string? database) =>
    [
        $"--host={connection.Host}", $"--port={connection.Port}", $"--username={connection.Username}", $"--dbname={database}", "--no-password"
    ];

    private static async Task RunAsync(string executable, NpgsqlConnectionStringBuilder connection, IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(executable) { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        if (!string.IsNullOrEmpty(connection.Password)) start.Environment["PGPASSWORD"] = connection.Password;

        using var process = new Process { StartInfo = start };
        try { process.Start(); }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            throw new BackupToolException($"Cannot start {Path.GetFileName(executable)}.");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            await Task.WhenAll(error, output);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            cancellationToken.ThrowIfCancellationRequested();
            throw new BackupToolException($"{Path.GetFileName(executable)} timed out.");
        }

        if (process.ExitCode != 0)
            throw new BackupToolException($"{Path.GetFileName(executable)} failed ({process.ExitCode}): {Truncate(error.Result)}");
    }

    private static string Truncate(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length <= MaxErrorLength ? trimmed : trimmed[..MaxErrorLength];
    }
}
