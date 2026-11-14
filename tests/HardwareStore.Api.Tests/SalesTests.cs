using System.Text.Json;
using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static HardwareStore.Api.Tests.RetailReceipts;
using static HardwareStore.Api.Tests.RetailSales;

namespace HardwareStore.Api.Tests;

/// <summary>Checkout, POS, the order workflow, reservations, blacklist, payments, tracking and the sold feed (roadmap 2.4).</summary>
[Trait("Category", "RequiresDocker")]
public sealed class SalesPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();

    private async Task<(RetailTestServices Retail, OwnerBootstrapDatabase Database, MutableTimeProvider Clock)> StartAsync()
    {
        var database = await fixture.CreateDatabaseAsync();
        var clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        return (database.CreateRetailServices(clock), database, clock);
    }

    private static async Task<OrderView> ReloadAsync(RetailTestServices retail, Guid id) =>
        (await retail.Orders.GetAdminOrderDetailAsync(id, false)).Order;

    private static async Task<AssemblyView> BuildAsync(RetailTestServices retail, string name, decimal price, IEnumerable<Guid> unitIds,
        string sellMode = AssemblySellMode.Full, int copies = 1, bool publish = true)
    {
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest(name, unitIds.ToList()));
        var card = await retail.Assemblies.UpdateAssemblyAsync(build.Id,
            new AssemblyUpdateRequest(name, "عنوان", "Title", price, build.RowVersion, SellMode: sellMode, Copies: copies));
        return publish ? await retail.Assemblies.SetAssemblyPublishedAsync(card.Id, true, card.RowVersion) : card;
    }

    private static async Task<OrderView> DeliverAsync(RetailTestServices retail, OrderView order) =>
        await retail.StatusesAsync(order, order.FulfillmentMethod == OrderFulfillment.Delivery
            ? [OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Shipped, OrderStatus.Delivered]
            : [OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Delivered]);

    private static async Task<int> AvailableAsync(RetailTestServices retail, StockItemAdminView item) =>
        (await retail.StockItems.GetAsync(item.Id, true, true)).AvailableCount;

    private static void AssertNear(DateTimeOffset expected, DateTimeOffset? actual)
    {
        Assert.NotNull(actual);
        Assert.True(Math.Abs((expected - actual.Value).TotalSeconds) < 1, $"Expected about {expected:O} but was {actual:O}.");
    }

    // ---------------------------------------------------------------- website checkout

    [Fact]
    public async Task Guest_checkout_creates_a_new_order_with_structured_address_fee_and_upfront()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Checkout GPU", 3, 1000m);

        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id, 2)], email: "Buyer@Example.test"));

        Assert.Equal((OrderStatus.New, OrderChannel.Online, PaymentStatus.Unpaid), (order.Status, order.Channel, order.PaymentStatus));
        Assert.Null(order.UserAccountId);
        Assert.Equal((2000m, 50m, 2050m), (order.Subtotal, order.DeliveryFee, order.Total));
        Assert.Equal(400m, order.UpfrontAmount);
        Assert.Equal((0m, 2050m), (order.PaidAmount, order.Remaining));
        Assert.True(order.PaymentRequired);
        Assert.Equal("buyer@example.test", order.CustomerEmail);
        Assert.Equal(new OrderAddress("C", "Nasr City", "Abbas St", "Bldg 4", null), order.Address);
        Assert.False(string.IsNullOrWhiteSpace(order.TrackingToken));
        var line = Assert.Single(order.Items);
        Assert.Equal((item.Id, item.Code, 2, 1000m), (line.StockItemId, line.Code, line.Quantity, line.UnitPrice));
        Assert.Contains(order.Events, entry => entry.EventType == "OrderCreated");
        Assert.Equal(3, await AvailableAsync(retail, item));
    }

    [Fact]
    public async Task Checkout_can_pick_the_with_board_price_and_pickup_skips_fee_and_upfront()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Board GPU", "New", 2, 100m, 1000m, WithBoardExtra: 150m, AloneExtra: 300m));
        await retail.PublishAsync(item);

        var withBoard = await retail.CheckoutAsync(Checkout([StockLine(item.Id, 1, priceOption: "WithBoard")], OrderFulfillment.Pickup));
        Assert.Equal((1150m, 0m, 1150m), (withBoard.Subtotal, withBoard.DeliveryFee, withBoard.Total));
        Assert.Equal(0m, withBoard.UpfrontAmount);
        Assert.False(withBoard.PaymentRequired);
        Assert.Null(withBoard.Address);

        var alone = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]));
        Assert.Equal(1300m, alone.Subtotal);
        Assert.Equal(260m, alone.UpfrontAmount);
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout([StockLine(item.Id, 1, priceOption: "Cheap")]))));
    }

    [Fact]
    public async Task Checkout_rejects_hidden_unpublished_oversold_and_draft_build_lines()
    {
        var (retail, _, _) = await StartAsync();
        var hidden = await retail.PublishedItemAsync("Hidden GPU", 2, 500m);
        await retail.StockItems.SetVisibilityAsync(new StockVisibilityRequest([hidden.Id], true));
        var twoPhotos = await retail.ReceiveItemAsync(new ReceiptLine("Two Photo GPU", "New", 2, 100m, 500m));
        for (var index = 0; index < 2; index++)
            await retail.StockItems.AddPhotoAsync(twoPhotos.Id, new StockItemPhoto($"a{index}.webp", $"a{index}-t.webp"));
        var scarce = await retail.PublishedItemAsync("Scarce GPU", 1, 500m);
        var parts = (await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null, [new ReceiptLine("Draft CPU", "New", 1, 100m, 300m)]), Actor))
            .SelectMany(entry => entry.Units).ToList();
        var draft = await BuildAsync(retail, "Draft build", 1000m, parts.Select(unit => unit.Id), publish: false);

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout([StockLine(hidden.Id)]))));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout([StockLine(twoPhotos.Id)]))));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout([StockLine(scarce.Id, 2)]))));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout([BuildLine(draft.Id)]))));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout([StockLine(Guid.NewGuid())]))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout([]))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout([new OrderLineRequest(null, null)]))));
        Assert.Empty(await retail.Orders.ListOrdersAsync(null));
    }

    [Fact]
    public async Task Checkout_rejects_unknown_governorate_disabled_payment_missing_terms_and_bad_contact()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Rules GPU", 5, 500m);
        var line = new[] { StockLine(item.Id) };

        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(
            Checkout(line, address: new OrderAddress("ZZ", "Area", "Street", null, null)))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(
            Checkout(line, address: new OrderAddress("C", "Area", " ", null, null)))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(
            Checkout(line, payment: OrderPaymentMethod.InstaPay), Policy(methods: [OrderPaymentMethod.CashOnDelivery]))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout(line, payment: OrderPaymentMethod.Cash))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout(line, terms: null))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout(line, terms: false))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout(line, phone: "12"))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout(line, email: "not-an-email"))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(
            Checkout(line) with { FulfillmentMethod = "Courier" })));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(
            Checkout(line) with { Usage = "Other" })));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(
            Checkout(line), Policy() with { MinimumOrder = 600m })));
        Assert.Empty(await retail.Orders.ListOrdersAsync(null));

        var ok = await retail.CheckoutAsync(Checkout(line) with { Usage = "Other", UsageNote = "CAD work", LeadSource = "Facebook" });
        Assert.Equal(("Other", "CAD work", "Facebook"), (ok.Usage, ok.UsageNote, ok.LeadSource));
    }

    // ---------------------------------------------------------------- workflow

    [Fact]
    public async Task Delivery_order_goes_through_shipped_and_a_pickup_order_skips_it()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Flow GPU", 4, 500m);

        var delivery = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.StatusAsync(delivery, OrderStatus.Delivered)));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.StatusAsync(delivery, OrderStatus.Shipped)));
        delivery = await retail.StatusAsync(delivery, OrderStatus.Confirmed);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.StatusAsync(delivery, OrderStatus.Shipped)));
        delivery = await retail.StatusAsync(delivery, OrderStatus.Preparing);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.StatusAsync(delivery, OrderStatus.Delivered)));
        delivery = await retail.StatusAsync(delivery, OrderStatus.Shipped);
        delivery = await retail.StatusAsync(delivery, OrderStatus.Delivered);
        Assert.Equal(OrderStatus.Delivered, delivery.Status);
        Assert.Equal(["OrderCreated", "Status:Confirmed", "Payment:InstaPay", "Status:Preparing", "Status:Shipped", "Status:Delivered"],
            delivery.Events.OrderBy(entry => entry.CreatedAtUtc).Select(entry => entry.EventType));

        var pickup = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], OrderFulfillment.Pickup));
        pickup = await retail.StatusesAsync(pickup, OrderStatus.Confirmed, OrderStatus.Preparing);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.StatusAsync(pickup, OrderStatus.Shipped)));
        Assert.Equal(OrderStatus.Delivered, (await retail.StatusAsync(pickup, OrderStatus.Delivered)).Status);
    }

    [Fact]
    public async Task Status_changes_reject_unknown_closed_stale_and_completed_targets()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Guard GPU", 3, 500m);
        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]));

        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.StatusAsync(order, "Ready")));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.StatusAsync(order, OrderStatus.Completed)));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.OrderStatus.UpdateStatusAsync(
            order.Id, new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion + 5), Actor, Reservation, new InvoicePolicy("INV", null, RetailSales.BusinessZone))));

        var cancelled = await retail.StatusAsync(order, OrderStatus.Cancelled);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.StatusAsync(cancelled, OrderStatus.Confirmed)));
        Assert.Equal(3, await AvailableAsync(retail, item));
    }

    [Fact]
    public async Task Confirming_sets_the_reservation_and_shipping_clears_it()
    {
        var (retail, _, clock) = await StartAsync();
        var item = await retail.PublishedItemAsync("Hold GPU", 2, 500m);
        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]));
        Assert.Null(order.ReservedUntilUtc);

        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor, new ReservationPolicy(48, true), new InvoicePolicy("INV", null, RetailSales.BusinessZone));
        AssertNear(clock.GetUtcNow().AddHours(48), confirmed.ReservedUntilUtc);
        Assert.True(confirmed.ReservationAutoRelease);
        Assert.Equal(1, await AvailableAsync(retail, item));

        var preparing = await retail.StatusAsync(confirmed, OrderStatus.Preparing);
        Assert.NotNull(preparing.ReservedUntilUtc);
        var shipped = await retail.StatusAsync(preparing, OrderStatus.Shipped);
        Assert.Null(shipped.ReservedUntilUtc);
        Assert.Equal(0, await retail.Fulfillment.ProcessExpiredReservationsAsync());
    }

    [Fact]
    public async Task Expired_reservation_without_auto_release_only_flags_the_order_and_keeps_the_stock()
    {
        var (retail, _, clock) = await StartAsync();
        var item = await retail.PublishedItemAsync("Notify GPU", 2, 500m);
        var order = await retail.PosAsync(Sale([StockLine(item.Id)], confirm: true));
        AssertNear(clock.GetUtcNow().AddHours(24), order.ReservedUntilUtc);
        Assert.False(order.ReservationAutoRelease);

        clock.Advance(TimeSpan.FromHours(23));
        Assert.Equal(0, await retail.Fulfillment.ProcessExpiredReservationsAsync());
        clock.Advance(TimeSpan.FromHours(2));
        Assert.Equal(1, await retail.Fulfillment.ProcessExpiredReservationsAsync());

        var flagged = await ReloadAsync(retail, order.Id);
        Assert.Equal(OrderStatus.Confirmed, flagged.Status);
        Assert.NotNull(flagged.ReservationExpiredAtUtc);
        Assert.NotNull(flagged.ReservedUntilUtc);
        Assert.Equal(1, await AvailableAsync(retail, item));
        Assert.Equal(0, await retail.Fulfillment.ProcessExpiredReservationsAsync());

        var restarted = await retail.Fulfillment.SetReservationAsync(order.Id, new OrderReservationRequest(48, true, flagged.RowVersion), Actor);
        Assert.Null(restarted.ReservationExpiredAtUtc);
        Assert.True(restarted.ReservationAutoRelease);
        AssertNear(clock.GetUtcNow().AddHours(48), restarted.ReservedUntilUtc);
    }

    [Fact]
    public async Task Expired_reservation_with_auto_release_cancels_the_order_and_returns_the_stock()
    {
        var (retail, _, clock) = await StartAsync();
        var item = await retail.PublishedItemAsync("Release GPU", 3, 500m);
        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id, 2)]));
        var confirmed = await retail.OrderStatus.UpdateStatusAsync(order.Id,
            new OrderStatusRequest(OrderStatus.Confirmed, null, order.RowVersion), Actor, new ReservationPolicy(1, true), new InvoicePolicy("INV", null, RetailSales.BusinessZone));
        Assert.Equal(1, await AvailableAsync(retail, item));

        clock.Advance(TimeSpan.FromMinutes(61));
        Assert.Equal(1, await retail.Fulfillment.ProcessExpiredReservationsAsync());

        var released = await ReloadAsync(retail, confirmed.Id);
        Assert.Equal(OrderStatus.Cancelled, released.Status);
        Assert.Null(released.ReservedUntilUtc);
        Assert.Contains(released.Events, entry => entry.EventType == "ReservationExpired:Released");
        Assert.Equal(3, await AvailableAsync(retail, item));
    }

    [Fact]
    public async Task Reservation_can_be_changed_only_while_the_order_holds_stock()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Edit Hold GPU", 2, 500m);
        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]));

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Fulfillment.SetReservationAsync(
            order.Id, new OrderReservationRequest(10, false, order.RowVersion), Actor)));
        var confirmed = await retail.StatusAsync(order, OrderStatus.Confirmed);
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Fulfillment.SetReservationAsync(
            order.Id, new OrderReservationRequest(0, false, confirmed.RowVersion), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Fulfillment.SetReservationAsync(
            order.Id, new OrderReservationRequest(721, false, confirmed.RowVersion), Actor)));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Fulfillment.SetReservationAsync(
            order.Id, new OrderReservationRequest(10, false, confirmed.RowVersion - 1), Actor)));

        var changed = await retail.Fulfillment.SetReservationAsync(order.Id, new OrderReservationRequest(null, true, confirmed.RowVersion), Actor);
        Assert.True(changed.ReservationAutoRelease);
        AssertNear(confirmed.ReservedUntilUtc!.Value, changed.ReservedUntilUtc);
    }

    // ---------------------------------------------------------------- blacklist

    [Fact]
    public async Task Order_from_a_blacklisted_phone_is_flagged_and_confirmed_only_after_full_payment()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Black GPU", 3, 1000m);
        await retail.Blacklist.AddAsync(new BlacklistRequest(null, "Bad Buyer", "0100 111 2222", null, null, "Fake orders"), Actor);

        var clean = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01009998888"));
        Assert.False(clean.IsBlacklisted);
        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01001112222"));
        Assert.True(order.IsBlacklisted);

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.StatusAsync(order, OrderStatus.Confirmed)));
        var partial = await retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.InstaPay, 500m, null, order.RowVersion), Actor);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.StatusAsync(partial, OrderStatus.Confirmed)));
        var paid = await retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.InstaPay, partial.Remaining, null, partial.RowVersion), Actor);
        Assert.Equal(PaymentStatus.Paid, paid.PaymentStatus);

        Assert.Equal(OrderStatus.Confirmed, (await retail.StatusAsync(paid, OrderStatus.Confirmed)).Status);
    }

    [Fact]
    public async Task Blacklist_matches_email_and_address_too()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Match GPU", 5, 500m);
        await retail.Blacklist.AddAsync(new BlacklistRequest(null, null, null, "Bad@Example.test", null, "Chargeback"), Actor);
        await retail.Blacklist.AddAsync(new BlacklistRequest(null, null, null, null, new OrderAddress("GZ", "Dokki", "Tahrir St", "B1", null), "No-show"), Actor);

        var byEmail = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01011111111", email: "BAD@example.test"));
        var byAddress = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01022222222",
            address: new OrderAddress("GZ", " dokki ", "tahrir st.", "b-1", null)));
        var other = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01033333333"));

        Assert.True(byEmail.IsBlacklisted);
        Assert.True(byAddress.IsBlacklisted);
        Assert.False(other.IsBlacklisted);
        var entries = await retail.Blacklist.ListAsync();
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, entry => entry.Email == "bad@example.test");
    }

    [Fact]
    public async Task Adding_from_an_order_copies_the_customer_and_flags_the_new_orders_of_that_customer()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Flag GPU", 8, 500m);
        var first = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "0100 555 6666", email: "x@example.test"));
        var second = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01005556666"));
        var confirmedBefore = await retail.StatusAsync(
            await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01005556666")), OrderStatus.Confirmed);
        var stranger = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01007778888"));

        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Blacklist.AddAsync(new BlacklistRequest(first.Id, null, null, null, null, " "), Actor)));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Blacklist.AddAsync(new BlacklistRequest(Guid.NewGuid(), null, null, null, null, "x"), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Blacklist.AddAsync(new BlacklistRequest(null, "Nobody", null, null, null, "x"), Actor)));
        var entry = await retail.Blacklist.AddAsync(new BlacklistRequest(first.Id, null, null, null, null, "Did not collect"), Actor);

        Assert.Equal(("Customer", "01005556666", "x@example.test"), (entry.Name, entry.PhoneNumber, entry.Email));
        Assert.True((await ReloadAsync(retail, first.Id)).IsBlacklisted);
        Assert.True((await ReloadAsync(retail, second.Id)).IsBlacklisted);
        Assert.False((await ReloadAsync(retail, confirmedBefore.Id)).IsBlacklisted);
        Assert.False((await ReloadAsync(retail, stranger.Id)).IsBlacklisted);

        await retail.Blacklist.RemoveAsync(entry.Id, Actor);
        Assert.Empty(await retail.Blacklist.ListAsync());
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Blacklist.RemoveAsync(entry.Id, Actor)));
        Assert.False((await retail.CheckoutAsync(Checkout([StockLine(item.Id)], phone: "01005556666", email: "x@example.test"))).IsBlacklisted);
    }

    // ---------------------------------------------------------------- payments and shipping details

    [Fact]
    public async Task Partial_then_full_payment_marks_the_order_paid_and_overpaying_is_rejected()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Pay GPU", 2, 1000m);
        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]));
        Assert.Equal(1050m, order.Total);

        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.Cash, 0m, null, order.RowVersion), Actor)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.CashOnDelivery, 100m, null, order.RowVersion), Actor)));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.Cash, 100m, null, order.RowVersion - 1), Actor)));

        var partial = await retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.InstaPay, 400m, "deposit", order.RowVersion), Actor);
        Assert.Equal((PaymentStatus.Unpaid, 400m, 650m), (partial.PaymentStatus, partial.PaidAmount, partial.Remaining));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.Cash, 650.01m, null, partial.RowVersion), Actor)));

        var full = await retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.Cash, 650m, null, partial.RowVersion), Actor);
        Assert.Equal((PaymentStatus.Paid, 1050m, 0m), (full.PaymentStatus, full.PaidAmount, full.Remaining));
        Assert.Equal(["InstaPay", "Cash"], full.Payments.OrderBy(payment => payment.CreatedAtUtc).Select(payment => payment.Method));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.Cash, 1m, null, full.RowVersion), Actor)));

        var cancelled = await retail.StatusAsync(full, OrderStatus.Cancelled);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.Cash, 1m, null, cancelled.RowVersion), Actor)));
    }

    [Fact]
    public async Task Shipping_details_are_only_for_delivery_orders()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Ship GPU", 3, 500m);
        var delivery = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]));
        var pickup = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], OrderFulfillment.Pickup));

        var saved = await retail.Fulfillment.SetShippingAsync(delivery.Id,
            new OrderShippingRequest("  Bosta  ", " WB-123 ", delivery.RowVersion), Actor);
        Assert.Equal(("Bosta", "WB-123"), (saved.CourierName, saved.WaybillNumber));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Fulfillment.SetShippingAsync(delivery.Id,
            new OrderShippingRequest("Bosta", "WB-1", delivery.RowVersion), Actor)));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Fulfillment.SetShippingAsync(pickup.Id,
            new OrderShippingRequest("Bosta", "WB-1", pickup.RowVersion), Actor)));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Fulfillment.SetShippingAsync(Guid.NewGuid(),
            new OrderShippingRequest("Bosta", "WB-1", 1), Actor)));
    }

    // ---------------------------------------------------------------- tracking

    [Fact]
    public async Task Tracking_page_hides_the_customer_and_shows_the_timeline_and_shipping()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Track GPU", 2, 500m);
        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]) with
        {
            Customer = new OrderCustomer("Mohamed Secret", "01099887766", true, "secret@example.test")
        });

        var fresh = await retail.Tracking.GetAsync(order.TrackingToken);
        Assert.Equal((order.Reference, OrderStatus.New, OrderFulfillment.Delivery), (fresh.Reference, fresh.Status, fresh.FulfillmentMethod));
        Assert.Equal([OrderStatus.New], fresh.Timeline.Select(step => step.Status));
        Assert.False(fresh.CanRate);

        var shipping = await retail.Fulfillment.SetShippingAsync(order.Id, new OrderShippingRequest("Bosta", "WB-77", order.RowVersion), Actor);
        var shipped = await retail.StatusesAsync(shipping, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Shipped);
        var view = await retail.Tracking.GetAsync(shipped.TrackingToken);

        Assert.Equal(OrderStatus.Shipped, view.Status);
        Assert.Equal([OrderStatus.New, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Shipped], view.Timeline.Select(step => step.Status));
        Assert.Equal(("Bosta", "WB-77", "Nasr City", "Abbas St"), (view.CourierName, view.WaybillNumber, view.Area, view.Street));
        Assert.Equal((item.Code, 1), (view.Items[0].Code, view.Items[0].Quantity));
        Assert.Equal(550m, view.Total);
        Assert.Equal((450m, 0m), (view.Remaining, view.UpfrontDue));

        var json = JsonSerializer.Serialize(view).ToLowerInvariant();
        foreach (var secret in new[] { "mohamed", "secret", "01099887766", "example.test", "tracktoken", order.TrackingToken.ToLowerInvariant() })
            Assert.DoesNotContain(secret, json);

        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Tracking.GetAsync("nope")));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Tracking.GetAsync(new string('a', 33))));
        Assert.Equal(404, await RetailSales.StatusCodeAsync(() => retail.Tracking.GetAsync(" ")));
    }

    [Fact]
    public async Task Rating_is_accepted_once_and_only_after_delivery()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Rate GPU", 2, 500m);
        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], OrderFulfillment.Pickup));

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Tracking.RateAsync(order.TrackingToken, new OrderRatingRequest(5, "early"))));
        var delivered = await DeliverAsync(retail, order);
        Assert.True((await retail.Tracking.GetAsync(delivered.TrackingToken)).CanRate);

        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Tracking.RateAsync(order.TrackingToken, new OrderRatingRequest(0, null))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Tracking.RateAsync(order.TrackingToken, new OrderRatingRequest(6, null))));
        var rated = await retail.Tracking.RateAsync(order.TrackingToken, new OrderRatingRequest(4, "  Fast   delivery "));
        Assert.Equal((4, false), (rated.SatisfactionScore, rated.CanRate));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.Tracking.RateAsync(order.TrackingToken, new OrderRatingRequest(1, null))));

        var stored = await ReloadAsync(retail, order.Id);
        Assert.Equal((4, "Fast delivery"), (stored.SatisfactionScore, stored.SatisfactionComment));
    }

    [Fact]
    public async Task A_completed_order_shows_as_delivered_on_the_tracking_page()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Complete GPU", 2, 500m);
        var order = await DeliverAsync(retail, await retail.CheckoutAsync(Checkout([StockLine(item.Id)], OrderFulfillment.Pickup)));
        var waived = await retail.Payments.WaivePaymentAsync(order.Id, new WaivePaymentRequest("Paid at the store", order.RowVersion), Actor);
        var completed = await retail.OrderStatus.CompleteOrderAsync(order.Id, new CompleteOrderRequest(waived.RowVersion, null), Actor);
        Assert.Equal(OrderStatus.Completed, completed.Status);

        var view = await retail.Tracking.GetAsync(order.TrackingToken);

        Assert.Equal(OrderStatus.Delivered, view.Status);
        Assert.DoesNotContain(view.Timeline, step => step.Status == OrderStatus.Completed);
        Assert.True(view.CanRate);
    }

    // ---------------------------------------------------------------- POS

    [Fact]
    public async Task Pos_price_below_the_shown_price_is_recorded_as_a_line_discount_and_above_it_replaces_the_price()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Pos GPU", "New", 4, 500m, 1000m));

        var cheaper = await retail.PosAsync(Sale([StockLine(item.Id, 1, 900m)]), PosLimited);
        var line = Assert.Single(cheaper.Items);
        Assert.Equal((1000m, 1000m, 100m), (line.UnitPrice, line.LineTotal, line.DiscountAmount));
        Assert.Equal((900m, 900m), (cheaper.Subtotal, cheaper.Total));
        Assert.Equal(OrderChannel.Store, cheaper.Channel);

        var dearer = await retail.PosAsync(Sale([StockLine(item.Id, 2, 1200m)]), PosLimited);
        line = Assert.Single(dearer.Items);
        Assert.Equal((1200m, 2400m, 0m), (line.UnitPrice, line.LineTotal, line.DiscountAmount));
        Assert.Equal(2400m, dearer.Total);

        var shown = await retail.PosAsync(Sale([StockLine(item.Id)]), PosLimited);
        Assert.Equal((1000m, 0m), (shown.Items[0].UnitPrice, shown.Items[0].DiscountAmount));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale([StockLine(item.Id, 1, -1m)]))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale([StockLine(item.Id, 1, 10.005m)]))));
    }

    [Fact]
    public async Task Pos_discount_above_the_limit_is_forbidden_unless_the_seller_may_override()
    {
        var (retail, _, _) = await StartAsync();
        var gpu = await retail.ReceiveItemAsync(new ReceiptLine("Limit GPU", "New", 6, 500m, 1000m));
        var fan = await retail.ReceiveItemAsync(new ReceiptLine("Limit Fan", "New", 6, 50m, 500m));
        IReadOnlyList<OrderLineRequest> Lines() => [StockLine(gpu.Id), StockLine(fan.Id)];

        var atLimit = await retail.PosAsync(Sale(Lines(), discount: 150m), PosLimited);
        Assert.Equal((150m, 1350m), (atLimit.Items.Sum(item => item.DiscountAmount), atLimit.Subtotal));
        Assert.Equal(403, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale(Lines(), discount: 150.01m), PosLimited)));
        // A line price cut counts toward the same limit.
        Assert.Equal(403, await RetailSales.StatusCodeAsync(() => retail.PosAsync(
            Sale([StockLine(gpu.Id, 1, 800m)]), PosLimited)));
        Assert.Equal(403, await RetailSales.StatusCodeAsync(() => retail.PosAsync(
            Sale([StockLine(gpu.Id, 1, 950m)], discount: 60m), PosLimited)));
        Assert.Equal(403, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale(Lines(), discount: 1m),
            new PosPolicy(0m, false, Reservation, StandardShipping))));

        var unlimited = await retail.PosAsync(Sale(Lines(), discount: 900m), PosUnlimited);
        Assert.Equal(600m, unlimited.Subtotal);
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale(Lines(), discount: 1500.01m), PosUnlimited)));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale(Lines(), discount: -1m), PosUnlimited)));
    }

    [Fact]
    public async Task Pos_split_payments_are_recorded_and_the_sale_is_paid()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Split Pay GPU", "New", 6, 500m, 1000m));
        IReadOnlyList<OrderLineRequest> Lines() => [StockLine(item.Id)];

        var split = await retail.PosAsync(Sale(Lines(), payments: [new(OrderPaymentMethod.Cash, 400m), new(OrderPaymentMethod.InstaPay, 600m)]));
        Assert.Equal((OrderPaymentMethod.Split, PaymentStatus.Paid, 1000m, 0m), (split.PaymentMethod, split.PaymentStatus, split.PaidAmount, split.Remaining));
        Assert.Equal(2, split.Payments.Count);

        var single = await retail.PosAsync(Sale(Lines(), payments: [new(OrderPaymentMethod.Wallet, 300m), new(OrderPaymentMethod.Wallet, 700m)]));
        Assert.Equal((OrderPaymentMethod.Wallet, PaymentStatus.Paid), (single.PaymentMethod, single.PaymentStatus));

        var partial = await retail.PosAsync(Sale(Lines(), payments: [new(OrderPaymentMethod.BankTransfer, 250m)]));
        Assert.Equal((OrderPaymentMethod.BankTransfer, PaymentStatus.Unpaid, 750m), (partial.PaymentMethod, partial.PaymentStatus, partial.Remaining));

        var unpaid = await retail.PosAsync(Sale(Lines()));
        Assert.Equal((OrderPaymentMethod.Cash, PaymentStatus.Unpaid, 0m), (unpaid.PaymentMethod, unpaid.PaymentStatus, unpaid.PaidAmount));

        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale(Lines(), payments: [new(OrderPaymentMethod.Cash, 1000.01m)]))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale(Lines(), payments: [new(OrderPaymentMethod.CashOnDelivery, 100m)]))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale(Lines(), payments: [new(OrderPaymentMethod.Cash, 0m)]))));
    }

    [Fact]
    public async Task Pos_confirm_claims_the_stock_now_and_saving_without_confirm_leaves_a_new_order()
    {
        var (retail, _, clock) = await StartAsync();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Confirm GPU", "New", 3, 500m, 1000m));

        var pending = await retail.PosAsync(Sale([StockLine(item.Id)], confirm: false));
        Assert.Equal(OrderStatus.New, pending.Status);
        Assert.Null(pending.ReservedUntilUtc);
        Assert.Equal(3, await AvailableAsync(retail, item));

        var confirmed = await retail.PosAsync(Sale([StockLine(item.Id, 2)], confirm: true), new PosPolicy(10m, true, new ReservationPolicy(6, true), StandardShipping));
        Assert.Equal(OrderStatus.Confirmed, confirmed.Status);
        AssertNear(clock.GetUtcNow().AddHours(6), confirmed.ReservedUntilUtc);
        Assert.True(confirmed.ReservationAutoRelease);
        Assert.Equal(2, Assert.Single(confirmed.Items).Components.Count);
        Assert.Contains(confirmed.Events, entry => entry.EventType == "Status:Confirmed");
        Assert.Equal(1, await AvailableAsync(retail, item));

        // The pending sale can still be confirmed later, but only while stock lasts.
        Assert.Equal(OrderStatus.Confirmed, (await retail.StatusAsync(pending, OrderStatus.Confirmed)).Status);
        Assert.Equal(0, await AvailableAsync(retail, item));
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale([StockLine(item.Id)], confirm: true))));
    }

    [Fact]
    public async Task Online_delivery_order_keeps_the_delivery_time_and_shows_the_upfront_due_to_staff_and_customer()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Time GPU", 3, 1000m);

        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]), Policy(deliveryTime: "خلال 2-3 أيام"));
        var tracking = await retail.Tracking.GetAsync(order.TrackingToken);

        Assert.Equal(("خلال 2-3 أيام", 200m, 200m), (order.DeliveryTime, order.UpfrontAmount, order.UpfrontDue));
        Assert.Equal(("خلال 2-3 أيام", 200m), (tracking.DeliveryTime, tracking.UpfrontDue));

        var pickup = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], OrderFulfillment.Pickup), Policy(deliveryTime: "خلال 2-3 أيام"));
        Assert.Equal((null, 0m), (pickup.DeliveryTime, pickup.UpfrontDue));
        Assert.Null((await retail.Tracking.GetAsync(pickup.TrackingToken)).DeliveryTime);

        var paid = await retail.Payments.AddPaymentAsync(order.Id, new OrderPaymentRequest(OrderPaymentMethod.InstaPay, 200m, null, order.RowVersion), Actor);
        Assert.Equal(0m, paid.UpfrontDue);
        Assert.Equal(0m, (await retail.Tracking.GetAsync(order.TrackingToken)).UpfrontDue);
    }

    [Fact]
    public async Task A_delivery_order_cannot_be_prepared_before_its_upfront_payment_is_collected()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.PublishedItemAsync("Upfront GPU", 4, 1000m);

        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id)]));
        var confirmed = await retail.StatusWithoutPaymentAsync(order, OrderStatus.Confirmed);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.StatusWithoutPaymentAsync(confirmed, OrderStatus.Preparing)));

        var partial = await retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.InstaPay, 150m, null, confirmed.RowVersion), Actor);
        Assert.Equal(50m, partial.UpfrontDue);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.StatusWithoutPaymentAsync(partial, OrderStatus.Preparing)));

        var paid = await retail.Payments.AddPaymentAsync(order.Id,
            new OrderPaymentRequest(OrderPaymentMethod.InstaPay, partial.UpfrontDue, null, partial.RowVersion), Actor);
        Assert.Equal(OrderStatus.Preparing, (await retail.StatusWithoutPaymentAsync(paid, OrderStatus.Preparing)).Status);

        var waivedOrder = await retail.StatusWithoutPaymentAsync(await retail.CheckoutAsync(Checkout([StockLine(item.Id)])), OrderStatus.Confirmed);
        var waived = await retail.Payments.WaivePaymentAsync(waivedOrder.Id, new WaivePaymentRequest("Owner-approved", waivedOrder.RowVersion), Actor);
        Assert.Equal(OrderStatus.Preparing, (await retail.StatusWithoutPaymentAsync(waived, OrderStatus.Preparing)).Status);

        var pickup = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], OrderFulfillment.Pickup));
        var pickupConfirmed = await retail.StatusWithoutPaymentAsync(pickup, OrderStatus.Confirmed);
        Assert.Equal(OrderStatus.Preparing, (await retail.StatusWithoutPaymentAsync(pickupConfirmed, OrderStatus.Preparing)).Status);
    }

    [Fact]
    public async Task Pos_delivery_takes_the_fee_and_time_from_the_shipping_settings_unless_staff_type_a_fee()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Rate Sale GPU", "New", 5, 500m, 1000m));
        var lines = new[] { StockLine(item.Id) };

        var preview = await retail.Pos.PreviewAsync(Sale(lines, fulfillment: OrderFulfillment.Delivery, fee: null), PosUnlimited, includeCosts: false);
        Assert.Equal((80m, 1080m), (preview.DeliveryFee, preview.Total));

        var fromSettings = await retail.PosAsync(Sale(lines, fulfillment: OrderFulfillment.Delivery, fee: null));
        Assert.Equal((80m, "خلال 2-3 أيام", 1080m), (fromSettings.DeliveryFee, fromSettings.DeliveryTime, fromSettings.Total));

        var typed = await retail.PosAsync(Sale(lines, fulfillment: OrderFulfillment.Delivery, fee: 0m));
        Assert.Equal((0m, "خلال 2-3 أيام"), (typed.DeliveryFee, typed.DeliveryTime));

        var pickup = await retail.PosAsync(Sale(lines, fee: null));
        Assert.Equal((0m, (string?)null), (pickup.DeliveryFee, pickup.DeliveryTime));
    }

    [Fact]
    public async Task Pos_delivery_sale_uses_the_typed_fee_and_needs_an_address()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Ship Sale GPU", "New", 4, 500m, 1000m));
        var lines = new[] { StockLine(item.Id) };

        var order = await retail.PosAsync(Sale(lines, fulfillment: OrderFulfillment.Delivery, fee: 70m));
        Assert.Equal((1000m, 70m, 1070m), (order.Subtotal, order.DeliveryFee, order.Total));
        Assert.Equal("C", order.Address?.Governorate);

        var pickup = await retail.PosAsync(Sale(lines, fee: 70m));
        Assert.Equal((0m, 1000m), (pickup.DeliveryFee, pickup.Total));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PosAsync(
            Sale(lines, fulfillment: OrderFulfillment.Delivery, fee: 70m) with { Address = null })));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale(lines, fulfillment: OrderFulfillment.Delivery, fee: -1m))));
    }

    [Fact]
    public async Task Pos_preview_warns_about_below_cost_and_over_limit_and_saves_nothing()
    {
        var (retail, _, _) = await StartAsync();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Preview GPU", "New", 3, 100m, 150m));

        var preview = await retail.Pos.PreviewAsync(Sale([StockLine(item.Id, 1, 80m)]), PosLimited, includeCosts: true);

        var line = Assert.Single(preview.Lines);
        Assert.True(line.BelowCost);
        Assert.Equal((150m, 80m, (decimal?)100m), (line.ShownPrice, line.SalePrice, line.Cost));
        Assert.Equal((150m, 70m, 80m), (preview.LinesTotal, preview.Discount, preview.Subtotal));
        Assert.Equal((100m, -20m), (preview.CostTotal, preview.Profit));
        Assert.Equal(2, preview.Warnings.Count);
        Assert.Contains(preview.Warnings, warning => warning.Contains("below its cost"));
        Assert.Contains(preview.Warnings, warning => warning.Contains("limit"));
        Assert.Empty(preview.ReturningToStock);

        var hidden = await retail.Pos.PreviewAsync(Sale([StockLine(item.Id, 1, 150m)]), PosLimited, includeCosts: false);
        Assert.Empty(hidden.Warnings);
        Assert.Null(hidden.CostTotal);
        Assert.Null(hidden.Profit);
        Assert.Null(hidden.Lines[0].Cost);

        Assert.Empty(await retail.Orders.ListOrdersAsync(null));
        Assert.Equal(3, await AvailableAsync(retail, item));
    }

    [Fact]
    public async Task Pos_split_build_sale_sells_the_chosen_parts_and_returns_the_rest_to_stock()
    {
        var (retail, database, _) = await StartAsync();
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
        [
            new ReceiptLine("Split CPU", "New", 1, 100m, 3000m), new ReceiptLine("Split GPU", "New", 1, 100m, 2000m),
            new ReceiptLine("Split RAM", "New", 1, 100m, 500m)
        ]), Actor);
        var (cpu, gpu, ram) = (items[0].Units[0].Id, items[1].Units[0].Id, items[2].Units[0].Id);
        var build = await BuildAsync(retail, "Split build", 5000m, [cpu, gpu, ram], AssemblySellMode.FullOrSplit);
        var request = Sale([BuildLine(build.Id, parts: [new SplitPartRequest(cpu), new SplitPartRequest(gpu, 1800m)])], confirm: true);

        var preview = await retail.Pos.PreviewAsync(request, PosUnlimited, includeCosts: true);
        var returning = Assert.Single(preview.ReturningToStock);
        Assert.Equal((ram, "Split RAM"), (returning.UnitId, returning.Name));
        Assert.Equal((5000m, 200m, 4800m), (preview.LinesTotal, preview.Discount, preview.Subtotal));

        var order = await retail.PosAsync(request);

        Assert.Equal((OrderStatus.Confirmed, 4800m), (order.Status, order.Total));
        Assert.Equal(2, order.Items.Count);
        Assert.All(order.Items, item => Assert.Equal(build.Id, item.AssemblyId));
        Assert.Equal([cpu, gpu], order.Items.Select(item => item.UnitId!.Value).OrderBy(id => id == cpu ? 0 : 1));
        await using var db = database.CreateRetailContext();
        var units = await db.Units.Where(unit => new[] { cpu, gpu, ram }.Contains(unit.Id)).ToDictionaryAsync(unit => unit.Id);
        Assert.Equal(UnitStatus.Sold, units[cpu].Status);
        Assert.Equal(UnitStatus.Sold, units[gpu].Status);
        Assert.Equal((UnitStatus.Available, (Guid?)null), (units[ram].Status, units[ram].AssemblyId));
        Assert.Equal(AssemblyStatus.Disassembled, (await db.Assemblies.SingleAsync(entry => entry.Id == build.Id)).Status);
        Assert.Equal(1, await AvailableAsync(retail, items[2]));
    }

    [Fact]
    public async Task Pos_split_sale_needs_a_split_build_and_parts_of_that_build()
    {
        var (retail, _, _) = await StartAsync();
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
        [
            new ReceiptLine("Whole CPU", "New", 1, 100m, 3000m), new ReceiptLine("Whole GPU", "New", 1, 100m, 2000m),
            new ReceiptLine("Other CPU", "New", 1, 100m, 1000m)
        ]), Actor);
        var (cpu, gpu, other) = (items[0].Units[0].Id, items[1].Units[0].Id, items[2].Units[0].Id);
        var whole = await BuildAsync(retail, "Whole only", 5000m, [cpu, gpu]);
        var parts = new[] { new SplitPartRequest(cpu) };

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale([BuildLine(whole.Id, parts: parts)], confirm: true))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout([BuildLine(whole.Id, parts: parts)]))));
        Assert.Equal(AssemblyStatus.Published, (await retail.Assemblies.ListAssembliesAsync()).Single(entry => entry.Id == whole.Id).Status);

        var splittable = await retail.Assemblies.UpdateAssemblyAsync(whole.Id, new AssemblyUpdateRequest(
            whole.Name, whole.PublicTitleAr, whole.PublicTitleEn, whole.SalePrice, (await retail.Assemblies.ListAssembliesAsync())
                .Single(entry => entry.Id == whole.Id).RowVersion, SellMode: AssemblySellMode.FullOrSplit));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale(
            [BuildLine(splittable.Id, parts: [new SplitPartRequest(other)])]))));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.PosAsync(Sale(
            [BuildLine(splittable.Id, parts: [new SplitPartRequest(cpu), new SplitPartRequest(cpu)])]))));
        Assert.Equal(OrderStatus.Confirmed, (await retail.PosAsync(Sale([BuildLine(splittable.Id, parts: parts)], confirm: true))).Status);
    }

    [Fact]
    public async Task Selling_one_copy_of_a_multi_copy_build_refills_it_from_the_same_stock_items()
    {
        var (retail, database, _) = await StartAsync();
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
        [
            new ReceiptLine("Copy CPU", "New", 2, 100m, 3000m), new ReceiptLine("Copy GPU", "New", 2, 100m, 2000m)
        ]), Actor);
        var first = items.Select(item => item.Units[0].Id).ToList();
        var build = await BuildAsync(retail, "Copies", 5000m, first, copies: 2);
        var partPrices = build.Units.ToDictionary(unit => unit.StockItemCode, unit => unit.PartPrice);

        var order = await retail.PosAsync(Sale([BuildLine(build.Id)], confirm: true));

        Assert.Equal(first.Order(), order.Items.Single().Components.Select(component => component.UnitId).Order());
        await using (var db = database.CreateRetailContext())
        {
            var card = await db.Assemblies.Include(entry => entry.Units).ThenInclude(link => link.Unit).ThenInclude(unit => unit.StockItem)
                .SingleAsync(entry => entry.Id == build.Id);
            Assert.Equal((AssemblyStatus.Published, 1), (card.Status, card.Copies));
            Assert.Equal(2, card.Units.Count);
            Assert.DoesNotContain(card.Units, link => first.Contains(link.UnitId));
            Assert.All(card.Units, link =>
            {
                Assert.Equal(UnitStatus.InAssembly, link.Unit.Status);
                Assert.Equal(partPrices[link.Unit.StockItem.Code], link.PartPrice);
            });
            Assert.Equal(100m, card.Units.Sum(link => link.AllocationPercent));
            Assert.All(await db.Units.Where(unit => first.Contains(unit.Id)).ToListAsync(), unit => Assert.Equal(UnitStatus.Sold, unit.Status));
        }
        foreach (var item in items) Assert.Equal(0, await AvailableAsync(retail, item));

        var last = await retail.PosAsync(Sale([BuildLine(build.Id)], confirm: true));
        Assert.Equal(OrderStatus.Confirmed, last.Status);
        await using var after = database.CreateRetailContext();
        Assert.Equal(AssemblyStatus.Sold, (await after.Assemblies.SingleAsync(entry => entry.Id == build.Id)).Status);
    }

    [Fact]
    public async Task A_multi_copy_build_goes_back_to_draft_when_a_part_has_no_replacement()
    {
        var (retail, database, _) = await StartAsync();
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
        [
            new ReceiptLine("Draft CPU", "New", 2, 100m, 3000m), new ReceiptLine("Draft GPU", "New", 1, 100m, 2000m)
        ]), Actor);
        var build = await BuildAsync(retail, "Short copies", 5000m, items.Select(item => item.Units[0].Id), copies: 2);

        await retail.PosAsync(Sale([BuildLine(build.Id)], confirm: true));

        await using var db = database.CreateRetailContext();
        var card = await db.Assemblies.Include(entry => entry.Units).ThenInclude(link => link.Unit).ThenInclude(unit => unit.StockItem)
            .SingleAsync(entry => entry.Id == build.Id);
        Assert.Equal((AssemblyStatus.Draft, 1), (card.Status, card.Copies));
        var kept = Assert.Single(card.Units);
        Assert.Equal(items[0].Code, kept.Unit.StockItem.Code);
        Assert.Equal(100m, kept.AllocationPercent);
        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => retail.CheckoutAsync(Checkout([BuildLine(build.Id)]))));
    }

    [Fact]
    public async Task Cancelling_a_confirmed_order_releases_its_units_and_a_new_order_holds_nothing()
    {
        var (retail, database, _) = await StartAsync();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Cancel GPU", "New", 3, 100m, 500m));

        var untouched = await retail.PosAsync(Sale([StockLine(item.Id, 3)]));
        await retail.StatusAsync(untouched, OrderStatus.Cancelled);
        Assert.Equal(3, await AvailableAsync(retail, item));

        var order = await retail.PosAsync(Sale([StockLine(item.Id, 2)], confirm: true));
        Assert.Equal(1, await AvailableAsync(retail, item));
        var cancelled = await retail.StatusAsync(order, OrderStatus.Cancelled, "customer changed mind");

        Assert.Equal((OrderStatus.Cancelled, null), (cancelled.Status, cancelled.ReservedUntilUtc));
        Assert.Equal(3, await AvailableAsync(retail, item));
        await using var db = database.CreateRetailContext();
        Assert.All(await db.Units.ToListAsync(), unit => Assert.Equal(UnitStatus.Available, unit.Status));

        var again = await retail.PosAsync(Sale([StockLine(item.Id, 3)], confirm: true));
        Assert.Equal(OrderStatus.Confirmed, again.Status);
    }

    [Fact]
    public async Task Pos_customer_search_returns_the_latest_details_for_a_phone()
    {
        var (retail, _, clock) = await StartAsync();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Known GPU", "New", 3, 100m, 500m));
        await retail.PosAsync(Sale([StockLine(item.Id)], phone: "0100 777 1234"));
        clock.Advance(TimeSpan.FromMinutes(1));
        await retail.PosAsync(Sale([StockLine(item.Id)], phone: "01007771234", fulfillment: OrderFulfillment.Delivery));

        var customer = await retail.Pos.FindCustomerAsync("0100 777 1234");

        Assert.NotNull(customer);
        Assert.Equal(("Customer", "01007771234", 2), (customer.Name, customer.Phone, customer.OrderCount));
        Assert.Equal("Nasr City", customer.Address?.Area);
        Assert.Null(await retail.Pos.FindCustomerAsync("01234567890"));
        Assert.Equal(400, await RetailSales.StatusCodeAsync(() => retail.Pos.FindCustomerAsync("12")));
    }

    // ---------------------------------------------------------------- sold feed

    [Fact]
    public async Task Sold_feed_lists_delivered_lines_without_prices_and_shows_the_governorate_only_when_asked()
    {
        var (retail, _, clock) = await StartAsync();
        var item = await retail.ReceiveItemAsync(new ReceiptLine("Feed Fan", "New", 5, 50m, 300m));
        var units = (await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
            [new ReceiptLine("Feed CPU", "Used", 1, 100m, 3000m), new ReceiptLine("Feed GPU", "New", 1, 100m, 2000m)]), Actor))
            .Select(entry => entry.Units[0].Id).ToList();
        var build = await BuildAsync(retail, "Feed build", 5000m, units);

        await DeliverAsync(retail, await retail.PosAsync(Sale([StockLine(item.Id, 2)])));
        clock.Advance(TimeSpan.FromMinutes(5));
        await DeliverAsync(retail, await retail.PosAsync(Sale([BuildLine(build.Id)], fulfillment: OrderFulfillment.Delivery, fee: 60m)));
        var pending = await retail.PosAsync(Sale([StockLine(item.Id)]));
        Assert.Equal(OrderStatus.New, pending.Status);

        var feed = await retail.SoldFeed.ListAsync(false, null, null, 1);
        Assert.Equal(["Build", "Item"], feed.Items.Select(entry => entry.Kind));
        var buildCard = feed.Items[0];
        Assert.Equal((build.Code, "Used", OrderFulfillment.Delivery), (buildCard.Code, buildCard.Condition, buildCard.FulfillmentMethod));
        Assert.Equal("عنوان", buildCard.Title);
        var itemCard = feed.Items[1];
        Assert.Equal(("Feed Fan", item.Code, "New"), (itemCard.Title, itemCard.Code, itemCard.Condition));
        Assert.All(feed.Items, entry => Assert.Null(entry.Governorate));
        Assert.Equal(2, feed.Total);
        var json = JsonSerializer.Serialize(feed).ToLowerInvariant();
        foreach (var secret in new[] { "price", "amount", "customer", "phone" }) Assert.DoesNotContain(secret, json);

        var withGovernorate = await retail.SoldFeed.ListAsync(true, null, null, 1);
        Assert.Equal("C", withGovernorate.Items.Single(entry => entry.Kind == "Build").Governorate);
        Assert.Null(withGovernorate.Items.Single(entry => entry.Kind == "Item").Governorate);

        Assert.Equal("Build", Assert.Single((await retail.SoldFeed.ListAsync(false, "Build", null, 1)).Items).Kind);
        Assert.Equal("Item", Assert.Single((await retail.SoldFeed.ListAsync(false, "Item", null, 1)).Items).Kind);
        Assert.Empty((await retail.SoldFeed.ListAsync(false, null, Guid.NewGuid(), 1)).Items);
        Assert.Empty((await retail.SoldFeed.ListAsync(false, null, null, 2)).Items);
        Assert.Equal(1, (await retail.SoldFeed.ListAsync(false, null, null, 0)).Page);
    }
}
