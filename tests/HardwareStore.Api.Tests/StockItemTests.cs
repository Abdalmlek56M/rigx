using System.Text.Json;
using HardwareStore.Modules.Retail;
using Xunit;
using static HardwareStore.Api.Tests.RetailReceipts;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class StockItemPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();

    private static ReceiveRequest Receive(params ReceiptLine[] lines) => Receipt(Guid.NewGuid(), null, lines);

    private static ReceiptLine UsedLine(string model, int quantity = 1, decimal price = 1000m) =>
        new(model, "Used", quantity, 600m, price, SerialNumber: quantity == 1 ? "SN-" + model : null);

    private static ReceiptLine NewLine(string model, int quantity, decimal price = 500m) =>
        new(model, "New", quantity, 300m, price);

    private static async Task PublishAsync(RetailTestServices retail, StockItemAdminView item, int photos = 3)
    {
        for (var index = 0; index < photos; index++)
            await retail.StockItems.AddPhotoAsync(item.Id, new StockItemPhoto($"items/{item.Code}/{index}.webp", $"items/{item.Code}/{index}-t.webp"));
    }

    [Fact]
    public async Task Used_lines_become_one_item_per_piece_and_new_lines_become_one_lot()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();

        var receipt = await retail.Units.ReceiveAsync(Receive(UsedLine("RTX 3060", 2), NewLine("Case Fan", 3)), Actor);

        Assert.Equal(3, receipt.Items.Count);
        var used = receipt.Items.Where(item => item.Condition == "Used").ToList();
        Assert.Equal(2, used.Count);
        Assert.All(used, item => Assert.Single(item.Units));
        Assert.Equal(["RX-0001", "RX-0002"], used.Select(item => item.Code));
        var lot = Assert.Single(receipt.Items, item => item.Condition == "New");
        Assert.Equal("RX-0003", lot.Code);
        Assert.Equal(3, lot.Units.Count);
        Assert.Equal(3, lot.AvailableCount);
    }

    [Fact]
    public async Task Receiving_the_same_draft_twice_returns_the_same_items()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var request = Receive(UsedLine("RX 6600"));
        var first = await retail.Units.ReceiveAsync(request, Actor);
        var second = await retail.Units.ReceiveAsync(request, Actor);
        Assert.Equal(first.ReceiptId, second.ReceiptId);
        Assert.Equal(first.Items.Select(item => item.Code), second.Items.Select(item => item.Code));
        Assert.Single((await retail.StockItems.ListAsync(new StockItemQuery(), true, true)).Items);
    }

    [Fact]
    public async Task Store_list_shows_only_visible_items_with_three_photos()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var items = (await retail.Units.ReceiveAsync(Receive(UsedLine("Shown GPU"), UsedLine("Two Photos GPU"), UsedLine("Hidden GPU")), Actor)).Items;
        await PublishAsync(retail, items[0]);
        await PublishAsync(retail, items[1], 2);
        await PublishAsync(retail, items[2]);
        await retail.StockItems.SetVisibilityAsync(new StockVisibilityRequest([items[2].Id], true));

        var page = await retail.Store.ListAsync(new StoreItemQuery());

        var card = Assert.Single(page.Items);
        Assert.Equal("Shown GPU", card.ModelName);
        Assert.Equal(1, page.Total);
        Assert.Equal(1, page.Counts.Used);
        Assert.Equal(0, page.Counts.New);
        Assert.Equal($"items/{items[0].Code}/0-t.webp", card.ThumbnailStorageKey);
        var detailError = await Assert.ThrowsAsync<RetailException>(() => retail.Store.GetAsync(items[1].Code));
        Assert.Equal(404, detailError.StatusCode);
    }

    [Fact]
    public async Task Store_search_finds_items_by_code_and_by_any_model_spelling_but_never_by_serial()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var items = (await retail.Units.ReceiveAsync(Receive(UsedLine("RTX 3060 12GB"), UsedLine("Ryzen 5 5600")), Actor)).Items;
        foreach (var item in items) await PublishAsync(retail, item);

        var byCode = await retail.Store.ListAsync(new StoreItemQuery("rx-1"));
        Assert.Equal("RTX 3060 12GB", Assert.Single(byCode.Items).ModelName);
        var byKey = await retail.Store.ListAsync(new StoreItemQuery("rtx3060"));
        Assert.Equal(items[0].Code, Assert.Single(byKey.Items).Code);
        Assert.Empty((await retail.Store.ListAsync(new StoreItemQuery("SN-Ryzen 5 5600"))).Items);
        var adminBySerial = await retail.StockItems.ListAsync(new StockItemQuery("SN-Ryzen 5 5600"), true, true);
        Assert.Equal(items[1].Code, Assert.Single(adminBySerial.Items).Code);
    }

    [Fact]
    public async Task Admin_serial_search_needs_serial_access()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        await retail.Units.ReceiveAsync(Receive(new ReceiptLine("Serial GPU", "Used", 1, 100m, 200m, SerialNumber: "ABC-123")), Actor);

        Assert.Single((await retail.StockItems.ListAsync(new StockItemQuery("ABC-123"), true, true)).Items);
        Assert.Empty((await retail.StockItems.ListAsync(new StockItemQuery("ABC-123"), true, false)).Items);
    }

    [Fact]
    public async Task Store_views_never_expose_cost_serial_partner_or_internal_notes()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var partner = await retail.CreateAsync("Secret Partner", 10m);
        var line = UsedLine("Secret GPU") with { PartnerPercentOverride = 15m, PartnerId = partner.Id, InternalNote = "bought from Ahmed" };
        var item = Assert.Single((await retail.Units.ReceiveAsync(Receive(line), Actor)).Items);
        await PublishAsync(retail, item);

        var json = JsonSerializer.Serialize(new object[]
        {
            await retail.Store.ListAsync(new StoreItemQuery()), await retail.Store.GetAsync(item.Code)
        }).ToLowerInvariant();

        foreach (var secret in new[] { "cost", "serial", "partner", "internalnote", "sn-secret", "ahmed" })
            Assert.DoesNotContain(secret, json);
        var admin = await retail.StockItems.GetAsync(item.Id, includeCosts: false, includeSerials: false);
        var unit = Assert.Single(admin.Units);
        Assert.Null(unit.AcquisitionCost);
        Assert.Null(unit.SerialNumber);
        Assert.Null(unit.PartnerPercentOverride);
        Assert.Null(unit.PartnerId);
    }

    [Fact]
    public async Task Updating_with_a_stale_row_version_is_a_conflict()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var item = Assert.Single((await retail.Units.ReceiveAsync(Receive(UsedLine("Old Name")), Actor)).Items);
        StockItemUpdate Update(long version) => new("New Name", null, null, 900m, 50m, 100m, "سنة", 12, true, false, null, null, version);

        var updated = await retail.StockItems.UpdateAsync(item.Id, Update(item.RowVersion));
        Assert.Equal("New Name", updated.ModelName);
        Assert.Equal(new StockItemPrices(900m, 950m, 1000m), updated.Prices);
        Assert.Equal(item.RowVersion + 1, updated.RowVersion);

        var stale = await Assert.ThrowsAsync<RetailException>(() => retail.StockItems.UpdateAsync(item.Id, Update(item.RowVersion)));
        Assert.Equal(409, stale.StatusCode);
        Assert.Equal("The item was changed by another user.", stale.Message);
        var badWarranty = await Assert.ThrowsAsync<RetailException>(() =>
            retail.StockItems.UpdateAsync(item.Id, Update(updated.RowVersion) with { WarrantyMonths = 500 }));
        Assert.Equal(400, badWarranty.StatusCode);
    }

    [Fact]
    public async Task Order_lines_for_a_stock_item_claim_its_units_and_fail_when_not_enough_are_available()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var item = Assert.Single((await retail.Units.ReceiveAsync(Receive(NewLine("Case Fan", 3)), Actor)).Items);

        Task<OrderView> PlaceAsync(int quantity) => retail.PosAsync(RetailSales.Sale([RetailSales.StockLine(item.Id, quantity)]));

        var first = await PlaceAsync(2);
        Assert.Equal(item.Id, Assert.Single(first.Items).StockItemId);
        var confirmed = await retail.StatusAsync(first, OrderStatus.Confirmed);
        Assert.Equal(2, Assert.Single(confirmed.Items).Components.Count);
        Assert.Equal(1, (await retail.StockItems.GetAsync(item.Id, true, true)).AvailableCount);

        Assert.Equal(409, await RetailSales.StatusCodeAsync(() => PlaceAsync(2)));
        var second = await PlaceAsync(1);
        Assert.Equal(OrderStatus.Confirmed, (await retail.StatusAsync(second, OrderStatus.Confirmed)).Status);
        Assert.Equal(0, (await retail.StockItems.GetAsync(item.Id, true, true)).AvailableCount);
    }

    [Fact]
    public async Task Builds_get_sequential_codes_and_default_allocations_follow_alone_prices()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var units = await retail.ReceiveUnitsAsync(Receive(UsedLine("CPU", price: 3000m), UsedLine("Board", price: 1000m), UsedLine("PSU")), Actor);

        var first = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Build A", [units[0].Id, units[1].Id]));
        var second = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Build B", [units[2].Id]));

        Assert.Equal("BLD-01", first.Code);
        Assert.Equal("BLD-02", second.Code);
        var cpu = first.Units.Single(unit => unit.UnitId == units[0].Id);
        Assert.Equal(75m, cpu.AllocationPercent);
        Assert.Equal(3000m, cpu.StandalonePrice);
        Assert.Equal("RX-0001", cpu.StockItemCode);
    }

    [Fact]
    public async Task Model_suggestions_group_spellings_of_the_same_model()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        await retail.Units.ReceiveAsync(Receive(UsedLine("RX 6600"), UsedLine("rx6600"), UsedLine("RX-6600"), UsedLine("RX 6700 XT"), UsedLine("GTX 1650")), Actor);

        var suggestions = await retail.StockItems.SuggestModelsAsync("rx-6600", null);

        var suggestion = Assert.Single(suggestions);
        Assert.Equal(3, suggestion.ItemCount);
        Assert.Contains(suggestion.ModelName, new[] { "RX 6600", "rx6600", "RX-6600" });
        Assert.Empty(await retail.StockItems.SuggestModelsAsync("r", null));
    }
}
