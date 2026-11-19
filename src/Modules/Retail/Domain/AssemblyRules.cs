namespace HardwareStore.Modules.Retail;

/// <summary>What a build is for (business §2: builds are grouped by usage, not by resolution).</summary>
public static class AssemblyUsage
{
    public const string Gaming = "Gaming";
    public const string Editing = "Editing";
    public const string StudyWork = "StudyWork";
    public const string Streaming = "Streaming";

    private static readonly string[] All = [Gaming, Editing, StudyWork, Streaming];

    /// <summary>Empty means "not set"; anything else must be one of the known values.</summary>
    public static string? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return All.FirstOrDefault(entry => entry == value.Trim())
            ?? throw new RetailException(400, "Usage must be Gaming, Editing, StudyWork or Streaming.");
    }
}

/// <summary>"كاملة بس" or "كاملة أو قطعة قطعة" (business §4).</summary>
public static class AssemblySellMode
{
    public const string Full = "Full";
    public const string FullOrSplit = "FullOrSplit";

    public static string Parse(string? value) => value?.Trim() switch
    {
        null or "" or Full => Full,
        FullOrSplit => FullOrSplit,
        _ => throw new RetailException(400, "Sell mode must be Full or FullOrSplit.")
    };
}

/// <summary>Build rules shared by every assembly use case.</summary>
public static class AssemblyRules
{
    public const int MaxParts = 50;
    public const int MaxCopies = 99;
    public const int MaxSuitableForLength = 500;

    /// <summary>Sold and disassembled builds are history and cannot change.</summary>
    public static void EnsureEditable(Assembly assembly)
    {
        if (assembly.Status is AssemblyStatus.Sold or AssemblyStatus.Disassembled)
            throw new RetailException(409, "This assembly can no longer be edited.");
    }

    public static void EnsureVersion(Assembly assembly, long rowVersion)
    {
        if (assembly.RowVersion != rowVersion) throw new RetailException(409, "The assembly was changed by another user.");
    }

    public static void ValidateCopies(int copies)
    {
        if (copies is < 1 or > MaxCopies) throw new RetailException(400, $"Copies must be between 1 and {MaxCopies}.");
    }

    public static string? CleanSuitableFor(string? text)
    {
        var clean = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (clean?.Length > MaxSuitableForLength) throw new RetailException(400, "The \"suitable for\" text is too long.");
        return clean;
    }

    /// <summary>Stock parts and typed parts together stay within <see cref="MaxParts"/>.</summary>
    public static void EnsureRoom(int units, int planned)
    {
        if (units + planned > MaxParts) throw new RetailException(409, $"A build can have at most {MaxParts} parts.");
    }

    /// <summary>A typed part needs a name; its price, when given, is not negative. A row without an id gets a new one.</summary>
    public static List<PlannedPart> CleanPlannedParts(IReadOnlyList<PlannedPartInput>? parts) =>
        (parts ?? []).Select(part =>
        {
            var name = StockItemRules.CleanModelName(part.Name);
            if (part.Price is < 0) throw new RetailException(400, "A part price cannot be negative.");
            return new PlannedPart(part.Id ?? Guid.CreateVersion7(), name, part.CategoryId, part.BrandId, part.Price, part.StockItemId, null);
        }).ToList();

    /// <summary>
    /// A whole build is sold from its stock units, so its typed parts must become real parts before it is published or sold
    /// (its real parts may still be sold one by one at the store).
    /// </summary>
    public static void EnsureNoPlannedParts(Assembly assembly)
    {
        if (assembly.PlannedParts.Count > 0)
            throw new RetailException(400, "The build still has typed parts that are not in stock; replace them with parts from stock first.");
    }

    public static void ValidatePartPrice(decimal price)
    {
        if (price < 0) throw new RetailException(400, "A part price cannot be negative.");
    }

    /// <summary>
    /// The warranty a part carries in this build: its own override first, then the build's warranty,
    /// then the stock item's warranty (build warranty "per part").
    /// </summary>
    public static (string? Label, int? Months) EffectiveWarranty(AssemblyUnit link, Assembly assembly, StockItem item)
    {
        if (link.WarrantyLabel is not null) return (link.WarrantyLabel, link.WarrantyMonths);
        if (assembly.WarrantyLabel is not null) return (assembly.WarrantyLabel, assembly.WarrantyMonths);
        return (item.WarrantyLabel, item.WarrantyMonths);
    }
}
