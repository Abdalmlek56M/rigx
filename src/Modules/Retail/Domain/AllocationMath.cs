namespace HardwareStore.Modules.Retail;

/// <summary>
/// Money/percent split rules shared by assemblies, stock claims and partner earnings.
/// Every split rounds to 2 decimals and gives the rounding remainder to the last row, so parts always add up to the total.
/// </summary>
internal static class AllocationMath
{
    /// <summary>True when percentages add up to 100 (within the 0.01 rounding tolerance).</summary>
    public static bool SumsToHundred(IEnumerable<decimal> percents) => Math.Abs(percents.Sum() - 100m) <= 0.01m;

    public static decimal Net(decimal revenue, decimal discount, decimal expense, decimal cost) =>
        decimal.Round(revenue - discount - expense - cost, 2);

    public static List<decimal> DefaultAllocations(IReadOnlyList<decimal> prices)
    {
        if (prices.Count == 0) return [];
        var total = prices.Sum();
        if (total <= 0) return EqualPercentages(prices.Count);
        var result = new List<decimal>();
        var assigned = 0m;
        for (var index = 0; index < prices.Count - 1; index++)
        {
            var percent = decimal.Round(prices[index] / total * 100m, 2);
            result.Add(percent); assigned += percent;
        }
        result.Add(decimal.Round(100m - assigned, 2));
        return result;
    }

    public static List<decimal> EqualPercentages(int count)
    {
        if (count <= 0) return [];
        var result = new List<decimal>();
        var assigned = 0m;
        for (var index = 0; index < count - 1; index++)
        {
            var percent = decimal.Round(100m / count, 2);
            result.Add(percent); assigned += percent;
        }
        result.Add(decimal.Round(100m - assigned, 2));
        return result;
    }

    public static void Rebalance(IReadOnlyList<AssemblyUnit> units)
    {
        if (units.Count == 0) return;
        var weights = units.Select(unit => unit.AllocationPercent).ToList();
        var total = weights.Sum();
        if (total <= 0) { weights = Enumerable.Repeat(1m, units.Count).ToList(); total = units.Count; }
        var assigned = 0m;
        for (var index = 0; index < units.Count; index++)
        {
            var percent = index == units.Count - 1
                ? decimal.Round(100m - assigned, 2)
                : decimal.Round(weights[index] / total * 100m, 2);
            units[index].AllocationPercent = percent;
            assigned += percent;
        }
    }

    public static List<decimal> AllocateAmount(decimal total, IReadOnlyList<decimal> percents)
    {
        var result = new List<decimal>();
        var assigned = 0m;
        for (var index = 0; index < percents.Count; index++)
        {
            if (index == percents.Count - 1)
            {
                result.Add(decimal.Round(total - assigned, 2));
                break;
            }
            var amount = decimal.Round(total * percents[index] / 100m, 2);
            result.Add(amount); assigned += amount;
        }
        return result;
    }
}
