using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Per-unit/product/category partner allocations with audit history and a precedence preview
/// (unit, then product, then category, then the default agreement).
/// </summary>
public sealed class PartnerAllocationService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<PartnerAllocationView>> ListAllocationsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.PartnerUnitAllocations.AsNoTracking()
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Select(item => new PartnerAllocationView(item.Id, item.PartnerName, item.UnitId, item.ProductId,
                item.Category, item.Percent, item.LossSharingEnabled, item.IsActive, item.RowVersion))
            .ToListAsync(cancellationToken);
    }

    public async Task<PartnerAllocationView> UpsertAllocationAsync(PartnerAllocationRequest request, Guid? actorId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PartnerName))
            throw new RetailException(400, "A partner name is required.");
        if (request.Percent is <= 0 or > 100)
            throw new RetailException(400, "The allocation percent must be greater than 0 and at most 100.");
        var scopes = new[] { request.UnitId is not null, request.ProductId is not null, !string.IsNullOrWhiteSpace(request.Category) };
        if (scopes.Count(value => value) != 1)
            throw new RetailException(400, "Choose exactly one allocation scope: unit, product, or category.");
        var partnerName = request.PartnerName.Trim();
        var category = request.Category?.Trim();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.PartnerUnitAllocations.SingleOrDefaultAsync(item =>
            item.PartnerName == partnerName && item.UnitId == request.UnitId &&
            item.ProductId == request.ProductId && item.Category == category, cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var reason = request.Reason?.Trim();
        if (entity is not null)
        {
            if (request.RowVersion is null)
                throw new RetailException(409, "An allocation for this partner/scope already exists; provide rowVersion to update it.");
            if (entity.RowVersion != request.RowVersion)
                throw new RetailException(409, "The allocation was changed by another user.");
            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 3)
                throw new RetailException(400, "A reason of at least 3 characters is required for allocation changes.");
        }
        var scopeRows = await db.PartnerUnitAllocations
            .Where(item => item.IsActive && item.UnitId == request.UnitId && item.ProductId == request.ProductId &&
                           item.Category == category && item.PartnerName != partnerName)
            .ToListAsync(cancellationToken);
        if (request.IsActive && scopeRows.Sum(item => item.Percent) + request.Percent > 100.01m)
            throw new RetailException(400, "Active partner allocations for this scope cannot exceed 100%.");
        if (entity is null)
        {
            entity = new PartnerUnitAllocation
            {
                Id = Guid.CreateVersion7(), PartnerName = partnerName, UnitId = request.UnitId,
                ProductId = request.ProductId, Category = category, CreatedAtUtc = now
            };
            db.PartnerUnitAllocations.Add(entity);
            reason = string.IsNullOrWhiteSpace(reason) ? "Initial allocation" : reason;
        }
        entity.Percent = request.Percent;
        entity.LossSharingEnabled = request.LossSharingEnabled;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = now;
        entity.RowVersion++;
        AddAllocationEvent(db, entity, "Upsert", actorId ?? Guid.Empty, reason!, now);
        await db.SaveChangesAsync(cancellationToken);
        return MapAllocation(entity);
    }

    public async Task<PartnerAllocationView> DeactivateAllocationAsync(Guid id, long rowVersion, string? reason,
        Guid actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
            throw new RetailException(400, "A reason of at least 3 characters is required to deactivate an allocation.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.PartnerUnitAllocations.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RetailException(404, "Allocation was not found.");
        if (entity.RowVersion != rowVersion)
            throw new RetailException(409, "The allocation was changed by another user.");
        if (!entity.IsActive) return MapAllocation(entity);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        entity.IsActive = false;
        entity.UpdatedAtUtc = now;
        entity.RowVersion++;
        AddAllocationEvent(db, entity, "Deactivate", actorId, reason.Trim(), now);
        await db.SaveChangesAsync(cancellationToken);
        return MapAllocation(entity);
    }

    public async Task<IReadOnlyList<PartnerAllocationEventView>> ListAllocationHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.PartnerAllocationEvents.AsNoTracking()
            .Where(item => item.AllocationId == id)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new PartnerAllocationEventView(item.Id, item.AllocationId, item.PartnerName,
                item.Action, item.ActorUserAccountId, item.Reason, item.SnapshotJson, item.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PartnerAllocationPreviewView> PreviewAllocationAsync(Guid? unitId, Guid? productId, string? category,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var allocations = await db.PartnerUnitAllocations.AsNoTracking().Where(item => item.IsActive).ToListAsync(cancellationToken);
        var source = "None";
        Guid? resolvedProduct = productId;
        string? resolvedCategory = category?.Trim();
        if (unitId is not null)
        {
            var unit = await db.Units.AsNoTracking()
                .Where(item => item.Id == unitId)
                .Select(item => new { item.ProductId, item.Category })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new RetailException(404, "Inventory unit was not found.");
            resolvedProduct = unit.ProductId;
            resolvedCategory = unit.Category;
            if (allocations.Any(item => item.UnitId == unitId)) source = "Unit";
        }
        if (source == "None" && resolvedProduct is not null && allocations.Any(item => item.UnitId == null && item.ProductId == resolvedProduct))
            source = "Product";
        if (source == "None" && !string.IsNullOrWhiteSpace(resolvedCategory) &&
            allocations.Any(item => item.UnitId == null && item.ProductId == null && item.Category == resolvedCategory))
            source = "Category";
        var rows = source switch
        {
            "Unit" => allocations.Where(item => item.UnitId == unitId),
            "Product" => allocations.Where(item => item.UnitId == null && item.ProductId == resolvedProduct),
            "Category" => allocations.Where(item => item.UnitId == null && item.ProductId == null && item.Category == resolvedCategory),
            _ => Enumerable.Empty<PartnerUnitAllocation>()
        };
        var views = rows.Select(MapAllocation).ToList();
        if (views.Count == 0)
        {
            var agreement = await db.PartnerAgreements.AsNoTracking()
                .Where(item => item.IsActive).OrderByDescending(item => item.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);
            if (agreement is not null)
            {
                source = "DefaultAgreement";
                views.Add(new PartnerAllocationView(Guid.Empty, agreement.PartnerName, null, null, null,
                    agreement.DefaultPercent, agreement.LossSharingEnabled, true, agreement.RowVersion));
            }
        }
        var total = decimal.Round(views.Sum(item => item.Percent), 2);
        return new PartnerAllocationPreviewView(source, views, total, decimal.Round(100m - total, 2));
    }

    private void AddAllocationEvent(RetailDbContext db, PartnerUnitAllocation entity, string action, Guid actorId,
        string reason, DateTimeOffset now)
    {
        db.PartnerAllocationEvents.Add(new PartnerAllocationEvent
        {
            Id = Guid.CreateVersion7(), AllocationId = entity.Id, PartnerName = entity.PartnerName,
            Action = action, ActorUserAccountId = actorId, Reason = reason,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                entity.PartnerName, entity.UnitId, entity.ProductId, entity.Category,
                entity.Percent, entity.LossSharingEnabled, entity.IsActive, entity.RowVersion
            }),
            CreatedAtUtc = now
        });
    }

    private static PartnerAllocationView MapAllocation(PartnerUnitAllocation item) =>
        new(item.Id, item.PartnerName, item.UnitId, item.ProductId, item.Category, item.Percent,
            item.LossSharingEnabled, item.IsActive, item.RowVersion);
}
