using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Inventory units: stock listing (cost/serial masking) and idempotent goods receipt from a purchase draft.
/// </summary>
public sealed class InventoryUnitService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<UnitView>> ListUnitsAsync(string? status, Guid? productId, bool includeCosts = true, bool includeSerials = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Units.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) { var value = status.Trim(); query = query.Where(unit => unit.Status == value); }
        if (productId is not null) query = query.Where(unit => unit.ProductId == productId);
        return (await query.OrderByDescending(unit => unit.ReceivedAtUtc).Take(500).ToListAsync(cancellationToken))
            .Select(unit => MapUnit(unit, includeCosts, includeSerials)).ToList();
    }

    public async Task<IReadOnlyList<UnitView>> ReceiveAsync(ReceiveRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count is < 1 or > 200)
            throw new RetailException(400, "A receipt needs between 1 and 200 lines.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existing = await db.Receipts.AsNoTracking().SingleOrDefaultAsync(
                receipt => receipt.PurchaseDraftId == request.PurchaseDraftId, cancellationToken);
            if (existing is not null)
            {
                var existingUnits = await db.Units.AsNoTracking()
                    .Where(unit => unit.ReceiptId == existing.Id).ToListAsync(cancellationToken);
                return existingUnits.Select(unit => MapUnit(unit, true, true)).ToList();
            }
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var receipt = new PurchaseReceipt
            {
                Id = Guid.CreateVersion7(),
                PurchaseDraftId = request.PurchaseDraftId,
                ReceivedByUserAccountId = actorId,
                ReceivedAtUtc = now,
                Notes = request.Notes?.Trim()
            };
            db.Receipts.Add(receipt);
            var units = new List<InventoryUnit>();
            foreach (var line in request.Lines)
            {
                if (string.IsNullOrWhiteSpace(line.Name) || line.Quantity is < 1 or > 1000)
                    throw new RetailException(400, "Each receipt line needs a name and a valid quantity.");
                if (line.AcquisitionCost < 0)
                    throw new RetailException(400, "Acquisition cost cannot be negative.");
                if (line.PartnerPercentOverride is < 0 or > 100)
                    throw new RetailException(400, "Partner percentage must be between 0 and 100.");
                if (line.StandalonePrice is < 0)
                    throw new RetailException(400, "Standalone price cannot be negative.");
                var condition = line.Condition is "New" or "Used" or "Open Box" ? line.Condition : "New";
                for (var index = 0; index < line.Quantity; index++)
                {
                    var unit = new InventoryUnit
                    {
                        Id = Guid.CreateVersion7(),
                        ProductId = line.ProductId,
                        VariantId = line.VariantId,
                        Sku = line.Sku,
                        Name = line.Name.Trim(),
                        Brand = line.Brand?.Trim(),
                        Model = line.Model?.Trim(),
                        Category = line.Category?.Trim(),
                        Condition = condition,
                        SerialNumber = line.Quantity == 1 ? line.SerialNumber?.Trim() : null,
                        WarrantyNote = line.WarrantyNote?.Trim(),
                        SourceName = line.SourceName?.Trim(),
                        SourceType = line.SourceType?.Trim(),
                        PurchaseKind = line.PurchaseKind?.Trim(),
                        PurchaseDate = line.PurchaseDate,
                        Description = line.Description?.Trim(),
                        ExternalCode = line.ExternalCode?.Trim(),
                        SpecificationsJson = line.SpecificationsJson,
                        ImageStorageKeysJson = line.ImageStorageKeysJson,
                        AcquisitionCost = line.AcquisitionCost,
                        StandalonePrice = line.StandalonePrice,
                        PartnerPercentOverride = line.PartnerPercentOverride,
                        Status = UnitStatus.Available,
                        ReceiptId = receipt.Id,
                        ReceivedAtUtc = now,
                        CreatedAtUtc = now
                    };
                    db.Units.Add(unit);
                    units.Add(unit);
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return units.Select(unit => MapUnit(unit, true, true)).ToList();
        }
        catch
        {
            await transaction.RollbackQuietlyAsync();
            throw;
        }
    }

    private static UnitView MapUnit(InventoryUnit unit, bool includeCosts = true, bool includeSerials = true) => new(unit.Id, unit.ProductId, unit.Name, unit.Sku,
        unit.Brand, unit.Model, unit.Category, unit.Description, unit.ExternalCode, unit.SpecificationsJson,
        unit.Condition, unit.InspectionCondition, includeSerials ? unit.SerialNumber : null,
        includeCosts ? unit.SourceName : null, includeCosts ? unit.SourceType : null,
        includeCosts ? unit.PurchaseKind : null, includeCosts ? unit.PurchaseDate : null,
        includeCosts ? unit.AcquisitionCost : null, includeCosts ? unit.StandalonePrice : null,
        includeCosts ? unit.PartnerPercentOverride : null, unit.Status, unit.AssemblyId, unit.ReceivedAtUtc);
}
