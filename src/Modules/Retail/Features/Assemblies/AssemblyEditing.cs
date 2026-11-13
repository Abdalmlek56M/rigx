using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Loading, part linking, allocation reset and saving shared by the build use cases.</summary>
internal static class AssemblyEditing
{
    /// <summary>Loads a build with its parts for editing; checks it is still editable and not changed meanwhile.</summary>
    public static async Task<Assembly> LoadEditableAsync(RetailDbContext db, Guid id, long rowVersion, CancellationToken cancellationToken)
    {
        var assembly = await db.Assemblies.WithUnits().SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new RetailException(404, "Assembly was not found.");
        AssemblyRules.EnsureEditable(assembly);
        AssemblyRules.EnsureVersion(assembly, rowVersion);
        return assembly;
    }

    /// <summary>Puts a unit into the build at its in-build price; the caller sets the allocation.</summary>
    public static AssemblyUnit Link(Assembly assembly, InventoryUnit unit, decimal allocationPercent)
    {
        var link = new AssemblyUnit
        {
            AssemblyId = assembly.Id, UnitId = unit.Id, AllocationPercent = allocationPercent,
            PartPrice = unit.StockItem.Price, Assembly = assembly, Unit = unit
        };
        assembly.Units.Add(link);
        unit.Status = UnitStatus.InAssembly; unit.AssemblyId = assembly.Id; unit.RowVersion++;
        return link;
    }

    public static void Unlink(RetailDbContext db, Assembly assembly, AssemblyUnit link)
    {
        assembly.Units.Remove(link);
        db.AssemblyUnits.Remove(link);
    }

    /// <summary>Revenue split back to the default: proportional to each part's price when sold alone (business §4).</summary>
    public static void ResetAllocations(Assembly assembly)
    {
        var links = assembly.Units.ToList();
        var percents = AllocationMath.DefaultAllocations(links.Select(link => AlonePrice(link.Unit.StockItem)).ToList());
        for (var index = 0; index < links.Count; index++) links[index].AllocationPercent = percents[index];
    }

    public static decimal AlonePrice(StockItem item) => StockItemRules.Prices(item.Price, item.WithBoardExtra, item.AloneExtra).Alone;

    public static void Touch(Assembly assembly, DateTimeOffset now)
    {
        assembly.UpdatedAtUtc = now;
        assembly.RowVersion++;
    }

    /// <summary>Loads an available unit (with its stock item) or fails with 409.</summary>
    public static async Task<InventoryUnit> LoadAvailableUnitAsync(RetailDbContext db, Guid unitId, CancellationToken cancellationToken) =>
        await db.Units.Include(unit => unit.StockItem)
            .SingleOrDefaultAsync(unit => unit.Id == unitId && unit.Status == UnitStatus.Available, cancellationToken)
        ?? throw new RetailException(409, "The selected unit is not available.");

    public static async Task SaveAsync(RetailDbContext db, CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new RetailException(409, "The assembly was changed by another user."); }
    }
}
