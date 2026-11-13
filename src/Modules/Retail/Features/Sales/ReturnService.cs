using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Returns: open (unit goes to PendingInspection), inspect (refund, unit back to stock or scrap,
/// partner earnings reversed) or reject (unit stays sold).
/// </summary>
public sealed class ReturnService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ReturnView>> ListReturnsAsync(string? status, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.OrderReturns.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) { var value = status.Trim(); query = query.Where(item => item.Status == value); }
        return (await query.OrderByDescending(item => item.CreatedAtUtc).Take(300).ToListAsync(cancellationToken)).Select(MapReturn).ToList();
    }

    public async Task<ReturnView> CreateReturnAsync(ReturnCreateRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new RetailException(400, "A return reason is required.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = await db.Orders.Include(item => item.Items).ThenInclude(item => item.Components)
                .SingleOrDefaultAsync(item => item.Id == request.OrderId, cancellationToken)
                ?? throw new RetailException(404, "Order was not found.");
            if (order.Status is not (OrderStatus.Delivered or OrderStatus.Completed))
                throw new RetailException(409, "A return can only be opened for a delivered or completed order.");
            var item = order.Items.SingleOrDefault(entry => entry.Id == request.OrderItemId)
                ?? throw new RetailException(404, "Order line was not found.");
            var component = item.Components.SingleOrDefault(entry => entry.UnitId == request.UnitId)
                ?? throw new RetailException(404, "The selected unit is not part of this order line.");
            if (await db.OrderReturns.AnyAsync(entry => entry.OrderItemId == item.Id && entry.UnitId == component.UnitId, cancellationToken))
                throw new RetailException(409, "This unit already has a return record.");
            var maxRefund = Math.Max(0, component.RevenueAmount - component.DiscountAmount);
            var refund = decimal.Round(request.RefundAmount, 2);
            if (refund < 0 || refund > maxRefund)
                throw new RetailException(400, "The refund must be between 0 and the paid amount for this unit.");
            var unit = await db.Units.SingleAsync(entry => entry.Id == component.UnitId, cancellationToken);
            if (unit.Status == UnitStatus.PendingInspection || unit.Status == UnitStatus.Scrapped)
                throw new RetailException(409, "This unit cannot be returned again.");
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            unit.Status = UnitStatus.PendingInspection; unit.RowVersion++;
            var entity = new OrderReturn
            {
                Id = Guid.CreateVersion7(), OrderId = order.Id, OrderItemId = item.Id, UnitId = component.UnitId,
                RefundAmount = refund, Reason = request.Reason.Trim(), Status = ReturnStatus.PendingInspection,
                CreatedByUserAccountId = actorId, CreatedAtUtc = now
            };
            db.OrderReturns.Add(entity);
            order.Events.Add(new OrderEvent { Id = Guid.CreateVersion7(), EventType = "Return:Opened", Notes = $"{item.Name} · {request.Reason.Trim()}", ActorUserAccountId = actorId, CreatedAtUtc = now });
            order.UpdatedAtUtc = now; order.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapReturn(entity);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    public async Task<ReturnView> InspectReturnAsync(Guid id, ReturnInspectRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var condition = request.Condition?.Trim();
        if (condition is not ("New" or "Used" or "Open Box" or "Scrap"))
            throw new RetailException(400, "Inspection condition must be New, Used, Open Box, or Scrap.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var entity = await db.OrderReturns.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new RetailException(404, "Return was not found.");
            if (entity.RowVersion != request.RowVersion) throw new RetailException(409, "The return was changed by another user.");
            if (entity.Status != ReturnStatus.PendingInspection) throw new RetailException(409, "Only a pending return can be inspected.");
            var order = await db.Orders.WithDetails()
                .SingleAsync(item => item.Id == entity.OrderId, cancellationToken);
            var item = order.Items.Single(entry => entry.Id == entity.OrderItemId);
            var component = item.Components.Single(entry => entry.UnitId == entity.UnitId);
            var unit = await db.Units.SingleAsync(entry => entry.Id == entity.UnitId, cancellationToken);
            var now = timeProvider.GetUtcNow().ToUniversalTime();

            entity.Status = ReturnStatus.Inspected; entity.InspectionCondition = condition; entity.InspectionNotes = request.Notes?.Trim();
            entity.InspectedByUserAccountId = actorId; entity.InspectedAtUtc = now; entity.RowVersion++;
            unit.InspectionCondition = condition;
            unit.Status = condition == "Scrap" ? UnitStatus.Scrapped : UnitStatus.Available;
            unit.AssemblyId = null; unit.RowVersion++;
            if (item.AssemblyId is not null)
            {
                var assembly = await db.Assemblies.SingleOrDefaultAsync(entry => entry.Id == item.AssemblyId, cancellationToken);
                if (assembly is not null && assembly.Status == AssemblyStatus.Sold)
                {
                    assembly.Status = AssemblyStatus.Disassembled;
                    assembly.UpdatedAtUtc = now; assembly.RowVersion++;
                }
            }
            order.RefundedAmount = decimal.Round(order.RefundedAmount + entity.RefundAmount, 2);
            if (order.RefundedAmount >= order.Total) order.PaymentStatus = PaymentStatus.Refunded;
            order.Events.Add(new OrderEvent { Id = Guid.CreateVersion7(), EventType = "Return:Inspected", Notes = $"{condition} · refund {entity.RefundAmount}", ActorUserAccountId = actorId, CreatedAtUtc = now });
            order.UpdatedAtUtc = now; order.RowVersion++;

            await WarrantyRegistry.VoidForUnitAsync(db, order.Id, unit.Id, "Returned", actorId, now, cancellationToken);
            await PartnerEarningsPosting.ReverseForReturnAsync(db, entity, item, unit.Id, condition, now, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapReturn(entity);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    public async Task<ReturnView> RejectReturnAsync(Guid id, ReturnRejectRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var entity = await db.OrderReturns.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new RetailException(404, "Return was not found.");
            if (entity.RowVersion != request.RowVersion) throw new RetailException(409, "The return was changed by another user.");
            if (entity.Status != ReturnStatus.PendingInspection) throw new RetailException(409, "Only a pending return can be rejected.");
            var unit = await db.Units.SingleAsync(entry => entry.Id == entity.UnitId, cancellationToken);
            unit.Status = UnitStatus.Sold; unit.RowVersion++;
            entity.Status = ReturnStatus.Rejected; entity.InspectionNotes = request.Notes?.Trim();
            entity.InspectedByUserAccountId = actorId; entity.InspectedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); entity.RowVersion++;
            var order = await db.Orders.Include(item => item.Events).SingleAsync(item => item.Id == entity.OrderId, cancellationToken);
            order.Events.Add(new OrderEvent { Id = Guid.CreateVersion7(), EventType = "Return:Rejected", Notes = request.Notes?.Trim(), ActorUserAccountId = actorId, CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime() });
            order.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); order.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapReturn(entity);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    private static ReturnView MapReturn(OrderReturn entity) => new(entity.Id, entity.OrderId, entity.OrderItemId,
        entity.UnitId, entity.RefundAmount, entity.Reason, entity.Status, entity.InspectionCondition,
        entity.InspectionNotes, entity.InspectedByUserAccountId, entity.InspectedAtUtc, entity.CreatedAtUtc, entity.RowVersion);
}
