using HardwareStore.Modules.Catalog;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>The starter spec lists of the base categories and how an untouched first list is upgraded.</summary>
public sealed class BaseCategorySpecsTests
{
    [Fact]
    public void Every_starter_list_is_a_valid_schema_and_only_the_case_limits_the_card_length()
    {
        foreach (var (slug, list) in BaseCategorySpecs.BySlug)
        {
            var normalized = CategorySpecSchema.Normalize(list);
            Assert.Equal(list.Count, normalized.Count);
            Assert.True(list.Count <= CategorySpecSchema.MaxDefinitions, slug);
        }
        Assert.Contains(BaseCategorySpecs.BySlug["graphics-cards"], spec => spec.Key == "length" && !spec.IsCompatibility);
        Assert.Contains(BaseCategorySpecs.BySlug["cases"], spec => spec.Key == "max_length" && spec.IsCompatibility);
        var power = BaseCategorySpecs.BySlug["power-supplies"];
        Assert.Equal(["power_w", "efficiency", "form_factor", "modular", "fan_size", "pcie_connectors"], power.Select(spec => spec.Key));
    }

    [Fact]
    public void An_untouched_first_list_is_upgraded_keeping_the_owner_definitions_and_adding_the_new_fields()
    {
        var first = new List<SpecDefinition>
        {
            new("power_w", "القدرة", SpecType.Number, Unit: "W"),
            new("efficiency", "الشهادة المعتمدة", SpecType.Choice, ["80+", "80+ Gold"], IsFilter: true)
        };

        var upgraded = BaseCategorySpecs.Upgrade("power-supplies", first);

        Assert.NotNull(upgraded);
        Assert.Equal(["power_w", "efficiency", "form_factor", "modular", "fan_size", "pcie_connectors"], upgraded.Select(spec => spec.Key));
        Assert.Equal("الشهادة المعتمدة", upgraded.Single(spec => spec.Key == "efficiency").Name);
        Assert.Equal(["80+", "80+ Gold"], upgraded.Single(spec => spec.Key == "efficiency").Options);
        Assert.Null(BaseCategorySpecs.Upgrade("power-supplies", upgraded));
    }

    [Fact]
    public void A_first_name_still_unchanged_takes_the_formal_name()
    {
        var first = new List<SpecDefinition> { new("vram", "الذاكرة", SpecType.Number, Unit: "GB"), new("vram_type", "نوع الذاكرة", SpecType.Choice, ["GDDR6"]), new("power_w", "استهلاك الكهربا", SpecType.Number, Unit: "W") };

        var upgraded = BaseCategorySpecs.Upgrade("graphics-cards", first);

        Assert.Equal("استهلاك الطاقة", upgraded!.Single(spec => spec.Key == "power_w").Name);
    }

    [Fact]
    public void A_list_the_owner_changed_is_never_upgraded()
    {
        var removed = new List<SpecDefinition> { new("power_w", "القدرة", SpecType.Number, Unit: "W") };
        var added = new List<SpecDefinition>
        {
            new("power_w", "القدرة", SpecType.Number, Unit: "W"),
            new("efficiency", "الشهادة", SpecType.Text),
            new("color", "اللون", SpecType.Text)
        };
        var reordered = new List<SpecDefinition> { new("efficiency", "الشهادة", SpecType.Text), new("power_w", "القدرة", SpecType.Number) };

        Assert.Null(BaseCategorySpecs.Upgrade("power-supplies", removed));
        Assert.Null(BaseCategorySpecs.Upgrade("power-supplies", added));
        Assert.Null(BaseCategorySpecs.Upgrade("power-supplies", reordered));
        Assert.Null(BaseCategorySpecs.Upgrade("accessories", removed));
    }
}
