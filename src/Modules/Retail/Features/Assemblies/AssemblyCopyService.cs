using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// "Several identical builds at once": each copy is its own draft build with its own new single-piece stock items and units
/// (same models, costs, prices, partners and warranty), so serials and edits stay per build. Photos are not copied.
/// </summary>
public sealed class AssemblyCopyService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AssemblyView>> CopyAsync(Guid assemblyId, AssemblyCopyRequest request, bool includeCosts = true,
        CancellationToken cancellationToken = default)
    {
        AssemblyRules.EnsureCopyCount(request.Count);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var source = await db.Assemblies.AsNoTracking().WithUnits().SingleOrDefaultAsync(a => a.Id == assemblyId, cancellationToken)
                ?? throw new RetailException(404, "Assembly was not found.");
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var copies = new List<Assembly>();
            for (var index = 0; index < request.Count; index++)
                copies.Add(await CopyOneAsync(db, source, now, cancellationToken));
            await AssemblyEditing.SaveAsync(db, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return copies.Select(copy => AssemblyMapping.ToView(copy, includeCosts)).ToList();
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    private static async Task<Assembly> CopyOneAsync(RetailDbContext db, Assembly source, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var copy = new Assembly
        {
            Id = Guid.CreateVersion7(), Code = await db.NextCodeAsync(CodeSequence.Assembly, PublicCode.Build, cancellationToken),
            Name = source.Name, Status = AssemblyStatus.Draft, PublicTitleAr = source.PublicTitleAr, PublicTitleEn = source.PublicTitleEn,
            SalePrice = source.SalePrice, Usage = source.Usage, SellMode = source.SellMode, WarrantyLabel = source.WarrantyLabel,
            WarrantyMonths = source.WarrantyMonths, SuitableForAr = source.SuitableForAr, SuitableForEn = source.SuitableForEn, Copies = 1,
            PlannedParts = source.PlannedParts.Select(part => part with { Id = Guid.CreateVersion7() }).ToList(),
            CreatedAtUtc = now, UpdatedAtUtc = now
        };
        foreach (var sourceLink in source.Units)
        {
            var unit = await NewPartAsync(db, sourceLink.Unit, now, cancellationToken);
            var link = AssemblyEditing.Link(copy, unit, sourceLink.AllocationPercent);
            link.PartPrice = sourceLink.PartPrice;
            link.WarrantyLabel = sourceLink.WarrantyLabel;
            link.WarrantyMonths = sourceLink.WarrantyMonths;
        }
        db.Assemblies.Add(copy);
        return copy;
    }

    /// <summary>A new single-piece stock item and unit with the facts of the source part (no serial).</summary>
    private static async Task<InventoryUnit> NewPartAsync(RetailDbContext db, InventoryUnit sourceUnit, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var sourceItem = sourceUnit.StockItem;
        var line = new ReceiptLine(sourceItem.ModelName, sourceItem.Condition, 1, sourceUnit.AcquisitionCost, sourceItem.Price,
            sourceItem.CategoryId, sourceItem.ProductId, sourceItem.WithBoardExtra, sourceItem.AloneExtra, null, sourceUnit.PartnerPercentOverride,
            sourceItem.WarrantyLabel, sourceItem.WarrantyMonths, sourceItem.HasBox, sourceItem.IsHidden, sourceItem.InternalNote, sourceItem.Specs,
            sourceUnit.PartnerId, sourceItem.BrandId);
        var source = new ReceiptSource(sourceUnit.SourceName, sourceUnit.SourceType, sourceUnit.PurchaseKind, sourceUnit.PurchaseDate);
        var item = (await StockItemFactory.CreateAsync(db, line, source, null, now, cancellationToken)).Single();
        item.OffersAlone = sourceItem.OffersAlone;
        item.OffersWithBoard = sourceItem.OffersWithBoard;
        item.OffersInBuild = sourceItem.OffersInBuild;
        item.ExcludedFromStockValue = sourceItem.ExcludedFromStockValue;
        var unit = item.Units.Single();
        unit.ExcludedFromStockValue = sourceUnit.ExcludedFromStockValue;
        unit.StockItem = item;
        return unit;
    }
}
