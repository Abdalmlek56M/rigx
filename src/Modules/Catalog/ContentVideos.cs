using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Catalog;

public sealed class ContentVideoAsset
{
    public Guid Id { get; set; }
    public required string TitleAr { get; set; }
    public required string TitleEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public required string StorageKey { get; set; }
    public string Provider { get; set; } = "Uploaded";
    public string? YouTubeVideoId { get; set; }
    public required string MimeType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? PosterStorageKey { get; set; }
    public required string Status { get; set; } = "Draft";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;
}

public sealed record ContentVideoView(
    Guid Id, string TitleAr, string TitleEn, string? DescriptionAr, string? DescriptionEn,
    string MimeType, long FileSizeBytes, string? PosterStorageKey, string Status,
    string Provider, [property: System.Text.Json.Serialization.JsonPropertyName("youtubeVideoId")] string? YouTubeVideoId,
    string? EmbedUrl, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc, long RowVersion);

public static class ContentVideoModelConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<ContentVideoAsset>(e =>
        {
            e.ToTable("content_videos", t =>
            {
                t.HasCheckConstraint("ck_content_videos_status", "status IN ('Draft','Published')");
                t.HasCheckConstraint("ck_content_videos_mime", "mime_type IN ('video/mp4','video/webm','video/youtube')");
                t.HasCheckConstraint("ck_content_videos_size", "file_size_bytes >= 0");
                t.HasCheckConstraint("ck_content_videos_provider", "provider IN ('Uploaded','YouTube')");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(x => x.TitleAr).HasColumnName("title_ar").HasMaxLength(200).IsRequired();
            e.Property(x => x.TitleEn).HasColumnName("title_en").HasMaxLength(200).IsRequired();
            e.Property(x => x.DescriptionAr).HasColumnName("description_ar").HasMaxLength(2000);
            e.Property(x => x.DescriptionEn).HasColumnName("description_en").HasMaxLength(2000);
            e.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(512).IsRequired();
            e.HasIndex(x => x.StorageKey).IsUnique().HasDatabaseName("ux_content_videos_storage_key");
            e.Property(x => x.Provider).HasColumnName("provider").HasMaxLength(16).HasDefaultValue("Uploaded").IsRequired();
            e.Property(x => x.YouTubeVideoId).HasColumnName("youtube_video_id").HasMaxLength(32);
            e.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(32).IsRequired();
            e.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes");
            e.Property(x => x.PosterStorageKey).HasColumnName("poster_storage_key").HasMaxLength(512);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone");
            e.Property(x => x.RowVersion).HasColumnName("row_version").HasDefaultValue(1L).IsConcurrencyToken();
            e.HasIndex(x => x.Status).HasDatabaseName("ix_content_videos_status");
        });
    }
}

public static class YouTubeUrl
{
    private static readonly string[] AllowedHosts = ["youtube.com", "www.youtube.com", "m.youtube.com", "youtu.be", "www.youtu.be", "youtube-nocookie.com", "www.youtube-nocookie.com"];

    public static string Normalize(string? raw)
    {
        if (!Uri.TryCreate(raw?.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            throw new CatalogValidationException("video", "Enter a valid https YouTube video URL.");
        var path = uri.AbsolutePath.Trim('/');
        string? candidate = null;
        if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            candidate = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }
        else if (path.Equals("watch", StringComparison.OrdinalIgnoreCase))
        {
            candidate = uri.Query.TrimStart('?').Split('&')
                .Select(part => part.Split('=', 2))
                .Where(pair => pair.Length == 2 && pair[0] == "v")
                .Select(pair => Uri.UnescapeDataString(pair[1]))
                .FirstOrDefault();
        }
        else
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2 && segments[0] is "embed" or "shorts" or "live" or "v")
                candidate = segments[1];
        }
        if (candidate is null || candidate.Length != 11 || candidate.Any(character => !IsAllowed(character)))
            throw new CatalogValidationException("video", "The URL does not contain a supported YouTube video ID.");
        return candidate;
    }

    private static bool IsAllowed(char character) =>
        char.IsAsciiLetterOrDigit(character) || character is '-' or '_';
}

