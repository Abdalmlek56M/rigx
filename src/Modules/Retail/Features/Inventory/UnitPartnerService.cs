using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Sets or clears who finances an unsold unit, with an optional percent that overrides the partner's default.</summary>
public sealed class UnitPartnerService(IDbContextFactory<RetailDbContext> contextFactory)
{
    public async Task<UnitView> AssignAsync(Guid unitId, UnitPartnerRequest request, CancellationToken cancellationToken = default)
    {
        var partnerId = request.PartnerId;
        var percent = partnerId is null ? null : request.Percent;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var unit = await db.Units.Include(item => item.StockItem).SingleOrDefaultAsync(item => item.Id == unitId, cancellationToken)
            ?? throw new RetailException(404, "The unit was not found.");
        if (unit.RowVersion != request.RowVersion) throw new RetailException(409, "The unit was changed by another user.");
        if (unit.Status == UnitStatus.Sold) throw new RetailException(409, "A sold unit's partner can no longer be changed.");
        await PartnerFinancing.EnsureAsync(db, partnerId, percent, cancellationToken);
        unit.PartnerId = partnerId;
        unit.PartnerPercentOverride = percent;
        unit.RowVersion++;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new RetailException(409, "The unit was changed by another user."); }
        return InventoryUnitService.MapUnit(unit, true, false);
    }
}
