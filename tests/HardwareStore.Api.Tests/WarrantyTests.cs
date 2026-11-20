using System.Text.Json;
using HardwareStore.Modules.Retail;
using Xunit;
using static HardwareStore.Api.Tests.RetailReceipts;
using static HardwareStore.Api.Tests.RetailSales;

namespace HardwareStore.Api.Tests;

/// <summary>Warranty registry: issue on delivery, snapshot at claim, public lookup, voiding and the admin list (roadmap 2.5).</summary>
[Trait("Category", "RequiresDocker")]
public sealed class WarrantyPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();
    private const string Store = "ضمان المحل";

    private async Task<(RetailTestServices Retail, MutableTimeProvider Clock)> StartAsync()
    {
        var database = await fixture.CreateDatabaseAsync();
        var clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        return (database.CreateRetailServices(clock), clock);
    }

    private static ReceiptLine Line(string model, int quantity = 1, string? label = Store, int? months = 3, string? serial = null) =>
        new(model, "New", quantity, 100m, 500m, SerialNumber: serial, WarrantyLabel: label, WarrantyMonths: months);

    private static async Task<OrderView> DeliverAsync(RetailTestServices retail, OrderView order) =>
        await retail.StatusesAsync(order, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Delivered);

    /// <summary>Sells one published item as a pickup order and delivers it.</summary>
    private static async Task<(StockItemAdminView Item, OrderView Order)> SoldAsync(RetailTestServices retail, ReceiptLine line,
        string phone = "01289186840", Guid? userId = null)
    {
        var item = await retail.ReceiveItemAsync(line);
        await retail.PublishAsync(item);
        var order = await retail.CheckoutAsync(Checkout([StockLine(item.Id)], OrderFulfillment.Pickup, phone: phone), Policy(userId));
        return (item, await DeliverAsync(retail, order));
    }

    private static async Task<WarrantyAdminView> OnlyAdminRowAsync(RetailTestServices retail, string query) =>
        Assert.Single((await retail.Warranties.ListAdminAsync(query, null, 1, true, true)).Items);

    private static async Task<int> AdminTotalAsync(RetailTestServices retail, string? status = null) =>
        (await retail.Warranties.ListAdminAsync(null, status, 1, true, true)).Total;

    private static void AssertNear(DateTimeOffset expected, DateTimeOffset? actual)
    {
        Assert.NotNull(actual);
        Assert.True(Math.Abs((expected - actual.Value).TotalSeconds) < 1, $"Expected about {expected:O} but was {actual:O}.");
    }

    // ---------------------------------------------------------------- issue

    [Fact]
    public async Task Delivery_issues_a_warranty_that_ends_after_the_item_months()
    {
        var (retail, clock) = await StartAsync();
        var (item, order) = await SoldAsync(retail, Line("Warranty GPU", serial: "SN-A1B2C3"));
        var now = clock.GetUtcNow();

        var row = await OnlyAdminRowAsync(retail, item.Code);

        Assert.Equal((item.Code, "Warranty GPU", "New", Store, order.Reference), (row.Code, row.Name, row.Condition, row.Label, row.OrderReference));
        Assert.Null(row.BuildCode);
        Assert.Equal(WarrantyStatus.Active, row.Status);
        AssertNear(now, row.StartsAtUtc);
        AssertNear(now.AddMonths(3), row.EndsAtUtc);
        Assert.Equal((int)Math.Ceiling((now.AddMonths(3) - now).TotalDays), row.DaysLeft);
        Assert.Equal(("01289186840", "SN-A1B2C3"), (row.PhoneNumber, row.SerialNumber));
    }

    [Fact]
    public async Task An_item_without_warranty_issues_none()
    {
        var (retail, _) = await StartAsync();
        var (_, order) = await SoldAsync(retail, Line("No warranty", label: null, months: null));

        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.Equal(0, await AdminTotalAsync(retail));
    }

    [Fact]
    public async Task A_manufacturer_warranty_has_no_end_date_and_stays_active()
    {
        var (retail, clock) = await StartAsync();
        var (item, _) = await SoldAsync(retail, Line("Agent GPU", label: "ضمان الوكيل", months: null));
        clock.Advance(TimeSpan.FromDays(5000));

        var row = await OnlyAdminRowAsync(retail, item.Code);

        Assert.Null(row.EndsAtUtc);
        Assert.Null(row.DaysLeft);
        Assert.Equal(WarrantyStatus.Active, row.Status);
    }

    [Fact]
    public async Task A_pos_sale_issues_its_warranty_only_when_delivered()
    {
        var (retail, _) = await StartAsync();
        var item = await retail.ReceiveItemAsync(Line("POS GPU", 2));
        var sale = await retail.PosAsync(Sale([StockLine(item.Id)]));
        var confirmed = await retail.StatusAsync(sale, OrderStatus.Confirmed);

        Assert.Equal(0, await AdminTotalAsync(retail));

        await retail.StatusesAsync(confirmed, OrderStatus.Preparing, OrderStatus.Delivered);

        Assert.Equal(1, await AdminTotalAsync(retail));
    }

    [Fact]
    public async Task A_build_gives_each_part_its_own_warranty_and_the_snapshot_survives_the_copies_refill()
    {
        var (retail, _) = await StartAsync();
        var items = await retail.ReceiveItemsAsync(Receipt(Guid.NewGuid(), null,
            [Line("Build CPU", 2, "Item", 1), Line("Build GPU", 2, "Item", 1)]), Actor);
        var first = items.Select(item => item.Units[0].Id).ToList();
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Warranty build", first));
        var card = await retail.Assemblies.UpdateAssemblyAsync(build.Id,
            new AssemblyUpdateRequest("Warranty build", "عنوان", "Title", 5000m, build.RowVersion, WarrantyLabel: "Build", WarrantyMonths: 12, Copies: 2));
        var cpu = card.Units.Single(unit => unit.StockItemCode == items[0].Code);
        card = await retail.Parts.UpdatePartAsync(card.Id, cpu.UnitId,
            new AssemblyPartUpdateRequest(cpu.PartPrice, "Part", 6, null, null, card.RowVersion));
        await retail.Assemblies.SetAssemblyPublishedAsync(card.Id, true, card.RowVersion);

        var order = await DeliverAsync(retail, await retail.PosAsync(Sale([BuildLine(build.Id)])));

        var rows = (await retail.Warranties.ListAdminAsync(null, null, 1, true, true)).Items;
        Assert.Equal(2, rows.Count);
        var cpuRow = rows.Single(row => row.Code == items[0].Code);
        var gpuRow = rows.Single(row => row.Code == items[1].Code);
        Assert.Equal(("Part", build.Code, order.Reference), (cpuRow.Label, cpuRow.BuildCode, cpuRow.OrderReference));
        Assert.Equal(("Build", build.Code), (gpuRow.Label, gpuRow.BuildCode));
        AssertNear(cpuRow.StartsAtUtc.AddMonths(6), cpuRow.EndsAtUtc);
        AssertNear(gpuRow.StartsAtUtc.AddMonths(12), gpuRow.EndsAtUtc);
    }

    // ---------------------------------------------------------------- public lookup

    [Fact]
    public async Task Public_lookup_returns_a_flat_newest_first_list_without_order_phone_or_serial()
    {
        var (retail, clock) = await StartAsync();
        var (first, firstOrder) = await SoldAsync(retail, Line("Lookup GPU", serial: "SN-SHARED-9"));
        clock.Advance(TimeSpan.FromHours(1));
        var (second, secondOrder) = await SoldAsync(retail, Line("Lookup RAM", serial: "SN-SHARED-9"));

        var result = await retail.Warranties.LookupAsync("sn-shared-9");

        Assert.Equal([second.Code, first.Code], result.Items.Select(item => item.Code));
        var json = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("SN-SHARED-9", json);
        Assert.DoesNotContain(firstOrder.Reference, json);
        Assert.DoesNotContain(secondOrder.Reference, json);
        Assert.DoesNotContain("01289186840", json);
        Assert.DoesNotContain("Customer", json);
    }

    [Fact]
    public async Task Public_lookup_finds_by_item_code_and_serial_and_unknown_is_empty()
    {
        var (retail, _) = await StartAsync();
        var (item, _) = await SoldAsync(retail, Line("Find GPU", serial: "SN-Find-77"));

        foreach (var query in new[] { item.Code, item.Code.ToLowerInvariant(), "sn-find-77" })
            Assert.Equal(item.Code, Assert.Single((await retail.Warranties.LookupAsync(query)).Items).Code);
        Assert.Empty((await retail.Warranties.LookupAsync("RX-9999")).Items);
        Assert.Equal(400, await StatusCodeAsync(() => retail.Warranties.LookupAsync(" ")));
    }

    [Theory]
    [InlineData("0128 918 6840")]
    [InlineData("RIGX-00000000-0000")]
    [InlineData("INV-2026-0001")]
    public async Task Public_lookup_refuses_phone_order_reference_and_invoice_number(string query)
    {
        var (retail, _) = await StartAsync();
        await SoldAsync(retail, Line("Private GPU"));

        Assert.Equal(400, await StatusCodeAsync(() => retail.Warranties.LookupAsync(query)));
    }

    [Fact]
    public async Task Admin_list_still_finds_by_phone_and_order_reference()
    {
        var (retail, _) = await StartAsync();
        var (item, order) = await SoldAsync(retail, Line("Admin find GPU"));

        Assert.Equal(item.Code, Assert.Single((await retail.Warranties.ListAdminAsync("0128 918 6840", null, 1, true, true)).Items).Code);
        Assert.Equal(item.Code, Assert.Single((await retail.Warranties.ListAdminAsync(order.Reference, null, 1, true, true)).Items).Code);
    }

    [Fact]
    public async Task Status_turns_expired_when_the_clock_passes_the_end_date()
    {
        var (retail, clock) = await StartAsync();
        var (item, _) = await SoldAsync(retail, Line("Expiring GPU", months: 1));

        Assert.Equal(WarrantyStatus.Active, Assert.Single((await retail.Warranties.LookupAsync(item.Code)).Items).Status);
        clock.Advance(TimeSpan.FromDays(45));

        var view = Assert.Single((await retail.Warranties.LookupAsync(item.Code)).Items);
        Assert.Equal((WarrantyStatus.Expired, 0), (view.Status, view.DaysLeft));
        Assert.Equal(1, await AdminTotalAsync(retail, WarrantyStatus.Expired));
        Assert.Equal(0, await AdminTotalAsync(retail, WarrantyStatus.Active));
    }

    // ---------------------------------------------------------------- voiding

    [Fact]
    public async Task Cancelling_a_delivered_order_voids_its_warranties()
    {
        var (retail, _) = await StartAsync();
        var (item, order) = await SoldAsync(retail, Line("Cancelled GPU"));

        await retail.StatusAsync(order, OrderStatus.Cancelled);

        var row = await OnlyAdminRowAsync(retail, item.Code);
        Assert.Equal((WarrantyStatus.Void, "Order cancelled", (int?)null), (row.Status, row.VoidReason, row.DaysLeft));
    }

    [Fact]
    public async Task An_accepted_return_voids_only_that_unit_and_a_rejected_return_keeps_it()
    {
        var (retail, _) = await StartAsync();
        var item = await retail.ReceiveItemAsync(Line("Return GPU", 2));
        await retail.PublishAsync(item);
        var order = await DeliverAsync(retail, await retail.CheckoutAsync(Checkout([StockLine(item.Id, 2)], OrderFulfillment.Pickup)));
        var line = order.Items.Single();
        var units = line.Components.Select(component => component.UnitId).Order().ToList();

        var accepted = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(order.Id, line.Id, units[0], 0m, "Faulty"), Actor);
        await retail.Returns.InspectReturnAsync(accepted.Id, new ReturnInspectRequest("Used", null, accepted.RowVersion), Actor);
        var rejected = await retail.Returns.CreateReturnAsync(new ReturnCreateRequest(order.Id, line.Id, units[1], 0m, "Changed mind"), Actor);
        await retail.Returns.RejectReturnAsync(rejected.Id, new ReturnRejectRequest("Not covered", rejected.RowVersion), Actor);

        var rows = (await retail.Warranties.ListAdminAsync(null, null, 1, true, true)).Items;
        Assert.Equal(2, rows.Count);
        Assert.Equal(1, await AdminTotalAsync(retail, WarrantyStatus.Void));
        Assert.Equal(1, await AdminTotalAsync(retail, WarrantyStatus.Active));
        Assert.Equal("Returned", Assert.Single(rows, row => row.Status == WarrantyStatus.Void).VoidReason);
    }

    [Fact]
    public async Task Admin_void_records_the_reason_and_rejects_a_second_void_and_a_stale_row_version()
    {
        var (retail, _) = await StartAsync();
        var (item, _) = await SoldAsync(retail, Line("Sticker GPU"));
        var row = await OnlyAdminRowAsync(retail, item.Code);

        Assert.Equal(409, await StatusCodeAsync(() => retail.Warranties.VoidAsync(row.Id, new WarrantyVoidRequest("Sticker removed", row.RowVersion + 5), Actor)));
        Assert.Equal(400, await StatusCodeAsync(() => retail.Warranties.VoidAsync(row.Id, new WarrantyVoidRequest(" ", row.RowVersion), Actor)));
        Assert.Equal(404, await StatusCodeAsync(() => retail.Warranties.VoidAsync(Guid.NewGuid(), new WarrantyVoidRequest("x", 1), Actor)));
        var voided = await retail.Warranties.VoidAsync(row.Id, new WarrantyVoidRequest("Sticker removed", row.RowVersion), Actor);

        Assert.Equal((WarrantyStatus.Void, "Sticker removed", row.RowVersion + 1), (voided.Status, voided.VoidReason, voided.RowVersion));
        Assert.Equal("01289186840", voided.PhoneNumber);
        Assert.Equal(409, await StatusCodeAsync(() => retail.Warranties.VoidAsync(row.Id, new WarrantyVoidRequest("Again", voided.RowVersion), Actor)));
        var publicView = Assert.Single((await retail.Warranties.LookupAsync(item.Code)).Items);
        Assert.Equal(WarrantyStatus.Void, publicView.Status);
    }

    // ---------------------------------------------------------------- account and admin lists

    [Fact]
    public async Task Customers_see_only_the_warranties_of_their_own_account()
    {
        var (retail, _) = await StartAsync();
        var mine = Guid.NewGuid();
        var (item, order) = await SoldAsync(retail, Line("Mine GPU"), userId: mine);
        await SoldAsync(retail, Line("Guest GPU"));
        await SoldAsync(retail, Line("Someone else"), userId: Guid.NewGuid());

        var warranties = await retail.Warranties.ListMineAsync(mine);

        var own = Assert.Single(warranties);
        Assert.Equal((order.Id, order.Reference, item.Code), (own.OrderId, own.OrderReference, own.Code));
        Assert.Empty(await retail.Warranties.ListMineAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Admin_list_hides_phone_and_serial_without_the_flags_and_validates_status_and_query()
    {
        var (retail, _) = await StartAsync();
        var (item, _) = await SoldAsync(retail, Line("Hidden GPU", serial: "SN-HIDE-1"));

        var plain = Assert.Single((await retail.Warranties.ListAdminAsync(null, null, 1, false, false)).Items);
        Assert.Equal((null, null, null), (plain.CustomerName, plain.PhoneNumber, plain.SerialNumber));
        var full = Assert.Single((await retail.Warranties.ListAdminAsync(item.Code, "Active", 1, true, true)).Items);
        Assert.Equal(("Customer", "01289186840", "SN-HIDE-1"), (full.CustomerName, full.PhoneNumber, full.SerialNumber));
        var page = await retail.Warranties.ListAdminAsync("sn-hide-1", null, 0, false, false);
        Assert.Equal((1, 50, 1), (page.Page, page.PageSize, page.Total));
        Assert.Equal(400, await StatusCodeAsync(() => retail.Warranties.ListAdminAsync(null, "Broken", 1, true, true)));
        Assert.Equal(400, await StatusCodeAsync(() => retail.Warranties.ListAdminAsync("ab", null, 1, true, true)));
    }
}
