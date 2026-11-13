using HardwareStore.Modules.Catalog;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class YouTubeUrlTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    public void Accepts_known_youtube_video_urls(string url, string expected)
    {
        Assert.Equal(expected, YouTubeUrl.Normalize(url));
    }

    [Theory]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://example.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/playlist?list=PL123")]
    [InlineData("https://www.youtube.com/watch?v=short")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXc!")]
    [InlineData("")]
    public void Rejects_arbitrary_or_malformed_urls(string url)
    {
        Assert.Throws<CatalogValidationException>(() => YouTubeUrl.Normalize(url));
    }
}
