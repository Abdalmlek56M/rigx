using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Store sales (POS): the sale price is typed at sale time (business §5), a warning shows below cost, the discount is
/// limited per permission, payments may be split, and a build may be sold whole or part by part.
/// </summary>
public sealed class PosService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    /// <summary>Totals, cost/profit (with cost permission), warnings and the parts a split sale returns to stock; saves nothing.</summary>
    public async Task<PosPreview> PreviewAsync(PosSaleRequest request, PosPolicy policy, bool includeCosts, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = await PriceAsync(db, request, cancellationToken);
        var lines = new List<PosPreviewLine>();
        var warnings = new List<string>();
        foreach (var item in items)
        {
            var cost = await CostOfAsync(db, item, cancellationToken);
            var sale = item.LineTotal - item.DiscountAmount;
            var belowCost = sale < cost;
            if (belowCost) warnings.Add($"{item.Name} ({item.Code}) is sold below its cost.");
            lines.Add(new PosPreviewLine(item.Name, item.Code, item.Quantity, item.UnitPrice, decimal.Round(sale / item.Quantity, 2),
                includeCosts ? cost : null, belowCost));
        }
        var discount = items.Sum(item => item.DiscountAmount);
        var linesTotal = items.Sum(item => item.LineTotal);
        try { OrderRules.EnsureDiscountAllowed(linesTotal, discount, policy.MaxDiscountPercent, policy.UnlimitedDiscount); }
        catch (RetailException exception) { warnings.Add(exception.Message); }
        var subtotal = linesTotal - discount;
        var fulfillment = OrderRules.Fulfillment(request.FulfillmentMethod);
        var fee = ShippingOf(request, fulfillment, policy).Fee;
        var costTotal = lines.Sum(line => line.Cost ?? 0m);
        return new PosPreview(lines, linesTotal, discount, subtotal, fee, subtotal + fee, includeCosts ? costTotal : null,
            includeCosts ? subtotal - costTotal : null, warnings, await ReturningPartsAsync(db, items, cancellationToken));
    }

    public async Task<OrderView> SellAsync(PosSaleRequest request, PosPolicy policy, Guid actorId, CancellationToken cancellationToken = default)
    {
        var fulfillment = OrderRules.Fulfillment(request.FulfillmentMethod);
        var address = OrderRules.Address(request.Address, fulfillment);
        var (usage, usageNote) = OrderUsage.Parse(request.Usage, request.UsageNote);
        var shipping = ShippingOf(request, fulfillment, policy);
        var payments = (request.Payments ?? [])
            .Select(payment => (Method: OrderRules.PaymentMethod(payment.Method, OrderChannel.Store), payment.Amount)).ToList();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var items = await PriceAsync(db, request, cancellationToken);
            OrderRules.EnsureDiscountAllowed(items.Sum(item => item.LineTotal), items.Sum(item => item.DiscountAmount),
                policy.MaxDiscountPercent, policy.UnlimitedDiscount);
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var order = OrderDrafting.NewOrder(request.Customer, fulfillment, address, OrderChannel.Store, items, shipping, now);
            order.PaymentMethod = payments.Select(payment => payment.Method).Distinct().Count() switch
            {
                0 => OrderPaymentMethod.Cash,
                1 => payments[0].Method,
                _ => OrderPaymentMethod.Split
            };
            order.LeadSource = OrderRules.Optional(request.LeadSource, 60, "Lead source");
            order.Usage = usage;
            order.UsageNote = usageNote;
            order.Notes = OrderRules.Optional(request.Notes, 2000, "Notes");
            order.IsBlacklisted = await BlacklistService.MatchesAsync(db, order, cancellationToken);
            order.Events.Add(OrderMapping.Event("OrderCreated", OrderChannel.Store, actorId, now));
            foreach (var payment in payments) OrderPaymentService.Record(order, payment.Method, payment.Amount, null, actorId, now);
            if (request.Confirm)
            {
                OrderRules.EnsureCanConfirm(order);
                await OrderStockAllocator.ClaimAsync(db, order, cancellationToken);
                OrderRules.ValidateReservationHours(policy.Reservation.Hours);
                order.Status = OrderStatus.Confirmed;
                order.ReservedUntilUtc = now.AddHours(policy.Reservation.Hours);
                order.ReservationAutoRelease = policy.Reservation.AutoRelease;
                order.Events.Add(OrderMapping.Event($"Status:{OrderStatus.Confirmed}", null, actorId, now));
            }
            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OrderMapping.ToView(order);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    /// <summary>"عميل قديم": the latest details used with this phone number, for the POS customer search.</summary>
    public async Task<PosCustomerView?> FindCustomerAsync(string? phone, CancellationToken cancellationToken = default)
    {
        var digits = OrderRules.Phone(phone);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var orders = db.Orders.AsNoTracking().Where(order => order.PhoneNumber == digits);
        var latest = await orders.OrderByDescending(order => order.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        return latest is null ? null : new PosCustomerView(latest.CustomerName, latest.PhoneNumber, latest.PhoneHasWhatsApp,
            latest.CustomerEmail, OrderMapping.AddressOf(latest), await orders.CountAsync(cancellationToken));
    }

    /// <summary>
    /// The fee typed by staff wins; otherwise the fee of the address governorate in the shipping settings (the delivery time always comes
    /// from there). Until a governorate is chosen (preview) the rate is empty; selling needs a valid address anyway.
    /// </summary>
    private static ShippingRate ShippingOf(PosSaleRequest request, string fulfillment, PosPolicy policy)
    {
        if (fulfillment != OrderFulfillment.Delivery) return new ShippingRate(0m, null);
        if (request.DeliveryFee is decimal typed && (typed < 0m || decimal.Round(typed, 2) != typed))
            throw new RetailException(400, "The shipping fee must be zero or more.");
        var governorate = request.Address?.Governorate?.Trim().ToUpperInvariant();
        var rate = EgyptGovernorates.IsKnown(governorate) ? policy.ShippingFor(governorate!) : new ShippingRate(0m, null);
        return request.DeliveryFee is decimal fee ? rate with { Fee = fee } : rate;
    }

    private static async Task<List<OrderItem>> PriceAsync(RetailDbContext db, PosSaleRequest request, CancellationToken cancellationToken)
    {
        var items = await OrderDrafting.PriceLinesAsync(db, request.Items, OrderChannel.Store, null, cancellationToken);
        OrderDrafting.ApplyOrderDiscount(items, request.Discount);
        return items;
    }

    /// <summary>Cost of what the line would take: the build's parts, the chosen part, or the oldest available units.</summary>
    private static async Task<decimal> CostOfAsync(RetailDbContext db, OrderItem item, CancellationToken cancellationToken)
    {
        if (item.UnitId is Guid unitId)
            return await db.Units.Where(unit => unit.Id == unitId).Select(unit => unit.AcquisitionCost).SingleAsync(cancellationToken);
        if (item.AssemblyId is Guid assemblyId)
            return await db.AssemblyUnits.Where(link => link.AssemblyId == assemblyId).SumAsync(link => link.Unit.AcquisitionCost, cancellationToken);
        return await db.Units.Where(unit => unit.StockItemId == item.StockItemId && unit.Status == UnitStatus.Available)
            .OrderBy(unit => unit.ReceivedAtUtc).Take(item.Quantity).SumAsync(unit => unit.AcquisitionCost, cancellationToken);
    }

    private static async Task<IReadOnlyList<PosReturningPart>> ReturningPartsAsync(RetailDbContext db, List<OrderItem> items,
        CancellationToken cancellationToken)
    {
        var split = items.Where(item => item.AssemblyId is not null && item.UnitId is not null).ToList();
        if (split.Count == 0) return [];
        var buildIds = split.Select(item => item.AssemblyId!.Value).Distinct().ToList();
        var sold = split.Select(item => item.UnitId!.Value).ToList();
        return await db.AssemblyUnits.AsNoTracking()
            .Where(link => buildIds.Contains(link.AssemblyId) && !sold.Contains(link.UnitId))
            .OrderBy(link => link.UnitId)
            .Select(link => new PosReturningPart(link.UnitId, link.Unit.Name, link.Unit.StockItem.Code)).ToListAsync(cancellationToken);
    }
}
