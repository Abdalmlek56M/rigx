using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Builds (assemblies): create from available units, edit, allocation percentages, publish,
/// remove/swap components and disassemble. Units move between Available and InAssembly here.
/// </summary>
public sealed class AssemblyService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AssemblyView>> ListAssembliesAsync(bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Assemblies.AsNoTracking().WithUnits()
            .OrderByDescending(a => a.UpdatedAtUtc).ToListAsync(cancellationToken);
        return rows.Select(assembly => MapAssembly(assembly, includeCosts)).ToList();
    }

    public async Task<AssemblyView> CreateAssemblyAsync(AssemblyCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new RetailException(400, "Assembly name is required.");
        var unitIds = request.UnitIds?.Distinct().ToList() ?? [];
        if (unitIds.Count is < 1 or > 50) throw new RetailException(400, "An assembly needs between 1 and 50 units.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var assembly = new Assembly { Id = Guid.CreateVersion7(), Code = await db.NextCodeAsync(CodeSequence.Assembly, PublicCode.Build, cancellationToken), Name = request.Name.Trim(), Status = AssemblyStatus.Draft, CreatedAtUtc = now, UpdatedAtUtc = now };
            var claimed = await db.Units.Include(unit => unit.StockItem)
                .Where(unit => unitIds.Contains(unit.Id) && unit.Status == UnitStatus.Available)
                .ToListAsync(cancellationToken);
            if (claimed.Count != unitIds.Count) throw new RetailException(409, "Some selected units are no longer available.");
            var allocations = AllocationMath.DefaultAllocations(claimed.Select(unit => AlonePrice(unit.StockItem)).ToList());
            for (var index = 0; index < claimed.Count; index++)
            {
                var unit = claimed[index];
                unit.Status = UnitStatus.InAssembly; unit.AssemblyId = assembly.Id; unit.RowVersion++;
                assembly.Units.Add(new AssemblyUnit { AssemblyId = assembly.Id, UnitId = unit.Id, AllocationPercent = allocations[index] });
            }
            db.Assemblies.Add(assembly);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapAssembly(assembly);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    public async Task<AssemblyView> UpdateAssemblyAsync(Guid id, AssemblyUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SalePrice < 0) throw new RetailException(400, "Sale price cannot be negative.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await db.Assemblies.WithUnits()
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw new RetailException(404, "Assembly was not found.");
        if (assembly.Status is AssemblyStatus.Sold or AssemblyStatus.Disassembled) throw new RetailException(409, "This assembly can no longer be edited.");
        if (assembly.RowVersion != request.RowVersion) throw new RetailException(409, "The assembly was changed by another user.");
        if (!string.IsNullOrWhiteSpace(request.Name)) assembly.Name = request.Name.Trim();
        assembly.PublicTitleAr = request.PublicTitleAr?.Trim(); assembly.PublicTitleEn = request.PublicTitleEn?.Trim();
        assembly.SalePrice = request.SalePrice; assembly.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); assembly.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return MapAssembly(assembly);
    }

    public async Task<AssemblyView> SetAssemblyAllocationsAsync(Guid id, AssemblyAllocationRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await db.Assemblies.WithUnits()
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw new RetailException(404, "Assembly was not found.");
        if (assembly.Status is AssemblyStatus.Sold or AssemblyStatus.Disassembled) throw new RetailException(409, "This assembly can no longer be edited.");
        if (assembly.RowVersion != request.RowVersion) throw new RetailException(409, "The assembly was changed by another user.");
        var inputs = request.Allocations?.ToList() ?? [];
        if (inputs.Count != assembly.Units.Count || inputs.Select(item => item.UnitId).Distinct().Count() != inputs.Count)
            throw new RetailException(400, "Provide one allocation row for every component.");
        if (inputs.Any(item => item.Percent is < 0 or > 100))
            throw new RetailException(400, "Each allocation percent must be between 0 and 100.");
        if (!AllocationMath.SumsToHundred(inputs.Select(item => item.Percent)))
            throw new RetailException(400, "Allocation percentages must sum to 100.");
        foreach (var link in assembly.Units)
        {
            var input = inputs.SingleOrDefault(item => item.UnitId == link.UnitId)
                ?? throw new RetailException(400, "An allocation row refers to a unit outside this assembly.");
            link.AllocationPercent = decimal.Round(input.Percent, 2);
        }
        assembly.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); assembly.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return MapAssembly(assembly);
    }

    public async Task<AssemblyView> SetAssemblyPublishedAsync(Guid id, bool published, long rowVersion, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await db.Assemblies.WithUnits()
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw new RetailException(404, "Assembly was not found.");
        if (assembly.Status is AssemblyStatus.Sold or AssemblyStatus.Disassembled) throw new RetailException(409, "This assembly can no longer be published.");
        if (assembly.RowVersion != rowVersion) throw new RetailException(409, "The assembly was changed by another user.");
        if (published && assembly.SalePrice <= 0) throw new RetailException(400, "A sale price is required before publishing.");
        if (published && (assembly.Units.Count == 0 || !AllocationMath.SumsToHundred(assembly.Units.Select(link => link.AllocationPercent))))
            throw new RetailException(400, "Component allocations must sum to 100 before publishing.");
        assembly.Status = published ? AssemblyStatus.Published : AssemblyStatus.Draft;
        assembly.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); assembly.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return MapAssembly(assembly);
    }

    public async Task<AssemblyView> RemoveUnitAsync(Guid assemblyId, Guid unitId, long rowVersion, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assembly = await db.Assemblies.WithUnits()
                .SingleOrDefaultAsync(a => a.Id == assemblyId, cancellationToken) ?? throw new RetailException(404, "Assembly was not found.");
            if (assembly.Status == AssemblyStatus.Sold) throw new RetailException(409, "A sold assembly cannot be changed.");
            if (assembly.RowVersion != rowVersion) throw new RetailException(409, "The assembly was changed by another user.");
            var link = assembly.Units.SingleOrDefault(item => item.UnitId == unitId) ?? throw new RetailException(404, "The unit is not part of this assembly.");
            assembly.Units.Remove(link);
            db.AssemblyUnits.Remove(link);
            var unit = await db.Units.SingleAsync(item => item.Id == unitId, cancellationToken);
            unit.Status = UnitStatus.Available; unit.AssemblyId = null; unit.RowVersion++;
            AllocationMath.Rebalance(assembly.Units.ToList());
            assembly.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); assembly.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapAssembly(assembly);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    public async Task<IReadOnlyList<AssemblyView>> SwapUnitAsync(SwapRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FromAssemblyId == request.ToAssemblyId) throw new RetailException(400, "Choose two different assemblies.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assemblies = await db.Assemblies.WithUnits()
                .Where(a => a.Id == request.FromAssemblyId || a.Id == request.ToAssemblyId).ToListAsync(cancellationToken);
            var from = assemblies.SingleOrDefault(a => a.Id == request.FromAssemblyId) ?? throw new RetailException(404, "Source assembly was not found.");
            var to = assemblies.SingleOrDefault(a => a.Id == request.ToAssemblyId) ?? throw new RetailException(404, "Target assembly was not found.");
            if (from.Status == AssemblyStatus.Sold || to.Status == AssemblyStatus.Sold) throw new RetailException(409, "Sold assemblies cannot be changed.");
            if (from.RowVersion != request.FromRowVersion || to.RowVersion != request.ToRowVersion) throw new RetailException(409, "An assembly was changed by another user.");
            var link = from.Units.SingleOrDefault(item => item.UnitId == request.UnitId) ?? throw new RetailException(404, "The unit is not in the source assembly.");
            var allocation = link.AllocationPercent;
            from.Units.Remove(link); db.AssemblyUnits.Remove(link);
            to.Units.Add(new AssemblyUnit { AssemblyId = to.Id, UnitId = request.UnitId, AllocationPercent = allocation });
            var unit = await db.Units.SingleAsync(item => item.Id == request.UnitId, cancellationToken);
            unit.AssemblyId = to.Id; unit.RowVersion++;
            AllocationMath.Rebalance(from.Units.ToList());
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            from.UpdatedAtUtc = now; from.RowVersion++; to.UpdatedAtUtc = now; to.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return [MapAssembly(from), MapAssembly(to)];
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    public async Task<AssemblyView> DisassembleAsync(Guid id, long rowVersion, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assembly = await db.Assemblies.WithUnits()
                .SingleOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw new RetailException(404, "Assembly was not found.");
            if (assembly.Status == AssemblyStatus.Sold) throw new RetailException(409, "A sold assembly cannot be disassembled.");
            if (assembly.RowVersion != rowVersion) throw new RetailException(409, "The assembly was changed by another user.");
            foreach (var link in assembly.Units)
            {
                var unit = await db.Units.SingleAsync(item => item.Id == link.UnitId, cancellationToken);
                unit.Status = UnitStatus.Available; unit.AssemblyId = null; unit.RowVersion++;
            }
            db.AssemblyUnits.RemoveRange(assembly.Units);
            assembly.Units.Clear();
            assembly.Status = AssemblyStatus.Disassembled;
            assembly.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); assembly.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapAssembly(assembly);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    public async Task<IReadOnlyList<AssemblyPublicView>> ListPublishedAssembliesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Assemblies.AsNoTracking().WithUnits()
            .Where(a => a.Status == AssemblyStatus.Published).OrderByDescending(a => a.UpdatedAtUtc).ToListAsync(cancellationToken);
        return rows.Select(a => new AssemblyPublicView(a.Id, a.Code, a.PublicTitleAr, a.PublicTitleEn, a.SalePrice,
            a.Units.Select(link => new PublicUnitView(link.Unit.ProductId, link.Unit.Name, link.Unit.StockItem.Code, link.Unit.Condition)).ToList())).ToList();
    }

    private static decimal AlonePrice(StockItem item) => StockItemRules.Prices(item.Price, item.WithBoardExtra, item.AloneExtra).Alone;

    private static AssemblyView MapAssembly(Assembly assembly, bool includeCosts = true)
    {
        var units = assembly.Units.Select(link => new AssemblyUnitView(link.UnitId, link.Unit.Name, link.Unit.StockItem.Code,
            link.Unit.Condition, includeCosts ? link.Unit.AcquisitionCost : null,
            includeCosts ? AlonePrice(link.Unit.StockItem) : null, link.AllocationPercent,
            includeCosts ? decimal.Round(assembly.SalePrice * link.AllocationPercent / 100m, 2) : null)).ToList();
        return new AssemblyView(assembly.Id, assembly.Code, assembly.Name, assembly.PublicTitleAr, assembly.PublicTitleEn,
            assembly.SalePrice, assembly.Status, units,
            includeCosts ? decimal.Round(units.Sum(unit => unit.AcquisitionCost ?? 0m), 2) : null,
            includeCosts ? decimal.Round(units.Sum(unit => unit.AllocatedAmount ?? 0m), 2) : null, assembly.RowVersion);
    }
}
