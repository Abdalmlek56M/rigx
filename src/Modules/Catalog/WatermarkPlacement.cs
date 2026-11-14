namespace HardwareStore.Modules.Catalog;

public readonly record struct WatermarkRect(int X, int Y, int Width, int Height);

/// <summary>Where the watermark logo goes on an image of a given size: pure math, no imaging library.</summary>
public static class WatermarkPlacement
{
    private const double MarginShare = 0.03;

    /// <summary>Logo width is <paramref name="sizePercent"/> of the image width (aspect kept, never taller than the image); the margin is 3% of the shorter side.</summary>
    public static WatermarkRect Compute(
        int imageWidth, int imageHeight, int logoWidth, int logoHeight, string position, int sizePercent)
    {
        if (imageWidth <= 0 || imageHeight <= 0 || logoWidth <= 0 || logoHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(imageWidth), "Image and logo sizes must be positive.");
        var width = Math.Max(1, (int)Math.Round(imageWidth * sizePercent / 100.0));
        var height = Math.Max(1, (int)Math.Round(width * (double)logoHeight / logoWidth));
        if (height > imageHeight)
        {
            width = Math.Max(1, (int)Math.Round(width * (double)imageHeight / height));
            height = imageHeight;
        }
        var margin = (int)Math.Round(Math.Min(imageWidth, imageHeight) * MarginShare);
        var right = Math.Max(0, imageWidth - width - margin);
        var bottom = Math.Max(0, imageHeight - height - margin);
        return position switch
        {
            "TopLeft" => new WatermarkRect(margin, margin, width, height),
            "TopRight" => new WatermarkRect(right, margin, width, height),
            "BottomLeft" => new WatermarkRect(margin, bottom, width, height),
            "BottomRight" => new WatermarkRect(right, bottom, width, height),
            "Center" => new WatermarkRect((imageWidth - width) / 2, (imageHeight - height) / 2, width, height),
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, "Unknown watermark position.")
        };
    }
}

/// <summary>Storage key areas that are not part of the public catalog media.</summary>
public static class CatalogMediaKeys
{
    public const string PrivatePrefix = "private/";
    public const string SettingsPrefix = "settings/";

    public static bool IsPrivate(string storageKey) => storageKey.StartsWith(PrivatePrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>The key of the untouched upload kept next to a stored image (<c>x.webp</c> becomes <c>x.original</c>).</summary>
    public static string OriginalKeyFor(string storageKey) =>
        storageKey.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            ? storageKey[..^5] + ".original"
            : throw new CatalogValidationException("storageKey", "Catalog media keys must end with .webp.");
}
