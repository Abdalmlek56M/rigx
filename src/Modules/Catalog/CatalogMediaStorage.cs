using SkiaSharp;

namespace HardwareStore.Modules.Catalog;

public sealed class CatalogMediaOptions
{
    public required string RootPath { get; init; }
    public int MaxUploadBytes { get; init; } = 12 * 1024 * 1024;
    public int MaxStoredBytes { get; init; } = 8 * 1024 * 1024;
    public int MaxVideoBytes { get; init; } = 256 * 1024 * 1024;
    public int MaxSourceDimension { get; init; } = 8192;
    public long MaxSourcePixels { get; init; } = 40_000_000;
    public int StoredMaxDimension { get; init; } = 2000;
    public int ThumbnailMaxDimension { get; init; } = 480;
    public int WebpQuality { get; init; } = 82;
    public TimeSpan OrphanGracePeriod { get; init; } = TimeSpan.FromMinutes(30);
}

public sealed record CatalogImageUpload(
    Stream Content,
    long Length,
    string? OriginalFileName,
    string? ContentType);

public sealed record ProcessedCatalogImage(
    byte[] Image,
    int Width,
    int Height,
    byte[] Thumbnail,
    int ThumbnailWidth,
    int ThumbnailHeight,
    byte[] Original);

public sealed record CatalogStoredMedia(
    string StorageKey,
    string ThumbnailStorageKey,
    string OriginalStorageKey,
    int Width,
    int Height,
    int ThumbnailWidth,
    int ThumbnailHeight,
    long FileSizeBytes);

public sealed record CatalogStoredVideo(string StorageKey, long FileSizeBytes);

public interface ICatalogImageProcessor
{
    Task<ProcessedCatalogImage> ProcessAsync(
        CatalogImageUpload upload,
        CancellationToken cancellationToken = default);
}

public interface ICatalogMediaStorage
{
    Task<CatalogStoredMedia> SaveImageAsync(
        Guid productId,
        Guid imageId,
        CatalogImageUpload image,
        CancellationToken cancellationToken = default);

    Task<string> SaveAssetImageAsync(
        string collection,
        Guid ownerId,
        Guid assetId,
        CatalogImageUpload image,
        CancellationToken cancellationToken = default);

    Task<CatalogStoredMedia> SaveContentImageAsync(
        Guid assetId,
        CatalogImageUpload image,
        bool replace,
        CancellationToken cancellationToken = default);

    Task<CatalogStoredVideo> SaveContentVideoAsync(
        Guid assetId,
        Stream content,
        long length,
        string extension,
        CancellationToken cancellationToken = default);

    Stream OpenRead(string storageKey);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoredMediaFile>> ListFilesAsync(CancellationToken cancellationToken = default);
    Task DeleteStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed record StoredMediaFile(string StorageKey, DateTimeOffset LastModifiedUtc, long SizeBytes);

public sealed class SkiaCatalogImageProcessor(CatalogMediaOptions options) : ICatalogImageProcessor
{
    public async Task<ProcessedCatalogImage> ProcessAsync(
        CatalogImageUpload upload,
        CancellationToken cancellationToken = default)
    {
        if (upload.Length <= 0 || upload.Length > options.MaxUploadBytes)
            throw new CatalogValidationException(
                "image", $"Image must be between 1 and {options.MaxUploadBytes} bytes.");

        var bytes = await ReadBoundedAsync(upload, cancellationToken);
        using var sourceStream = new MemoryStream(bytes, writable: false);
        SKCodec? codec;
        try
        {
            codec = SKCodec.Create(sourceStream);
        }
        catch
        {
            throw new CatalogValidationException("image", "The uploaded file is not a supported image.");
        }
        using var codecScope = codec;
        if (codec is null)
            throw new CatalogValidationException("image", "The uploaded file is not a supported image.");
        var sourceInfo = codec.Info;
        if (sourceInfo.Width <= 0 || sourceInfo.Height <= 0 ||
            sourceInfo.Width > options.MaxSourceDimension ||
            sourceInfo.Height > options.MaxSourceDimension ||
            (long)sourceInfo.Width * sourceInfo.Height > options.MaxSourcePixels)
        {
            throw new CatalogValidationException(
                "image", $"Image dimensions must not exceed {options.MaxSourceDimension}px per side or {options.MaxSourcePixels} pixels.");
        }

        SKBitmap? decoded;
        try
        {
            decoded = SKBitmap.Decode(bytes);
        }
        catch
        {
            throw new CatalogValidationException("image", "The uploaded image could not be decoded.");
        }
        if (decoded is null)
            throw new CatalogValidationException("image", "The uploaded image could not be decoded.");
        using var decodedScope = decoded;
        using var oriented = ApplyOrientation(decoded, codec.EncodedOrigin);
        using var full = ResizeToFit(oriented, options.StoredMaxDimension);
        using var thumbnail = ResizeToFit(oriented, options.ThumbnailMaxDimension);
        return new ProcessedCatalogImage(
            EncodeWebp(full, options.WebpQuality),
            full.Width,
            full.Height,
            EncodeWebp(thumbnail, options.WebpQuality),
            thumbnail.Width,
            thumbnail.Height,
            bytes);
    }

