using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Prices order lines from stock items and builds and starts a new order; shared by checkout and POS.</summary>
internal static class OrderDrafting
{
    public const string PriceAlone = "Alone";
    public const string PriceWithBoard = "WithBoard";

    /// <summary>
    /// Online: only published items/builds at their shown price. Store: hidden items too, a free sale price per line
    /// (below the shown price it is recorded as the line discount) and split build sales.
    /// </summary>
    public static async Task<List<OrderItem>> PriceLinesAsync(RetailDbContext db, IReadOnlyList<OrderLineRequest>? lines, string channel,
        CancellationToken cancellationToken)
    {
        if (lines is null || lines.Count is < 1 or > OrderRules.MaxLines)
            throw new RetailException(400, $"An order needs between 1 and {OrderRules.MaxLines} lines.");
        var store = channel == OrderChannel.Store;
        var items = new List<OrderItem>();
        foreach (var line in lines)
        {
            if ((line.StockItemId is null) == (line.AssemblyId is null))
                throw new RetailException(400, "Each order line needs either a stock item or a build.");
            if (line.AssemblyId is Guid assemblyId) items.AddRange(await PriceBuildAsync(db, assemblyId, line, store, cancellationToken));
            else items.Add(await PriceStockItemAsync(db, line.StockItemId!.Value, line, store, cancellationToken));
        }
        if (items.Where(item => item.UnitId is not null).GroupBy(item => item.UnitId).Any(group => group.Count() > 1))
            throw new RetailException(400, "A part is listed twice.");
        return items;
    }

