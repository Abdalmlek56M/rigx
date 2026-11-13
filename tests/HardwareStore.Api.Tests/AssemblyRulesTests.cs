using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class AssemblyRulesTests
{
    private static Assembly Build(string? label = null, int? months = null, string status = AssemblyStatus.Draft) =>
        new() { Code = "BLD-01", Name = "Build", Status = status, WarrantyLabel = label, WarrantyMonths = months };

    private static StockItem Item(string? label = null, int? months = null) =>
        new() { Code = "RX-0001", Condition = "New", ModelName = "Part", ModelKey = "part", WarrantyLabel = label, WarrantyMonths = months };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_usage_means_not_set(string? value) => Assert.Null(AssemblyUsage.Parse(value));

    [Theory]
    [InlineData("Gaming")]
    [InlineData("Editing")]
    [InlineData("StudyWork")]
    [InlineData("Streaming")]
    public void Known_usage_is_accepted_and_trimmed(string value) => Assert.Equal(value, AssemblyUsage.Parse($" {value} "));

    [Theory]
    [InlineData("gaming")]
    [InlineData("Cooking")]
    public void Unknown_usage_is_a_400(string value) =>
        Assert.Equal(400, Assert.Throws<RetailException>(() => AssemblyUsage.Parse(value)).StatusCode);

    [Theory]
    [InlineData(null, AssemblySellMode.Full)]
    [InlineData("", AssemblySellMode.Full)]
    [InlineData("Full", AssemblySellMode.Full)]
    [InlineData(" FullOrSplit ", AssemblySellMode.FullOrSplit)]
    public void Sell_mode_defaults_to_full(string? value, string expected) => Assert.Equal(expected, AssemblySellMode.Parse(value));

    [Fact]
    public void Unknown_sell_mode_is_a_400() =>
        Assert.Equal(400, Assert.Throws<RetailException>(() => AssemblySellMode.Parse("Split")).StatusCode);

    [Theory]
    [InlineData(AssemblyStatus.Sold)]
    [InlineData(AssemblyStatus.Disassembled)]
    public void Sold_and_disassembled_builds_are_not_editable(string status) =>
        Assert.Equal(409, Assert.Throws<RetailException>(() => AssemblyRules.EnsureEditable(Build(status: status))).StatusCode);

    [Theory]
    [InlineData(AssemblyStatus.Draft)]
    [InlineData(AssemblyStatus.Published)]
    public void Draft_and_published_builds_are_editable(string status) => AssemblyRules.EnsureEditable(Build(status: status));

    [Fact]
    public void Stale_row_version_is_a_409()
    {
        var build = Build();
        build.RowVersion = 5;
        AssemblyRules.EnsureVersion(build, 5);
        Assert.Equal(409, Assert.Throws<RetailException>(() => AssemblyRules.EnsureVersion(build, 4)).StatusCode);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(99, true)]
    [InlineData(0, false)]
    [InlineData(100, false)]
    [InlineData(-1, false)]
    public void Copies_must_be_between_one_and_the_maximum(int copies, bool valid)
    {
        if (valid) AssemblyRules.ValidateCopies(copies);
        else Assert.Equal(400, Assert.Throws<RetailException>(() => AssemblyRules.ValidateCopies(copies)).StatusCode);
    }

    [Fact]
    public void Suitable_for_text_is_trimmed_emptied_and_length_limited()
    {
        Assert.Null(AssemblyRules.CleanSuitableFor("  "));
        Assert.Null(AssemblyRules.CleanSuitableFor(null));
        Assert.Equal("ألعاب", AssemblyRules.CleanSuitableFor(" ألعاب "));
        var tooLong = new string('x', AssemblyRules.MaxSuitableForLength + 1);
        Assert.Equal(400, Assert.Throws<RetailException>(() => AssemblyRules.CleanSuitableFor(tooLong)).StatusCode);
    }

    [Fact]
    public void Part_price_cannot_be_negative()
    {
        AssemblyRules.ValidatePartPrice(0m);
        Assert.Equal(400, Assert.Throws<RetailException>(() => AssemblyRules.ValidatePartPrice(-0.01m)).StatusCode);
    }

    [Fact]
    public void Effective_warranty_prefers_part_then_build_then_item()
    {
        var item = Item("Item", 3);
        var plain = new AssemblyUnit();
        Assert.Equal(("Item", (int?)3), AssemblyRules.EffectiveWarranty(plain, Build(), item));
        Assert.Equal(("Build", (int?)12), AssemblyRules.EffectiveWarranty(plain, Build("Build", 12), item));
        var overridden = new AssemblyUnit { WarrantyLabel = "Part", WarrantyMonths = 6 };
        Assert.Equal(("Part", (int?)6), AssemblyRules.EffectiveWarranty(overridden, Build("Build", 12), item));
        Assert.Equal(((string?)null, (int?)null), AssemblyRules.EffectiveWarranty(plain, Build(), Item()));
    }
}
