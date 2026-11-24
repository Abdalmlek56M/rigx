using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>All Retail feature services over one database, as the tests use them.</summary>
public sealed record RetailTestServices(
    InventoryUnitService Units,
    AssemblyService Assemblies,
    AssemblyPartService Parts,
    OrderService Orders,
    OrderStatusService OrderStatus,
    OrderPaymentService Payments,
    ReturnService Returns,
    PartnerService Partners,
    PartnerCapitalService Capital,
    PartnerLedgerService PartnerLedger,
    PartnerMonthCloseService MonthClose,
    PartnerStatementService Statements,
    UnitPartnerService UnitPartners,
    ExpenseService Expenses,
    BusinessReportService Reports,
    StockItemAdminService StockItems,
    StoreItemService Store,
    OrderFulfillmentService Fulfillment,
    OrderTrackingService Tracking,
    BlacklistService Blacklist,
    PosService Pos,
    WarrantyService Warranties,
    MoneyAccountService Money,
    ExpenseTypeService ExpenseTypes,
    RecurringExpenseService Recurring,
    DashboardService Dashboard,
    CouponService Coupons)
{
    public static RetailTestServices Create(IDbContextFactory<RetailDbContext> factory, TimeProvider time)
    {
        var zone = RetailSales.BusinessZone;
        var reports = new BusinessReportService(factory, zone);
        var recurring = new RecurringExpenseService(factory, time);
        return new(new(factory, time), new(factory, time), new(factory, time), new(factory, time), new(factory, time),
            new(factory, time, zone), new(factory, time, zone), new(factory, time, zone), new(factory, time, zone), new(factory, time, zone),
            new(factory, reports, time), new(factory), new(factory), new(factory, reports, time, zone), reports, new(factory, time), new(factory),
            new(factory, time), new(factory, time), new(factory, time), new(factory, time, zone), new(factory, time),
            new(factory, time, zone), new(factory, time), recurring, new(factory, reports, recurring, time), new(factory, time));
    }
}

/// <summary>Partner helpers for tests.</summary>
public static class RetailPartners
{
    public static Task<PartnerView> CreateAsync(this RetailTestServices retail, string name, decimal percent, bool lossSharing = false,
        string basis = PartnerBasis.FinancedParts, DateOnly? joinDate = null) =>
        retail.Partners.CreateAsync(new PartnerRequest(name, null, joinDate ?? new DateOnly(2020, 1, 1), percent, basis, lossSharing));
}

/// <summary>Order-side helpers: default policies, a published item, and one-line checkout/POS requests.</summary>
public static class RetailSales
{
    public static readonly ReservationPolicy Reservation = new(24, false);
    public static readonly TimeZoneInfo BusinessZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
    /// <summary>Shipping settings in the tests: 80 EGP "خلال 2-3 أيام" for every governorate.</summary>
    public static readonly Func<string, ShippingRate> StandardShipping = _ => new ShippingRate(80m, "خلال 2-3 أيام");
    public static readonly PosPolicy PosUnlimited = new(10m, true, Reservation, StandardShipping);
    public static readonly PosPolicy PosLimited = new(10m, false, Reservation, StandardShipping);
    public static readonly OrderCustomer Customer = new("Customer", "01000000000");
    public static readonly OrderAddress Cairo = new("C", "Nasr City", "Abbas St", "Bldg 4", null);
    public static readonly string[] OnlineMethods = [OrderPaymentMethod.CashOnDelivery, OrderPaymentMethod.InstaPay, OrderPaymentMethod.Wallet];

    /// <summary>
    /// Delivery fee 50, upfront 20% of the subtotal, every online payment method enabled. <paramref name="freeShippingFrom"/> turns the
    /// free-shipping rule on for orders (after discounts) from that amount.
    /// </summary>
    public static CheckoutPolicy Policy(Guid? userId = null, string[]? methods = null, decimal fee = 50m, string? deliveryTime = null,
        BuildPriceRule? buildPrice = null, decimal? freeShippingFrom = null) =>
        new(userId, "3", ShippingFor(fee, deliveryTime, freeShippingFrom),
            (subtotal, _) => decimal.Round(subtotal * 0.2m, 2), methods ?? OnlineMethods, 0m, buildPrice,
            subtotal => decimal.Round(subtotal * 0.2m, 2));

