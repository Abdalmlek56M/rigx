using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

/// <summary>What <c>manifest.json</c> says about an archive.</summary>
public sealed record BackupManifest(DateTimeOffset CreatedAtUtc, string AppVersion, long DatabaseSizeBytes, int FileCount,
    string DatabaseDumpSha256, IReadOnlyDictionary<string, IReadOnlyList<string>> Migrations);

/// <summary>The backup zip: <c>database.dump</c>, the media folder under <c>media/</c> and <c>manifest.json</c>.</summary>
public static class BackupArchive
{
    public const string DumpEntry = "database.dump";
    public const string ManifestEntry = "manifest.json";
    private const string MediaPrefix = "media/";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static async Task<BackupManifest> WriteAsync(string zipPath, string dumpPath, string mediaRoot, DateTimeOffset createdAtUtc,
        string appVersion, long databaseSizeBytes, IReadOnlyDictionary<string, IReadOnlyList<string>> migrations, CancellationToken cancellationToken)
    {
        var dumpHash = await HashFileAsync(dumpPath, cancellationToken);
        await using var zipStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);
        await AddFileAsync(archive, DumpEntry, dumpPath, cancellationToken);

        var fileCount = 0;
        if (Directory.Exists(mediaRoot))
            foreach (var file in Directory.EnumerateFiles(mediaRoot, "*", SearchOption.AllDirectories))
                if (await TryAddFileAsync(archive, MediaPrefix + Path.GetRelativePath(mediaRoot, file).Replace('\\', '/'), file, cancellationToken))
                    fileCount++;

        var manifest = new BackupManifest(createdAtUtc, appVersion, databaseSizeBytes, fileCount, dumpHash, migrations);
        var entry = archive.CreateEntry(ManifestEntry, CompressionLevel.Fastest);
        await using (var output = entry.Open())
            await JsonSerializer.SerializeAsync(output, manifest, Json, cancellationToken);
        return manifest;
    }

    /// <summary>Extracts <c>database.dump</c> to <paramref name="dumpTarget"/> and returns the manifest plus whether the dump's SHA-256 matches it.</summary>
    public static async Task<(BackupManifest Manifest, bool HashMatches)> ExtractDumpAsync(string zipPath, string dumpTarget, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var manifestEntry = archive.GetEntry(ManifestEntry) ?? throw new BackupToolException("The archive has no manifest.");
        BackupManifest manifest;
        await using (var input = manifestEntry.Open())
            manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(input, Json, cancellationToken)
                ?? throw new BackupToolException("The archive manifest is empty.");
        var dumpEntry = archive.GetEntry(DumpEntry) ?? throw new BackupToolException("The archive has no database dump.");
        await using (var input = dumpEntry.Open())
        await using (var output = new FileStream(dumpTarget, FileMode.Create, FileAccess.Write, FileShare.None))
            await input.CopyToAsync(output, cancellationToken);
        var actual = await HashFileAsync(dumpTarget, cancellationToken);
        return (manifest, string.Equals(actual, manifest.DatabaseDumpSha256, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<string> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
    }

    private static async Task AddFileAsync(ZipArchive archive, string entryName, string path, CancellationToken cancellationToken)
    {
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        await using var output = archive.CreateEntry(entryName, CompressionLevel.Fastest).Open();
        await input.CopyToAsync(output, cancellationToken);
    }

    /// <summary>Photos can be deleted while the backup runs; a file that vanished is simply skipped.</summary>
    private static async Task<bool> TryAddFileAsync(ZipArchive archive, string entryName, string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return false;
        try
        {
            await AddFileAsync(archive, entryName, path, cancellationToken);
            return true;
        }
        catch (FileNotFoundException) { return false; }
    }
}
