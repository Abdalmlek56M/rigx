using System.Globalization;
using HardwareStore.Common.Domain;

namespace HardwareStore.Modules.PcBuilder;

public sealed class PcBuilderException(int statusCode, string message) : AppException(statusCode, message);

/// <summary>
/// The builder's slots. A slot's options are the stock items in the builder categories whose code is the slot
/// (category flag "show in builder" + code, business §3), including their sub-categories.
/// </summary>
public static class BuilderSlot
{
    public const string Cpu = "CPU";
    public const string Board = "BOARD";
    public const string Ram = "RAM";
    public const string Gpu = "GPU";
    public const string Storage = "SSD";
    public const string Psu = "PSU";
    public const string Case = "CASE";
    public const string Cooler = "COOL";
    /// <summary>Case fans (front, top, rear). The slot exists only when the owner makes a "show in builder" category with this code.</summary>
    public const string Fan = "FAN";

    /// <summary>Display order of the slots.</summary>
    public static readonly IReadOnlyList<string> All = [Cpu, Board, Ram, Gpu, Storage, Psu, Case, Cooler, Fan];

    /// <summary>Most parts a build can have: the sum of every slot's part limit.</summary>
    public static int MaxParts => All.Sum(slot => LimitOf(slot).MaxParts);

    /// <summary>
    /// What a slot takes (owner decision, 23 Nov 2026): storage up to three devices, fans one model in a quantity up to six, every other slot
    /// one model in a quantity up to four (memory sticks).
    /// </summary>
    public static SlotLimit LimitOf(string slot) => slot switch
    {
        Storage => new SlotLimit(3, 3),
        Fan => new SlotLimit(1, 6),
        _ => new SlotLimit(1, 4)
    };