    private async Task<byte[]> ReadBoundedAsync(CatalogImageUpload upload, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream(Math.Min(options.MaxUploadBytes, (int)Math.Max(1, upload.Length)));
        var rented = new byte[64 * 1024];
        var remaining = options.MaxUploadBytes + 1L;
        try
        {
            int read;
            while ((read = await upload.Content.ReadAsync(rented, cancellationToken)) > 0)
            {
                remaining -= read;
                if (remaining < 0)
                    throw new CatalogValidationException("image", $"Image must not exceed {options.MaxUploadBytes} bytes.");
                await buffer.WriteAsync(rented.AsMemory(0, read), cancellationToken);
            }
        }
        finally
        {
            Array.Clear(rented);
        }
        return buffer.ToArray();
    }

    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        var swapsAxes = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = swapsAxes ? source.Height : source.Width;
        var height = swapsAxes ? source.Width : source.Height;
        var target = new SKBitmap(new SKImageInfo(
            width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Transparent);
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix
            {
                ScaleX = -1, ScaleY = 1, TransX = width, Persp2 = 1
            },
            SKEncodedOrigin.BottomRight => new SKMatrix
            {
                ScaleX = -1, ScaleY = -1, TransX = width, TransY = height, Persp2 = 1
            },
            SKEncodedOrigin.BottomLeft => new SKMatrix
            {
                ScaleX = 1, ScaleY = -1, TransY = height, Persp2 = 1
            },
            SKEncodedOrigin.LeftTop => new SKMatrix
            {
                ScaleX = 0, SkewX = 1, SkewY = 1, ScaleY = 0, Persp2 = 1
            },
            SKEncodedOrigin.RightTop => new SKMatrix
            {
                ScaleX = 0, SkewX = -1, TransX = height,
                SkewY = 1, ScaleY = 0, Persp2 = 1
            },
            SKEncodedOrigin.RightBottom => new SKMatrix
            {
                ScaleX = 0, SkewX = -1, TransX = height,
                SkewY = -1, ScaleY = 0, TransY = width, Persp2 = 1
            },
            SKEncodedOrigin.LeftBottom => new SKMatrix
            {
                ScaleX = 0, SkewX = 1,
                SkewY = -1, ScaleY = 0, TransY = width, Persp2 = 1
            },
            _ => SKMatrix.CreateIdentity()
        };
        canvas.SetMatrix(matrix);
        canvas.DrawBitmap(
            source,
            0,
            0,
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear),
            null);
        canvas.Flush();
        return target;
    }

    private static SKBitmap ResizeToFit(SKBitmap source, int maximum)
    {
        var longest = Math.Max(source.Width, source.Height);
        if (longest <= maximum)
            return source.Copy();
        var scale = maximum / (double)longest;
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        return source.Resize(
            new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new InvalidOperationException("Image resizing failed.");
    }

    private static byte[] EncodeWebp(SKBitmap bitmap, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Webp, quality)
            ?? throw new InvalidOperationException("WebP encoding failed.");
        return data.ToArray();
    }
}

