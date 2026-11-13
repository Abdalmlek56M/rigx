using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace HardwareStore.Modules.Retail;

public sealed record ReceiptLine(Guid? ProductId, Guid? VariantId, string Name, string? Sku, string Condition, string? SerialNumber, decimal AcquisitionCost, int Quantity, decimal? PartnerPercentOverride, string? WarrantyNote, decimal? StandalonePrice = null, string? SourceName = null, string? Brand = null, string? Model = null, string? Category = null, string? Description = null, string? ExternalCode = null, string? SpecificationsJson = null, string? PurchaseKind = null, string? SourceType = null, DateOnly? PurchaseDate = null, string? ImageStorageKeysJson = null);
public sealed record ReceiveRequest(Guid PurchaseDraftId, string? Notes, IReadOnlyList<ReceiptLine> Lines);
public sealed record AssemblyCreateRequest(string? Name, IReadOnlyList<Guid>? UnitIds);
public sealed record AssemblyUpdateRequest(string? Name, string? PublicTitleAr, string? PublicTitleEn, decimal SalePrice, long RowVersion);
public sealed record AssemblyAllocationRequest(long RowVersion, IReadOnlyList<AssemblyAllocationInput>? Allocations);
public sealed record AssemblyAllocationInput(Guid UnitId, decimal Percent);
public sealed record SwapRequest(Guid FromAssemblyId, Guid ToAssemblyId, Guid UnitId, long FromRowVersion, long ToRowVersion);
public sealed record OrderLine(Guid? ProductId, Guid? AssemblyId, string Name, string? Sku, int Quantity, decimal UnitPrice, decimal? DiscountAmount = null, decimal? DirectExpenseAmount = null, string? DirectExpenseNote = null, Guid? VariantId = null, string? VariantName = null, string? ThumbnailStorageKeySnapshot = null);
public sealed record OrderCreateRequest(string? CustomerName, string? PhoneNumber, string? AddressText, string? FulfillmentMethod, string? PaymentMethod, string? Notes, IReadOnlyList<OrderLine>? Items, string? LeadSource, bool? PaymentRequired = null, bool? TermsAccepted = null, string? TermsPolicyVersion = null);
public sealed record OrderStatusRequest(string? Status, string? Notes, long RowVersion);
public sealed record CompleteOrderRequest(long RowVersion, string? Notes);
public sealed record PaymentStatusRequest(string? PaymentStatus, string? Notes, long RowVersion);
public sealed record WaivePaymentRequest(string? Reason, long RowVersion);
public sealed record PartnerAgreementRequest(string? PartnerName, decimal DefaultPercent, bool IsActive, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool LossSharingEnabled = false);
public sealed record PartnerAllocationRequest(string? PartnerName, Guid? UnitId, Guid? ProductId, string? Category, decimal Percent, bool LossSharingEnabled, bool IsActive, long? RowVersion = null, string? Reason = null);
public sealed record SettlementRequest(string? PartnerName, decimal Amount, string? Method, string? Reference, string? Note);
public sealed record ExpenseCreateRequest(DateOnly? ExpenseDate, decimal Amount, string? Category, string? Classification,
    string? Note, Guid? OrderId, Guid? UnitId, string? AttachmentReference);
public sealed record ReturnCreateRequest(Guid OrderId, Guid OrderItemId, Guid UnitId, decimal RefundAmount, string? Reason);
public sealed record ReturnInspectRequest(string? Condition, string? Notes, long RowVersion);
public sealed record ReturnRejectRequest(string? Notes, long RowVersion);
public sealed record AssemblyPublicView(Guid Id, string? TitleAr, string? TitleEn, decimal SalePrice, IReadOnlyList<PublicUnitView> Units);
public sealed record PublicUnitView(Guid? ProductId, string Name, string? Sku, string Condition);

