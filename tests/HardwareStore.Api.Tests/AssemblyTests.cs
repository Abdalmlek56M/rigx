using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static HardwareStore.Api.Tests.RetailReceipts;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class AssemblyPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly Guid Actor = Guid.NewGuid();

    private static ReceiptLine Line(string name, decimal cost, decimal price, string? warrantyLabel = null, int? warrantyMonths = null) =>
        new(name, "New", 1, cost, price, WarrantyLabel: warrantyLabel, WarrantyMonths: warrantyMonths);

    private static AssemblyUpdateRequest Card(AssemblyView build, decimal price = 1000m, string? usage = null, string? sellMode = null,
        string? warrantyLabel = null, int? warrantyMonths = null, int copies = 1, string? suitableAr = null, string? suitableEn = null) =>
        new(build.Name, "عنوان", "Title", price, build.RowVersion, usage, sellMode, warrantyLabel, warrantyMonths, copies, suitableAr, suitableEn);

    private static async Task<(RetailTestServices Retail, OwnerBootstrapDatabase Database, List<StockUnitView> Units)> StartAsync(
        OwnerBootstrapPostgreSqlFixture fixture, params ReceiptLine[] lines)
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        return (retail, database, await retail.ReceiveUnitsAsync(Receipt(Guid.NewGuid(), null, lines), Actor));
    }

    private static async Task<int> StatusOfAsync(Func<Task> action) => (await Assert.ThrowsAsync<RetailException>(action)).StatusCode;

    [Fact]
    public async Task Build_card_is_saved_and_validated()
    {
        var (retail, _, units) = await StartAsync(fixture, Line("Card CPU", 100m, 400m), Line("Card GPU", 100m, 600m));
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Card", units.Select(unit => unit.Id).ToList()));
        Assert.Equal(1, build.Copies);
        Assert.Equal(AssemblySellMode.Full, build.SellMode);

        var saved = await retail.Assemblies.UpdateAssemblyAsync(build.Id, Card(build, 2000m, AssemblyUsage.Gaming, AssemblySellMode.FullOrSplit,
            "ضمان", 12, 3, "ألعاب", "Gaming"));
        Assert.Equal(AssemblyUsage.Gaming, saved.Usage);
        Assert.Equal(AssemblySellMode.FullOrSplit, saved.SellMode);
        Assert.Equal(("ضمان", (int?)12), (saved.WarrantyLabel, saved.WarrantyMonths));
        Assert.Equal(3, saved.Copies);
        Assert.Equal(("ألعاب", "Gaming"), (saved.SuitableForAr, saved.SuitableForEn));
        Assert.Equal(2000m, saved.SalePrice);

        Assert.Equal(400, await StatusOfAsync(() => retail.Assemblies.UpdateAssemblyAsync(build.Id, Card(saved, usage: "Cooking"))));
        Assert.Equal(400, await StatusOfAsync(() => retail.Assemblies.UpdateAssemblyAsync(build.Id, Card(saved, sellMode: "Both"))));
        Assert.Equal(400, await StatusOfAsync(() => retail.Assemblies.UpdateAssemblyAsync(build.Id, Card(saved, copies: 0))));
        Assert.Equal(400, await StatusOfAsync(() => retail.Assemblies.UpdateAssemblyAsync(build.Id, Card(saved, copies: 100))));
    }

    [Fact]
    public async Task Effective_warranty_uses_part_override_then_build_then_item()
    {
        var (retail, _, units) = await StartAsync(fixture, Line("War CPU", 100m, 400m, "Item", 3), Line("War GPU", 100m, 600m, "Item", 3));
        var (cpu, gpu) = (units[0].Id, units[1].Id);
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Warranty", [cpu, gpu]));
        Assert.All(build.Units, unit => Assert.Equal(("Item", (int?)3), (unit.WarrantyLabel, unit.WarrantyMonths)));

        var withWarranty = await retail.Assemblies.UpdateAssemblyAsync(build.Id, Card(build, warrantyLabel: "Build", warrantyMonths: 12));
        Assert.All(withWarranty.Units, unit => Assert.Equal(("Build", (int?)12), (unit.WarrantyLabel, unit.WarrantyMonths)));
        Assert.All(withWarranty.Units, unit => Assert.False(unit.WarrantyOverridden));

        var overridden = await retail.Parts.UpdatePartAsync(build.Id, cpu,
            new AssemblyPartUpdateRequest(400m, "Part", 6, null, null, withWarranty.RowVersion));
        var cpuView = overridden.Units.Single(unit => unit.UnitId == cpu);
        Assert.Equal(("Part", (int?)6, true), (cpuView.WarrantyLabel, cpuView.WarrantyMonths, cpuView.WarrantyOverridden));
        var gpuView = overridden.Units.Single(unit => unit.UnitId == gpu);
        Assert.Equal(("Build", (int?)12), (gpuView.WarrantyLabel, gpuView.WarrantyMonths));

        var cleared = await retail.Assemblies.UpdateAssemblyAsync(build.Id, Card(overridden));
        var cpuAgain = cleared.Units.Single(unit => unit.UnitId == cpu);
        var gpuAgain = cleared.Units.Single(unit => unit.UnitId == gpu);
        Assert.Equal(("Part", (int?)6), (cpuAgain.WarrantyLabel, cpuAgain.WarrantyMonths));
        Assert.Equal(("Item", (int?)3), (gpuAgain.WarrantyLabel, gpuAgain.WarrantyMonths));
    }

    [Fact]
    public async Task Adding_a_part_from_stock_resets_allocations_and_rejects_unavailable_units()
    {
        var (retail, database, units) = await StartAsync(fixture, Line("Add CPU", 100m, 300m), Line("Add GPU", 100m, 100m), Line("Add Fan", 10m, 100m));
        var (cpu, gpu, fan) = (units[0].Id, units[1].Id, units[2].Id);
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Add", [cpu, gpu]));
        var custom = await retail.Assemblies.SetAssemblyAllocationsAsync(build.Id,
            new AssemblyAllocationRequest(build.RowVersion, [new(cpu, 10m), new(gpu, 90m)]));

        var added = await retail.Parts.AddPartAsync(build.Id, new AssemblyAddPartRequest(fan, custom.RowVersion));
        Assert.Equal(3, added.Units.Count);
        Assert.Equal(100m, added.Units.Sum(unit => unit.AllocationPercent));
        Assert.Equal(60m, added.Units.Single(unit => unit.UnitId == cpu).AllocationPercent);
        Assert.Equal(20m, added.Units.Single(unit => unit.UnitId == fan).AllocationPercent);
        Assert.Equal(100m, added.Units.Single(unit => unit.UnitId == fan).PartPrice);
        await using var db = database.CreateRetailContext();
        var unitRow = await db.Units.SingleAsync(entry => entry.Id == fan);
        Assert.Equal((UnitStatus.InAssembly, (Guid?)build.Id), (unitRow.Status, unitRow.AssemblyId));

        Assert.Equal(409, await StatusOfAsync(() => retail.Parts.AddPartAsync(build.Id, new AssemblyAddPartRequest(fan, added.RowVersion))));
    }

    [Fact]
    public async Task A_build_can_start_empty_and_be_listed_from_parts_that_are_not_in_stock()
    {
        var retail = (await fixture.CreateDatabaseAsync()).CreateRetailServices();
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Made to order", []));
        Assert.Empty(build.Units);

        var gpu = new ReceiptLine("RTX 4060 8GB", "New", 1, 12000m, 14000m, Specs: new Dictionary<string, string> { ["vram"] = "8" });
        var cpu = new ReceiptLine("Ryzen 5 7600", "New", 1, 7000m, 8000m);
        var withGpu = await retail.Parts.AddNewPartAsync(build.Id, new AssemblyNewPartRequest(gpu, build.RowVersion));
        var both = await retail.Parts.AddNewPartAsync(build.Id, new AssemblyNewPartRequest(cpu, withGpu.RowVersion));

        Assert.Equal(2, both.Units.Count);
        Assert.Equal(100m, both.Units.Sum(unit => unit.AllocationPercent));
    }

    [Fact]
    public async Task Typed_parts_list_a_build_without_stock_and_block_publishing_until_replaced()
    {
        var (retail, database, units) = await StartAsync(fixture, Line("Typed GPU", 100m, 300m));
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Made to order", []));
        await using var context = database.CreateRetailContext();
        var gpuItemId = (await context.Units.SingleAsync(unit => unit.Id == units[0].Id)).StockItemId;

        var saved = await retail.Parts.SetPlannedPartsAsync(build.Id, new AssemblyPlannedPartsRequest(
            [new PlannedPartInput(null, "Ryzen 5 7600", null, null, 8000m, null), new PlannedPartInput(null, "Typed GPU", null, null, null, gpuItemId)],
            build.RowVersion));

        Assert.Equal(2, saved.PlannedParts.Count);
        Assert.StartsWith("RX-", saved.PlannedParts[1].StockItemCode);
        Assert.Empty(saved.Units);
        await using (var db = database.CreateRetailContext())
            Assert.Equal(UnitStatus.Available, (await db.Units.SingleAsync()).Status);
        var priced = await retail.Assemblies.UpdateAssemblyAsync(saved.Id, new AssemblyUpdateRequest("Made to order", null, null, 20000m, saved.RowVersion));
        Assert.Equal(400, await StatusOfAsync(() => retail.Assemblies.SetAssemblyPublishedAsync(saved.Id, true, priced.RowVersion)));
        Assert.Equal(400, await StatusOfAsync(() => retail.Parts.SetPlannedPartsAsync(build.Id,
            new AssemblyPlannedPartsRequest([new PlannedPartInput(null, " ", null, null, null, null)], priced.RowVersion))));
        Assert.Equal(404, await StatusOfAsync(() => retail.Parts.SetPlannedPartsAsync(build.Id,
            new AssemblyPlannedPartsRequest([new PlannedPartInput(null, "X", null, null, null, Guid.NewGuid())], priced.RowVersion))));

        var kept = saved.PlannedParts[0];
        var edited = await retail.Parts.SetPlannedPartsAsync(build.Id,
            new AssemblyPlannedPartsRequest([new PlannedPartInput(kept.Id, kept.Name, null, null, 7500m, null)], priced.RowVersion));
        Assert.Equal((kept.Id, (decimal?)7500m), (Assert.Single(edited.PlannedParts).Id, edited.PlannedParts[0].Price));

        await retail.Parts.AddPartAsync(build.Id, new AssemblyAddPartRequest(units[0].Id, edited.RowVersion));
        Assert.Equal(400, await StatusOfAsync(() => retail.Pos.PreviewAsync(RetailSales.Sale([RetailSales.BuildLine(build.Id)]), RetailSales.PosUnlimited, false)));
    }

    [Fact]
    public async Task Adding_a_new_part_creates_a_stock_item_inside_the_build()
    {
        var (retail, database, units) = await StartAsync(fixture, Line("New CPU", 100m, 300m));
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("NewPart", [units[0].Id]));

        var part = new ReceiptLine("Brand New RAM", "Used", 1, 150m, 250m, SerialNumber: "RAM-1");
        Assert.Equal(400, await StatusOfAsync(() => retail.Parts.AddNewPartAsync(build.Id,
            new AssemblyNewPartRequest(part with { Quantity = 2 }, build.RowVersion))));

        var added = await retail.Parts.AddNewPartAsync(build.Id, new AssemblyNewPartRequest(part, build.RowVersion));
        Assert.Equal(2, added.Units.Count);
        Assert.Equal(100m, added.Units.Sum(unit => unit.AllocationPercent));
        var ram = added.Units.Single(unit => unit.Name == "Brand New RAM");
        Assert.StartsWith("RX-", ram.StockItemCode);
        Assert.Equal((150m, 250m, 250m), (ram.AcquisitionCost, ram.StandalonePrice, (decimal?)ram.PartPrice));

        await using var db = database.CreateRetailContext();
        var item = await db.StockItems.Include(entry => entry.Units).SingleAsync(entry => entry.Code == ram.StockItemCode);
        var unitRow = Assert.Single(item.Units);
        Assert.Equal((UnitStatus.InAssembly, (Guid?)build.Id), (unitRow.Status, unitRow.AssemblyId));
        Assert.Equal((150m, 250m), (unitRow.AcquisitionCost, item.Price));
    }

    [Fact]
    public async Task Updating_a_part_saves_price_warranty_and_partner_override()
    {
        var (retail, database, units) = await StartAsync(fixture, Line("Upd CPU", 100m, 300m), Line("Upd GPU", 100m, 100m));
        var (cpu, gpu) = (units[0].Id, units[1].Id);
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Update", [cpu, gpu]));
        var partner = await retail.CreateAsync("Part Partner", 10m);

        var updated = await retail.Parts.UpdatePartAsync(build.Id, cpu, new AssemblyPartUpdateRequest(350m, "Part", 6, 12.5m, partner.Id, build.RowVersion));
        var view = updated.Units.Single(unit => unit.UnitId == cpu);
        Assert.Equal((350m, "Part", (int?)6, (decimal?)12.5m), (view.PartPrice, view.WarrantyLabel, view.WarrantyMonths, view.PartnerPercentOverride));
        Assert.Equal(partner.Id, view.PartnerId);
        await using (var db = database.CreateRetailContext())
        {
            var unit = await db.Units.SingleAsync(row => row.Id == cpu);
            Assert.Equal((12.5m, (Guid?)partner.Id), (unit.PartnerPercentOverride, unit.PartnerId));
        }
        Assert.Equal(400, await StatusOfAsync(() => retail.Parts.UpdatePartAsync(build.Id, cpu,
            new AssemblyPartUpdateRequest(350m, null, null, 12.5m, null, updated.RowVersion))));

        var cleared = await retail.Parts.UpdatePartAsync(build.Id, cpu, new AssemblyPartUpdateRequest(350m, null, null, null, null, updated.RowVersion));
        view = cleared.Units.Single(unit => unit.UnitId == cpu);
        Assert.False(view.WarrantyOverridden);
        Assert.Null(view.PartnerPercentOverride);
        Assert.Null(view.PartnerId);

        Assert.Equal(400, await StatusOfAsync(() => retail.Parts.UpdatePartAsync(build.Id, cpu,
            new AssemblyPartUpdateRequest(-1m, null, null, null, null, cleared.RowVersion))));
        Assert.Equal(409, await StatusOfAsync(() => retail.Parts.UpdatePartAsync(build.Id, cpu,
            new AssemblyPartUpdateRequest(10m, null, null, null, null, build.RowVersion))));
    }

    [Fact]
    public async Task Replacing_a_part_from_stock_takes_the_old_share_and_frees_the_old_unit()
    {
        var (retail, _, units) = await StartAsync(fixture, Line("Rep CPU", 100m, 300m), Line("Rep GPU", 100m, 100m), Line("Rep GPU 2", 100m, 150m));
        var (cpu, gpu, gpu2) = (units[0].Id, units[1].Id, units[2].Id);
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Replace", [cpu, gpu]));
        var custom = await retail.Assemblies.SetAssemblyAllocationsAsync(build.Id,
            new AssemblyAllocationRequest(build.RowVersion, [new(cpu, 70m), new(gpu, 30m)]));

        var replaced = Assert.Single(await retail.Parts.ReplacePartAsync(build.Id, gpu, new AssemblyReplacePartRequest(gpu2, custom.RowVersion)));
        Assert.Equal(2, replaced.Units.Count);
        Assert.DoesNotContain(replaced.Units, unit => unit.UnitId == gpu);
        Assert.Equal(30m, replaced.Units.Single(unit => unit.UnitId == gpu2).AllocationPercent);
        Assert.Equal(70m, replaced.Units.Single(unit => unit.UnitId == cpu).AllocationPercent);
        var stock = await retail.Units.ListUnitsAsync(null, null);
        Assert.Equal(UnitStatus.Available, stock.Single(unit => unit.Id == gpu).Status);
        Assert.Equal(UnitStatus.InAssembly, stock.Single(unit => unit.Id == gpu2).Status);
    }

    [Fact]
    public async Task Replacing_a_part_with_one_from_another_build_swaps_them_and_keeps_shares()
    {
        var (retail, database, units) = await StartAsync(fixture,
            Line("Sw CPU A", 100m, 300m), Line("Sw GPU A", 100m, 100m), Line("Sw CPU B", 100m, 200m), Line("Sw GPU B", 100m, 200m));
        var (cpuA, gpuA, cpuB, gpuB) = (units[0].Id, units[1].Id, units[2].Id, units[3].Id);
        var first = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("A", [cpuA, gpuA]));
        var second = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("B", [cpuB, gpuB]));
        Assert.Equal(75m, first.Units.Single(unit => unit.UnitId == cpuA).AllocationPercent);
        Assert.Equal(50m, second.Units.Single(unit => unit.UnitId == cpuB).AllocationPercent);

        Assert.Equal(400, await StatusOfAsync(() => retail.Parts.ReplacePartAsync(first.Id, cpuA,
            new AssemblyReplacePartRequest(cpuB, first.RowVersion))));
        var result = await retail.Parts.ReplacePartAsync(first.Id, cpuA, new AssemblyReplacePartRequest(cpuB, first.RowVersion, second.RowVersion));
        Assert.Equal(2, result.Count);
        Assert.Equal(75m, result[0].Units.Single(unit => unit.UnitId == cpuB).AllocationPercent);
        Assert.Equal(50m, result[1].Units.Single(unit => unit.UnitId == cpuA).AllocationPercent);
        Assert.All(result, view => Assert.Equal(100m, view.Units.Sum(unit => unit.AllocationPercent)));

        await using var db = database.CreateRetailContext();
        Assert.Equal(first.Id, (await db.Units.SingleAsync(unit => unit.Id == cpuB)).AssemblyId);
        Assert.Equal(second.Id, (await db.Units.SingleAsync(unit => unit.Id == cpuA)).AssemblyId);
        Assert.All(await db.Units.ToListAsync(), unit => Assert.Equal(UnitStatus.InAssembly, unit.Status));
    }

    [Fact]
    public async Task Sold_and_disassembled_builds_cannot_be_edited()
    {
        var (retail, database, units) = await StartAsync(fixture,
            Line("Ro CPU", 100m, 300m), Line("Ro GPU", 100m, 100m), Line("Ro Fan", 10m, 50m), Line("Ro PSU", 10m, 50m));
        var (cpu, gpu, fan, psu) = (units[0].Id, units[1].Id, units[2].Id, units[3].Id);
        var sold = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Sold", [cpu, gpu]));
        var gone = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Gone", [psu]));
        await using (var db = database.CreateRetailContext())
        {
            (await db.Assemblies.SingleAsync(entry => entry.Id == sold.Id)).Status = AssemblyStatus.Sold;
            await db.SaveChangesAsync();
        }
        var disassembled = await retail.Assemblies.DisassembleAsync(gone.Id, gone.RowVersion);

        foreach (var (build, unit) in new[] { (sold, cpu), (disassembled, psu) })
        {
            Assert.Equal(409, await StatusOfAsync(() => retail.Assemblies.UpdateAssemblyAsync(build.Id, Card(build))));
            Assert.Equal(409, await StatusOfAsync(() => retail.Parts.AddPartAsync(build.Id, new AssemblyAddPartRequest(fan, build.RowVersion))));
            Assert.Equal(409, await StatusOfAsync(() => retail.Parts.UpdatePartAsync(build.Id, unit,
                new AssemblyPartUpdateRequest(1m, null, null, null, null, build.RowVersion))));
            Assert.Equal(409, await StatusOfAsync(() => retail.Parts.RemovePartAsync(build.Id, unit, new AssemblyRemovePartRequest(build.RowVersion))));
            Assert.Equal(409, await StatusOfAsync(() => retail.Parts.ReplacePartAsync(build.Id, unit, new AssemblyReplacePartRequest(fan, build.RowVersion))));
            Assert.Equal(409, await StatusOfAsync(() => retail.Assemblies.AddPhotoAsync(build.Id, new StockItemPhoto("a.webp", "a-t.webp"))));
        }
    }

    [Fact]
    public async Task Public_list_filters_by_usage_and_price_and_shows_part_prices_only_when_split_is_allowed()
    {
        var (retail, _, units) = await StartAsync(fixture,
            Line("Pub CPU 1", 100m, 300m), Line("Pub GPU 1", 100m, 100m), Line("Pub CPU 2", 100m, 300m), Line("Pub GPU 2", 100m, 100m));
        var gaming = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("G", [units[0].Id, units[1].Id]));
        var office = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("O", [units[2].Id, units[3].Id]));
        var gamingCard = await retail.Assemblies.UpdateAssemblyAsync(gaming.Id, Card(gaming, 3000m, AssemblyUsage.Gaming, AssemblySellMode.FullOrSplit));
        var officeCard = await retail.Assemblies.UpdateAssemblyAsync(office.Id, Card(office, 1000m, AssemblyUsage.StudyWork, AssemblySellMode.Full));
        await retail.Assemblies.SetAssemblyPublishedAsync(gaming.Id, true, gamingCard.RowVersion);
        await retail.Assemblies.SetAssemblyPublishedAsync(office.Id, true, officeCard.RowVersion);

        Assert.Equal(2, (await retail.Assemblies.ListPublishedAssembliesAsync()).Count);
        Assert.Equal(gaming.Id, Assert.Single(await retail.Assemblies.ListPublishedAssembliesAsync(new AssemblyStoreQuery(Usage: "Gaming"))).Id);
        Assert.Equal(office.Id, Assert.Single(await retail.Assemblies.ListPublishedAssembliesAsync(new AssemblyStoreQuery(MaxPrice: 1500m))).Id);
        Assert.Equal(gaming.Id, Assert.Single(await retail.Assemblies.ListPublishedAssembliesAsync(new AssemblyStoreQuery(MinPrice: 1500m, MaxPrice: 3000m))).Id);
        Assert.Empty(await retail.Assemblies.ListPublishedAssembliesAsync(new AssemblyStoreQuery(Usage: "Gaming", MaxPrice: 1500m)));
        Assert.Equal(400, await StatusOfAsync(() => retail.Assemblies.ListPublishedAssembliesAsync(new AssemblyStoreQuery(Usage: "Cooking"))));

        var published = await retail.Assemblies.ListPublishedAssembliesAsync();
        Assert.All(published.Single(view => view.Id == gaming.Id).Units, unit => Assert.NotNull(unit.PartPrice));
        Assert.All(published.Single(view => view.Id == office.Id).Units, unit => Assert.Null(unit.PartPrice));

        Assert.Equal(gaming.Id, (await retail.Assemblies.GetPublishedAssemblyAsync(gaming.Code.ToLowerInvariant())).Id);
        await retail.Assemblies.SetAssemblyPublishedAsync(office.Id, false, (await retail.Assemblies.GetAsync(office.Id, false)).RowVersion);
        Assert.Equal(404, await StatusOfAsync(() => retail.Assemblies.GetPublishedAssemblyAsync(office.Code)));
        Assert.Equal(404, await StatusOfAsync(() => retail.Assemblies.GetPublishedAssemblyAsync("RX-0001")));

        var publicNames = typeof(AssemblyPublicView).GetProperties().Concat(typeof(PublicUnitView).GetProperties()).Select(property => property.Name).ToList();
        Assert.DoesNotContain(publicNames, name => name.Contains("Cost") || name.Contains("Partner") || name.Contains("Allocat") || name.Contains("Serial"));
    }

    [Fact]
    public async Task Views_hide_cost_fields_without_the_cost_permission()
    {
        var (retail, _, units) = await StartAsync(fixture, Line("Cost CPU", 123m, 300m), Line("Cost GPU", 77m, 100m));
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Costs", units.Select(unit => unit.Id).ToList()));
        var priced = await retail.Assemblies.UpdateAssemblyAsync(build.Id, Card(build, 2000m));
        var partner = await retail.CreateAsync("Cost Partner", 10m);
        var full = await retail.Parts.UpdatePartAsync(build.Id, units[0].Id, new AssemblyPartUpdateRequest(300m, null, null, 15m, partner.Id, priced.RowVersion));

        Assert.Equal(200m, full.ComponentCost);
        Assert.NotNull(full.AllocatedTotal);
        Assert.All(full.Units, unit =>
        {
            Assert.NotNull(unit.AcquisitionCost);
            Assert.NotNull(unit.StandalonePrice);
            Assert.NotNull(unit.AllocatedAmount);
        });
        Assert.Equal((15m, (Guid?)partner.Id), (full.Units.Single(unit => unit.UnitId == units[0].Id).PartnerPercentOverride,
            full.Units.Single(unit => unit.UnitId == units[0].Id).PartnerId));

        var hidden = new[] { await retail.Assemblies.GetAsync(build.Id, false), Assert.Single(await retail.Assemblies.ListAssembliesAsync(false)) };
        foreach (var view in hidden)
        {
            Assert.Null(view.ComponentCost);
            Assert.Null(view.AllocatedTotal);
            Assert.All(view.Units, unit =>
            {
                Assert.Null(unit.AcquisitionCost);
                Assert.Null(unit.StandalonePrice);
                Assert.Null(unit.PartnerPercentOverride);
                Assert.Null(unit.PartnerId);
                Assert.Null(unit.AllocatedAmount);
            });
        }
        var viaPart = await retail.Parts.AddNewPartAsync(build.Id, new AssemblyNewPartRequest(Line("Cost Fan", 5m, 50m), full.RowVersion), includeCosts: false);
        Assert.Null(viaPart.ComponentCost);
        Assert.All(viaPart.Units, unit => Assert.Null(unit.AcquisitionCost));
    }

    [Fact]
    public async Task Photos_can_be_added_and_removed_on_a_build()
    {
        var (retail, _, units) = await StartAsync(fixture, Line("Ph CPU", 100m, 300m));
        var build = await retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Photos", [units[0].Id]));

        var photo = new StockItemPhoto($"builds/{build.Code}/0.webp", $"builds/{build.Code}/0-t.webp");
        var photos = await retail.Assemblies.AddPhotoAsync(build.Id, photo);
        Assert.Equal(photo, Assert.Single(photos));
        Assert.Equal(photo, Assert.Single((await retail.Assemblies.GetAsync(build.Id, true)).Photos));

        Assert.Equal(404, await StatusOfAsync(() => retail.Assemblies.RemovePhotoAsync(build.Id, "missing")));
        Assert.Equal(photo, await retail.Assemblies.RemovePhotoAsync(build.Id, photo.StorageKey));
        Assert.Empty((await retail.Assemblies.GetAsync(build.Id, true)).Photos);
    }
}
