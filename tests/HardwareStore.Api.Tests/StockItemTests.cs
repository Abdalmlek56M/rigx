using System.Text.Json;
using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
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
    public async Task Store_availability_filter_lists_in_stock_or_sold_out_items_and_all_lists_sold_out_last()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var items = (await retail.Units.ReceiveAsync(Receive(UsedLine("Free GPU"), UsedLine("Gone GPU")), Actor)).Items;
        foreach (var item in items) await PublishAsync(retail, item);
        await using (var db = database.CreateRetailContext())
            await db.Units.Where(unit => unit.StockItemId == items[1].Id)
                .ExecuteUpdateAsync(set => set.SetProperty(unit => unit.Status, UnitStatus.Sold));

        Assert.Equal("Free GPU", Assert.Single((await retail.Store.ListAsync(new StoreItemQuery(InStock: true))).Items).ModelName);
        Assert.Equal("Gone GPU", Assert.Single((await retail.Store.ListAsync(new StoreItemQuery(InStock: false))).Items).ModelName);
        var all = await retail.Store.ListAsync(new StoreItemQuery());
        Assert.Equal(["Free GPU", "Gone GPU"], all.Items.Select(item => item.ModelName));
        Assert.Equal(["Free GPU", "Gone GPU"], (await retail.Store.ListAsync(new StoreItemQuery(Sort: "price_desc"))).Items.Select(item => item.ModelName));
    }

    [Fact]
    public async Task Store_brand_filter_lists_the_brand_items_and_the_page_carries_the_brand()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var msi = Guid.NewGuid();
        var items = (await retail.Units.ReceiveAsync(Receive(UsedLine("MSI GPU") with { BrandId = msi }, UsedLine("Other GPU")), Actor)).Items;
        foreach (var item in items) await PublishAsync(retail, item);

        Assert.Equal("MSI GPU", Assert.Single((await retail.Store.ListAsync(new StoreItemQuery(BrandId: msi))).Items).ModelName);
        Assert.Equal(2, (await retail.Store.ListAsync(new StoreItemQuery())).Total);
        Assert.Equal(msi, (await retail.Store.GetAsync(items[0].Code)).BrandId);
        Assert.Null((await retail.Store.GetAsync(items[1].Code)).BrandId);
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
        var brandId = Guid.NewGuid();
        StockItemUpdate Update(long version) => new("New Name", null, brandId, null, 900m, 50m, 100m, "سنة", 12, true, false, null, null, version);

        var updated = await retail.StockItems.UpdateAsync(item.Id, Update(item.RowVersion));
        Assert.Equal("New Name", updated.ModelName);
        Assert.Equal(brandId, updated.BrandId);
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
    public async Task A_unit_inside_a_build_shows_which_build_it_is_in_and_a_loose_unit_shows_none()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var receipt = await retail.Units.ReceiveAsync(Receive(UsedLine("CPU"), UsedLine("Board")), Actor);
        var cpu = receipt.Items[0];
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Gaming PC", [cpu.Units[0].Id]));

        var inBuild = await retail.StockItems.GetAsync(cpu.Id, true, true);
        var loose = await retail.StockItems.GetAsync(receipt.Items[1].Id, true, true);

        var ref1 = inBuild.Units.Single().Build;
        Assert.NotNull(ref1);
        Assert.Equal((build.Code, "Gaming PC"), (ref1.Code, ref1.Name));
        Assert.Null(loose.Units.Single().Build);
        var listed = (await retail.StockItems.ListAsync(new StockItemQuery(), true, true)).Items.Single(item => item.Id == cpu.Id);
        Assert.Equal(build.Code, listed.Units.Single().Build?.Code);
    }

    [Fact]
    public async Task Photos_can_be_put_in_a_new_order_so_any_photo_becomes_the_main_one()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var item = (await retail.Units.ReceiveAsync(Receive(UsedLine("RTX 3060")), Actor)).Items.Single();
        await PublishAsync(retail, item, 3);
        var keys = (await retail.StockItems.GetAsync(item.Id, true, true)).Photos.Select(photo => photo.StorageKey).ToList();

        var reordered = await retail.StockItems.ReorderPhotosAsync(item.Id, [keys[2], keys[0], keys[1]]);

        Assert.Equal([keys[2], keys[0], keys[1]], reordered.Select(photo => photo.StorageKey));
        Assert.Equal(keys[2], (await retail.StockItems.GetAsync(item.Id, true, true)).Photos[0].StorageKey);
        Assert.Equal(400, (await Assert.ThrowsAsync<RetailException>(() => retail.StockItems.ReorderPhotosAsync(item.Id, [keys[0], keys[1]]))).StatusCode);
        Assert.Equal(400, (await Assert.ThrowsAsync<RetailException>(() => retail.StockItems.ReorderPhotosAsync(item.Id, [keys[0], keys[0], keys[1]]))).StatusCode);
        Assert.Equal(400, (await Assert.ThrowsAsync<RetailException>(() => retail.StockItems.ReorderPhotosAsync(item.Id, [keys[0], keys[1], "other.webp"]))).StatusCode);
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

    [Fact]
    public async Task A_model_suggestion_carries_the_newest_item_with_photos_as_the_template_to_copy()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var gpu = UsedLine("RTX 4060 8GB") with { Specs = new Dictionary<string, string> { ["vram"] = "8" }, BrandId = Guid.NewGuid() };
        var items = (await retail.Units.ReceiveAsync(Receive(gpu, gpu, gpu), Actor)).Items;
        await retail.StockItems.AddPhotoAsync(items[1].Id, new StockItemPhoto("items/a.webp", "items/a-t.webp"));

        var template = Assert.Single(await retail.StockItems.SuggestModelsAsync("rtx 4060", null)).Template;

        Assert.NotNull(template);
        Assert.Equal(items[1].Id, template.ItemId);
        Assert.Equal(items[1].Code, template.Code);
        Assert.Equal(gpu.BrandId, template.BrandId);
        Assert.Equal("8", template.Specs["vram"]);
        Assert.Equal(1, template.PhotoCount);
        Assert.Single(await retail.StockItems.FindPhotosAsync(items[1].Id));
        Assert.Empty(await retail.StockItems.FindPhotosAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Items_get_readable_slugs_and_are_found_by_slug_or_old_code()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var items = (await retail.Units.ReceiveAsync(Receive(UsedLine("RTX 3060 12GB PNY", 3)), Actor)).Items;
        var ordered = items.OrderBy(item => item.Code).ToList();
        foreach (var item in ordered) await PublishAsync(retail, item);
        var slugs = new List<string>();
        foreach (var item in ordered) slugs.Add((await retail.Store.GetAsync(item.Code)).Slug);
        Assert.Equal(["rtx-3060-12gb-pny", "rtx-3060-12gb-pny-2", "rtx-3060-12gb-pny-3"], slugs);
        var first = ordered[0];

        var bySlug = await retail.Store.GetAsync("rtx-3060-12gb-pny");
        var byCode = await retail.Store.GetAsync(first.Code.ToLowerInvariant());
        Assert.Equal("rtx-3060-12gb-pny", bySlug.Slug);
        Assert.Equal(bySlug.Slug, byCode.Slug);
        Assert.Equal(404, (await Assert.ThrowsAsync<RetailException>(() => retail.Store.GetAsync("no-such-item"))).StatusCode);
    }

    [Fact]
    public async Task Renaming_changes_the_slug_unless_only_spacing_or_case_changed()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var items = (await retail.Units.ReceiveAsync(Receive(UsedLine("Old Name"), UsedLine("New Name")), Actor)).Items;
        var item = items.Single(i => i.ModelName == "Old Name");
        StockItemUpdate Update(string name, long version) => new(name, null, null, null, 900m, 50m, 100m, "سنة", 12, true, false, null, null, version);

        await PublishAsync(retail, item);
        async Task<string> SlugAsync() => (await retail.Store.GetAsync(item.Code)).Slug;
        Assert.Equal("old-name", await SlugAsync());

        var current = await retail.StockItems.GetAsync(item.Id, false, false);
        var same = await retail.StockItems.UpdateAsync(item.Id, Update("  OLD   name ", current.RowVersion));
        Assert.Equal("old-name", await SlugAsync());
        var renamed = await retail.StockItems.UpdateAsync(item.Id, Update("New Name", same.RowVersion));
        Assert.Equal("new-name-2", await SlugAsync());
        await retail.StockItems.UpdateAsync(item.Id, Update("Fresh Name", renamed.RowVersion));
        Assert.Equal("fresh-name", await SlugAsync());
    }

    [Fact]
    public async Task An_old_slug_keeps_leading_to_the_renamed_item_and_is_never_given_to_another_item()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var item = Assert.Single((await retail.Units.ReceiveAsync(Receive(UsedLine("RTX 4070 Super")), Actor)).Items);
        await PublishAsync(retail, item);
        StockItemUpdate Update(string name, long version) => new(name, null, null, null, 900m, 50m, 100m, "سنة", 12, true, false, null, null, version);

        var current = await retail.StockItems.GetAsync(item.Id, false, false);
        var renamed = await retail.StockItems.UpdateAsync(item.Id, Update("RTX 4070 Super Gaming OC", current.RowVersion));
        var byOldSlug = await retail.Store.GetAsync("rtx-4070-super");
        Assert.Equal(item.Code, byOldSlug.Code);
        Assert.Equal("rtx-4070-super-gaming-oc", byOldSlug.Slug);

        var newcomer = Assert.Single((await retail.Units.ReceiveAsync(Receive(UsedLine("RTX 4070 Super")), Actor)).Items);
        await PublishAsync(retail, newcomer);
        Assert.Equal("rtx-4070-super-2", (await retail.Store.GetAsync(newcomer.Code)).Slug);
        Assert.Equal(item.Code, (await retail.Store.GetAsync("rtx-4070-super")).Code);

        await retail.StockItems.UpdateAsync(item.Id, Update("RTX 4070 Super", renamed.RowVersion));
        Assert.Equal("rtx-4070-super", (await retail.Store.GetAsync(item.Code)).Slug);
        Assert.Equal(item.Code, (await retail.Store.GetAsync("rtx-4070-super-gaming-oc")).Code);
    }
}
