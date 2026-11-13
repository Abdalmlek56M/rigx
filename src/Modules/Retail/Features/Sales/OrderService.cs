using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Website checkout (guest or signed in) and order reads for admin and customers.
/// </summary>
public sealed class OrderService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    /// <summary>
    /// Creates a <c>New</c> order from the cart: lines are re-priced from published items/builds, the fee and upfront part
    /// come from the store settings (<paramref name="policy"/>), and a blacklisted customer's order is flagged.
    /// </summary>
    public async Task<OrderView> CreateOnlineAsync(CheckoutRequest request, CheckoutPolicy policy, CancellationToken cancellationToken = default)
    {
        if (request.TermsAccepted != true) throw new RetailException(400, "Terms and Privacy consent is required.");
        var fulfillment = OrderRules.Fulfillment(request.FulfillmentMethod);
        var address = OrderRules.Address(request.Address, fulfillment);
        var method = OrderRules.PaymentMethod(request.PaymentMethod, OrderChannel.Online);
        if (!policy.PaymentMethods.Contains(method)) throw new RetailException(400, "This payment method is not available.");
        var (usage, usageNote) = OrderUsage.Parse(request.Usage, request.UsageNote);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = await OrderDrafting.PriceLinesAsync(db, request.Items, OrderChannel.Online, cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var order = OrderDrafting.NewOrder(request.Customer, fulfillment, address, OrderChannel.Online, items, policy.Shipping, now);
        if (order.Subtotal < policy.MinimumOrder) throw new RetailException(400, $"The minimum order is {policy.MinimumOrder:0.##} EGP.");
        order.UserAccountId = policy.UserAccountId;
        order.PaymentMethod = method;
        order.PaymentRequired = fulfillment == OrderFulfillment.Delivery;
        order.UpfrontAmount = fulfillment == OrderFulfillment.Delivery ? policy.UpfrontFor(order.Subtotal) : 0m;
        order.LeadSource = OrderRules.Optional(request.LeadSource, 60, "Lead source");
        order.Usage = usage;
        order.UsageNote = usageNote;
        order.Notes = OrderRules.Optional(request.Notes, 2000, "Notes");
        order.TermsAcceptedAtUtc = now;
        order.TermsPolicyVersion = policy.TermsPolicyVersion;
        order.IsBlacklisted = await BlacklistService.MatchesAsync(db, order, cancellationToken);
        order.Events.Add(OrderMapping.Event("OrderCreated", OrderChannel.Online, policy.UserAccountId, now));
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return OrderMapping.ToView(order);
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
