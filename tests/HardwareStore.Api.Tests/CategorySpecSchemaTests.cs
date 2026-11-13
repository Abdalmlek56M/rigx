using HardwareStore.Modules.Catalog;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class CategorySpecSchemaTests
{
    private static SpecDefinition Number(string key) => new(key, key, SpecType.Number);
    private static SpecDefinition Choice(string key, params string[] options) => new(key, key, SpecType.Choice, options);

    private static IReadOnlyDictionary<string, string> Values(params (string Key, string Value)[] pairs) =>
        pairs.ToDictionary(pair => pair.Key, pair => pair.Value);

    [Theory]
    [InlineData("my key")]
    [InlineData("1abc")]
    [InlineData("")]
    public void Normalize_rejects_bad_keys(string key)
    {
        var bad = Number("x") with { Key = key };
        Assert.Throws<CatalogValidationException>(() => CategorySpecSchema.Normalize([bad]));
    }

    [Fact]
    public void Normalize_rejects_duplicate_keys_and_bad_options()
    {
        Assert.Throws<CatalogValidationException>(() => CategorySpecSchema.Normalize([Number("tdp"), Number("tdp")]));
        Assert.Throws<CatalogValidationException>(() => CategorySpecSchema.Normalize([new SpecDefinition("socket", "Socket", SpecType.Choice)]));
        Assert.Throws<CatalogValidationException>(() =>
            CategorySpecSchema.Normalize([new SpecDefinition("tdp", "TDP", SpecType.Number, ["65"])]));
    }

    [Fact]
    public void Effective_lets_a_child_override_a_parent_key_in_place_and_add_its_own()
    {
        var parent = new[] { Choice("socket", "AM4"), Number("tdp") };
        var child = new[] { Choice("socket", "AM4", "AM5"), Number("cores") };

        var effective = CategorySpecSchema.Effective([parent, child]);

        Assert.Equal(["socket", "tdp", "cores"], effective.Select(definition => definition.Key));
        Assert.Equal(["AM4", "AM5"], effective[0].Options);
    }

    [Fact]
    public void CheckValues_cleans_values_by_type()
    {
        var schema = new[] { Number("tdp"), new SpecDefinition("igpu", "iGPU", SpecType.YesNo), Choice("socket", "AM4", "LGA1700") };

        var clean = CategorySpecSchema.CheckValues(schema, Values(("tdp", "65"), ("igpu", "Yes"), ("socket", "am4"), ("other", " ")));

        Assert.Equal("65", clean["tdp"]);
        Assert.Equal("yes", clean["igpu"]);
        Assert.Equal("AM4", clean["socket"]);
        Assert.DoesNotContain("other", clean.Keys);
        Assert.Equal("yes", CategorySpecSchema.CheckValues(schema, Values(("igpu", "true")))["igpu"]);
    }

    [Fact]
    public void CheckValues_rejects_bad_values_and_unknown_keys()
    {
        var schema = new[] { Number("tdp"), Choice("socket", "AM4") };

        Assert.Throws<CatalogValidationException>(() => CategorySpecSchema.CheckValues(schema, Values(("tdp", "abc"))));
        Assert.Throws<CatalogValidationException>(() => CategorySpecSchema.CheckValues(schema, Values(("socket", "AM9"))));
        Assert.Throws<CatalogValidationException>(() => CategorySpecSchema.CheckValues(schema, Values(("color", "red"))));
    }

    [Fact]
    public void CheckValues_allows_any_key_when_the_schema_is_empty()
    {
        var clean = CategorySpecSchema.CheckValues([], Values(("anything", "goes")));

        Assert.Equal("goes", clean["anything"]);
    }

    [Fact]
    public void Category_code_is_upper_cased_and_checked()
    {
        Assert.Equal("CPU", CategorySpecSchema.NormalizeCategoryCode("cpu"));
        Assert.Null(CategorySpecSchema.NormalizeCategoryCode(" "));
        Assert.Throws<CatalogValidationException>(() => CategorySpecSchema.NormalizeCategoryCode("C"));
    }
}
