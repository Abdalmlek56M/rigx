using Npgsql;

/// <summary>What a restore drill found in the restored copy: tables and the total rows in them.</summary>
public sealed record DrillResult(int Tables, long Rows);

/// <summary>
/// Restores a dump into a throwaway database (<c>rigx_restore_check_*</c>), counts what came back and always drops the database
/// afterwards. The live database is never touched.
/// </summary>
public sealed class RestoreDrill(BackupOptions options, IDatabaseBackupTool tool, ILogger<RestoreDrill> logger)
{
    public const string DatabasePrefix = "rigx_restore_check_";

    public async Task<DrillResult> RunAsync(string dumpPath, CancellationToken cancellationToken)
    {
        var name = DatabasePrefix + Guid.NewGuid().ToString("N")[..12];
        await ExecuteAsync(options.ConnectionString, $"CREATE DATABASE {Quote(name)}", cancellationToken);
        try
        {
            await tool.RestoreAsync(dumpPath, name, cancellationToken);
            return await CountAsync(name, cancellationToken);
        }
        finally
        {
            try { await ExecuteAsync(options.ConnectionString, $"DROP DATABASE IF EXISTS {Quote(name)} WITH (FORCE)", CancellationToken.None); }
            catch (Exception exception) { logger.LogError(exception, "Could not drop the restore-check database {Database}.", name); }
        }
    }

    private async Task<DrillResult> CountAsync(string database, CancellationToken cancellationToken)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(options.ConnectionString) { Database = database, Pooling = false }.ConnectionString;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var tables = new List<string>();
        await using (var list = new NpgsqlCommand(
            "SELECT table_schema, table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema NOT IN ('pg_catalog', 'information_schema')",
            connection))
        await using (var reader = await list.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                tables.Add($"{Quote(reader.GetString(0))}.{Quote(reader.GetString(1))}");

        long rows = 0;
        foreach (var table in tables)
        {
            await using var count = new NpgsqlCommand($"SELECT count(*) FROM {table}", connection);
            rows += (long)(await count.ExecuteScalarAsync(cancellationToken))!;
        }
        return new DrillResult(tables.Count, rows);
    }

    private static async Task ExecuteAsync(string connectionString, string sql, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
