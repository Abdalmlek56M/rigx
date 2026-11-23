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

    private static BuilderPart Another(string slot, string code, int quantity = 1, params (string Key, string Value)[] specs) =>
        new(slot, code, quantity, specs.ToDictionary(spec => spec.Key, spec => spec.Value));

    private static void AssertBuilderStatus(int expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<PcBuilderException>(action).StatusCode);

    // ---------------------------------------------------------------- selection

    [Fact]
    public void A_selection_is_normalized_to_unique_rx_codes_with_quantity_one_to_six()
    {
        var parts = BuilderRules.NormalizeSelection([("rx 12", 1), ("RX-0034", 6)]);

        Assert.Equal([("RX-0012", 1), ("RX-0034", 6)], parts);
        Assert.Empty(BuilderRules.NormalizeSelection(null));
    }

    [Theory]
    [InlineData("BLD-01", 1)]
    [InlineData("nonsense", 1)]
    [InlineData(null, 1)]
    [InlineData("RX-0001", 0)]
    [InlineData("RX-0001", 7)]
    public void A_bad_code_or_quantity_is_a_bad_request(string? code, int quantity) =>
        AssertBuilderStatus(400, () => BuilderRules.NormalizeSelection([(code, quantity)]));

    [Fact]
    public void A_part_listed_twice_or_more_parts_than_the_slots_take_together_is_a_bad_request()
    {
        AssertBuilderStatus(400, () => BuilderRules.NormalizeSelection([("RX-0001", 1), ("rx1", 2)]));
        Assert.Equal(11, BuilderSlot.MaxParts);
        var tooMany = Enumerable.Range(1, 12).Select(number => ((string?)$"RX-{number:0000}", 1)).ToList();
        AssertBuilderStatus(400, () => BuilderRules.NormalizeSelection(tooMany));
        Assert.Equal(11, BuilderRules.NormalizeSelection(tooMany.Take(11).ToList()).Count);
    }

    [Fact]
    public void Parts_come_back_in_slot_order_and_each_part_needs_a_builder_slot()
    {
        var ordered = BuilderRules.InSlotOrder([Part(BuilderSlot.Psu), Part(BuilderSlot.Board), Part(BuilderSlot.Cpu)]);

        Assert.Equal([BuilderSlot.Cpu, BuilderSlot.Board, BuilderSlot.Psu], ordered.Select(part => part.Slot));
        AssertBuilderStatus(400, () => BuilderRules.InSlotOrder([Part(string.Empty)]));
    }

    [Fact]
    public void A_slot_with_one_model_refuses_a_second_model_and_a_quantity_over_four_while_memory_takes_up_to_four_sticks()
    {
        AssertBuilderStatus(400, () => BuilderRules.InSlotOrder([Part(BuilderSlot.Cpu), Another(BuilderSlot.Cpu, "RX-0099")]));
        AssertBuilderStatus(400, () => BuilderRules.InSlotOrder([Part(BuilderSlot.Ram, 5)]));
        Assert.Equal(4, BuilderRules.InSlotOrder([Part(BuilderSlot.Ram, 4)])[0].Quantity);
    }

    [Fact]
    public void Storage_takes_up_to_three_devices_in_all_and_fans_up_to_six_of_one_model()
    {
        var drives = new[] { Another(BuilderSlot.Storage, "RX-0030"), Another(BuilderSlot.Storage, "RX-0010"), Another(BuilderSlot.Storage, "RX-0020") };

        var ordered = BuilderRules.InSlotOrder(drives);
        Assert.Equal(["RX-0010", "RX-0020", "RX-0030"], ordered.Select(part => part.Code));
        Assert.Equal(3, BuilderRules.InSlotOrder([Another(BuilderSlot.Storage, "RX-0010", 2), Another(BuilderSlot.Storage, "RX-0020")]).Sum(part => part.Quantity));
        AssertBuilderStatus(400, () => BuilderRules.InSlotOrder([.. drives, Another(BuilderSlot.Storage, "RX-0040")]));
        AssertBuilderStatus(400, () => BuilderRules.InSlotOrder([Another(BuilderSlot.Storage, "RX-0010", 2), Another(BuilderSlot.Storage, "RX-0020", 2)]));
        AssertBuilderStatus(400, () => BuilderRules.InSlotOrder([Another(BuilderSlot.Storage, "RX-0010", 4)]));
        Assert.Equal(6, BuilderRules.InSlotOrder([Part(BuilderSlot.Fan, 6)])[0].Quantity);
        AssertBuilderStatus(400, () => BuilderRules.InSlotOrder([Part(BuilderSlot.Fan, 1), Another(BuilderSlot.Fan, "RX-0099")]));
    }

    [Fact]
    public void One_more_unit_fits_a_slot_until_its_pieces_or_its_models_run_out()
    {
        Assert.True(BuilderRules.Fits(BuilderSlot.Storage, [], "RX-0001"));
        Assert.True(BuilderRules.Fits(BuilderSlot.Storage, [("RX-0001", 1), ("RX-0002", 1)], "RX-0003"));
        Assert.False(BuilderRules.Fits(BuilderSlot.Storage, [("RX-0001", 2), ("RX-0002", 1)], "RX-0001"));
        Assert.True(BuilderRules.Fits(BuilderSlot.Fan, [("RX-0001", 5)], "RX-0001"));
        Assert.False(BuilderRules.Fits(BuilderSlot.Fan, [("RX-0001", 6)], "RX-0001"));
        Assert.False(BuilderRules.Fits(BuilderSlot.Fan, [("RX-0001", 2)], "RX-0002"));
        Assert.False(BuilderRules.Fits(BuilderSlot.Cpu, [("RX-0001", 1)], "RX-0002"));
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

    [Fact]
    public void Two_parts_of_one_slot_are_never_compared_with_each_other_but_each_is_checked_against_the_others()
    {
        var keys = new HashSet<string> { "interface", "socket" };
        var drives = BuilderRules.Evaluate(
            [Another(BuilderSlot.Storage, "RX-0010", 1, ("interface", "SATA")), Another(BuilderSlot.Storage, "RX-0020", 1, ("interface", "NVMe"))], keys);
        var againstBoard = BuilderRules.Evaluate(
            [Part(BuilderSlot.Board, 1, ("interface", "NVMe")), Another(BuilderSlot.Storage, "RX-0010", 1, ("interface", "NVMe")),
             Another(BuilderSlot.Storage, "RX-0020", 1, ("interface", "SATA"))], keys);

        Assert.Empty(drives.Issues);
        var issue = Assert.Single(againstBoard.Issues);
        Assert.Equal((BuilderIssueCode.SpecMismatch, BuilderSlot.Storage, BuilderSlot.Board), (issue.Code, issue.Slot, issue.OtherSlot));
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
    public void Every_drive_and_every_fan_adds_its_watts_times_its_quantity()
    {
        var check = BuilderRules.Evaluate(
        [
            Part(BuilderSlot.Cpu, 1, ("power_w", "100")),
            Another(BuilderSlot.Storage, "RX-0010", 2, ("power_w", "5")),
            Another(BuilderSlot.Storage, "RX-0020", 1, ("power_w", "8")),
            Part(BuilderSlot.Fan, 4, ("power_w", "3")),
            Part(BuilderSlot.Psu, 1, ("power_w", "200"))
        ], Socket);

        Assert.Equal((130m, 200m), (check.PowerEstimateWatts, check.PsuCapacityWatts));
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
