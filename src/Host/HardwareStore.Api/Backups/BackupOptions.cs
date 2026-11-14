/// <summary>
/// <c>System:Backup</c> settings. Paths come only from configuration, never from the web API. With no destination, backups are off.
/// The connection used for the dump and the restore drill is <c>ConnectionStrings:Backup</c>, else <c>Migrations</c>, else <c>Default</c>
/// (the role needs read access to every table, and CREATEDB for the drill).
/// </summary>
public sealed class BackupOptions
{
    public string PgDumpPath { get; init; } = "pg_dump";
    public string PgRestorePath { get; init; } = "pg_restore";
    public IReadOnlyList<string> Destinations { get; init; } = [];
    public string ConnectionString { get; init; } = string.Empty;

    public bool IsConfigured => Destinations.Count > 0;

    public static BackupOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("System:Backup");
        return new BackupOptions
        {
            PgDumpPath = section["PgDumpPath"] is { Length: > 0 } dump ? dump : "pg_dump",
            PgRestorePath = section["PgRestorePath"] is { Length: > 0 } restore ? restore : "pg_restore",
            Destinations = [.. (section.GetSection("Destinations").Get<string[]>() ?? []).Where(path => !string.IsNullOrWhiteSpace(path)).Select(Path.GetFullPath)],
            ConnectionString = configuration.GetConnectionString("Backup") ?? configuration.GetConnectionString("Migrations")
                ?? configuration.GetConnectionString("Default") ?? string.Empty
        };
    }
}
