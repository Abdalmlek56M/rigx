using System.Globalization;
using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class BuilderRulesTests
{
    private static readonly IReadOnlySet<string> Socket = new HashSet<string> { "socket" };

    private static BuilderPart Part(string slot, int quantity = 1, params (string Key, string Value)[] specs) =>
        new(slot, $"RX-{slot}", quantity, specs.ToDictionary(spec => spec.Key, spec => spec.Value));

    private static void AssertBuilderStatus(int expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<PcBuilderException>(action).StatusCode);

    // ---------------------------------------------------------------- selection

    [Fact]
    public void A_selection_is_normalized_to_unique_rx_codes_with_quantity_one_to_four()
    {
        var parts = BuilderRules.NormalizeSelection([("rx 12", 1), ("RX-0034", 4)]);

        Assert.Equal([("RX-0012", 1), ("RX-0034", 4)], parts);
        Assert.Empty(BuilderRules.NormalizeSelection(null));
    }

    [Theory]
    [InlineData("BLD-01", 1)]
    [InlineData("nonsense", 1)]
    [InlineData(null, 1)]
    [InlineData("RX-0001", 0)]
    [InlineData("RX-0001", 5)]
    public void A_bad_code_or_quantity_is_a_bad_request(string? code, int quantity) =>
        AssertBuilderStatus(400, () => BuilderRules.NormalizeSelection([(code, quantity)]));

    [Fact]
    public void A_part_listed_twice_or_more_than_eight_parts_is_a_bad_request()
    {
        AssertBuilderStatus(400, () => BuilderRules.NormalizeSelection([("RX-0001", 1), ("rx1", 2)]));
        var nine = Enumerable.Range(1, 9).Select(number => ((string?)$"RX-{number:0000}", 1)).ToList();
        AssertBuilderStatus(400, () => BuilderRules.NormalizeSelection(nine));
        Assert.Equal(8, BuilderRules.NormalizeSelection(nine.Take(8).ToList()).Count);
    }

    [Fact]
    public void Parts_come_back_in_slot_order_and_each_needs_exactly_one_slot()
    {
        var ordered = BuilderRules.InSlotOrder([Part(BuilderSlot.Psu), Part(BuilderSlot.Board), Part(BuilderSlot.Cpu)]);

        Assert.Equal([BuilderSlot.Cpu, BuilderSlot.Board, BuilderSlot.Psu], ordered.Select(part => part.Slot));
        AssertBuilderStatus(400, () => BuilderRules.InSlotOrder([Part(string.Empty)]));
        AssertBuilderStatus(400, () => BuilderRules.InSlotOrder(
            [Part(BuilderSlot.Cpu), new BuilderPart(BuilderSlot.Cpu, "RX-0099", 1, new Dictionary<string, string>())]));
    }

    [Fact]
    public void The_in_build_price_needs_a_processor_and_a_motherboard()
    {
        Assert.True(BuilderRules.UsesBuildPrice([BuilderSlot.Board, BuilderSlot.Cpu, BuilderSlot.Ram]));
        Assert.False(BuilderRules.UsesBuildPrice([BuilderSlot.Cpu]));
        Assert.False(BuilderRules.UsesBuildPrice([BuilderSlot.Board, BuilderSlot.Gpu]));
        Assert.False(BuilderRules.UsesBuildPrice([]));
    }

    // ---------------------------------------------------------------- compatibility

    [Fact]
    public void Different_values_of_a_compatibility_spec_are_a_conflict_on_the_second_part()
    {
        var check = BuilderRules.Evaluate([Part(BuilderSlot.Cpu, 1, ("socket", "AM4")), Part(BuilderSlot.Board, 1, ("socket", "AM5"))], Socket);

        var issue = Assert.Single(check.Issues);
        Assert.Equal((BuilderIssueLevel.Conflict, BuilderIssueCode.SpecMismatch, BuilderSlot.Board, BuilderSlot.Cpu, "socket"),
            (issue.Level, issue.Code, issue.Slot, issue.OtherSlot, issue.SpecKey));
        Assert.True(check.HasConflicts);
    }

    [Fact]
    public void Value_lists_that_overlap_are_compatible_and_missing_values_are_never_a_conflict()
    {
        var overlap = BuilderRules.Evaluate([Part(BuilderSlot.Cpu, 1, ("socket", "AM4, AM5")), Part(BuilderSlot.Board, 1, ("socket", "am5"))], Socket);
        var missing = BuilderRules.Evaluate([Part(BuilderSlot.Cpu, 1, ("socket", "AM4")), Part(BuilderSlot.Board)], Socket);
        var notCompatibilityKey = BuilderRules.Evaluate(
            [Part(BuilderSlot.Cpu, 1, ("color", "red")), Part(BuilderSlot.Board, 1, ("color", "blue"))], Socket);

        Assert.Empty(overlap.Issues);
        Assert.Empty(missing.Issues);
        Assert.Empty(notCompatibilityKey.Issues);
    }

    [Fact]
    public void A_max_limit_on_one_part_caps_the_number_on_another()
    {
        var keys = new HashSet<string> { "max_gpu_length_mm" };
        var over = BuilderRules.Evaluate(
            [Part(BuilderSlot.Gpu, 1, ("gpu_length_mm", "320")), Part(BuilderSlot.Case, 1, ("max_gpu_length_mm", "300"))], keys);
        var fits = BuilderRules.Evaluate(
            [Part(BuilderSlot.Gpu, 1, ("gpu_length_mm", "300")), Part(BuilderSlot.Case, 1, ("max_gpu_length_mm", "300"))], keys);

        var issue = Assert.Single(over.Issues);
        Assert.Equal((BuilderIssueCode.OverLimit, BuilderSlot.Gpu, BuilderSlot.Case, "gpu_length_mm"),
            (issue.Code, issue.Slot, issue.OtherSlot, issue.SpecKey));
        Assert.Empty(fits.Issues);
    }

    // ---------------------------------------------------------------- power

    private static BuilderCheck Power(decimal psuWatts) => BuilderRules.Evaluate(
    [
        Part(BuilderSlot.Cpu, 1, ("power_w", "100")),
        Part(BuilderSlot.Ram, 2, ("power_w", "10")),
        Part(BuilderSlot.Gpu, 1, ("power_w", "200")),
        Part(BuilderSlot.Psu, 1, ("power_w", psuWatts.ToString(CultureInfo.InvariantCulture)))
    ], Socket);

    [Fact]
    public void The_power_estimate_sums_watts_times_quantity_without_the_power_supply()
    {
        var check = Power(400m);

        Assert.Equal((320m, 400m), (check.PowerEstimateWatts, check.PsuCapacityWatts));
    }

    [Fact]
    public void Power_over_the_supply_is_a_conflict_over_eighty_percent_a_warning_and_exactly_eighty_percent_nothing()
    {
        var conflict = Power(300m);
        var warning = Power(380m);
        var exact = Power(400m);

        var over = Assert.Single(conflict.Issues);
        Assert.Equal((BuilderIssueLevel.Conflict, BuilderIssueCode.PowerOverCapacity, BuilderSlot.Psu), (over.Level, over.Code, over.Slot));
        Assert.True(conflict.HasConflicts);
        var near = Assert.Single(warning.Issues);
        Assert.Equal((BuilderIssueLevel.Warning, BuilderIssueCode.PowerNearCapacity), (near.Level, near.Code));
        Assert.False(warning.HasConflicts);
        Assert.Empty(exact.Issues);
    }

    [Fact]
    public void Without_a_power_supply_there_is_an_estimate_but_no_power_issue()
    {
        var check = BuilderRules.Evaluate([Part(BuilderSlot.Cpu, 1, ("power_w", "100"))], Socket);

        Assert.Equal((100m, (decimal?)null), (check.PowerEstimateWatts, check.PsuCapacityWatts));
        Assert.Empty(check.Issues);
    }

    // ---------------------------------------------------------------- online build price (Retail)

    [Fact]
    public void The_online_in_build_price_needs_a_processor_and_a_motherboard_category()
    {
        Guid cpu = Guid.NewGuid(), board = Guid.NewGuid(), ram = Guid.NewGuid();
        var rule = new BuildPriceRule(new HashSet<Guid> { cpu }, new HashSet<Guid> { board });

        OrderRules.EnsureBuildPrice([cpu, board, ram, null], rule);
        Assert.Equal(400, Assert.Throws<RetailException>(() => OrderRules.EnsureBuildPrice([cpu, ram], rule)).StatusCode);
        Assert.Equal(400, Assert.Throws<RetailException>(() => OrderRules.EnsureBuildPrice([board], rule)).StatusCode);
        Assert.Equal(400, Assert.Throws<RetailException>(() => OrderRules.EnsureBuildPrice([cpu, board], null)).StatusCode);
    }
}