    public static string Parse(string? value) =>
        All.FirstOrDefault(slot => string.Equals(slot, value?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new PcBuilderException(400, "Unknown builder slot.");
}

/// <summary><paramref name="MaxParts"/> different models and <paramref name="MaxUnits"/> pieces in all (the quantities added up).</summary>
public sealed record SlotLimit(int MaxParts, int MaxUnits);

/// <summary>Spec keys the builder reads by convention (defined per category in the spec schema).</summary>
public static class BuilderSpecKey
{
    /// <summary>Power draw of a part in watts; on the power supply it is its output.</summary>
    public const string PowerWatts = "power_w";

    /// <summary>A compatibility number <c>max_x</c> on one part limits the number <c>x</c> on the others (e.g. GPU length).</summary>
    public const string MaxPrefix = "max_";
}

public static class BuilderIssueLevel
{
    /// <summary>The build cannot be ordered as it is.</summary>
    public const string Conflict = "Conflict";
    public const string Warning = "Warning";
}

public static class BuilderIssueCode
{
    /// <summary>Two parts give different values for the same compatibility spec (socket, memory type…).</summary>
    public const string SpecMismatch = "SpecMismatch";
    /// <summary>A part's number is over another part's <c>max_</c> limit.</summary>
    public const string OverLimit = "OverLimit";
    public const string PowerOverCapacity = "PowerOverCapacity";
    public const string PowerNearCapacity = "PowerNearCapacity";
}

/// <summary>A selected part with its slot and its spec values (as stored on the stock item).</summary>
public sealed record BuilderPart(string Slot, string Code, int Quantity, IReadOnlyDictionary<string, string> Specs);

/// <summary><paramref name="Slot"/> is the part with the problem; <paramref name="OtherSlot"/> the part it clashes with.</summary>
public sealed record BuilderIssue(string Level, string Code, string Slot, string? OtherSlot, string? SpecKey);

public sealed record BuilderCheck(IReadOnlyList<BuilderIssue> Issues, decimal? PowerEstimateWatts, decimal? PsuCapacityWatts)
{
    public bool HasConflicts => Issues.Any(issue => issue.Level == BuilderIssueLevel.Conflict);
}

/// <summary>Builder rules (roadmap 2.13): selection limits, compatibility from category specs, power estimate, build price.</summary>
public static class BuilderRules
{
    /// <summary>The largest quantity any part can carry (fans); a slot's own limit (<see cref="BuilderSlot.LimitOf"/>) is checked once the slots are known.</summary>
    public const int MaxQuantity = 6;
    /// <summary>Above this share of the power supply's output the build gets a warning; above 100 % it is a conflict.</summary>
    public const decimal PowerWarningShare = 0.8m;

    private static readonly char[] ListSeparators = [',', '/', '|'];

    /// <summary>Each RX code once, quantity 1–6, no more parts than the slots take together. The slot comes from the item's category.</summary>
    public static IReadOnlyList<(string Code, int Quantity)> NormalizeSelection(IReadOnlyList<(string? Code, int Quantity)>? selection)
    {
        var parts = new List<(string Code, int Quantity)>();
        foreach (var (rawCode, quantity) in selection ?? [])
        {
            if (!PublicCode.TryNormalize(rawCode, PublicCode.StockItem, out var code))
                throw new PcBuilderException(400, "Each part needs a valid RX code.");
            if (quantity is < 1 or > MaxQuantity) throw new PcBuilderException(400, $"Part quantity must be between 1 and {MaxQuantity}.");
            if (parts.Any(part => part.Code == code)) throw new PcBuilderException(400, "A part is listed twice.");
            parts.Add((code, quantity));
        }
        if (parts.Count > BuilderSlot.MaxParts) throw new PcBuilderException(400, $"A build has at most {BuilderSlot.MaxParts} parts.");
        return parts;
    }

    /// <summary>Whether one more unit of <paramref name="code"/> fits a slot that already holds <paramref name="held"/> (codes with quantities).</summary>
    public static bool Fits(string slot, IReadOnlyList<(string Code, int Quantity)> held, string code)
    {
        var limit = BuilderSlot.LimitOf(slot);
        return held.Sum(part => part.Quantity) < limit.MaxUnits && (held.Count < limit.MaxParts || held.Any(part => part.Code == code));
    }

    /// <summary>
    /// Every part must belong to a builder slot and each slot keeps to its limit (<see cref="BuilderSlot.LimitOf"/>: models and pieces);
    /// returns the parts in slot order, the parts of one slot by code so the same build is always the same list.
    /// </summary>
    public static IReadOnlyList<BuilderPart> InSlotOrder(IReadOnlyList<BuilderPart> parts)
    {
        if (parts.Any(part => !BuilderSlot.All.Contains(part.Slot)))
            throw new PcBuilderException(400, "A part is not in a builder category.");
        foreach (var group in parts.GroupBy(part => part.Slot))
        {
            var limit = BuilderSlot.LimitOf(group.Key);
            if (group.Count() > limit.MaxParts) throw new PcBuilderException(400, $"This slot takes at most {limit.MaxParts} different parts.");
            if (group.Sum(part => part.Quantity) > limit.MaxUnits) throw new PcBuilderException(400, $"This slot takes at most {limit.MaxUnits} pieces.");
        }
        return parts.OrderBy(part => IndexOf(part.Slot)).ThenBy(part => part.Code, StringComparer.Ordinal).ToList();
    }

    /// <summary>The in-build price applies when the build has a processor and a motherboard (owner decision, 13 Nov 2026).</summary>
    public static bool UsesBuildPrice(IEnumerable<string> slots)
    {
        var set = slots.ToHashSet(StringComparer.Ordinal);
        return set.Contains(BuilderSlot.Cpu) && set.Contains(BuilderSlot.Board);
    }

    /// <summary>
    /// Checks every pair of parts on the compatibility spec keys, and the power estimate against the power supply.
    /// Specs are optional (business §3): a value missing on either part is never a conflict.
    /// </summary>
    public static BuilderCheck Evaluate(IReadOnlyList<BuilderPart> parts, IReadOnlySet<string> compatibilityKeys)
    {
        var issues = new List<BuilderIssue>();
        for (var first = 0; first < parts.Count; first++)
            for (var second = first + 1; second < parts.Count; second++)
            {
                // Two parts of one slot (two drives) are never compared: they only have to fit the others.
                if (parts[first].Slot == parts[second].Slot) continue;
                CompareValues(parts[first], parts[second], compatibilityKeys, issues);
                CompareLimits(parts[first], parts[second], compatibilityKeys, issues);
                CompareLimits(parts[second], parts[first], compatibilityKeys, issues);
            }
        var (estimate, capacity) = Power(parts);
        if (estimate is decimal draw && capacity is decimal output)
        {
            if (draw > output)
                issues.Add(new BuilderIssue(BuilderIssueLevel.Conflict, BuilderIssueCode.PowerOverCapacity, BuilderSlot.Psu, null, BuilderSpecKey.PowerWatts));
            else if (draw > output * PowerWarningShare)
                issues.Add(new BuilderIssue(BuilderIssueLevel.Warning, BuilderIssueCode.PowerNearCapacity, BuilderSlot.Psu, null, BuilderSpecKey.PowerWatts));
        }
        return new BuilderCheck(issues, estimate, capacity);
    }

    /// <summary>Estimate = sum of <c>power_w</c> × quantity of every part except the power supply; capacity = the supply's own value.</summary>
    public static (decimal? Estimate, decimal? Capacity) Power(IReadOnlyList<BuilderPart> parts)
    {
        decimal? estimate = null;
        foreach (var part in parts.Where(part => part.Slot != BuilderSlot.Psu))
            if (Number(part, BuilderSpecKey.PowerWatts) is decimal watts) estimate = (estimate ?? 0m) + watts * part.Quantity;
        var psu = parts.FirstOrDefault(part => part.Slot == BuilderSlot.Psu);
        return (estimate, psu is null ? null : Number(psu, BuilderSpecKey.PowerWatts));
    }

    /// <summary>Same key on two parts: compatible when their value lists share at least one value ("AM4, AM5" fits "AM4").</summary>
    private static void CompareValues(BuilderPart left, BuilderPart right, IReadOnlySet<string> keys, List<BuilderIssue> issues)
    {
        foreach (var key in keys)
        {
            if (key == BuilderSpecKey.PowerWatts || key.StartsWith(BuilderSpecKey.MaxPrefix, StringComparison.Ordinal)) continue;
            if (!left.Specs.TryGetValue(key, out var leftValue) || !right.Specs.TryGetValue(key, out var rightValue)) continue;
            if (!Values(leftValue).Overlaps(Values(rightValue)))
                issues.Add(new BuilderIssue(BuilderIssueLevel.Conflict, BuilderIssueCode.SpecMismatch, right.Slot, left.Slot, key));
        }
    }

    private static void CompareLimits(BuilderPart limiting, BuilderPart other, IReadOnlySet<string> keys, List<BuilderIssue> issues)
    {
        foreach (var limitKey in keys.Where(key => key.StartsWith(BuilderSpecKey.MaxPrefix, StringComparison.Ordinal)))
        {
            var key = limitKey[BuilderSpecKey.MaxPrefix.Length..];
            if (Number(limiting, limitKey) is decimal limit && Number(other, key) is decimal value && value > limit)
                issues.Add(new BuilderIssue(BuilderIssueLevel.Conflict, BuilderIssueCode.OverLimit, other.Slot, limiting.Slot, key));
        }
    }

    private static HashSet<string> Values(string value) =>
        value.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static decimal? Number(BuilderPart part, string key) =>
        part.Specs.TryGetValue(key, out var text) && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    private static int IndexOf(string slot) => BuilderSlot.All.ToList().IndexOf(slot);
}
