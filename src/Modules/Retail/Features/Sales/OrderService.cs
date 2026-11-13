using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Orders: creation (fulfillment/payment rules, line validation) and order reads for admin and customers.
/// </summary>
public sealed class OrderService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
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
                    StockItemId = line.StockItemId,
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
            return OrderMapping.ToView(order);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    public async Task<IReadOnlyList<OrderView>> ListOrdersAsync(string? status, bool includeContact = true, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Orders.AsNoTracking().WithDetails().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) { var value = status.Trim(); query = query.Where(order => order.Status == value); }
        return (await query.OrderByDescending(order => order.CreatedAtUtc).Take(300).ToListAsync(cancellationToken))
            .Select(order => OrderMapping.ToView(order, includeContact)).ToList();
    }

    public async Task<IReadOnlyList<OrderView>> ListMyOrdersAsync(Guid userAccountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Orders.AsNoTracking().WithDetails()
            .Where(order => order.UserAccountId == userAccountId).OrderByDescending(order => order.CreatedAtUtc).ToListAsync(cancellationToken);
        return rows.Select(order => OrderMapping.ToView(order, true)).ToList();
    }

    public async Task<OrderView> GetMyOrderAsync(Guid id, Guid userAccountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.AsNoTracking().WithDetails()
            .SingleOrDefaultAsync(item => item.Id == id && item.UserAccountId == userAccountId, cancellationToken)
            ?? throw new RetailException(404, "Order was not found.");
        return OrderMapping.ToView(order, true);
    }

    public async Task<AdminOrderDetailView> GetAdminOrderDetailAsync(Guid id, bool includeSerials,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.AsNoTracking().WithDetails()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RetailException(404, "Order was not found.");
        var unitIds = order.Items.SelectMany(item => item.Components).Select(component => component.UnitId).Distinct().ToList();
        var units = await db.Units.AsNoTracking().Where(unit => unitIds.Contains(unit.Id))
            .Select(unit => new AdminOrderUnitView(unit.Id, unit.ProductId, unit.Name, unit.StockItem.Code, unit.Condition,
                unit.InspectionCondition, includeSerials ? unit.SerialNumber : null, unit.Status))
            .ToListAsync(cancellationToken);
        return new AdminOrderDetailView(OrderMapping.ToView(order, true), units);
    }
}
