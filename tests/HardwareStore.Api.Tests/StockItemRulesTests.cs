using HardwareStore.Common.Domain;
using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class StockItemRulesTests
{
    [Theory]
    [InlineData(PublicCode.StockItem, 1, "RX-0001")]
    [InlineData(PublicCode.Build, 1, "BLD-01")]
    [InlineData(PublicCode.Purchase, 1, "PO-0001")]
    [InlineData(PublicCode.StockItem, 12345, "RX-12345")]
    public void Public_codes_have_a_minimum_width_and_grow_when_needed(string prefix, long number, string expected) =>
        Assert.Equal(expected, PublicCode.Format(prefix, number));

    [Theory]
    [InlineData("rx-142", "RX-0142")]
    [InlineData("RX0142", "RX-0142")]
    [InlineData("rx 142", "RX-0142")]
    public void Typed_codes_are_normalized(string typed, string expected)
    {
        Assert.True(PublicCode.TryNormalize(typed, PublicCode.StockItem, out var code));
        Assert.Equal(expected, code);
    }

    [Theory]
    [InlineData("RTX 3060")]
    [InlineData("RX-")]
    [InlineData("RX-0")]
    [InlineData("")]
    public void Texts_that_are_not_codes_are_rejected(string typed) =>
        Assert.False(PublicCode.TryNormalize(typed, PublicCode.StockItem, out _));

    [Fact]
    public void Model_key_ignores_spacing_dashes_and_case()
    {
        var key = ModelName.Key("rx6600");
        Assert.Equal(key, ModelName.Key("RX 6600"));
        Assert.Equal(key, ModelName.Key("RX-6600"));
    }

    [Fact]
    public void A_used_line_becomes_one_item_per_piece_and_a_new_line_becomes_one_lot()
    {
        Assert.Equal([1, 1, 1], StockItemRules.UnitsPerItem(StockCondition.Used, 3));
        Assert.Equal([3], StockItemRules.UnitsPerItem(StockCondition.New, 3));
        Assert.Equal(400, Assert.Throws<RetailException>(() => StockItemRules.UnitsPerItem(StockCondition.New, 0)).StatusCode);
    }

    [Fact]
    public void Context_prices_add_the_optional_extras_to_the_base_price()
    {
        Assert.Equal(new StockItemPrices(1000m, 1100m, 1250m), StockItemRules.Prices(1000m, 100m, 250m));
        Assert.Equal(new StockItemPrices(1000m, 1000m, 1000m), StockItemRules.Prices(1000m, null, null));
    }

    [Theory]
    [InlineData(false, 3, true)]
    [InlineData(false, 2, false)]
    [InlineData(true, 3, false)]
    public void An_item_is_published_only_when_visible_with_three_photos(bool hidden, int photos, bool expected) =>
        Assert.Equal(expected, StockItemRules.IsPublished(hidden, photos));

    [Fact]
    public void Specs_drop_empty_values()
    {
        var specs = StockItemRules.CleanSpecs(new Dictionary<string, string> { ["VRAM"] = " 8GB ", ["Color"] = " ", [" "] = "x" });
        Assert.Equal("8GB", Assert.Single(specs).Value);
    }

    [Fact]
    public void Only_new_and_used_conditions_exist() =>
        Assert.Equal(400, Assert.Throws<RetailException>(() => StockCondition.Parse("Open Box")).StatusCode);

    [Theory]
    [InlineData("RTX 3060 12GB PNY", "rtx-3060-12gb-pny")]
    [InlineData("  Ryzen 5 5600X + Cooler ", "ryzen-5-5600x-cooler")]
    [InlineData("DDR4 2x8GB 3200MHz", "ddr4-2x8gb-3200mhz")]
    [InlineData("كيسة جيمنج", "item")]
    [InlineData("كيسة Lian Li O11", "lian-li-o11")]
    public void Slug_base_keeps_only_latin_letters_and_digits(string name, string expected) =>
        Assert.Equal(expected, StockItemSlug.Base(name));

    [Fact]
    public void Slug_base_is_capped_and_never_ends_with_a_hyphen()
    {
        var slug = StockItemSlug.Base(string.Join(' ', Enumerable.Repeat("abcdefghi", 20)));
        Assert.True(slug.Length <= 160);
        Assert.DoesNotContain(slug[^1], "-");
    }

    [Fact]
    public void Slug_collisions_get_the_first_free_number()
    {
        Assert.Equal("gpu", StockItemSlug.FirstFree("gpu", new HashSet<string>()));
        Assert.Equal("gpu-2", StockItemSlug.FirstFree("gpu", new HashSet<string> { "gpu" }));
        Assert.Equal("gpu-3", StockItemSlug.FirstFree("gpu", new HashSet<string> { "gpu", "gpu-2" }));
    }

    [Fact]
    public void Old_slugs_of_other_items_stay_reserved_but_an_item_may_take_back_its_own()
    {
        var item = Guid.NewGuid();
        var other = Guid.NewGuid();
        StockItemOldSlug Old(string slug, Guid owner) => new() { Slug = slug, StockItemId = owner };

        Assert.Equal("gpu-2", StockItemSlug.Choose("gpu", item, [], [Old("gpu", other)]));
        Assert.Equal("gpu-3", StockItemSlug.Choose("gpu", item, ["gpu-2"], [Old("gpu", other)]));
        Assert.Equal("gpu", StockItemSlug.Choose("gpu", item, [], [Old("gpu", item)]));
        Assert.Equal("gpu-2", StockItemSlug.Choose("gpu", item, ["gpu"], [Old("gpu", item)]));
    }

    [Theory]
    [InlineData("gpu", true)]
    [InlineData("gpu-3", true)]
    [InlineData("gpu-02", false)]
    [InlineData("gpu-1", false)]
    [InlineData("gpu-x", false)]
    [InlineData("cpu-3", false)]
    public void A_slug_belongs_to_its_base_or_a_numbered_copy(string slug, bool expected) =>
        Assert.Equal(expected, StockItemSlug.BelongsTo(slug, "gpu"));

    [Fact]
    public void A_store_description_is_optional_trimmed_and_limited()
    {
        Assert.Null(StockItemRules.CleanDescription("  ", "description"));
        Assert.Equal("one\ntwo", StockItemRules.CleanDescription(" one\r\ntwo ", "description"));
        var error = Assert.Throws<RetailException>(() =>
            StockItemRules.CleanDescription(new string('x', StockItemRules.MaxDescriptionLength + 1), "descriptionEn"));
        Assert.Equal(400, error.StatusCode);
    }
}
