using System.Net;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;
using Npgsql;
using Xunit;
using static HardwareStore.Api.Tests.RetailReceipts;
using static HardwareStore.Api.Tests.RetailSales;

namespace HardwareStore.Api.Tests;

/// <summary>Several storage devices (up to 3) and case fans (a quantity up to 6) in a build (4.11 P2): price, power, limits, saving, quote, checkout, "edit this build".</summary>
public sealed partial class BuilderTests
{
    private sealed record Rig(StockItemAdminView Cpu, StockItemAdminView Board, StockItemAdminView Ram, StockItemAdminView[] Drives,
        StockItemAdminView Fan, StockItemAdminView Psu);

    /// <summary>A store with the optional storage and fan slots, and one item in each.</summary>
    private static async Task<(Env Env, Rig Rig)> StartRigAsync(OwnerBootstrapPostgreSqlFixture fixture)
    {
        var env = await Env.StartAsync(fixture);
        await env.AddSlotAsync("SSD");
        await env.AddSlotAsync("FAN");
        var rig = new Rig(
            await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5", power: "100"),
            await env.ItemAsync("BOARD", "B650 Board", 800m, socket: "AM5"),
            await env.ItemAsync("RAM", "DDR5 16GB", 200m, 10, power: "10"),
            [
                await env.ItemAsync("SSD", "NVMe 500GB", 300m, power: "5"),
                await env.ItemAsync("SSD", "NVMe 1TB", 350m, power: "5"),
                await env.ItemAsync("SSD", "SATA 2TB", 400m, power: "5"),
                await env.ItemAsync("SSD", "NVMe 2TB", 600m, power: "5")
            ],
            await env.ItemAsync("FAN", "120mm Fan", 50m, 10, power: "3"),
            await env.ItemAsync("PSU", "500W PSU", 500m, power: "400"));
        return (env, rig);
    }

    [Fact]
    public async Task Storage_and_fans_are_slots_only_when_the_store_has_their_categories()
    {
        await using var env = await Env.StartAsync(fixture);
        var before = await ReadAsync<List<BuilderSlotView>>(await env.Anonymous.GetAsync($"{Root}/slots"));
        await env.AddSlotAsync("SSD");
        await env.AddSlotAsync("FAN");

        var after = await ReadAsync<List<BuilderSlotView>>(await env.Anonymous.GetAsync($"{Root}/slots"));

        Assert.Equal(["CPU", "BOARD", "RAM", "GPU", "PSU"], before.Select(slot => slot.Slot));
        Assert.Equal(["CPU", "BOARD", "RAM", "GPU", "SSD", "PSU", "FAN"], after.Select(slot => slot.Slot));
    }

    [Fact]
    public async Task Three_drives_and_four_fans_are_priced_powered_and_ordered_by_their_quantities()
    {
        var (env, rig) = await StartRigAsync(fixture);
        await using var _ = env;
        var parts = Parts((rig.Cpu, 1), (rig.Board, 1), (rig.Ram, 2), (rig.Drives[2], 1), (rig.Drives[0], 1), (rig.Drives[1], 1), (rig.Fan, 4), (rig.Psu, 1));

        var build = await CheckAsync(env, parts);

        // 1000 + 800 + 2×200 + (300 + 350 + 400) + 4×50 + 500 = 3950, at the in-build prices.
        Assert.Equal(["CPU", "BOARD", "RAM", "SSD", "SSD", "SSD", "PSU", "FAN"], build.Parts.Select(part => part.Slot));
        Assert.Equal([rig.Drives[0].Code, rig.Drives[1].Code, rig.Drives[2].Code], build.Parts.Where(part => part.Slot == "SSD").Select(part => part.Item.Code));
        Assert.Equal(3950m, build.Total);
        Assert.Equal(build.Total, build.Parts.Sum(part => part.UnitPrice * part.Quantity));
        Assert.Equal(200m, build.Parts.Single(part => part.Slot == "FAN").LineTotal);
        // 100 + 2×10 + 3×5 + 4×3 = 147 W against the 400 W supply.
        Assert.Equal((147m, 400m), (build.PowerEstimateWatts, build.PsuCapacityWatts));
        Assert.Empty(build.Issues);
        Assert.True(build.CanOrder);
        Assert.Equal(8, build.CartLines.Count);
        Assert.Equal(4, build.CartLines.Single(line => line.StockItemId == rig.Fan.Id).Quantity);
        Assert.All(build.CartLines, line => Assert.Equal(OrderPriceOption.InBuild, line.PriceOption));

        var order = await env.Retail.CheckoutAsync(Checkout(build.CartLines, OrderFulfillment.Pickup), Policy(buildPrice: await env.BuildPriceRuleAsync()));
        Assert.Equal(build.Total, order.Subtotal);
        Assert.Equal(4, order.Items.Single(line => line.UnitPrice == 50m).Quantity);
    }

