using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Inventory units: stock listing (cost/serial masking) and idempotent goods receipt from a purchase draft.
/// Receiving creates the stock items the store sells (a used piece is its own item, new pieces form a lot).
/// </summary>
public sealed class InventoryUnitService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    private const int MaxReceiptLines = 200;

    public async Task<IReadOnlyList<UnitView>> ListUnitsAsync(string? status, Guid? productId, bool includeCosts = true, bool includeSerials = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Units.AsNoTracking().Include(unit => unit.StockItem).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) { var value = status.Trim(); query = query.Where(unit => unit.Status == value); }
        if (productId is not null) query = query.Where(unit => unit.ProductId == productId);
        return (await query.OrderByDescending(unit => unit.ReceivedAtUtc).Take(500).ToListAsync(cancellationToken))
            .Select(unit => MapUnit(unit, includeCosts, includeSerials)).ToList();
    }

    public async Task<ReceiptResult> ReceiveAsync(ReceiveRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count is < 1 or > MaxReceiptLines)
            throw new RetailException(400, $"A receipt needs between 1 and {MaxReceiptLines} lines.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existing = await db.Receipts.AsNoTracking().SingleOrDefaultAsync(
                receipt => receipt.PurchaseDraftId == request.PurchaseDraftId, cancellationToken);
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            if (existing is not null)
            {
                var existingItems = await db.StockItems.AsNoTracking().Include(item => item.Units)
                    .Where(item => item.Units.Any(unit => unit.ReceiptId == existing.Id))
                    .OrderBy(item => item.Code.Length).ThenBy(item => item.Code).ToListAsync(cancellationToken);
                return new ReceiptResult(existing.Id, existingItems.Select(item => StockItemMapping.ToAdminView(item, now, true, true)).ToList());
            }
            var receipt = new PurchaseReceipt
            {
                Id = Guid.CreateVersion7(),
                PurchaseDraftId = request.PurchaseDraftId,
                ReceivedByUserAccountId = actorId,
                ReceivedAtUtc = now,
                Notes = request.Notes?.Trim()
            };
            db.Receipts.Add(receipt);
            var items = new List<StockItem>();
            foreach (var line in request.Lines)
                items.AddRange(await CreateItemsAsync(db, line, request.Source, receipt.Id, now, cancellationToken));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ReceiptResult(receipt.Id, items.Select(item => StockItemMapping.ToAdminView(item, now, true, true)).ToList());
        }
        catch
        {
            await transaction.RollbackQuietlyAsync();
            throw;
        }
    }

    private static async Task<List<StockItem>> CreateItemsAsync(RetailDbContext db, ReceiptLine line, ReceiptSource source,
        Guid receiptId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var condition = StockCondition.Parse(line.Condition);
        var modelName = StockItemRules.CleanModelName(line.ModelName);
        StockItemRules.ValidatePrices(line.Price, line.WithBoardExtra, line.AloneExtra);
        StockItemRules.ValidateWarranty(line.WarrantyLabel, line.WarrantyMonths);
        var specs = StockItemRules.CleanSpecs(line.Specs);
        var note = StockItemRules.CleanNote(line.InternalNote);
        if (line.AcquisitionCost < 0) throw new RetailException(400, "Acquisition cost cannot be negative.");
        if (line.PartnerPercentOverride is < 0 or > 100) throw new RetailException(400, "Partner percentage must be between 0 and 100.");
        var items = new List<StockItem>();
        foreach (var unitCount in StockItemRules.UnitsPerItem(condition, line.Quantity))
        {
            var item = new StockItem
            {
                Id = Guid.CreateVersion7(),
                Code = await db.NextCodeAsync(CodeSequence.StockItem, PublicCode.StockItem, cancellationToken),
                Condition = condition,
                CategoryId = line.CategoryId,
                ProductId = line.ProductId,
                ModelName = modelName,
                ModelKey = ModelName.Key(modelName),
                Price = line.Price,
                WithBoardExtra = line.WithBoardExtra,
                AloneExtra = line.AloneExtra,
                WarrantyLabel = line.WarrantyLabel?.Trim(),
                WarrantyMonths = line.WarrantyMonths,
                HasBox = line.HasBox,
                IsHidden = line.IsHidden,
                InternalNote = note,
                Specs = new Dictionary<string, string>(specs),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            for (var index = 0; index < unitCount; index++)
                item.Units.Add(new InventoryUnit
                {
                    Id = Guid.CreateVersion7(),
                    StockItemId = item.Id,
                    ProductId = line.ProductId,
                    Name = modelName,
                    Condition = condition,
                    SerialNumber = unitCount == 1 && line.Quantity == 1 ? line.SerialNumber?.Trim() : null,
                    SourceName = source.SourceName?.Trim(),
                    SourceType = source.SourceType?.Trim(),
                    PurchaseKind = source.PurchaseKind?.Trim(),
                    PurchaseDate = source.PurchaseDate,
                    AcquisitionCost = line.AcquisitionCost,
                    PartnerPercentOverride = line.PartnerPercentOverride,
                    Status = UnitStatus.Available,
                    ReceiptId = receiptId,
                    ReceivedAtUtc = now,
                    CreatedAtUtc = now
                });
            db.StockItems.Add(item);
            items.Add(item);
        }
        return items;
    }

    private static UnitView MapUnit(InventoryUnit unit, bool includeCosts, bool includeSerials) => new(unit.Id, unit.StockItemId,
        unit.StockItem.Code, unit.ProductId, unit.Name, unit.Condition, unit.InspectionCondition,
        includeSerials ? unit.SerialNumber : null, includeCosts ? unit.SourceName : null, includeCosts ? unit.SourceType : null,
        includeCosts ? unit.PurchaseKind : null, includeCosts ? unit.PurchaseDate : null,
        includeCosts ? unit.AcquisitionCost : null, includeCosts ? unit.PartnerPercentOverride : null,
        unit.Status, unit.AssemblyId, unit.ReceivedAtUtc);
}