    private static Func<decimal, ShippingRate> ShippingFor(decimal fee, string? deliveryTime, decimal? freeShippingFrom)
    {
        var rule = new ShippingSettings { FreeShippingEnabled = freeShippingFrom is not null, FreeShippingMinimum = freeShippingFrom ?? 0m };
        return subtotal => new ShippingRate(rule.FeeFor(fee, subtotal), deliveryTime);
    }

    public static OrderLineRequest StockLine(Guid stockItemId, int quantity = 1, decimal? salePrice = null, string? priceOption = null) =>
        new(stockItemId, null, quantity, priceOption, salePrice);

    public static OrderLineRequest BuildLine(Guid assemblyId, decimal? salePrice = null, IReadOnlyList<SplitPartRequest>? parts = null) =>
        new(null, assemblyId, 1, null, salePrice, parts);

    public static CheckoutRequest Checkout(IReadOnlyList<OrderLineRequest> lines, string fulfillment = OrderFulfillment.Delivery,
        string payment = OrderPaymentMethod.CashOnDelivery, string phone = "01000000000", string? email = null,
        OrderAddress? address = null, bool? terms = true) =>
        new(new OrderCustomer("Customer", phone, false, email), fulfillment,
            fulfillment == OrderFulfillment.Delivery ? address ?? Cairo : null, payment, lines, TermsAccepted: terms);

    public static PosSaleRequest Sale(IReadOnlyList<OrderLineRequest> lines, bool confirm = false, decimal discount = 0m,
        string fulfillment = OrderFulfillment.Pickup, decimal? fee = 0m, IReadOnlyList<PosPaymentRequest>? payments = null,
        string phone = "01000000000") =>
        new(new OrderCustomer("Customer", phone), fulfillment, fulfillment == OrderFulfillment.Delivery ? Cairo : null, lines,
            discount, fee, payments, Confirm: confirm);

    /// <summary>Receives a "New" lot of <paramref name="quantity"/> units and gives it three photos so the website sells it.</summary>
    public static async Task<StockItemAdminView> PublishedItemAsync(this RetailTestServices retail, string model, int quantity,
        decimal price, decimal cost = 100m)
    {
        var item = await ReceiveItemAsync(retail, new ReceiptLine(model, "New", quantity, cost, price));
        await retail.PublishAsync(item);
        return item;
    }

    public static async Task PublishAsync(this RetailTestServices retail, StockItemAdminView item)
    {
        for (var index = 0; index < 3; index++)
            await retail.StockItems.AddPhotoAsync(item.Id, new StockItemPhoto($"items/{item.Code}/{index}.webp", $"items/{item.Code}/{index}-t.webp"));
    }

    public static async Task<StockItemAdminView> ReceiveItemAsync(this RetailTestServices retail, ReceiptLine line) =>
        Assert.Single((await retail.Units.ReceiveAsync(RetailReceipts.Receipt(Guid.NewGuid(), null, [line]), Guid.NewGuid())).Items);

    public static async Task<OrderView> CheckoutAsync(this RetailTestServices retail, CheckoutRequest request, CheckoutPolicy? policy = null) =>
        (await retail.Orders.CreateOnlineAsync(request, policy ?? Policy())).Order;

    public static Task<OrderView> PosAsync(this RetailTestServices retail, PosSaleRequest request, PosPolicy? policy = null) =>
        retail.Pos.SellAsync(request, policy ?? PosUnlimited, Guid.NewGuid());

