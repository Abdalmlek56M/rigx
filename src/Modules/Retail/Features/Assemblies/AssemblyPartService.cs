using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Build parts: add from stock, add a new part that is not in stock, edit a part (price, warranty override, partner %),
/// remove it back to stock or into another build, and replace it from stock or by swapping with another build's part.
/// Adding resets the revenue split to the default; removing rebalances the rest; a replacement takes the old part's share.
/// </summary>
public sealed class AssemblyPartService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<AssemblyView> AddPartAsync(Guid assemblyId, AssemblyAddPartRequest request, bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assembly = await AssemblyEditing.LoadEditableAsync(db, assemblyId, request.RowVersion, cancellationToken);
            EnsureRoom(assembly);
            AssemblyEditing.Link(assembly, await AssemblyEditing.LoadAvailableUnitAsync(db, request.UnitId, cancellationToken), 0m);
            return await FinishAddAsync(db, transaction, assembly, includeCosts, cancellationToken);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    /// <summary>The new part becomes its own single-piece stock item (RX code) placed straight into the build.</summary>
    public async Task<AssemblyView> AddNewPartAsync(Guid assemblyId, AssemblyNewPartRequest request, bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        if (request.Part.Quantity != 1) throw new RetailException(400, "A new build part is one piece.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assembly = await AssemblyEditing.LoadEditableAsync(db, assemblyId, request.RowVersion, cancellationToken);
            EnsureRoom(assembly);
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var item = (await StockItemFactory.CreateAsync(db, request.Part, new ReceiptSource(null, null, null, null), null, now, cancellationToken)).Single();
            var unit = item.Units.Single();
            unit.StockItem = item;
            AssemblyEditing.Link(assembly, unit, 0m);
            return await FinishAddAsync(db, transaction, assembly, includeCosts, cancellationToken);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    public async Task<AssemblyView> UpdatePartAsync(Guid assemblyId, Guid unitId, AssemblyPartUpdateRequest request,
        bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        AssemblyRules.ValidatePartPrice(request.PartPrice);
        StockItemRules.ValidateWarranty(request.WarrantyLabel, request.WarrantyMonths);
        if (request.PartnerPercentOverride is < 0 or > 100) throw new RetailException(400, "Partner percentage must be between 0 and 100.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await AssemblyEditing.LoadEditableAsync(db, assemblyId, request.RowVersion, cancellationToken);
        var link = FindPart(assembly, unitId);
        link.PartPrice = request.PartPrice;
        link.WarrantyLabel = string.IsNullOrWhiteSpace(request.WarrantyLabel) ? null : request.WarrantyLabel.Trim();
        link.WarrantyMonths = link.WarrantyLabel is null ? null : request.WarrantyMonths;
        if (link.Unit.PartnerPercentOverride != request.PartnerPercentOverride)
        {
            link.Unit.PartnerPercentOverride = request.PartnerPercentOverride;
            link.Unit.RowVersion++;
        }
        AssemblyEditing.Touch(assembly, timeProvider.GetUtcNow().ToUniversalTime());
        await AssemblyEditing.SaveAsync(db, cancellationToken);
        return AssemblyMapping.ToView(assembly, includeCosts);
    }

    /// <summary>Returns the changed builds: the source first, then the target build when the part moved into one.</summary>
    public async Task<IReadOnlyList<AssemblyView>> RemovePartAsync(Guid assemblyId, Guid unitId, AssemblyRemovePartRequest request,
        bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        if (request.TargetAssemblyId == assemblyId) throw new RetailException(400, "Choose another build to move the part into.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var source = await AssemblyEditing.LoadEditableAsync(db, assemblyId, request.RowVersion, cancellationToken);
            var link = FindPart(source, unitId);
            var unit = link.Unit;
            AssemblyEditing.Unlink(db, source, link);
            AllocationMath.Rebalance(source.Units.ToList());
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            AssemblyEditing.Touch(source, now);
            Assembly? target = null;
            if (request.TargetAssemblyId is Guid targetId)
            {
                target = await AssemblyEditing.LoadEditableAsync(db, targetId,
                    request.TargetRowVersion ?? throw new RetailException(400, "The target build version is required."), cancellationToken);
                EnsureRoom(target);
                AssemblyEditing.Link(target, unit, 0m);
                AssemblyEditing.ResetAllocations(target);
                AssemblyEditing.Touch(target, now);
            }
            else
            {
                unit.Status = UnitStatus.Available; unit.AssemblyId = null; unit.RowVersion++;
            }
            await AssemblyEditing.SaveAsync(db, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return target is null
                ? [AssemblyMapping.ToView(source, includeCosts)]
                : [AssemblyMapping.ToView(source, includeCosts), AssemblyMapping.ToView(target, includeCosts)];
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    /// <summary>
    /// Replaces a part. From stock: the old part becomes available. From another build: the two parts change places,
    /// each taking the other's price share. Returns this build first, then the other build when one was involved.
    /// </summary>
    public async Task<IReadOnlyList<AssemblyView>> ReplacePartAsync(Guid assemblyId, Guid unitId, AssemblyReplacePartRequest request,
        bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assembly = await AssemblyEditing.LoadEditableAsync(db, assemblyId, request.RowVersion, cancellationToken);
            var oldLink = FindPart(assembly, unitId);
            var replacement = await db.Units.Include(unit => unit.StockItem)
                .SingleOrDefaultAsync(unit => unit.Id == request.ReplacementUnitId, cancellationToken)
                ?? throw new RetailException(404, "The replacement unit was not found.");
            if (replacement.AssemblyId == assembly.Id) throw new RetailException(400, "The replacement is already in this build.");
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var share = oldLink.AllocationPercent;
            var oldUnit = oldLink.Unit;
            AssemblyEditing.Unlink(db, assembly, oldLink);
            Assembly? other = null;
            if (replacement.Status == UnitStatus.Available)
            {
                oldUnit.Status = UnitStatus.Available; oldUnit.AssemblyId = null; oldUnit.RowVersion++;
            }
            else if (replacement.Status == UnitStatus.InAssembly && replacement.AssemblyId is Guid otherId)
            {
                other = await AssemblyEditing.LoadEditableAsync(db, otherId,
                    request.OtherRowVersion ?? throw new RetailException(400, "The other build version is required."), cancellationToken);
                var otherLink = FindPart(other, replacement.Id);
                var otherShare = otherLink.AllocationPercent;
                AssemblyEditing.Unlink(db, other, otherLink);
                AssemblyEditing.Link(other, oldUnit, otherShare);
                AssemblyEditing.Touch(other, now);
            }
            else throw new RetailException(409, "The replacement unit is not available.");
            AssemblyEditing.Link(assembly, replacement, share);
            AssemblyEditing.Touch(assembly, now);
            await AssemblyEditing.SaveAsync(db, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return other is null
                ? [AssemblyMapping.ToView(assembly, includeCosts)]
                : [AssemblyMapping.ToView(assembly, includeCosts), AssemblyMapping.ToView(other, includeCosts)];
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    private async Task<AssemblyView> FinishAddAsync(RetailDbContext db, IDbContextTransaction transaction, Assembly assembly,
        bool includeCosts, CancellationToken cancellationToken)
    {
        AssemblyEditing.ResetAllocations(assembly);
        AssemblyEditing.Touch(assembly, timeProvider.GetUtcNow().ToUniversalTime());
        await AssemblyEditing.SaveAsync(db, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return AssemblyMapping.ToView(assembly, includeCosts);
    }

    private static AssemblyUnit FindPart(Assembly assembly, Guid unitId) =>
        assembly.Units.SingleOrDefault(link => link.UnitId == unitId) ?? throw new RetailException(404, "The unit is not part of this assembly.");

    private static void EnsureRoom(Assembly assembly)
    {
        if (assembly.Units.Count >= AssemblyRules.MaxParts)
            throw new RetailException(409, $"A build can have at most {AssemblyRules.MaxParts} parts.");
    }
}
