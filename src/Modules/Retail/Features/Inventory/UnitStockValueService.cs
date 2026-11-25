using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Leaves one unit out of the stock value (or counts it again); the unit stays in stock and sellable.</summary>
public sealed class UnitStockValueService(IDbContextFactory<RetailDbContext> contextFactory)
{
    public async Task<UnitView> SetExcludedAsync(Guid unitId, UnitStockValueRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var unit = await db.Units.Include(item => item.StockItem).SingleOrDefaultAsync(item => item.Id == unitId, cancellationToken)
            ?? throw new RetailException(404, "The unit was not found.");
        unit.ExcludedFromStockValue = request.Excluded;
        unit.RowVersion++;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new RetailException(409, "The unit was changed by another user."); }
        return InventoryUnitService.MapUnit(unit, true, false);
    }
}