    /// <summary>Moves the order; before "Preparing" it first collects the upfront payment the rule asks for.</summary>
    public static async Task<OrderView> StatusAsync(this RetailTestServices retail, OrderView order, string status, string? notes = null)
    {
        if (status == OrderStatus.Preparing && order.UpfrontDue > 0m)
            order = await retail.Payments.AddPaymentAsync(order.Id,
                new OrderPaymentRequest(OrderPaymentMethod.InstaPay, order.UpfrontDue, null, order.RowVersion), Guid.NewGuid());
        return await retail.StatusWithoutPaymentAsync(order, status, notes);
    }

    public static Task<OrderView> StatusWithoutPaymentAsync(this RetailTestServices retail, OrderView order, string status, string? notes = null) =>
        retail.OrderStatus.UpdateStatusAsync(order.Id, new OrderStatusRequest(status, notes, order.RowVersion), Guid.NewGuid(), Reservation,
            new InvoicePolicy("INV", null, BusinessZone));

    public static async Task<OrderView> StatusesAsync(this RetailTestServices retail, OrderView order, params string[] statuses)
    {
        foreach (var status in statuses) order = await retail.StatusAsync(order, status);
        return order;
    }

    public static async Task<int> StatusCodeAsync(Func<Task> action) => (await Assert.ThrowsAsync<RetailException>(action)).StatusCode;
}

/// <summary>Receipt helpers for tests: build a request without a source and read the received units directly.</summary>
public static class RetailReceipts
{
    private static readonly ReceiptSource NoSource = new(null, null, null, null);

    public static ReceiveRequest Receipt(Guid draftId, string? notes, IReadOnlyList<ReceiptLine> lines) =>
        new(draftId, notes, NoSource, lines);

    public static async Task<List<StockUnitView>> ReceiveUnitsAsync(this RetailTestServices retail, ReceiveRequest request, Guid actorId) =>
        (await retail.Units.ReceiveAsync(request, actorId)).Items.SelectMany(item => item.Units).ToList();

    public static async Task<List<StockItemAdminView>> ReceiveItemsAsync(this RetailTestServices retail, ReceiveRequest request, Guid actorId) =>
        (await retail.Units.ReceiveAsync(request, actorId)).Items.ToList();
}

/// <summary>Money helpers: the four seeded accounts start empty, so a test funds one before it pays anything out of it.</summary>
public static class RetailMoney
{
    public static async Task<MoneyAccountView> AccountAsync(this RetailTestServices retail, string kind = MoneyAccountKind.Cash) =>
        (await retail.Money.ListAsync()).First(account => account.Kind == kind);

    public static async Task<decimal> BalanceAsync(this RetailTestServices retail, string kind = MoneyAccountKind.Cash) =>
        (await retail.AccountAsync(kind)).Balance;

    /// <summary>Adds <paramref name="amount"/> to the opening balance of the first account of the kind.</summary>
    public static async Task<MoneyAccountView> FundAsync(this RetailTestServices retail, decimal amount, string kind = MoneyAccountKind.Cash)
    {
        var account = await retail.AccountAsync(kind);
        return await retail.Money.UpdateAsync(account.Id, new MoneyAccountRequest(account.Name, account.Kind, account.OpeningBalance + amount,
            account.OpeningDate, account.IsActive, account.SortOrder, account.RowVersion));
    }

    /// <summary>Sells at the POS and walks the order to Completed (pickup: confirmed, preparing, delivered, completed).</summary>
    public static async Task<OrderView> CompletedSaleAsync(this RetailTestServices retail, PosSaleRequest sale)
    {
        var order = await retail.StatusesAsync(await retail.PosAsync(sale), OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Delivered);
        return await retail.OrderStatus.CompleteOrderAsync(order.Id, new CompleteOrderRequest(order.RowVersion, "done"), Guid.NewGuid());
    }

    /// <summary>One item sold at <paramref name="price"/>, paid in full with one method (into the chosen account, else the default one).</summary>
    public static PosSaleRequest PaidSale(StockItemAdminView item, decimal price, string method = OrderPaymentMethod.Cash, Guid? accountId = null) =>
        RetailSales.Sale([RetailSales.StockLine(item.Id, 1, price)], payments: [new PosPaymentRequest(method, price, accountId)]);
}
