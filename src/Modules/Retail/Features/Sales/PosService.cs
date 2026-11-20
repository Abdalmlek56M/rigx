using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Store sales (POS): the sale price is typed at sale time (business §5), a warning shows below cost, the discount is
/// limited per permission, payments may be split, and a build may be sold whole or part by part.
/// </summary>
public sealed class PosService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider, TimeZoneInfo businessZone)
{
    /// <summary>Totals, cost/profit (with cost permission), warnings and the parts a split sale returns to stock; saves nothing.</summary>
    public async Task<PosPreview> PreviewAsync(PosSaleRequest request, PosPolicy policy, bool includeCosts, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = await PriceAsync(db, request, cancellationToken);
        var lines = new List<PosPreviewLine>();
        var warnings = new List<string>();
        var financed = new List<(Guid PartnerId, decimal? Percent, decimal Profit)>();
        foreach (var item in items)
        {
            var units = await UnitsOfAsync(db, item, cancellationToken);
            var cost = units.Sum(unit => unit.Cost);
            var sale = item.LineTotal - item.DiscountAmount;
            var unitProfits = PartnerRules.SplitProfitByCost(sale - cost, units.Select(unit => unit.Cost).ToList());
            for (var index = 0; index < units.Count; index++)
                if (units[index].PartnerId is Guid partnerId) financed.Add((partnerId, units[index].Percent, unitProfits[index]));
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
            includeCosts ? subtotal - costTotal : null, warnings, await ReturningPartsAsync(db, items, cancellationToken),
            includeCosts ? await PartnerSharesAsync(db, financed, cancellationToken) : null);
    }

    public async Task<OrderView> SellAsync(PosSaleRequest request, PosPolicy policy, Guid actorId, CancellationToken cancellationToken = default)
    {
        var fulfillment = OrderRules.Fulfillment(request.FulfillmentMethod);
        var address = OrderRules.Address(request.Address, fulfillment);
        var (usage, usageNote) = OrderUsage.Parse(request.Usage, request.UsageNote);
        var shipping = ShippingOf(request, fulfillment, policy);
        var payments = (request.Payments ?? [])
            .Select(payment => (Method: OrderRules.PaymentMethod(payment.Method, OrderChannel.Store), payment.Amount, payment.MoneyAccountId)).ToList();
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
            foreach (var payment in payments)
                await OrderPaymentService.RecordAsync(db, order, payment.Method, payment.MoneyAccountId, payment.Amount, null, actorId, now, businessZone, cancellationToken);
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

    /// <summary>
    /// "عميل قديم": past buyers matching part of a phone or a name (<see cref="PosCustomerQuery"/>), one per phone number with the
    /// latest details used with it, the most recent buyer first.
    /// </summary>
    public async Task<IReadOnlyList<PosCustomerView>> SearchCustomersAsync(string? search, CancellationToken cancellationToken = default)
    {
        var query = PosCustomerQuery.Parse(search);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var matching = query.PhoneDigits is string digits
            ? db.Orders.Where(order => order.PhoneNumber.Contains(digits))
            : db.Orders.Where(order => EF.Functions.ILike(order.CustomerName, query.NamePattern!));
        var phones = await matching.GroupBy(order => order.PhoneNumber)
            .Select(group => new { Phone = group.Key, Last = group.Max(order => order.CreatedAtUtc) })
            .OrderByDescending(group => group.Last).Take(PosCustomerQuery.MaxResults).Select(group => group.Phone)
            .ToListAsync(cancellationToken);
        var orders = await db.Orders.AsNoTracking().Where(order => phones.Contains(order.PhoneNumber))
            .OrderByDescending(order => order.CreatedAtUtc).ToListAsync(cancellationToken);
        return orders.GroupBy(order => order.PhoneNumber).Select(group =>
        {
            var latest = group.First();
            return new PosCustomerView(latest.CustomerName, latest.PhoneNumber, latest.PhoneHasWhatsApp, latest.CustomerEmail,
                OrderMapping.AddressOf(latest), group.Count());
        }).ToList();
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

    private sealed record PosUnit(decimal Cost, Guid? PartnerId, decimal? Percent);

    /// <summary>The units the line would take: the build's parts, the chosen part, or the oldest available units.</summary>
    private static async Task<List<PosUnit>> UnitsOfAsync(RetailDbContext db, OrderItem item, CancellationToken cancellationToken)
    {
        IQueryable<InventoryUnit> units;
        if (item.UnitId is Guid unitId) units = db.Units.Where(unit => unit.Id == unitId);
        else if (item.AssemblyId is Guid assemblyId) units = db.AssemblyUnits.Where(link => link.AssemblyId == assemblyId).Select(link => link.Unit);
        else units = db.Units.Where(unit => unit.StockItemId == item.StockItemId && unit.Status == UnitStatus.Available)
            .OrderBy(unit => unit.ReceivedAtUtc).Take(item.Quantity);
        return await units.AsNoTracking().Select(unit => new PosUnit(unit.AcquisitionCost, unit.PartnerId, unit.PartnerPercentOverride))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Estimated share of each financing partner over the financed units of the sale (real shares post when the order completes).</summary>
    private static async Task<IReadOnlyList<PartnerSharePreview>> PartnerSharesAsync(RetailDbContext db,
        List<(Guid PartnerId, decimal? Percent, decimal Profit)> financed, CancellationToken cancellationToken)
    {
        if (financed.Count == 0) return [];
        var ids = financed.Select(item => item.PartnerId).Distinct().ToList();
        var partners = await db.Partners.AsNoTracking().Where(partner => ids.Contains(partner.Id)).ToDictionaryAsync(partner => partner.Id, cancellationToken);
        return financed.GroupBy(item => item.PartnerId).Select(group =>
        {
            var partner = partners[group.Key];
            return new PartnerSharePreview(partner.Id, partner.Name, group.Sum(item =>
                PartnerRules.Share(item.Profit, PartnerRules.EffectivePercent(item.Percent, partner.DefaultPercent), partner.LossSharingEnabled)));
        }).OrderBy(share => share.PartnerName).ToList();
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