public sealed class LocalCatalogMediaStorage(
    CatalogMediaOptions options,
    ICatalogImageProcessor imageProcessor) : ICatalogMediaStorage
{
    public async Task<CatalogStoredMedia> SaveImageAsync(
        Guid productId,
        Guid imageId,
        CatalogImageUpload image,
        CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty || imageId == Guid.Empty)
            throw new CatalogValidationException("image", "Product and image ids are required.");

        var processed = await imageProcessor.ProcessAsync(image, cancellationToken);
        if (processed.Image.LongLength > options.MaxStoredBytes ||
            processed.Thumbnail.LongLength > options.MaxStoredBytes)
            throw new CatalogValidationException("image", "Processed image exceeds the storage size limit.");

        var relativeDirectory = Path.Combine("products", productId.ToString("N"));
        var storageKey = $"{relativeDirectory.Replace('\\', '/')}/{imageId:N}.webp";
        var thumbnailKey = $"{relativeDirectory.Replace('\\', '/')}/{imageId:N}-thumb.webp";
        var originalKey = $"{relativeDirectory.Replace('\\', '/')}/{imageId:N}.original";
        var fullPath = ResolvePath(storageKey);
        var thumbnailPath = ResolvePath(thumbnailKey);
        var originalPath = ResolvePath(originalKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await WriteAtomicallyAsync(originalPath, processed.Original, cancellationToken);
        try
        {
            await WriteAtomicallyAsync(fullPath, processed.Image, cancellationToken);
            await WriteAtomicallyAsync(thumbnailPath, processed.Thumbnail, cancellationToken);
        }
        catch
        {
            await DeletePathAsync(fullPath);
            await DeletePathAsync(thumbnailPath);
            await DeletePathAsync(originalPath);
            throw;
        }

        return new CatalogStoredMedia(
            storageKey,
            thumbnailKey,
            originalKey,
            processed.Width,
            processed.Height,
            processed.ThumbnailWidth,
            processed.ThumbnailHeight,
            processed.Image.LongLength);
    }

    public async Task<string> SaveAssetImageAsync(
        string collection,
        Guid ownerId,
        Guid assetId,
        CatalogImageUpload image,
        CancellationToken cancellationToken = default)
    {
        if (collection is not ("categories" or "brands" or "avatars") ||
            ownerId == Guid.Empty || assetId == Guid.Empty)
            throw new CatalogValidationException("image", "The media owner is invalid.");
        var processed = await imageProcessor.ProcessAsync(image, cancellationToken);
        if (processed.Image.LongLength > options.MaxStoredBytes)
            throw new CatalogValidationException("image", "Processed image exceeds the storage size limit.");
        var storageKey = $"{collection}/{ownerId:N}/{assetId:N}.webp";
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await WriteAtomicallyAsync(path, processed.Image, cancellationToken);
        return storageKey;
    }

    public async Task<CatalogStoredMedia> SaveContentImageAsync(
        Guid assetId,
        CatalogImageUpload image,
        bool replace,
        CancellationToken cancellationToken = default)
    {
        if (assetId == Guid.Empty)
            throw new CatalogValidationException("image", "A media asset id is required.");
        var processed = await imageProcessor.ProcessAsync(image, cancellationToken);
        if (processed.Image.LongLength > options.MaxStoredBytes ||
            processed.Thumbnail.LongLength > options.MaxStoredBytes)
            throw new CatalogValidationException("image", "Processed image exceeds the storage size limit.");

        var directory = $"content/{assetId:N}";
        var storageKey = $"{directory}/{assetId:N}.webp";
        var thumbnailKey = $"{directory}/{assetId:N}-thumb.webp";
        var originalKey = $"{directory}/{assetId:N}.original";
        var fullPath = ResolvePath(storageKey);
        var thumbnailPath = ResolvePath(thumbnailKey);
        var originalPath = ResolvePath(originalKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await WriteAtomicallyAsync(originalPath, processed.Original, cancellationToken, replace);
        try
        {
            await WriteAtomicallyAsync(fullPath, processed.Image, cancellationToken, replace);
            await WriteAtomicallyAsync(thumbnailPath, processed.Thumbnail, cancellationToken, replace);
        }
        catch
        {
            if (!replace)
            {
                await DeletePathAsync(fullPath);
                await DeletePathAsync(thumbnailPath);
                await DeletePathAsync(originalPath);
            }
            throw;
        }

        return new CatalogStoredMedia(
            storageKey,
            thumbnailKey,
            originalKey,
            processed.Width,
            processed.Height,
            processed.ThumbnailWidth,
            processed.ThumbnailHeight,
            processed.Image.LongLength);
    }

    public async Task<CatalogStoredVideo> SaveContentVideoAsync(
        Guid assetId,
        Stream content,
        long length,
        string extension,
        CancellationToken cancellationToken = default)
    {
        if (assetId == Guid.Empty || length <= 0 || length > options.MaxVideoBytes)
            throw new CatalogValidationException("video", $"Video must be between 1 and {options.MaxVideoBytes} bytes.");
        if (extension is not ("mp4" or "webm"))
            throw new CatalogValidationException("video", "Only MP4 or WebM video is supported.");
        var storageKey = $"videos/{assetId:N}/{assetId:N}.{extension}";
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous))
            {
                var buffer = new byte[128 * 1024];
                long written = 0;
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    written += read;
                    if (written > options.MaxVideoBytes)
                        throw new CatalogValidationException("video", "Video exceeds the storage limit.");
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
                await output.FlushAsync(cancellationToken);
            }
            File.Move(temporary, path, overwrite: false);
            return new CatalogStoredVideo(storageKey, length);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public Stream OpenRead(string storageKey)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
            throw new FileNotFoundException("Catalog media was not found.", storageKey);
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, true);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return;

        await DeletePathAsync(ResolvePath(storageKey));
        await DeletePathAsync(ResolvePath(ThumbnailKeyFor(storageKey)));
        await DeletePathAsync(ResolvePath(OriginalKeyFor(storageKey)));
    }

    public Task<IReadOnlyList<StoredMediaFile>> ListFilesAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(options.RootPath))
            return Task.FromResult<IReadOnlyList<StoredMediaFile>>([]);

        var enumeration = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };
        var files = Directory.EnumerateFiles(options.RootPath, "*", enumeration)
            .Where(path => path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".original", StringComparison.OrdinalIgnoreCase))
            .Select(path =>
            {
                var key = Path.GetRelativePath(options.RootPath, path).Replace('\\', '/');
                var info = new FileInfo(path);
                return new StoredMediaFile(key, info.LastWriteTimeUtc, info.Length);
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<StoredMediaFile>>(files);
    }

    public async Task DeleteStorageKeyAsync(string storageKey, CancellationToken cancellationToken = default) =>
        await DeletePathAsync(ResolvePath(storageKey));

    private static string ThumbnailKeyFor(string storageKey) =>
        storageKey.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            ? storageKey[..^5] + "-thumb.webp"
            : throw new CatalogValidationException("storageKey", "Catalog media keys must end with .webp.");

    private static string OriginalKeyFor(string storageKey) =>
        storageKey.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            ? storageKey[..^5] + ".original"
            : throw new CatalogValidationException("storageKey", "Catalog media keys must end with .webp.");

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.StartsWith('/') ||
            storageKey.StartsWith('\\') ||
            storageKey.Contains('\\') ||
            storageKey.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new CatalogValidationException("storageKey", "The media storage key is invalid.");
        }

        var candidate = Path.GetFullPath(Path.Combine(
            options.RootPath,
            storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = options.RootPath.EndsWith(Path.DirectorySeparatorChar)
            ? options.RootPath
            : options.RootPath + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new CatalogValidationException("storageKey", "The media storage key is invalid.");
        return candidate;
    }

    private static async Task WriteAtomicallyAsync(
        string destination,
        byte[] content,
        CancellationToken cancellationToken,
        bool overwrite = false)
    {
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(
                             temporary,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             64 * 1024,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporary, destination, overwrite);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private static Task DeletePathAsync(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }
}
