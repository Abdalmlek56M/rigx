using HardwareStore.Modules.Retail;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Leaving an item or a unit out of the stock value, and the limit on identical builds added at once.</summary>
public sealed class StockValueRuleTests
{
    private static StockItem Item(bool excluded) => new() { Code = "RX-0001", Condition = "New", ModelName = "Card", ModelKey = "card", ExcludedFromStockValue = excluded };

    private static InventoryUnit Unit(StockItem item, bool excluded) =>
        new() { Name = "Card", Condition = "New", Status = UnitStatus.Available, StockItem = item, ExcludedFromStockValue = excluded };

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void A_unit_counts_only_when_neither_it_nor_its_item_is_left_out(bool itemExcluded, bool unitExcluded, bool counts)
    {
        var item = Item(itemExcluded);
        var unit = Unit(item, unitExcluded);

        Assert.Equal(counts, StockValueRules.Counts(unit, item));
        Assert.Equal(counts, StockValueRules.Counted.Compile()(unit));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void One_to_nine_identical_builds_are_allowed(int count) => AssemblyRules.EnsureCopyCount(count);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10)]
    public void Any_other_count_of_identical_builds_is_rejected(int count) =>
        Assert.Equal(400, Assert.Throws<RetailException>(() => AssemblyRules.EnsureCopyCount(count)).StatusCode);
}
