using HardwareStore.Modules.Storefront;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class FavouriteRulesTests
{
    [Theory]
    [InlineData("rx-12", "RX-0012")]
    [InlineData("RX0012", "RX-0012")]
    [InlineData(" rx 142 ", "RX-0142")]
    [InlineData("RX-12345", "RX-12345")]
    public void A_typed_code_is_normalized(string text, string expected) => Assert.Equal(expected, FavouriteRules.Code(text));

    [Theory]
    [InlineData("BLD-01")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("RX-0")]
    public void Anything_that_is_not_a_stock_item_code_is_a_400(string? text) =>
        Assert.Equal(400, Assert.Throws<StorefrontException>(() => FavouriteRules.Code(text)).StatusCode);

    [Fact]
    public void New_codes_drop_invalid_duplicate_and_already_saved_codes()
    {
        var codes = FavouriteRules.NewCodes(["rx-1", "RX0001", "BLD-01", null, "", "RX-2", "rx 3", "RX-0002"], ["RX-0003"]);

        Assert.Equal(["RX-0001", "RX-0002"], codes);
    }

    [Fact]
    public void New_codes_stop_when_the_list_is_full()
    {
        var saved = Enumerable.Range(1, FavouriteRules.Max - 2).Select(number => $"RX-{number:0000}").ToList();

        var codes = FavouriteRules.NewCodes(["RX-9001", "RX-9002", "RX-9003"], saved);

        Assert.Equal(["RX-9001", "RX-9002"], codes);
        Assert.Empty(FavouriteRules.NewCodes(["RX-9001"], [.. saved, "RX-9001", "RX-9002"]));
    }
}
