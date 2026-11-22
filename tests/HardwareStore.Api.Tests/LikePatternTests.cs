using HardwareStore.Common.Domain;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class LikePatternTests
{
    [Theory]
    [InlineData("gpu", "%gpu%")]
    [InlineData("  gpu  ", "%gpu%")]
    [InlineData("100%", @"%100\%%")]
    [InlineData("a_b", @"%a\_b%")]
    [InlineData("a\\b", "%a\\\\b%")]
    [InlineData("%_\\", "%\\%\\_\\\\%")]
    public void Search_text_is_matched_literally(string text, string expected) => Assert.Equal(expected, LikePattern.Contains(text));

    [Fact]
    public void The_escape_character_is_the_backslash_the_patterns_use() => Assert.Equal(@"\", LikePattern.Escape);
}
