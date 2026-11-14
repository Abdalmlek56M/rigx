using HardwareStore.Common.Domain;

/// <summary>The folders archives are copied to (from configuration only): publish atomically, find, and apply retention.</summary>
public sealed class BackupDestinations(BackupOptions options, ILogger<BackupDestinations> logger)
{
    /// <summary>Copies the archive to every destination (a <c>.tmp</c> file first, then a move) and returns how many succeeded.</summary>
    public async Task<int> PublishAsync(string archivePath, string fileName, CancellationToken cancellationToken)
    {
        var written = 0;
        foreach (var destination in options.Destinations)
        {
            var temporary = Path.Combine(destination, fileName + ".tmp");
            try
            {
                Directory.CreateDirectory(destination);
                await using (var input = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                await using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                    await input.CopyToAsync(output, cancellationToken);
                File.Move(temporary, Path.Combine(destination, fileName), overwrite: true);
                written++;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger.LogError(exception, "Backup copy to {Destination} failed.", destination);
                TryDelete(temporary);
            }
        }
        return written;
    }

    /// <summary>The archive in the first destination that has it, or null.</summary>
    public string? Find(string fileName) =>
        options.Destinations.Select(destination => Path.Combine(destination, fileName)).FirstOrDefault(File.Exists);

    /// <summary>Deletes archives (only names this app writes) older than the cutoff from every destination.</summary>
    public void ApplyRetention(DateTimeOffset cutoffUtc)
    {
        foreach (var destination in options.Destinations.Where(Directory.Exists))
            foreach (var file in Directory.EnumerateFiles(destination).Where(path => BackupRules.IsExpired(Path.GetFileName(path), cutoffUtc)))
                try { File.Delete(file); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    logger.LogWarning(exception, "Could not delete the expired backup {File}.", Path.GetFileName(file));
                }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A leftover .tmp file is harmless; the next run overwrites it.
        }
    }
}