    private static async Task<OrderItem> PriceStockItemAsync(RetailDbContext db, Guid stockItemId, OrderLineRequest line, bool store,
        CancellationToken cancellationToken)
    {
        if (line.Quantity is < 1 or > OrderRules.MaxLines) throw new RetailException(400, "Line quantity must be between 1 and 50.");
        var query = db.StockItems.AsNoTracking().Where(item => item.Id == stockItemId);
        if (!store) query = query.Where(StockItemRules.PublishedFilter);
        var row = await query.Select(item => new
            {
                Item = item,
                Available = item.Units.Count(unit => unit.Status == UnitStatus.Available)
            }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new RetailException(409, "An item in the order is no longer available.");
        if (row.Available < line.Quantity) throw new RetailException(409, $"Only {row.Available} of {row.Item.Code} are available.");
        var prices = StockItemRules.Prices(row.Item.Price, row.Item.WithBoardExtra, row.Item.AloneExtra);
        var shown = line.PriceOption?.Trim() switch
        {
            null or "" or PriceAlone => prices.Alone,
            PriceWithBoard => prices.WithBoard,
            _ => throw new RetailException(400, "Price option must be Alone or WithBoard.")
        };
        return Line(row.Item.ModelName, row.Item.Code, row.Item.Photos.FirstOrDefault()?.ThumbnailStorageKey, line.Quantity, shown,
            store ? line.SalePrice : null, stockItemId, null, null);
    }

    private static async Task<IEnumerable<OrderItem>> PriceBuildAsync(RetailDbContext db, Guid assemblyId, OrderLineRequest line, bool store,
        CancellationToken cancellationToken)
    {
        if (line.Quantity != 1) throw new RetailException(400, "A build line sells one build.");
        var assembly = await db.Assemblies.AsNoTracking().WithUnits().SingleOrDefaultAsync(a => a.Id == assemblyId, cancellationToken);
        var sellable = assembly is not null && (assembly.Status == AssemblyStatus.Published || (store && assembly.Status == AssemblyStatus.Draft));
        if (!sellable || assembly!.Units.Count == 0) throw new RetailException(409, "The selected build is no longer available.");
        var photo = assembly.Photos.FirstOrDefault()?.ThumbnailStorageKey;
        if (line.Parts is not { Count: > 0 } parts)
            return [Line(assembly.PublicTitleAr ?? assembly.PublicTitleEn ?? assembly.Name, assembly.Code, photo, 1, assembly.SalePrice,
                store ? line.SalePrice : null, null, assemblyId, null)];
        if (!store) throw new RetailException(400, "Parts of a build are sold one by one at the store only.");
        if (assembly.SellMode != AssemblySellMode.FullOrSplit) throw new RetailException(409, "This build is sold as a whole only.");
        return parts.Select(part =>
        {
            var link = assembly.Units.SingleOrDefault(entry => entry.UnitId == part.UnitId)
                ?? throw new RetailException(400, "A selected part is not in this build.");
            var item = link.Unit.StockItem;
            return Line(link.Unit.Name, item.Code, item.Photos.FirstOrDefault()?.ThumbnailStorageKey ?? photo, 1, link.PartPrice,
                part.SalePrice, item.Id, assemblyId, link.UnitId);
        }).ToList();
    }

    /// <summary>A sale price above the shown price replaces it; below it, the difference is the line discount.</summary>
    private static OrderItem Line(string name, string code, string? thumbnail, int quantity, decimal shownPrice, decimal? salePrice,
        Guid? stockItemId, Guid? assemblyId, Guid? unitId)
    {
        if (salePrice is < 0 || (salePrice is decimal price && decimal.Round(price, 2) != price))
            throw new RetailException(400, "A sale price must be zero or more with at most 2 decimals.");
        var unitPrice = Math.Max(shownPrice, salePrice ?? shownPrice);
        var lineTotal = decimal.Round(unitPrice * quantity, 2);
        return new OrderItem
        {
            Id = Guid.CreateVersion7(), Name = name, Code = code, ThumbnailStorageKeySnapshot = thumbnail, Quantity = quantity,
            StockItemId = stockItemId, AssemblyId = assemblyId, UnitId = unitId, UnitPrice = unitPrice, LineTotal = lineTotal,
            DiscountAmount = salePrice is decimal sale ? decimal.Round((unitPrice - sale) * quantity, 2) : 0m
        };
    }

    /// <summary>Spreads an order-level discount over the lines in proportion to what is left on each.</summary>
    public static void ApplyOrderDiscount(IReadOnlyList<OrderItem> items, decimal discount)
    {
        if (discount == 0) return;
        if (discount < 0 || decimal.Round(discount, 2) != discount) throw new RetailException(400, "The discount must be zero or more.");
        var left = items.Select(item => item.LineTotal - item.DiscountAmount).ToList();
        var total = left.Sum();
        if (discount > total) throw new RetailException(400, "The discount is larger than the order.");
        var shares = AllocationMath.AllocateAmount(discount, left.Select(value => total == 0 ? 0m : value / total * 100m).ToList());
        for (var index = 0; index < items.Count; index++) items[index].DiscountAmount += shares[index];
    }

    /// <summary>Starts an order with its customer, lines and totals; the caller sets channel-specific fields.</summary>
    public static StoreOrder NewOrder(OrderCustomer? customer, string fulfillment, OrderAddress? address, string channel,
        List<OrderItem> items, ShippingRate shipping, DateTimeOffset now)
    {
        var order = new StoreOrder
        {
            Id = Guid.CreateVersion7(),
            Reference = $"RIGX-{now:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}",
            TrackingToken = OrderRules.NewTrackingToken(),
            Channel = channel,
            CustomerName = OrderRules.Required(customer?.Name, 200, "Customer name"),
            PhoneNumber = OrderRules.Phone(customer?.Phone),
            PhoneHasWhatsApp = customer?.PhoneHasWhatsApp == true,
            CustomerEmail = OrderRules.Email(customer?.Email),
            Governorate = address?.Governorate, Area = address?.Area, Street = address?.Street,
            Building = address?.Building, Landmark = address?.Landmark,
            FulfillmentMethod = fulfillment,
            Status = OrderStatus.New,
            PaymentStatus = PaymentStatus.Unpaid,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        foreach (var item in items) order.Items.Add(item);
        order.Subtotal = items.Sum(item => item.LineTotal - item.DiscountAmount);
        var delivery = fulfillment == OrderFulfillment.Delivery;
        order.DeliveryFee = delivery ? shipping.Fee : 0m;
        order.DeliveryTime = delivery ? OrderRules.Optional(shipping.DeliveryTime, 60, "Delivery time") : null;
        order.Total = order.Subtotal + order.DeliveryFee;
        return order;
    }
}
