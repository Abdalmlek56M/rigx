namespace HardwareStore.Modules.Storefront;

/// <summary>Reads a YouTube video id from an https YouTube URL (watch, youtu.be, embed, shorts, live) or a bare id.</summary>
public static class YouTubeUrl
{
    private static readonly string[] AllowedHosts = ["youtube.com", "www.youtube.com", "m.youtube.com", "youtu.be", "www.youtu.be", "youtube-nocookie.com", "www.youtube-nocookie.com"];

    public static string ToVideoId(string? urlOrId)
    {
        var raw = urlOrId?.Trim() ?? string.Empty;
        if (IsVideoId(raw)) return raw;
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            throw new StorefrontException(400, "Enter a valid https YouTube video URL.");
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? candidate = null;
        if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
            candidate = segments.FirstOrDefault();
        else if (segments is ["watch"])
            candidate = uri.Query.TrimStart('?').Split('&')
                .Select(part => part.Split('=', 2))
                .Where(pair => pair.Length == 2 && pair[0] == "v")
                .Select(pair => Uri.UnescapeDataString(pair[1]))
                .FirstOrDefault();
        else if (segments.Length >= 2 && segments[0] is "embed" or "shorts" or "live" or "v")
            candidate = segments[1];
        return candidate is not null && IsVideoId(candidate)
            ? candidate
            : throw new StorefrontException(400, "The URL does not contain a supported YouTube video ID.");
    }

    /// <summary>Privacy-friendly embed address for the frontend.</summary>
    public static string EmbedUrl(string videoId) => $"https://www.youtube-nocookie.com/embed/{videoId}";

    private static bool IsVideoId(string value) =>
        value.Length == 11 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