public sealed class RetailService(
    IDbContextFactory<RetailDbContext> contextFactory,
    TimeProvider timeProvider)
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
            await RollbackAsync(transaction);
            throw;
        }
    }

    public async Task<IReadOnlyList<AssemblyView>> ListAssembliesAsync(bool includeCosts = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Assemblies.AsNoTracking().Include(a => a.Units).ThenInclude(link => link.Unit)
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
            var assembly = new Assembly { Id = Guid.CreateVersion7(), Name = request.Name.Trim(), Status = AssemblyStatus.Draft, CreatedAtUtc = now, UpdatedAtUtc = now };
            var claimed = await db.Units
                .Where(unit => unitIds.Contains(unit.Id) && unit.Status == UnitStatus.Available)
                .ToListAsync(cancellationToken);
            if (claimed.Count != unitIds.Count) throw new RetailException(409, "Some selected units are no longer available.");
            var allocations = DefaultAllocations(claimed.Select(unit => unit.StandalonePrice ?? 0m).ToList());
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
        catch { await RollbackAsync(transaction); throw; }
    }

    public async Task<AssemblyView> UpdateAssemblyAsync(Guid id, AssemblyUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SalePrice < 0) throw new RetailException(400, "Sale price cannot be negative.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var assembly = await db.Assemblies.Include(a => a.Units).ThenInclude(link => link.Unit)
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
        var assembly = await db.Assemblies.Include(a => a.Units).ThenInclude(link => link.Unit)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw new RetailException(404, "Assembly was not found.");
        if (assembly.Status is AssemblyStatus.Sold or AssemblyStatus.Disassembled) throw new RetailException(409, "This assembly can no longer be edited.");
        if (assembly.RowVersion != request.RowVersion) throw new RetailException(409, "The assembly was changed by another user.");
        var inputs = request.Allocations?.ToList() ?? [];
        if (inputs.Count != assembly.Units.Count || inputs.Select(item => item.UnitId).Distinct().Count() != inputs.Count)
            throw new RetailException(400, "Provide one allocation row for every component.");
        if (inputs.Any(item => item.Percent is < 0 or > 100))
            throw new RetailException(400, "Each allocation percent must be between 0 and 100.");
        if (Math.Abs(inputs.Sum(item => item.Percent) - 100m) > 0.01m)
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
        var assembly = await db.Assemblies.Include(a => a.Units).ThenInclude(link => link.Unit)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw new RetailException(404, "Assembly was not found.");
        if (assembly.Status is AssemblyStatus.Sold or AssemblyStatus.Disassembled) throw new RetailException(409, "This assembly can no longer be published.");
        if (assembly.RowVersion != rowVersion) throw new RetailException(409, "The assembly was changed by another user.");
        if (published && assembly.SalePrice <= 0) throw new RetailException(400, "A sale price is required before publishing.");
        if (published && (assembly.Units.Count == 0 || Math.Abs(assembly.Units.Sum(link => link.AllocationPercent) - 100m) > 0.01m))
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
            var assembly = await db.Assemblies.Include(a => a.Units).ThenInclude(link => link.Unit)
                .SingleOrDefaultAsync(a => a.Id == assemblyId, cancellationToken) ?? throw new RetailException(404, "Assembly was not found.");
            if (assembly.Status == AssemblyStatus.Sold) throw new RetailException(409, "A sold assembly cannot be changed.");
            if (assembly.RowVersion != rowVersion) throw new RetailException(409, "The assembly was changed by another user.");
            var link = assembly.Units.SingleOrDefault(item => item.UnitId == unitId) ?? throw new RetailException(404, "The unit is not part of this assembly.");
            assembly.Units.Remove(link);
            db.AssemblyUnits.Remove(link);
            var unit = await db.Units.SingleAsync(item => item.Id == unitId, cancellationToken);
            unit.Status = UnitStatus.Available; unit.AssemblyId = null; unit.RowVersion++;
            RebalanceAllocations(assembly.Units.ToList());
            assembly.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); assembly.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapAssembly(assembly);
        }
        catch { await RollbackAsync(transaction); throw; }
    }

    public async Task<IReadOnlyList<AssemblyView>> SwapUnitAsync(SwapRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FromAssemblyId == request.ToAssemblyId) throw new RetailException(400, "Choose two different assemblies.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assemblies = await db.Assemblies.Include(a => a.Units).ThenInclude(link => link.Unit)
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
            RebalanceAllocations(from.Units.ToList());
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            from.UpdatedAtUtc = now; from.RowVersion++; to.UpdatedAtUtc = now; to.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return [MapAssembly(from), MapAssembly(to)];
        }
        catch { await RollbackAsync(transaction); throw; }
    }

    public async Task<AssemblyView> DisassembleAsync(Guid id, long rowVersion, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var assembly = await db.Assemblies.Include(a => a.Units).ThenInclude(link => link.Unit)
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
        catch { await RollbackAsync(transaction); throw; }
    }

    public async Task<IReadOnlyList<AssemblyPublicView>> ListPublishedAssembliesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Assemblies.AsNoTracking().Include(a => a.Units).ThenInclude(link => link.Unit)
            .Where(a => a.Status == AssemblyStatus.Published).OrderByDescending(a => a.UpdatedAtUtc).ToListAsync(cancellationToken);
        return rows.Select(a => new AssemblyPublicView(a.Id, a.PublicTitleAr, a.PublicTitleEn, a.SalePrice,
            a.Units.Select(link => new PublicUnitView(link.Unit.ProductId, link.Unit.Name, link.Unit.Sku, link.Unit.Condition)).ToList())).ToList();
    }

    public async Task<OrderView> CreateOrderAsync(OrderCreateRequest request, Guid? userAccountId, CancellationToken cancellationToken = default)
    {
        var items = request.Items ?? [];
        if (items.Count is < 1 or > 50) throw new RetailException(400, "An order needs between 1 and 50 lines.");
        if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new RetailException(400, "Customer name and phone are required.");
        var fulfillment = request.FulfillmentMethod?.Trim() switch
        {
            "Delivery" => OrderFulfillment.Delivery,
            "Pickup" or "Handover" => OrderFulfillment.Pickup,
            _ => throw new RetailException(400, "Fulfillment must be Delivery or Pickup by arrangement.")
        };
        string paymentMethod;
        bool paymentRequired;
        if (fulfillment == OrderFulfillment.Delivery)
        {
            if (string.IsNullOrWhiteSpace(request.AddressText))
                throw new RetailException(400, "A delivery address is required.");
            var method = request.PaymentMethod?.Trim();
            if (method is not (OrderPaymentMethod.CashOnDelivery or OrderPaymentMethod.BankTransfer or OrderPaymentMethod.MobileWalletTransfer))
                throw new RetailException(400, "Delivery payment must be Cash on Delivery, Bank Transfer, or Mobile Wallet transfer.");
            paymentMethod = method;
            paymentRequired = true;
        }
        else
        {
            paymentMethod = OrderPaymentMethod.ToBeArranged;
            paymentRequired = false;
        }
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var order = new StoreOrder
            {
                Id = Guid.CreateVersion7(),
                Reference = $"RIGX-{now:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}",
                UserAccountId = userAccountId,
                CustomerName = request.CustomerName.Trim(),
                PhoneNumber = request.PhoneNumber.Trim(),
                AddressText = fulfillment == OrderFulfillment.Delivery ? request.AddressText?.Trim() : null,
                FulfillmentMethod = fulfillment,
                LeadSource = request.LeadSource is LeadSource.Phone or LeadSource.Social or LeadSource.Manual ? request.LeadSource! : LeadSource.Website,
                Status = OrderStatus.New,
                PaymentStatus = PaymentStatus.Unpaid,
                PaymentMethod = paymentMethod,
                PaymentRequired = paymentRequired,
                TermsAcceptedAtUtc = request.TermsAccepted == true ? now : null,
                TermsPolicyVersion = request.TermsAccepted == true ? request.TermsPolicyVersion?.Trim() : null,
                Notes = request.Notes?.Trim(),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            foreach (var line in items)
            {
                if (string.IsNullOrWhiteSpace(line.Name) || line.Quantity is < 1 or > 50 || line.UnitPrice < 0)
                    throw new RetailException(400, "Order line quantity or price is invalid.");
                var lineTotal = Math.Round(line.UnitPrice * line.Quantity, 2);
                var discount = Math.Round(line.DiscountAmount ?? 0m, 2);
                var expense = Math.Round(line.DirectExpenseAmount ?? 0m, 2);
                if (discount < 0 || discount > lineTotal)
                    throw new RetailException(400, "A line discount must be between 0 and the line total.");
                if (expense < 0)
                    throw new RetailException(400, "A line direct expense cannot be negative.");
                order.Items.Add(new OrderItem
                {
                    Id = Guid.CreateVersion7(), Name = line.Name.Trim(), Sku = line.Sku?.Trim(),
                    ProductId = line.ProductId, VariantId = line.VariantId, VariantName = line.VariantName?.Trim(),
                    ThumbnailStorageKeySnapshot = line.ThumbnailStorageKeySnapshot,
                    AssemblyId = line.AssemblyId,
                    Quantity = line.Quantity, UnitPrice = line.UnitPrice, LineTotal = lineTotal,
                    DiscountAmount = discount, DirectExpenseAmount = expense,
                    DirectExpenseNote = line.DirectExpenseNote?.Trim()
                });
            }
            order.Subtotal = order.Items.Sum(item => item.LineTotal - item.DiscountAmount);
            order.Total = order.Subtotal + order.DeliveryFee;
            order.Events.Add(new OrderEvent { Id = Guid.CreateVersion7(), EventType = "OrderCreated", Notes = request.LeadSource ?? LeadSource.Website, CreatedAtUtc = now, ActorUserAccountId = userAccountId });
            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapOrder(order);
        }
        catch { await RollbackAsync(transaction); throw; }
    }

    public async Task<IReadOnlyList<OrderView>> ListOrdersAsync(string? status, bool includeContact = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Orders.AsNoTracking().Include(order => order.Items).ThenInclude(item => item.Components).Include(order => order.Events).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) { var value = status.Trim(); query = query.Where(order => order.Status == value); }
        return (await query.OrderByDescending(order => order.CreatedAtUtc).Take(300).ToListAsync(cancellationToken))
            .Select(order => MapOrder(order, includeContact)).ToList();
    }

    public async Task<IReadOnlyList<OrderView>> ListMyOrdersAsync(Guid userAccountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Orders.AsNoTracking().Include(order => order.Items).ThenInclude(item => item.Components).Include(order => order.Events)
            .Where(order => order.UserAccountId == userAccountId).OrderByDescending(order => order.CreatedAtUtc).ToListAsync(cancellationToken);
        return rows.Select(order => MapOrder(order, true)).ToList();
    }

    public async Task<OrderView> GetMyOrderAsync(Guid id, Guid userAccountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.AsNoTracking().Include(item => item.Items).ThenInclude(item => item.Components)
            .Include(item => item.Events)
            .SingleOrDefaultAsync(item => item.Id == id && item.UserAccountId == userAccountId, cancellationToken)
            ?? throw new RetailException(404, "Order was not found.");
        return MapOrder(order, true);
    }

    public async Task<AdminOrderDetailView> GetAdminOrderDetailAsync(Guid id, bool includeSerials,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.AsNoTracking().Include(item => item.Items).ThenInclude(item => item.Components)
            .Include(item => item.Events)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RetailException(404, "Order was not found.");
        var unitIds = order.Items.SelectMany(item => item.Components).Select(component => component.UnitId).Distinct().ToList();
        var units = await db.Units.AsNoTracking().Where(unit => unitIds.Contains(unit.Id))
            .Select(unit => new AdminOrderUnitView(unit.Id, unit.ProductId, unit.Name, unit.Sku, unit.Condition,
                unit.InspectionCondition, includeSerials ? unit.SerialNumber : null, unit.Status))
            .ToListAsync(cancellationToken);
        return new AdminOrderDetailView(MapOrder(order, true), units);
    }

    public async Task<OrderView> UpdateStatusAsync(Guid id, OrderStatusRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var next = request.Status?.Trim();
        if (next is not (OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.Ready or OrderStatus.Delivered or OrderStatus.Cancelled))
            throw new RetailException(400, next == OrderStatus.Completed
                ? "Completion requires the dedicated completion action and permission."
                : "Unsupported order status.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1 FROM store_orders WHERE id = {0} FOR UPDATE", new object[] { id }, cancellationToken);
            var order = await db.Orders.Include(item => item.Items).ThenInclude(item => item.Components).Include(item => item.Events)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new RetailException(404, "Order was not found.");
            if (order.RowVersion != request.RowVersion) throw new RetailException(409, "The order was changed by another user.");
            if (order.Status == next) return MapOrder(order);
            if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed) throw new RetailException(409, "A closed order cannot change status.");
            var allowed = order.Status switch
            {
                OrderStatus.New => next is OrderStatus.Confirmed or OrderStatus.Cancelled,
                OrderStatus.Confirmed => next is OrderStatus.Preparing or OrderStatus.Cancelled,
                OrderStatus.Preparing => next is OrderStatus.Ready or OrderStatus.Cancelled,
                OrderStatus.Ready => next is OrderStatus.Delivered or OrderStatus.Cancelled,
                OrderStatus.Delivered => next is OrderStatus.Cancelled,
                _ => false
            };
            if (!allowed) throw new RetailException(409, $"The order cannot move from {order.Status} to {next}.");

            if (next == OrderStatus.Confirmed)
            {
                await ClaimStockAsync(db, order, cancellationToken);
            }
            if (next == OrderStatus.Cancelled && order.Status is OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.Ready or OrderStatus.Delivered)
            {
                await ReleaseStockAsync(db, order, cancellationToken);
            }
            order.Status = next;
            order.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); order.RowVersion++;
            order.Events.Add(new OrderEvent { Id = Guid.CreateVersion7(), EventType = $"Status:{next}", Notes = request.Notes?.Trim(), ActorUserAccountId = actorId, CreatedAtUtc = order.UpdatedAtUtc });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapOrder(order);
        }
        catch { await RollbackAsync(transaction); throw; }
    }

    public async Task<OrderView> CompleteOrderAsync(Guid id, CompleteOrderRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1 FROM store_orders WHERE id = {0} FOR UPDATE", new object[] { id }, cancellationToken);
            var order = await db.Orders.Include(item => item.Items).ThenInclude(item => item.Components).Include(item => item.Events)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new RetailException(404, "Order was not found.");
            if (order.RowVersion != request.RowVersion) throw new RetailException(409, "The order was changed by another user.");
            if (order.Status == OrderStatus.Completed) return MapOrder(order);
            if (order.Status != OrderStatus.Delivered)
                throw new RetailException(409, "Only a delivered order can be completed.");
            if (order.PaymentRequired && order.PaymentStatus != PaymentStatus.Paid)
                throw new RetailException(409, "Payment must be confirmed before completion.");
            if (!order.PaymentRequired && order.PaymentStatus != PaymentStatus.Waived)
                throw new RetailException(409, "Record an explicit payment waiver before completion.");

            await CreatePartnerEarningsAsync(db, order, cancellationToken);
            order.Status = OrderStatus.Completed;
            order.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); order.RowVersion++;
            order.Events.Add(new OrderEvent { Id = Guid.CreateVersion7(), EventType = "Status:Completed", Notes = request.Notes?.Trim(), ActorUserAccountId = actorId, CreatedAtUtc = order.UpdatedAtUtc });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapOrder(order);
        }
        catch { await RollbackAsync(transaction); throw; }
    }

    public async Task<OrderView> UpdatePaymentAsync(Guid id, PaymentStatusRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var next = request.PaymentStatus?.Trim();
        if (next is not (PaymentStatus.Unpaid or PaymentStatus.PendingVerification or PaymentStatus.Paid))
            throw new RetailException(400, "Unsupported payment status. Refunds and waivers use their dedicated actions.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.Include(item => item.Items).ThenInclude(item => item.Components).Include(item => item.Events)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new RetailException(404, "Order was not found.");
        if (order.RowVersion != request.RowVersion) throw new RetailException(409, "The order was changed by another user.");
        if (order.PaymentStatus == PaymentStatus.Waived) throw new RetailException(409, "Payment was waived for this order.");
        if (order.Status is OrderStatus.Cancelled) throw new RetailException(409, "A cancelled order cannot change payment.");
        order.PaymentStatus = next;
        order.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime(); order.RowVersion++;
        order.Events.Add(new OrderEvent { Id = Guid.CreateVersion7(), EventType = $"Payment:{next}", Notes = request.Notes?.Trim(), ActorUserAccountId = actorId, CreatedAtUtc = order.UpdatedAtUtc });
        await db.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    public async Task<OrderView> WaivePaymentAsync(Guid id, WaivePaymentRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
            throw new RetailException(400, "A waiver reason of at least 5 characters is required.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.Include(item => item.Items).ThenInclude(item => item.Components).Include(item => item.Events)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new RetailException(404, "Order was not found.");
        if (order.RowVersion != request.RowVersion) throw new RetailException(409, "The order was changed by another user.");
        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new RetailException(409, "A closed order cannot change payment.");
        if (order.PaymentStatus == PaymentStatus.Paid)
            throw new RetailException(409, "A paid order cannot be waived.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        order.PaymentRequired = false;
        order.PaymentStatus = PaymentStatus.Waived;
        order.PaymentWaivedAtUtc = now;
        order.PaymentWaivedByUserAccountId = actorId;
        order.PaymentWaiverReason = request.Reason.Trim();
        order.UpdatedAtUtc = now; order.RowVersion++;
        order.Events.Add(new OrderEvent { Id = Guid.CreateVersion7(), EventType = "Payment:Waived", Notes = request.Reason.Trim(), ActorUserAccountId = actorId, CreatedAtUtc = now });
        await db.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    public async Task<IReadOnlyList<PartnerEarningView>> ListEarningsAsync(string? partner = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.PartnerEarnings.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(partner)) { var value = partner.Trim(); query = query.Where(item => item.PartnerName == value); }
        return (await query.OrderByDescending(item => item.CreatedAtUtc).Take(500).ToListAsync(cancellationToken))
            .Select(item => new PartnerEarningView(item.Id, item.OrderId, item.OrderItemId, item.UnitId, item.PartnerName, item.Percent,
                item.RevenueAmount, item.DiscountAmount, item.DirectExpenseAmount, item.CostAmount, item.GrossProfit,
                item.Amount, item.PaidAmount, item.OffsetAmount, item.Status, item.ReversedAtUtc, item.CreatedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<PartnerAdjustmentView>> ListAdjustmentsAsync(string? partner = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.PartnerAdjustments.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(partner)) { var value = partner.Trim(); query = query.Where(item => item.PartnerName == value); }
        return (await query.OrderByDescending(item => item.CreatedAtUtc).Take(500).ToListAsync(cancellationToken))
            .Select(item => new PartnerAdjustmentView(item.Id, item.PartnerName, item.Amount, item.AppliedAmount,
                decimal.Round(item.Amount - item.AppliedAmount, 2), item.Status, item.Reason, item.OrderReturnId, item.CreatedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<PartnerLedgerView>> ListPartnerLedgerAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var earnings = await db.PartnerEarnings.AsNoTracking()
            .Where(item => item.Status == EarningStatus.Outstanding).ToListAsync(cancellationToken);
        var adjustments = await db.PartnerAdjustments.AsNoTracking()
            .Where(item => item.Status == AdjustmentStatus.Open).ToListAsync(cancellationToken);
        var names = earnings.Select(item => item.PartnerName).Concat(adjustments.Select(item => item.PartnerName)).Distinct();
        return names.Select(name =>
        {
            var due = earnings.Where(item => item.PartnerName == name && item.Amount > 0)
                .Sum(item => item.Amount - item.PaidAmount - item.OffsetAmount);
            var recoverable = adjustments.Where(item => item.PartnerName == name)
                .Sum(item => decimal.Round(item.Amount - item.AppliedAmount, 2));
            return new PartnerLedgerView(name, decimal.Round(due, 2), decimal.Round(due, 2), Math.Abs(decimal.Round(recoverable, 2)));
        }).OrderBy(item => item.PartnerName).ToList();
    }

    public async Task<ExpenseView> CreateExpenseAsync(ExpenseCreateRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            throw new RetailException(400, "Expense amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.Category))
            throw new RetailException(400, "Expense category is required.");
        var classification = request.Classification is "Direct" or "GeneralOverhead" ? request.Classification : "GeneralOverhead";
        var date = request.ExpenseDate ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var entity = new RetailExpense
        {
            Id = Guid.CreateVersion7(), ExpenseDate = date, Amount = decimal.Round(request.Amount, 2),
            Category = request.Category.Trim(), Classification = classification,
            Note = request.Note?.Trim(), OrderId = request.OrderId, UnitId = request.UnitId,
            AttachmentReference = request.AttachmentReference?.Trim(),
            CreatedByUserAccountId = actorId, CreatedAtUtc = now
        };
        db.Expenses.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return MapExpense(entity);
    }

    public async Task<IReadOnlyList<ExpenseView>> ListExpensesAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Expenses.AsNoTracking().AsQueryable();
        if (from is not null) query = query.Where(item => item.ExpenseDate >= from);
        if (to is not null) query = query.Where(item => item.ExpenseDate <= to);
        return (await query.OrderByDescending(item => item.ExpenseDate).ThenByDescending(item => item.CreatedAtUtc)
            .Take(1000).ToListAsync(cancellationToken)).Select(MapExpense).ToList();
    }

    public async Task<BusinessReportView> BuildReportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (to < from) throw new RetailException(400, "Report end date cannot precede its start date.");
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var orders = await db.Orders.AsNoTracking()
            .Include(order => order.Items).ThenInclude(item => item.Components)
            .Where(order => order.Status == OrderStatus.Completed && order.UpdatedAtUtc >= fromUtc && order.UpdatedAtUtc < toUtc)
            .ToListAsync(cancellationToken);
        var expenses = await db.Expenses.AsNoTracking()
            .Where(item => item.ExpenseDate >= from && item.ExpenseDate <= to)
            .ToListAsync(cancellationToken);
        var earnings = await db.PartnerEarnings.AsNoTracking()
            .Where(item => item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc)
            .ToListAsync(cancellationToken);
        var settlements = await db.PartnerSettlements.AsNoTracking()
            .Where(item => item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc)
            .ToListAsync(cancellationToken);
        var openAdjustments = await db.PartnerAdjustments.AsNoTracking()
            .Where(item => item.Status == AdjustmentStatus.Open)
            .ToListAsync(cancellationToken);
        var returns = await db.OrderReturns.AsNoTracking()
            .Where(item => item.Status == ReturnStatus.Inspected && item.InspectedAtUtc >= fromUtc && item.InspectedAtUtc < toUtc)
            .CountAsync(cancellationToken);

        var gross = decimal.Round(orders.Sum(order => order.Total), 2);
        var discounts = decimal.Round(orders.SelectMany(order => order.Items).Sum(item => item.DiscountAmount), 2);
        var refunds = decimal.Round(orders.Sum(order => order.RefundedAmount), 2);
        var cogs = decimal.Round(orders.SelectMany(order => order.Items).SelectMany(item => item.Components).Sum(item => item.CostAmount), 2);
        var itemDirect = decimal.Round(orders.SelectMany(order => order.Items).Sum(item => item.DirectExpenseAmount), 2);
        var netItem = decimal.Round(orders.SelectMany(order => order.Items).SelectMany(item => item.Components).Sum(item => item.NetProfit) - refunds, 2);
        var additionalDirect = decimal.Round(expenses.Where(item => item.Classification == "Direct").Sum(item => item.Amount), 2);
        var overhead = decimal.Round(expenses.Where(item => item.Classification == "GeneralOverhead").Sum(item => item.Amount), 2);
        var accrued = decimal.Round(earnings.Where(item => item.Amount > 0).Sum(item => item.Amount), 2);
        var paid = decimal.Round(settlements.Sum(item => item.Amount), 2);
        var recoverable = Math.Abs(decimal.Round(openAdjustments.Sum(item => item.Amount - item.AppliedAmount), 2));
        return new BusinessReportView(from, to, gross, discounts, refunds, cogs, itemDirect, additionalDirect,
            netItem, overhead, decimal.Round(netItem - additionalDirect - overhead, 2), accrued, paid, recoverable,
            orders.Count, returns);
    }

    private static ExpenseView MapExpense(RetailExpense item) =>
        new(item.Id, item.ExpenseDate, item.Amount, item.Category, item.Classification, item.Note,
            item.OrderId, item.UnitId, item.AttachmentReference, item.CreatedByUserAccountId,
            item.CreatedAtUtc, item.RowVersion);

    public async Task<IReadOnlyList<PartnerAgreement>> ListAgreementsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.PartnerAgreements.AsNoTracking().OrderByDescending(item => item.EffectiveFrom).ToListAsync(cancellationToken);
    }

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

    public async Task<PartnerAgreement> UpsertAgreementAsync(PartnerAgreementRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PartnerName) || request.DefaultPercent is < 0 or > 100)
            throw new RetailException(400, "Partner name and a percentage between 0 and 100 are required.");
        if (request.EffectiveTo is not null && request.EffectiveTo < request.EffectiveFrom)
            throw new RetailException(400, "The agreement end date cannot precede its start date.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var agreement = await db.PartnerAgreements.SingleOrDefaultAsync(item => item.PartnerName == request.PartnerName.Trim(), cancellationToken);
        if (agreement is null)
        {
            agreement = new PartnerAgreement { Id = Guid.CreateVersion7(), PartnerName = request.PartnerName.Trim(), CreatedAtUtc = now };
            db.PartnerAgreements.Add(agreement);
        }
        agreement.DefaultPercent = request.DefaultPercent; agreement.IsActive = request.IsActive;
        agreement.LossSharingEnabled = request.LossSharingEnabled;
        agreement.EffectiveFrom = request.EffectiveFrom; agreement.EffectiveTo = request.EffectiveTo;
        agreement.UpdatedAtUtc = now; agreement.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return agreement;
    }

    public async Task<PartnerSettlementView> SettleAsync(SettlementRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PartnerName) || request.Amount <= 0)
            throw new RetailException(400, "Partner and a positive settlement amount are required.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var name = request.PartnerName.Trim();
            var earnings = await db.PartnerEarnings
                .Where(item => item.PartnerName == name && item.Status == EarningStatus.Outstanding && item.Amount > 0)
                .OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
            var available = earnings.Sum(item => item.Amount - item.PaidAmount - item.OffsetAmount);
            if (request.Amount > available) throw new RetailException(400, "Settlement exceeds the cash due after offsets.");
            var remaining = request.Amount;
            foreach (var earning in earnings)
            {
                if (remaining <= 0) break;
                var due = earning.Amount - earning.PaidAmount - earning.OffsetAmount;
                var applied = Math.Min(due, remaining);
                earning.PaidAmount += applied; remaining -= applied;
                if (earning.PaidAmount + earning.OffsetAmount >= earning.Amount) earning.Status = EarningStatus.Paid;
            }
            var settlement = new PartnerSettlement
            {
                Id = Guid.CreateVersion7(), PartnerName = name, Amount = request.Amount,
                Method = request.Method?.Trim() is { Length: > 0 } method ? method : "Cash",
                Reference = request.Reference?.Trim(), Note = request.Note?.Trim(),
                ActorUserAccountId = actorId, CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
            };
            db.PartnerSettlements.Add(settlement);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new PartnerSettlementView(settlement.Id, settlement.PartnerName, settlement.Amount, settlement.OffsetApplied, settlement.Method, settlement.Reference, settlement.Note, settlement.CreatedAtUtc);
        }
        catch { await RollbackAsync(transaction); throw; }
    }

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
        catch { await RollbackAsync(transaction); throw; }
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
            var order = await db.Orders.Include(item => item.Items).ThenInclude(item => item.Components).Include(item => item.Events)
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

            var earnings = await db.PartnerEarnings
                .Where(entry => entry.OrderItemId == item.Id && entry.UnitId == unit.Id)
                .ToListAsync(cancellationToken);
            var touchedPartners = new HashSet<string>(StringComparer.Ordinal);
            foreach (var earning in earnings.Where(entry => entry.Status != EarningStatus.Reversed))
            {
                var settled = decimal.Round(earning.PaidAmount + earning.OffsetAmount, 2);
                if (settled > 0)
                {
                    db.PartnerAdjustments.Add(new PartnerAdjustment
                    {
                        Id = Guid.CreateVersion7(), PartnerName = earning.PartnerName,
                        PartnerEarningId = earning.Id, OrderReturnId = entity.Id,
                        Amount = -settled, AppliedAmount = 0, Status = AdjustmentStatus.Open,
                        Reason = $"Return recovery for {item.Name}", CreatedAtUtc = now
                    });
                }
                earning.Status = EarningStatus.Reversed;
                earning.ReversedAtUtc = now;
                earning.ReversalReason = $"Order return {entity.Id} ({condition})";
                touchedPartners.Add(earning.PartnerName);
            }
            await db.SaveChangesAsync(cancellationToken);
            foreach (var partnerName in touchedPartners)
                await ApplyRecoverableAsync(db, partnerName, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapReturn(entity);
        }
        catch { await RollbackAsync(transaction); throw; }
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
        catch { await RollbackAsync(transaction); throw; }
    }

    private async Task ClaimStockAsync(RetailDbContext db, StoreOrder order, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            if (item.AssemblyId is not null)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "SELECT 1 FROM assemblies WHERE id = {0} FOR UPDATE",
                    new object[] { item.AssemblyId.Value }, cancellationToken);
                var assembly = await db.Assemblies.Include(a => a.Units).ThenInclude(link => link.Unit)
                    .SingleOrDefaultAsync(a => a.Id == item.AssemblyId && a.Status == AssemblyStatus.Published, cancellationToken)
                    ?? throw new RetailException(409, "The selected build is no longer available.");
                if (assembly.Units.Count == 0 || Math.Abs(assembly.Units.Sum(link => link.AllocationPercent) - 100m) > 0.01m)
                    throw new RetailException(409, "The build allocation is incomplete.");
                if (assembly.Units.Any(link => link.Unit.Status is not (UnitStatus.InAssembly or UnitStatus.Available)))
                    throw new RetailException(409, "One or more build components are no longer available.");
                var lines = assembly.Units.OrderBy(link => link.UnitId).ToList();
                var revenues = AllocateAmount(item.LineTotal, lines.Select(link => link.AllocationPercent).ToList());
                var discounts = AllocateAmount(item.DiscountAmount, lines.Select(link => link.AllocationPercent).ToList());
                var expenses = AllocateAmount(item.DirectExpenseAmount, lines.Select(link => link.AllocationPercent).ToList());
                for (var index = 0; index < lines.Count; index++)
                {
                    var link = lines[index];
                    var cost = link.Unit.AcquisitionCost;
                    item.Components.Add(new OrderItemComponent
                    {
                        Id = Guid.CreateVersion7(), OrderItemId = item.Id, UnitId = link.UnitId,
                        AllocationPercent = link.AllocationPercent,
                        RevenueAmount = revenues[index], DiscountAmount = discounts[index],
                        DirectExpenseAmount = expenses[index], CostAmount = cost,
                        NetProfit = Net(revenues[index], discounts[index], expenses[index], cost)
                    });
                    link.Unit.Status = UnitStatus.Sold; link.Unit.RowVersion++;
                }
                item.UnitCostSnapshot = decimal.Round(item.Components.Sum(component => component.CostAmount), 2);
                item.NetProfit = decimal.Round(item.Components.Sum(component => component.NetProfit), 2);
                assembly.Status = AssemblyStatus.Sold;
                continue;
            }
            if (item.ProductId is null)
                throw new RetailException(409, "A stock line is missing its product reference.");
            var lockedIds = await db.Database.SqlQuery<Guid>(
                $"SELECT id AS \"Value\" FROM inventory_units WHERE product_id = {item.ProductId.Value} AND status = 'Available' ORDER BY received_at_utc LIMIT {item.Quantity} FOR UPDATE SKIP LOCKED")
                .ToListAsync(cancellationToken);
            if (lockedIds.Count != item.Quantity)
                throw new RetailException(409, "Not enough available stock to confirm this order.");
            var available = await db.Units
                .Where(unit => lockedIds.Contains(unit.Id) && unit.Status == UnitStatus.Available)
                .ToListAsync(cancellationToken);
            if (available.Count != item.Quantity)
                throw new RetailException(409, "Not enough available stock to confirm this order.");
            var percents = EqualPercentages(available.Count);
            var lineRevenues = AllocateAmount(item.LineTotal, percents);
            var lineDiscounts = AllocateAmount(item.DiscountAmount, percents);
            var lineExpenses = AllocateAmount(item.DirectExpenseAmount, percents);
            for (var index = 0; index < available.Count; index++)
            {
                var unit = available[index];
                item.Components.Add(new OrderItemComponent
                {
                    Id = Guid.CreateVersion7(), OrderItemId = item.Id, UnitId = unit.Id,
                    AllocationPercent = percents[index],
                    RevenueAmount = lineRevenues[index], DiscountAmount = lineDiscounts[index],
                    DirectExpenseAmount = lineExpenses[index], CostAmount = unit.AcquisitionCost,
                    NetProfit = Net(lineRevenues[index], lineDiscounts[index], lineExpenses[index], unit.AcquisitionCost)
                });
                unit.Status = UnitStatus.Sold; unit.RowVersion++;
            }
            item.UnitId = available[0].Id;
            item.UnitCostSnapshot = decimal.Round(item.Components.Sum(component => component.CostAmount), 2);
            item.NetProfit = decimal.Round(item.Components.Sum(component => component.NetProfit), 2);
        }
    }

    private static async Task ReleaseStockAsync(RetailDbContext db, StoreOrder order, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            var unitIds = item.Components.Select(component => component.UnitId).ToList();
            var units = unitIds.Count > 0
                ? await db.Units.Where(unit => unit.Status == UnitStatus.Sold && unitIds.Contains(unit.Id)).ToListAsync(cancellationToken)
                : await db.Units.Where(unit => unit.Status == UnitStatus.Sold &&
                    (unit.Id == item.UnitId || (item.AssemblyId != null && unit.AssemblyId == item.AssemblyId)))
                    .ToListAsync(cancellationToken);
            foreach (var unit in units) { unit.Status = item.AssemblyId is null ? UnitStatus.Available : UnitStatus.InAssembly; unit.RowVersion++; }
            if (item.AssemblyId is not null)
            {
                var assembly = await db.Assemblies.SingleOrDefaultAsync(a => a.Id == item.AssemblyId, cancellationToken);
                if (assembly is not null) { assembly.Status = AssemblyStatus.Published; assembly.RowVersion++; }
            }
        }
    }

    private async Task CreatePartnerEarningsAsync(RetailDbContext db, StoreOrder order, CancellationToken cancellationToken)
    {
        var orderDate = DateOnly.FromDateTime(order.CreatedAtUtc.UtcDateTime);
        var agreement = await db.PartnerAgreements.AsNoTracking()
            .Where(item => item.IsActive && item.EffectiveFrom <= orderDate && (item.EffectiveTo == null || item.EffectiveTo >= orderDate))
            .OrderByDescending(item => item.EffectiveFrom).ThenByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var allocations = await db.PartnerUnitAllocations.AsNoTracking()
            .Where(item => item.IsActive).ToListAsync(cancellationToken);
        var unitIds = order.Items.SelectMany(item => item.Components).Select(component => component.UnitId).Distinct().ToList();
        var units = await db.Units.AsNoTracking().Where(unit => unitIds.Contains(unit.Id))
            .ToDictionaryAsync(unit => unit.Id, cancellationToken);
        var existing = (await db.PartnerEarnings.AsNoTracking()
                .Where(earning => earning.OrderId == order.Id)
                .Select(earning => new { earning.OrderItemComponentId, earning.PartnerName })
                .ToListAsync(cancellationToken))
            .Select(entry => (entry.OrderItemComponentId, entry.PartnerName))
            .ToHashSet();
        var touchedPartners = new HashSet<string>(StringComparer.Ordinal);

        void AddEarning(OrderItem item, OrderItemComponent component, string partnerName, decimal percent, bool lossSharing)
        {
            if (string.IsNullOrWhiteSpace(partnerName) || percent <= 0) return;
            if (!existing.Add((component.Id, partnerName))) return;
            var profit = decimal.Round(component.NetProfit, 2);
            var amount = decimal.Round(profit * percent / 100m, 2);
            if (amount < 0 && !lossSharing) amount = 0;
            db.PartnerEarnings.Add(new PartnerEarning
            {
                Id = Guid.CreateVersion7(), OrderItemId = item.Id, OrderItemComponentId = component.Id,
                UnitId = component.UnitId, OrderId = order.Id, PartnerName = partnerName,
                Percent = percent, LossSharingEnabled = lossSharing,
                RevenueAmount = component.RevenueAmount, DiscountAmount = component.DiscountAmount,
                DirectExpenseAmount = component.DirectExpenseAmount, CostAmount = component.CostAmount,
                GrossProfit = profit, Amount = amount, PaidAmount = 0, OffsetAmount = 0,
                Status = EarningStatus.Outstanding, CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
            });
            touchedPartners.Add(partnerName);
        }

        foreach (var item in order.Items)
        {
            foreach (var component in item.Components)
            {
                if (!units.TryGetValue(component.UnitId, out var unit)) continue;
                var scoped = allocations.Where(allocation => allocation.UnitId == component.UnitId).ToList();
                if (scoped.Count == 0 && unit.ProductId is not null)
                    scoped = allocations.Where(allocation => allocation.UnitId == null && allocation.ProductId == unit.ProductId).ToList();
                if (scoped.Count == 0 && !string.IsNullOrWhiteSpace(unit.Category))
                    scoped = allocations.Where(allocation => allocation.UnitId == null && allocation.ProductId == null &&
                        allocation.Category == unit.Category).ToList();
                if (scoped.Count > 0)
                {
                    if (scoped.Sum(allocation => allocation.Percent) > 100.01m)
                        throw new RetailException(409, "Partner allocations for a sold unit exceed 100%.");
                    foreach (var allocation in scoped)
                        AddEarning(item, component, allocation.PartnerName, allocation.Percent, allocation.LossSharingEnabled);
                    continue;
                }
                if (agreement is null) continue;
                AddEarning(item, component, agreement.PartnerName,
                    unit.PartnerPercentOverride ?? agreement.DefaultPercent, agreement.LossSharingEnabled);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        var losses = await db.PartnerEarnings
            .Where(item => item.OrderId == order.Id && item.Amount < 0 && item.Status == EarningStatus.Outstanding)
            .ToListAsync(cancellationToken);
        foreach (var loss in losses)
        {
            db.PartnerAdjustments.Add(new PartnerAdjustment
            {
                Id = Guid.CreateVersion7(), PartnerName = loss.PartnerName, PartnerEarningId = loss.Id,
                Amount = loss.Amount, AppliedAmount = 0, Status = AdjustmentStatus.Open,
                Reason = "Loss sharing converted to recoverable partner balance",
                CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
            });
            loss.Status = EarningStatus.Reversed;
            loss.ReversedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
            loss.ReversalReason = "Converted to recoverable balance under explicit loss sharing";
            touchedPartners.Add(loss.PartnerName);
        }
        await db.SaveChangesAsync(cancellationToken);
        foreach (var partnerName in touchedPartners)
            await ApplyRecoverableAsync(db, partnerName, cancellationToken);
    }

    private async Task ApplyRecoverableAsync(RetailDbContext db, string partnerName, CancellationToken cancellationToken)
    {
        var adjustments = await db.PartnerAdjustments
            .Where(item => item.PartnerName == partnerName && item.Status == AdjustmentStatus.Open && item.Amount < 0)
            .OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
        if (adjustments.Count == 0) return;
        var earnings = await db.PartnerEarnings
            .Where(item => item.PartnerName == partnerName && item.Status == EarningStatus.Outstanding && item.Amount > 0)
            .OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
        foreach (var adjustment in adjustments)
        {
            var remaining = decimal.Round(-(adjustment.Amount - adjustment.AppliedAmount), 2);
            foreach (var earning in earnings)
            {
                if (remaining <= 0) break;
                var due = decimal.Round(earning.Amount - earning.PaidAmount - earning.OffsetAmount, 2);
                if (due <= 0) continue;
                var offset = Math.Min(due, remaining);
                earning.OffsetAmount += offset;
                adjustment.AppliedAmount -= offset;
                remaining -= offset;
                if (earning.PaidAmount + earning.OffsetAmount >= earning.Amount) earning.Status = EarningStatus.Paid;
            }
            if (decimal.Round(adjustment.Amount - adjustment.AppliedAmount, 2) >= 0) adjustment.Status = AdjustmentStatus.Applied;
        }
    }

    private static decimal Net(decimal revenue, decimal discount, decimal expense, decimal cost) =>
        decimal.Round(revenue - discount - expense - cost, 2);

    private static List<decimal> DefaultAllocations(IReadOnlyList<decimal> prices)
    {
        if (prices.Count == 0) return [];
        var total = prices.Sum();
        if (total <= 0) return EqualPercentages(prices.Count);
        var result = new List<decimal>();
        var assigned = 0m;
        for (var index = 0; index < prices.Count - 1; index++)
        {
            var percent = decimal.Round(prices[index] / total * 100m, 2);
            result.Add(percent); assigned += percent;
        }
        result.Add(decimal.Round(100m - assigned, 2));
        return result;
    }

    private static List<decimal> EqualPercentages(int count)
    {
        if (count <= 0) return [];
        var result = new List<decimal>();
        var assigned = 0m;
        for (var index = 0; index < count - 1; index++)
        {
            var percent = decimal.Round(100m / count, 2);
            result.Add(percent); assigned += percent;
        }
        result.Add(decimal.Round(100m - assigned, 2));
        return result;
    }

    private static void RebalanceAllocations(IReadOnlyList<AssemblyUnit> units)
    {
        if (units.Count == 0) return;
        var weights = units.Select(unit => unit.AllocationPercent).ToList();
        var total = weights.Sum();
        if (total <= 0) { weights = Enumerable.Repeat(1m, units.Count).ToList(); total = units.Count; }
        var assigned = 0m;
        for (var index = 0; index < units.Count; index++)
        {
            var percent = index == units.Count - 1
                ? decimal.Round(100m - assigned, 2)
                : decimal.Round(weights[index] / total * 100m, 2);
            units[index].AllocationPercent = percent;
            assigned += percent;
        }
    }

    private static List<decimal> AllocateAmount(decimal total, IReadOnlyList<decimal> percents)
    {
        var result = new List<decimal>();
        var assigned = 0m;
        for (var index = 0; index < percents.Count; index++)
        {
            if (index == percents.Count - 1)
            {
                result.Add(decimal.Round(total - assigned, 2));
                break;
            }
            var amount = decimal.Round(total * percents[index] / 100m, 2);
            result.Add(amount); assigned += amount;
        }
        return result;
    }

    private static UnitView MapUnit(InventoryUnit unit, bool includeCosts = true, bool includeSerials = true) => new(unit.Id, unit.ProductId, unit.Name, unit.Sku,
        unit.Brand, unit.Model, unit.Category, unit.Description, unit.ExternalCode, unit.SpecificationsJson,
        unit.Condition, unit.InspectionCondition, includeSerials ? unit.SerialNumber : null,
        includeCosts ? unit.SourceName : null, includeCosts ? unit.SourceType : null,
        includeCosts ? unit.PurchaseKind : null, includeCosts ? unit.PurchaseDate : null,
        includeCosts ? unit.AcquisitionCost : null, includeCosts ? unit.StandalonePrice : null,
        includeCosts ? unit.PartnerPercentOverride : null, unit.Status, unit.AssemblyId, unit.ReceivedAtUtc);

    private static AssemblyView MapAssembly(Assembly assembly, bool includeCosts = true)
    {
        var units = assembly.Units.Select(link => new AssemblyUnitView(link.UnitId, link.Unit.Name, link.Unit.Sku,
            link.Unit.Condition, includeCosts ? link.Unit.AcquisitionCost : null,
            includeCosts ? link.Unit.StandalonePrice : null, link.AllocationPercent,
            includeCosts ? decimal.Round(assembly.SalePrice * link.AllocationPercent / 100m, 2) : null)).ToList();
        return new AssemblyView(assembly.Id, assembly.Name, assembly.PublicTitleAr, assembly.PublicTitleEn,
            assembly.SalePrice, assembly.Status, units,
            includeCosts ? decimal.Round(units.Sum(unit => unit.AcquisitionCost ?? 0m), 2) : null,
            includeCosts ? decimal.Round(units.Sum(unit => unit.AllocatedAmount ?? 0m), 2) : null, assembly.RowVersion);
    }

    private static OrderView MapOrder(StoreOrder order, bool includeContact = true) => new(order.Id, order.Reference, order.UserAccountId,
        order.CustomerName, includeContact ? order.PhoneNumber : Mask(order.PhoneNumber), includeContact ? order.AddressText : null,
        order.FulfillmentMethod, order.LeadSource, order.Status, order.PaymentStatus, order.PaymentMethod, order.PaymentRequired,
        order.PaymentWaivedAtUtc, order.PaymentWaivedByUserAccountId, order.PaymentWaiverReason, order.Subtotal, order.DeliveryFee,
        order.Total, order.RefundedAmount, order.Notes, order.CreatedAtUtc, order.UpdatedAtUtc, order.RowVersion,
        order.Items.Select(item => new OrderItemView(item.Id, item.ProductId, item.VariantId, item.VariantName,
            item.AssemblyId, item.Name, item.Sku, item.ThumbnailStorageKeySnapshot,
            item.Quantity, item.UnitPrice, item.LineTotal, item.DiscountAmount, item.DirectExpenseAmount,
            item.DirectExpenseNote, item.NetProfit,
            item.Components.Select(component => new OrderComponentView(component.Id, component.UnitId,
                component.AllocationPercent, component.RevenueAmount, component.DiscountAmount,
                component.DirectExpenseAmount, component.CostAmount, component.NetProfit)).ToList())).ToList(),
        order.Events.OrderBy(entry => entry.CreatedAtUtc).Select(entry => new OrderEventView(entry.EventType, entry.Notes, entry.ActorUserAccountId, entry.CreatedAtUtc)).ToList());

    private static string Mask(string value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : new string('•', Math.Min(12, Math.Max(4, value.Length)));

    private static ReturnView MapReturn(OrderReturn entity) => new(entity.Id, entity.OrderId, entity.OrderItemId,
        entity.UnitId, entity.RefundAmount, entity.Reason, entity.Status, entity.InspectionCondition,
        entity.InspectionNotes, entity.InspectedByUserAccountId, entity.InspectedAtUtc, entity.CreatedAtUtc, entity.RowVersion);

    private static async Task RollbackAsync(IDbContextTransaction transaction)
    {
        try { await transaction.RollbackAsync(CancellationToken.None); } catch { /* preserve original */ }
    }
}

public static class RetailRegistration
{
    public static IServiceCollection AddRetailModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        services.AddDbContextFactory<RetailDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<RetailService>();
        return services;
    }
}