    [Fact]
    public async Task A_part_that_is_not_in_stock_in_the_chosen_quantity_stops_the_order_but_not_the_check()
    {
        var (env, rig) = await StartRigAsync(fixture);
        await using var _ = env;

        var plenty = await CheckAsync(env, Parts((rig.Cpu, 1), (rig.Board, 1), (rig.Fan, 6), (rig.Drives[0], 3)));
        Assert.True(plenty.CanOrder);

        await env.Retail.PosAsync(Sale([StockLine(rig.Drives[1].Id, 2)], confirm: true));
        var oneLeft = await CheckAsync(env, Parts((rig.Cpu, 1), (rig.Board, 1), (rig.Drives[1], 2)));
        Assert.False(oneLeft.Parts.Single(part => part.Slot == "SSD").IsAvailable);
        Assert.False(oneLeft.CanOrder);
    }

    [Fact]
    public async Task Storage_stops_at_three_pieces_and_fans_at_six_of_one_model_and_a_bad_quantity_is_refused()
    {
        var (env, rig) = await StartRigAsync(fixture);
        await using var _ = env;
        var check = $"{Root}/check";

        Assert.Equal(HttpStatusCode.OK, await StatusAsync(env.Anonymous, HttpMethod.Post, check, new { parts = Parts((rig.Drives[0], 1), (rig.Drives[1], 1), (rig.Drives[2], 1)) }));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(env.Anonymous, HttpMethod.Post, check, new { parts = Parts((rig.Drives[0], 2), (rig.Drives[1], 1)) }));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(env.Anonymous, HttpMethod.Post, check, new { parts = Parts((rig.Fan, 6)) }));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(env.Anonymous, HttpMethod.Post, check, new { parts = Parts((rig.Ram, 4)) }));
        // A fourth device, a third piece of the same one with another, a seventh fan, a fifth stick, a quantity of zero.
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(env.Anonymous, HttpMethod.Post, check, new { parts = Parts(rig.Drives) }));
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(env.Anonymous, HttpMethod.Post, check, new { parts = Parts((rig.Drives[0], 2), (rig.Drives[1], 2)) }));
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(env.Anonymous, HttpMethod.Post, check, new { parts = Parts((rig.Fan, 7)) }));
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(env.Anonymous, HttpMethod.Post, check, new { parts = Parts((rig.Ram, 5)) }));
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(env.Anonymous, HttpMethod.Post, check, new { parts = Parts((rig.Fan, 0)) }));
        // The same limits guard saving.
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(env.Anonymous, HttpMethod.Post, $"{Root}/saved", new { parts = Parts((rig.Fan, 7)) }));
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(env.Anonymous, HttpMethod.Post, $"{Root}/saved", new { parts = Parts(rig.Drives) }));
    }

    [Fact]
    public async Task The_options_of_storage_keep_the_chosen_drives_and_judge_the_power_of_the_new_one_on_top()
    {
        var (env, rig) = await StartRigAsync(fixture);
        await using var _ = env;
        var hungry = await env.ItemAsync("SSD", "Hungry Drive", 500m, power: "20");
        var small = await env.ItemAsync("PSU", "130W PSU", 100m, power: "130");
        var chosen = Parts((rig.Cpu, 1), (small, 1), (hungry, 1));

        // 100 + 20 = 120 W on a 130 W supply is already a warning; one more 5 W drive keeps it a warning, a 20 W one goes over.
        var options = await ReadAsync<PagedResult<BuilderOptionView>>(await TestSignIn.PostJsonAsync(env.Anonymous, $"{Root}/options", new { slot = "SSD", parts = chosen }));

        var byCode = options.Items.ToDictionary(option => option.Item.Code);
        Assert.True(byCode[rig.Drives[0].Code].IsCompatible);
        Assert.Empty(byCode[rig.Drives[0].Code].Issues);
        Assert.True(byCode[hungry.Code].IsCompatible);
        var tooMuch = await env.ItemAsync("SSD", "Another Hungry Drive", 500m, power: "20");
        var again = await ReadAsync<PagedResult<BuilderOptionView>>(await TestSignIn.PostJsonAsync(env.Anonymous, $"{Root}/options", new { slot = "SSD", parts = chosen }));
        var over = again.Items.Single(option => option.Item.Code == tooMuch.Code);
        Assert.False(over.IsCompatible);
        Assert.Equal(BuilderIssueCode.PowerOverCapacity, Assert.Single(over.Issues).Code);
    }

    [Fact]
    public async Task A_replacement_fan_is_judged_at_the_quantity_of_the_fans_it_replaces()
    {
        var (env, rig) = await StartRigAsync(fixture);
        await using var _ = env;
        var small = await env.ItemAsync("PSU", "130W PSU", 100m, power: "130");
        var strong = await env.ItemAsync("FAN", "Strong Fan", 80m, 10, power: "10");
        var chosen = Parts((rig.Cpu, 1), (small, 1), (rig.Fan, 4));

        // 100 + 4×3 = 112 W today; four strong fans would be 140 W, one would be 110 W.
        var options = await ReadAsync<PagedResult<BuilderOptionView>>(await TestSignIn.PostJsonAsync(env.Anonymous, $"{Root}/options", new { slot = "FAN", parts = chosen }));

        var option = options.Items.Single(candidate => candidate.Item.Code == strong.Code);
        Assert.False(option.IsCompatible);
        Assert.Equal(BuilderIssueCode.PowerOverCapacity, Assert.Single(option.Issues).Code);
        Assert.True(options.Items.Single(candidate => candidate.Item.Code == rig.Fan.Code).IsCompatible);
    }

    [Fact]
    public async Task A_saved_build_keeps_every_drive_and_the_fan_quantity_and_the_same_parts_in_any_order_are_one_build()
    {
        var (env, rig) = await StartRigAsync(fixture);
        await using var _ = env;
        using var owner = await env.SignedInAsync("owner");
        using var visitor = await env.SignedInAsync("visitor");
        var forward = Parts((rig.Cpu, 1), (rig.Board, 1), (rig.Drives[0], 1), (rig.Drives[1], 1), (rig.Fan, 3));
        var backward = Parts((rig.Fan, 3), (rig.Drives[1], 1), (rig.Drives[0], 1), (rig.Board, 1), (rig.Cpu, 1));

        var saved = await SaveAsync(owner, forward);
        var visitorOwn = await SaveAsync(visitor, backward);
        var copied = await ReadAsync<SavedBuildSummary>(await TestSignIn.PostJsonAsync(visitor, $"{Root}/saved/{saved.Token}/copy", new { }));
        var read = await ReadAsync<SavedBuildView>(await owner.GetAsync($"{Root}/saved/{saved.Token}"));

        Assert.Equal(["CPU", "BOARD", "SSD", "SSD", "FAN"], read.Evaluation.Parts.Select(part => part.Slot));
        Assert.Equal(3, read.Evaluation.Parts.Single(part => part.Slot == "FAN").Quantity);
        Assert.Equal(1000m + 800m + 300m + 350m + 3 * 50m, read.Evaluation.Total);
        Assert.Equal(visitorOwn.Token, copied.Token);
        Assert.Equal(5, copied.PartCount);
    }

    [Fact]
    public async Task A_build_saved_before_the_extra_slots_still_opens_with_its_quantities_and_prices()
    {
        var (env, rig) = await StartRigAsync(fixture);
        await using var _ = env;
        var saved = await SaveAsync(env.Anonymous, Parts((rig.Cpu, 1), (rig.Board, 1), (rig.Ram, 2)));
        // The shape every build was saved in before: a code and a quantity per part, one part per slot, no fans, no second drive.
        await using (var connection = new NpgsqlConnection(env.Database.ConnectionString))
        {
            await connection.OpenAsync();
            await using var update = connection.CreateCommand();
            update.CommandText = "UPDATE saved_builds SET parts = @parts::jsonb WHERE token = @token";
            update.Parameters.AddWithValue("parts", $"[{{\"Code\":\"{rig.Cpu.Code}\",\"Quantity\":1}},{{\"Code\":\"{rig.Board.Code}\",\"Quantity\":1}},{{\"Code\":\"{rig.Ram.Code}\",\"Quantity\":2}}]");
            update.Parameters.AddWithValue("token", saved.Token);
            Assert.Equal(1, await update.ExecuteNonQueryAsync());
        }

        var read = await ReadAsync<SavedBuildView>(await env.Anonymous.GetAsync($"{Root}/saved/{saved.Token}"));

        Assert.Equal([1, 1, 2], read.Evaluation.Parts.Select(part => part.Quantity));
        Assert.Equal(1000m + 800m + 2 * 200m, read.Evaluation.Total);
        Assert.True(read.Evaluation.CanOrder);
    }

    [Fact]
    public async Task The_quote_lists_every_drive_and_the_fans_with_their_quantities_and_the_total()
    {
        var (env, rig) = await StartRigAsync(fixture);
        await using var _ = env;

        var quote = await ReadAsync<QuoteDocument>(await TestSignIn.PostJsonAsync(env.Anonymous, $"{Root}/quote",
            new { name = "Fan PC", parts = Parts((rig.Cpu, 1), (rig.Board, 1), (rig.Drives[0], 1), (rig.Drives[1], 1), (rig.Fan, 5)) }));

        Assert.Equal(5, quote.Quote.Lines.Count);
        Assert.Equal(5, quote.Quote.Lines.Single(line => line.Name == rig.Fan.ModelName).Quantity);
        Assert.Equal(1000m + 800m + 300m + 350m + 5 * 50m, quote.Quote.Total);
    }

    [Fact]
    public async Task Edit_this_build_fits_up_to_three_drives_and_counts_the_fans_as_a_quantity()
    {
        var (env, rig) = await StartRigAsync(fixture);
        await using var _ = env;
        var units = new List<Guid>();
        foreach (var (model, category, count) in new[]
                 {
                     ("Ryzen 5 7600", "CPU", 1), ("NVMe 500GB", "SSD", 1), ("NVMe 1TB", "SSD", 1), ("SATA 2TB", "SSD", 1), ("NVMe 2TB", "SSD", 1),
                     ("120mm Fan", "FAN", 3)
                 })
            units.AddRange((await env.Retail.ReceiveUnitsAsync(Receipt(Guid.NewGuid(), null,
                [new ReceiptLine(model, "New", count, 100m, 200m, env.CategoryId(category))]), Actor)).Select(unit => unit.Id));
        var created = await env.Retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("PC", units));
        var card = await env.Retail.Assemblies.UpdateAssemblyAsync(created.Id, new AssemblyUpdateRequest("PC", "عنوان", "Title", 2000m, created.RowVersion));
        var published = await env.Retail.Assemblies.SetAssemblyPublishedAsync(card.Id, true, card.RowVersion);

        var view = await ReadAsync<BuilderFromBuildView>(await env.Anonymous.GetAsync($"{Root}/from-build/{published.Code}"));

        var drives = view.Evaluation.Parts.Where(part => part.Slot == "SSD").ToList();
        Assert.Equal(3, drives.Count);
        Assert.Equal(3, view.Evaluation.Parts.Single(part => part.Slot == "FAN").Quantity);
        Assert.Equal(rig.Fan.Code, view.Evaluation.Parts.Single(part => part.Slot == "FAN").Item.Code);
        // The fourth drive (the last by code) did not fit and is listed.
        var unmatched = Assert.Single(view.Unmatched);
        Assert.Equal(("NVMe 2TB", "New"), (unmatched.ModelName, unmatched.Condition));
    }
}