public sealed class ContentVideoService(
    IDbContextFactory<CatalogDbContext> contextFactory,
    ICatalogMediaStorage storage,
    CatalogMediaOptions options,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ContentVideoView>> ListAsync(bool publishedOnly, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.ContentVideos.AsNoTracking();
        if (publishedOnly) query = query.Where(video => video.Status == "Published");
        return await query.OrderByDescending(video => video.UpdatedAtUtc)
            .Select(video => Map(video)).ToListAsync(cancellationToken);
    }

    public async Task<ContentVideoView> UploadAsync(
        string? titleAr, string? titleEn, string? descriptionAr, string? descriptionEn,
        Stream content, long length, string? fileName,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).TrimStart('.').ToLowerInvariant();
        var mime = extension switch
        {
            "mp4" => "video/mp4",
            "webm" => "video/webm",
            _ => throw new CatalogValidationException("video", "Only MP4 or WebM video is supported.")
        };
        if (length <= 0 || length > options.MaxVideoBytes)
            throw new CatalogValidationException("video", "Video size is not supported.");
        if (content.CanSeek)
        {
            var header = new byte[16];
            var read = await content.ReadAsync(header, cancellationToken);
            content.Position = 0;
            if (!HasValidSignature(header, read, extension))
                throw new CatalogValidationException("video", "The uploaded file is not a valid MP4 or WebM video.");
        }
        var id = Guid.CreateVersion7();
        var stored = await storage.SaveContentVideoAsync(id, content, length, extension, cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var video = new ContentVideoAsset
        {
            Id = id,
            TitleAr = Required(titleAr, 200, "Arabic title"),
            TitleEn = Required(titleEn, 200, "English title"),
            DescriptionAr = Optional(descriptionAr, 2000),
            DescriptionEn = Optional(descriptionEn, 2000),
            StorageKey = stored.StorageKey,
            MimeType = mime,
            FileSizeBytes = stored.FileSizeBytes,
            Status = "Draft",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.ContentVideos.Add(video);
        await db.SaveChangesAsync(cancellationToken);
        return Map(video);
    }

    public async Task<ContentVideoView> CreateYouTubeAsync(
        string? titleAr, string? titleEn, string? descriptionAr, string? descriptionEn, string? url,
        CancellationToken cancellationToken = default)
    {
        var videoId = YouTubeUrl.Normalize(url);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.ContentVideos.AnyAsync(item => item.Provider == "YouTube" && item.YouTubeVideoId == videoId, cancellationToken))
            throw new CatalogValidationException("video", "This YouTube video is already in the library.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var video = new ContentVideoAsset
        {
            Id = Guid.CreateVersion7(),
            TitleAr = Required(titleAr, 200, "Arabic title"),
            TitleEn = Required(titleEn, 200, "English title"),
            DescriptionAr = Optional(descriptionAr, 2000),
            DescriptionEn = Optional(descriptionEn, 2000),
            StorageKey = $"youtube:{videoId}",
            Provider = "YouTube",
            YouTubeVideoId = videoId,
            MimeType = "video/youtube",
            FileSizeBytes = 0,
            Status = "Draft",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.ContentVideos.Add(video);
        await db.SaveChangesAsync(cancellationToken);
        return Map(video);
    }

    public async Task<ContentVideoView> SetPublishedAsync(Guid id, bool published, long rowVersion, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var video = await db.ContentVideos.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new CatalogValidationException("video", "Video was not found.");
        if (video.RowVersion != rowVersion)
            throw new CatalogValidationException("video", "The video was changed by another user.");
        video.Status = published ? "Published" : "Draft";
        video.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        video.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Map(video);
    }

    public async Task<ContentVideoAsset> GetPublishedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ContentVideos.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.Status == "Published", cancellationToken)
            ?? throw new CatalogValidationException("video", "Video was not found.");
    }

    public Stream OpenRead(string storageKey) => storage.OpenRead(storageKey);

    private static bool HasValidSignature(byte[] header, int read, string extension) =>
        extension == "mp4"
            ? read >= 12 && header[4] == (byte)'f' && header[5] == (byte)'t' && header[6] == (byte)'y' && header[7] == (byte)'p'
            : read >= 4 && header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3;

    private static ContentVideoView Map(ContentVideoAsset video) => new(
        video.Id, video.TitleAr, video.TitleEn, video.DescriptionAr, video.DescriptionEn,
        video.MimeType, video.FileSizeBytes, video.PosterStorageKey, video.Status,
        video.Provider, video.YouTubeVideoId,
        video.YouTubeVideoId is null ? null : $"https://www.youtube-nocookie.com/embed/{video.YouTubeVideoId}",
        video.CreatedAtUtc, video.UpdatedAtUtc, video.RowVersion);

    private static string Required(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new CatalogValidationException("video", $"{field} is required and must not exceed {max} characters.");
        return value.Trim();
    }

    private static string? Optional(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length > max
            ? throw new CatalogValidationException("video", "A video field is too long.")
            : value.Trim();
}
