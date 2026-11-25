using HardwareStore.Modules.Catalog;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class CategoryIconsTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    [InlineData("GPU", "gpu")]
    [InlineData(" cooling ", "cooling")]
    public void Normalize_keeps_a_ready_icon_or_none(string? key, string? expected) =>
        Assert.Equal(expected, CategoryIcons.Normalize(key));

    [Fact]
    public void Normalize_rejects_an_icon_outside_the_set()
    {
        var error = Assert.Throws<CatalogValidationException>(() => CategoryIcons.Normalize("rocket"));
        Assert.Contains("iconKey", error.Errors.Keys);
    }
}
