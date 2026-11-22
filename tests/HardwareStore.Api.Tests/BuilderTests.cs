using System.Net;
using System.Net.Http.Json;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;
using static HardwareStore.Api.Tests.RetailReceipts;
using static HardwareStore.Api.Tests.RetailSales;

namespace HardwareStore.Api.Tests;

/// <summary>PC builder v2 (roadmap 2.13): slots, options, check, quote, saved builds and "edit this build".</summary>
[Trait("Category", "RequiresDocker")]
public sealed class BuilderTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Root = "/api/v1/builder";
    private static readonly Guid Actor = Guid.NewGuid();

    private sealed class Env(OwnerBootstrapDatabase database, IdentityLoginApiFactory factory) : IAsyncDisposable
    {
        public OwnerBootstrapDatabase Database { get; } = database;
        public IdentityLoginApiFactory Factory { get; } = factory;
        public RetailTestServices Retail { get; } = database.CreateRetailServices();
        public HttpClient Anonymous { get; } = factory.CreatePreflightedClient(new() { HandleCookies = true });
        public Guid CpuCategory { get; private set; }
        public Guid BoardCategory { get; private set; }

        /// <summary>Categories CPU/BOARD/RAM/GPU/PSU in the builder (CPU and BOARD share the "socket" compatibility spec; the rest carry power_w).</summary>
        public static async Task<Env> StartAsync(OwnerBootstrapPostgreSqlFixture fixture)
        {
            var database = await fixture.CreateDatabaseAsync();
            var env = new Env(database, new IdentityLoginApiFactory(database.ConnectionString, configureTestServices: services =>
            {
                services.RemoveAll<IDbContextFactory<RetailDbContext>>();
                services.AddDbContextFactory<RetailDbContext>(options => options.UseNpgsql(database.ConnectionString));
                services.RemoveAll<IDbContextFactory<CatalogDbContext>>();
                services.AddDbContextFactory<CatalogDbContext>(options => options.UseNpgsql(database.ConnectionString));
                services.RemoveAll<IDbContextFactory<PcBuilderDbContext>>();
                services.AddDbContextFactory<PcBuilderDbContext>(options => options.UseNpgsql(database.ConnectionString));
                services.RemoveAll<IDbContextFactory<StorefrontDbContext>>();
                services.AddDbContextFactory<StorefrontDbContext>(options => options.UseNpgsql(database.ConnectionString));
            }));
            SpecDefinition[] socket = [new("socket", "Socket", SpecType.Choice, ["AM4", "AM5"], IsCompatibility: true), Power];
            env.CpuCategory = await env.CategoryAsync("CPU", socket);
            env.BoardCategory = await env.CategoryAsync("BOARD", [socket[0]]);
            foreach (var code in new[] { "RAM", "GPU", "PSU" }) env.Categories[code] = await env.CategoryAsync(code, [Power]);
            await env.CategoryAsync("SSD", [Power], inBuilder: false);
            return env;
        }

        private static readonly SpecDefinition Power = new("power_w", "Power", SpecType.Number, Unit: "W");
        private Dictionary<string, Guid> Categories { get; } = [];

        private async Task<Guid> CategoryAsync(string code, SpecDefinition[] specs, bool inBuilder = true)
        {
            using var scope = Factory.Services.CreateScope();
            var taxonomy = scope.ServiceProvider.GetRequiredService<TaxonomyService>();
            var category = await taxonomy.SaveCategoryAsync(null, new CategoryInput(code, $"{code.ToLowerInvariant()}-{Guid.NewGuid():N}"[..20], null,
                null, null, 0, true, null, code, ShowInBuilder: inBuilder));
            await scope.ServiceProvider.GetRequiredService<CategorySpecService>().SetSpecsAsync(category.Id, specs, category.RowVersion);
            return category.Id;
        }

        public Guid CategoryId(string code) => code switch { "CPU" => CpuCategory, "BOARD" => BoardCategory, _ => Categories[code] };

        /// <summary>A published item in the slot's category; price is the in-build price, alone is 20% higher.</summary>
        public async Task<StockItemAdminView> ItemAsync(string slot, string model, decimal price, int quantity = 3, string condition = "New",
            string? socket = null, string? power = null, bool publish = true)
        {
            var specs = new Dictionary<string, string>();
            if (socket is not null) specs["socket"] = socket;
            if (power is not null) specs["power_w"] = power;
            var item = await Retail.ReceiveItemAsync(new ReceiptLine(model, condition, quantity, price / 2, price, CategoryId(slot),
                AloneExtra: price * 0.2m, SerialNumber: condition == "Used" ? "SN-" + model : null, Specs: specs));
            if (publish) await Retail.PublishAsync(item);
            return item;
        }

        public async Task<StockItemAdminView> RepriceAsync(StockItemAdminView item, decimal price)
        {
            var current = await Retail.StockItems.GetAsync(item.Id, false, false);
            return await Retail.StockItems.UpdateAsync(item.Id, new StockItemUpdate(current.ModelName, current.CategoryId, current.BrandId, null, price,
                current.WithBoardExtra, current.AloneExtra, current.WarrantyLabel, current.WarrantyMonths, current.HasBox, false, null,
                current.Specs, current.RowVersion));
        }

        public async Task<HttpClient> SignedInAsync(string name)
        {
            await EmailCodeSignInTests.CreateAccountAsync(Database, $"{name}@example.test", verified: true, active: true, roleCode: $"{name}-role");
            var client = Factory.CreatePreflightedClient(new() { HandleCookies = true });
            await TestSignIn.SignInAsync(client, Factory.Services, $"{name}@example.test");
            return client;
        }

        public async Task<BuildPriceRule> BuildPriceRuleAsync()
        {
            using var scope = Factory.Services.CreateScope();
            return (await scope.ServiceProvider.GetRequiredService<BuilderCatalogLoader>().LoadAsync(CancellationToken.None)).BuildPriceRule;
        }

        public ValueTask DisposeAsync()
        {
            Anonymous.Dispose();
            return Factory.DisposeAsync();
        }
    }

    private static object Parts(params (StockItemAdminView Item, int Quantity)[] parts) =>
        parts.Select(part => new { code = part.Item.Code, quantity = part.Quantity }).ToArray();

    private static object Parts(params StockItemAdminView[] items) => Parts(items.Select(item => (item, 1)).ToArray());

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using (response)
        {
            Assert.True(expected == response.StatusCode, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }

    private static async Task<BuilderEvaluation> CheckAsync(Env env, object parts) =>
        await ReadAsync<BuilderEvaluation>(await TestSignIn.PostJsonAsync(env.Anonymous, $"{Root}/check", new { parts }));

    private static async Task<SavedBuildView> SaveAsync(HttpClient client, object parts, string? name = "Gaming PC", string? usage = "Gaming") =>
        await ReadAsync<SavedBuildView>(await TestSignIn.PostJsonAsync(client, $"{Root}/saved", new { name, usage, parts }));

    private static async Task<HttpStatusCode> StatusAsync(HttpClient client, HttpMethod method, string url, object? body = null)
    {
        using var response = await TestSignIn.SendJsonAsync(client, method, url, body);
        return response.StatusCode;
    }

    // ---------------------------------------------------------------- slots and options

    [Fact]
    public async Task Slots_list_only_the_builder_categories_in_display_order()
    {
        await using var env = await Env.StartAsync(fixture);

        var slots = await ReadAsync<List<BuilderSlotView>>(await env.Anonymous.GetAsync($"{Root}/slots"));

        Assert.Equal(["CPU", "BOARD", "RAM", "GPU", "PSU"], slots.Select(slot => slot.Slot));
        Assert.Equal([env.CpuCategory], slots[0].CategoryIds);
    }

    [Fact]
    public async Task Options_show_compatibility_with_the_chosen_parts_keep_sold_out_items_and_filter_by_condition()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        var match = await env.ItemAsync("BOARD", "B650 Board", 800m, socket: "AM5");
        var other = await env.ItemAsync("BOARD", "B450 Board", 500m, socket: "AM4");
        var used = await env.ItemAsync("BOARD", "X670 Board", 900m, 1, "Used", socket: "AM5");
        var soldOut = await env.ItemAsync("BOARD", "A620 Board", 400m, 1, socket: "AM5");
        await env.Retail.PosAsync(Sale([StockLine(soldOut.Id)], confirm: true));
        await env.ItemAsync("BOARD", "Draft Board", 300m, socket: "AM5", publish: false);

        var all = await ReadAsync<PagedResult<BuilderOptionView>>(await TestSignIn.PostJsonAsync(env.Anonymous, $"{Root}/options",
            new { slot = "board", parts = Parts(cpu) }));
        var onlyUsed = await ReadAsync<PagedResult<BuilderOptionView>>(await TestSignIn.PostJsonAsync(env.Anonymous, $"{Root}/options",
            new { slot = "BOARD", parts = Parts(cpu), condition = "Used" }));

        Assert.Equal(4, all.Total);
        var byCode = all.Items.ToDictionary(option => option.Item.Code);
        Assert.True(byCode[match.Code].IsCompatible);
        Assert.Empty(byCode[match.Code].Issues);
        var mismatch = byCode[other.Code];
        Assert.False(mismatch.IsCompatible);
        Assert.Equal(BuilderIssueCode.SpecMismatch, Assert.Single(mismatch.Issues).Code);
        Assert.False(byCode[soldOut.Code].IsAvailable);
        Assert.True(byCode[match.Code].IsAvailable);
        Assert.Equal(soldOut.Code, all.Items[^1].Item.Code);
        Assert.Equal(used.Code, Assert.Single(onlyUsed.Items).Item.Code);
        Assert.Equal(400, (int)await StatusAsync(env.Anonymous, HttpMethod.Post, $"{Root}/options", new { slot = "MONITOR" }));
    }

    // ---------------------------------------------------------------- check

    [Fact]
    public async Task A_build_with_a_processor_and_a_board_uses_the_in_build_price_and_a_lone_part_the_alone_price()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        var board = await env.ItemAsync("BOARD", "B650 Board", 800m, socket: "AM5");
        var ram = await env.ItemAsync("RAM", "DDR5 16GB", 200m);

        var build = await CheckAsync(env, Parts((board, 1), (ram, 2), (cpu, 1)));
        var alone = await CheckAsync(env, Parts(cpu));

        Assert.True(build.UsesBuildPrice);
        Assert.Equal(OrderPriceOption.InBuild, build.PriceOption);
        Assert.Equal(["CPU", "BOARD", "RAM"], build.Parts.Select(part => part.Slot));
        Assert.Equal([1000m, 800m, 200m], build.Parts.Select(part => part.UnitPrice));
        Assert.Equal(2200m, build.Total);
        Assert.True(build.CanOrder);
        Assert.Empty(build.Issues);
        Assert.All(build.CartLines, line => Assert.Equal(OrderPriceOption.InBuild, line.PriceOption));
        Assert.Equal(2, build.CartLines.Single(line => line.StockItemId == ram.Id).Quantity);
        Assert.False(alone.UsesBuildPrice);
        Assert.Equal(OrderPriceOption.Alone, alone.PriceOption);
        Assert.Equal((1200m, true), (alone.Total, alone.CanOrder));
    }

    [Fact]
    public async Task A_conflict_a_missing_code_or_a_sold_out_part_stops_the_order_and_power_is_estimated()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5", power: "100");
        var oldBoard = await env.ItemAsync("BOARD", "B450 Board", 500m, socket: "AM4");
        var ram = await env.ItemAsync("RAM", "DDR5 16GB", 200m, power: "10");
        var gpu = await env.ItemAsync("GPU", "RTX 4070", 2000m, power: "200");
        var psu = await env.ItemAsync("PSU", "380W PSU", 300m, power: "380");
        var draft = await env.ItemAsync("RAM", "Draft RAM", 100m, publish: false);
        var gone = await env.ItemAsync("RAM", "Gone RAM", 100m, 1);
        await env.Retail.PosAsync(Sale([StockLine(gone.Id)], confirm: true));

        var conflict = await CheckAsync(env, Parts(cpu, oldBoard));
        var missing = await CheckAsync(env, new[] { new { code = cpu.Code, quantity = 1 }, new { code = "RX-9999", quantity = 1 },
            new { code = draft.Code, quantity = 1 } });
        var soldOut = await CheckAsync(env, Parts(cpu, gone));
        var power = await CheckAsync(env, Parts((cpu, 1), (ram, 2), (gpu, 1), (psu, 1)));

        var issue = Assert.Single(conflict.Issues);
        Assert.Equal((BuilderIssueCode.SpecMismatch, BuilderSlot.Board, "socket"), (issue.Code, issue.Slot, issue.SpecKey));
        Assert.False(conflict.CanOrder);
        Assert.Equal(["RX-9999", draft.Code], missing.MissingCodes);
        Assert.False(missing.CanOrder);
        Assert.False(soldOut.Parts.Single(part => part.Item.Code == gone.Code).IsAvailable);
        Assert.False(soldOut.CanOrder);
        Assert.Equal((320m, 380m), (power.PowerEstimateWatts, power.PsuCapacityWatts));
        Assert.Equal(BuilderIssueCode.PowerNearCapacity, Assert.Single(power.Issues).Code);
        Assert.True(power.CanOrder);
        Assert.Equal(400, (int)await StatusAsync(env.Anonymous, HttpMethod.Post, $"{Root}/check",
            new { parts = new[] { new { code = cpu.Code, quantity = 5 } } }));
    }

    // ---------------------------------------------------------------- checkout and quote

    [Fact]
    public async Task Checkout_with_the_builder_lines_sells_at_the_in_build_prices_and_needs_a_processor_and_a_board()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        var board = await env.ItemAsync("BOARD", "B650 Board", 800m, socket: "AM5");
        var gpu = await env.ItemAsync("GPU", "RTX 4070", 2000m);
        var rule = await env.BuildPriceRuleAsync();
        var build = await CheckAsync(env, Parts(cpu, board, gpu));

        var order = await env.Retail.CheckoutAsync(Checkout(build.CartLines, OrderFulfillment.Pickup), Policy(buildPrice: rule));
        var withoutBoard = await CheckAsync(env, Parts(cpu, gpu));
        var inBuildLines = withoutBoard.CartLines.Select(line => line with { PriceOption = OrderPriceOption.InBuild }).ToList();

        Assert.Equal(build.Total, order.Subtotal);
        Assert.Equal([1000m, 800m, 2000m], order.Items.Select(line => line.UnitPrice));
        Assert.Equal(400, await StatusCodeAsync(() => env.Retail.CheckoutAsync(Checkout(inBuildLines), Policy(buildPrice: rule))));
        Assert.Equal(400, await StatusCodeAsync(() => env.Retail.CheckoutAsync(Checkout(build.CartLines), Policy())));
    }

    [Fact]
    public async Task The_quote_has_the_name_usage_and_in_build_total_and_a_conflicting_build_is_refused()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        var board = await env.ItemAsync("BOARD", "B650 Board", 800m, socket: "AM5");
        var oldBoard = await env.ItemAsync("BOARD", "B450 Board", 500m, socket: "AM4");

        var quote = await ReadAsync<QuoteDocument>(await TestSignIn.PostJsonAsync(env.Anonymous, $"{Root}/quote",
            new { name = " Office PC ", usage = "StudyWork", parts = Parts(cpu, board) }));

        Assert.Equal(("Office PC", "StudyWork", 1800m), (quote.Quote.Title, quote.Quote.Usage, quote.Quote.Total));
        Assert.Equal([1000m, 800m], quote.Quote.Lines.Select(line => line.UnitPrice));
        Assert.Equal(409, (int)await StatusAsync(env.Anonymous, HttpMethod.Post, $"{Root}/quote", new { name = "X", parts = Parts(cpu, oldBoard) }));
    }

    // ---------------------------------------------------------------- saved builds

    [Fact]
    public async Task A_saved_link_shows_the_live_evaluation_and_a_build_saved_without_an_account_cannot_be_changed()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        var board = await env.ItemAsync("BOARD", "B650 Board", 800m, socket: "AM5");
        var saved = await SaveAsync(env.Anonymous, Parts(cpu, board));
        await env.RepriceAsync(cpu, 1100m);

        var again = await ReadAsync<SavedBuildView>(await env.Anonymous.GetAsync($"{Root}/saved/{saved.Token}"));
        using var signedIn = await env.SignedInAsync("someone");
        var update = new { name = "Mine", parts = Parts(cpu), rowVersion = 1 };

        Assert.False(string.IsNullOrWhiteSpace(saved.Token));
        Assert.Equal((false, 1L, 1800m, "Gaming PC", "Gaming"), (saved.IsOwner, saved.RowVersion, saved.Evaluation.Total, saved.Name, saved.Usage));
        Assert.Equal(1900m, again.Evaluation.Total);
        Assert.Null(saved.AuthorName);
        Assert.Null(again.AuthorName);
        Assert.Equal(HttpStatusCode.Unauthorized, await StatusAsync(env.Anonymous, HttpMethod.Put, $"{Root}/saved/{saved.Token}", update));
        Assert.Equal(HttpStatusCode.Unauthorized, await StatusAsync(env.Anonymous, HttpMethod.Delete, $"{Root}/saved/{saved.Token}"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(signedIn, HttpMethod.Put, $"{Root}/saved/{saved.Token}", update));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(signedIn, HttpMethod.Delete, $"{Root}/saved/{saved.Token}"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(env.Anonymous, HttpMethod.Get, $"{Root}/saved/no-such-token"));
    }

    [Fact]
    public async Task A_signed_in_owner_lists_updates_with_the_row_version_and_deletes_and_others_cannot()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        var board = await env.ItemAsync("BOARD", "B650 Board", 800m, socket: "AM5");
        var draft = await env.ItemAsync("RAM", "Draft RAM", 100m, publish: false);
        using var owner = await env.SignedInAsync("owner");
        using var stranger = await env.SignedInAsync("stranger");
        var saved = await SaveAsync(owner, Parts(cpu));

        var mine = await ReadAsync<List<SavedBuildSummary>>(await owner.GetAsync("/api/v1/account/builds"));
        var updated = await ReadAsync<SavedBuildView>(await TestSignIn.SendJsonAsync(owner, HttpMethod.Put, $"{Root}/saved/{saved.Token}",
            new { name = "Better", usage = "Editing", parts = Parts(cpu, board), rowVersion = saved.RowVersion }));
        var stale = await StatusAsync(owner, HttpMethod.Put, $"{Root}/saved/{saved.Token}",
            new { name = "Stale", parts = Parts(cpu), rowVersion = saved.RowVersion });
        var strangerView = await ReadAsync<SavedBuildView>(await stranger.GetAsync($"{Root}/saved/{saved.Token}"));

        Assert.True(saved.IsOwner);
        var summary = Assert.Single(mine);
        Assert.Equal((saved.Token, 1), (summary.Token, summary.PartCount));
        Assert.Empty(await ReadAsync<List<SavedBuildSummary>>(await stranger.GetAsync("/api/v1/account/builds")));
        Assert.Equal((2L, "Better", "Editing", 2), (updated.RowVersion, updated.Name, updated.Usage, updated.Evaluation.Parts.Count));
        Assert.Equal(HttpStatusCode.Conflict, stale);
        Assert.False(strangerView.IsOwner);
        // The signed-in creator's first name shows to everyone with the link (the test accounts' full name is the part before the @).
        Assert.Equal(("owner", "owner", "owner"), (saved.AuthorName, updated.AuthorName, strangerView.AuthorName));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(stranger, HttpMethod.Put, $"{Root}/saved/{saved.Token}",
            new { name = "Hijack", parts = Parts(cpu), rowVersion = 2 }));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(stranger, HttpMethod.Delete, $"{Root}/saved/{saved.Token}"));
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(owner, HttpMethod.Put, $"{Root}/saved/{saved.Token}",
            new { name = "Draft", parts = Parts(cpu, draft), rowVersion = 2 }));
        Assert.Equal(HttpStatusCode.NoContent, await StatusAsync(owner, HttpMethod.Delete, $"{Root}/saved/{saved.Token}"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(owner, HttpMethod.Get, $"{Root}/saved/{saved.Token}"));
        Assert.Empty(await ReadAsync<List<SavedBuildSummary>>(await owner.GetAsync("/api/v1/account/builds")));
    }

    [Fact]
    public async Task Saving_refuses_an_unpublished_code_an_empty_build_and_a_too_long_name()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 1000m, socket: "AM5");
        var draft = await env.ItemAsync("RAM", "Draft RAM", 100m, publish: false);

        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(env.Anonymous, HttpMethod.Post, $"{Root}/saved", new { parts = Parts(cpu, draft) }));
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(env.Anonymous, HttpMethod.Post, $"{Root}/saved", new { parts = Array.Empty<object>() }));
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(env.Anonymous, HttpMethod.Post, $"{Root}/saved",
            new { name = new string('x', 101), parts = Parts(cpu) }));
    }

    // ---------------------------------------------------------------- edit this build

    [Fact]
    public async Task From_build_matches_the_same_model_from_free_stock_and_lists_the_parts_it_cannot_match()
    {
        await using var env = await Env.StartAsync(fixture);
        var cpuUnits = await env.Retail.ReceiveUnitsAsync(Receipt(Guid.NewGuid(), null,
            [new ReceiptLine("Ryzen 5 7600", "New", 1, 500m, 1000m, env.CpuCategory)]), Actor);
        var boardUnits = await env.Retail.ReceiveUnitsAsync(Receipt(Guid.NewGuid(), null,
            [new ReceiptLine("Unique Board", "New", 1, 400m, 800m, env.BoardCategory)]), Actor);
        var freeCpu = await env.ItemAsync("CPU", "Ryzen 5 7600", 900m, 1, "Used", socket: "AM5");
        var created = await env.Retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("PC", [.. cpuUnits.Select(unit => unit.Id), .. boardUnits.Select(unit => unit.Id)]));
        var card = await env.Retail.Assemblies.UpdateAssemblyAsync(created.Id, new AssemblyUpdateRequest("PC", "عنوان", "Title", 2000m, created.RowVersion));
        var published = await env.Retail.Assemblies.SetAssemblyPublishedAsync(card.Id, true, card.RowVersion);

        var view = await ReadAsync<BuilderFromBuildView>(await env.Anonymous.GetAsync($"{Root}/from-build/{published.Code.ToLowerInvariant()}"));

        Assert.Equal(published.Code, view.Code);
        Assert.Equal(freeCpu.Code, Assert.Single(view.Evaluation.Parts).Item.Code);
        Assert.Equal(freeCpu.Prices.Alone, view.Evaluation.Total);
        var unmatched = Assert.Single(view.Unmatched);
        Assert.Equal(("Unique Board", "New"), (unmatched.ModelName, unmatched.Condition));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(env.Anonymous, HttpMethod.Get, $"{Root}/from-build/BLD-99"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(env.Anonymous, HttpMethod.Get, $"{Root}/from-build/garbage"));
    }

    [Fact]
    public async Task An_unpublished_build_cannot_be_edited_in_the_builder()
    {
        await using var env = await Env.StartAsync(fixture);
        var units = await env.Retail.ReceiveUnitsAsync(Receipt(Guid.NewGuid(), null,
            [new ReceiptLine("Draft CPU", "New", 1, 100m, 300m, env.CpuCategory)]), Actor);
        var created = await env.Retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest("Draft", units.Select(unit => unit.Id).ToList()));

        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(env.Anonymous, HttpMethod.Get, $"{Root}/from-build/{created.Code}"));
    }

    // ---------------------------------------------------------------- old routes

    [Fact]
    public async Task The_old_template_routes_are_gone()
    {
        await using var env = await Env.StartAsync(fixture);
        using var signedIn = await env.SignedInAsync("someone");

        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(signedIn, HttpMethod.Get, "/api/v1/pc-builder/templates"));
    }
}

/// <summary>M32 replaces the template tables with saved_builds on a database that already has the old ones.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class BuilderMigrationTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string BeforeSavedBuilds = "20261109162811_M1_PcBuilderTemplates";

    [Fact]
    public async Task The_template_tables_are_dropped_and_saved_builds_is_created()
    {
        var name = $"builder_migration_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(fixture.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{name}\"";
            await create.ExecuteNonQueryAsync();
        }
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = name }.ConnectionString;
        await using var db = new PcBuilderDbContext(new DbContextOptionsBuilder<PcBuilderDbContext>().UseNpgsql(connectionString).Options);
        await db.GetService<IMigrator>().MigrateAsync(BeforeSavedBuilds);
        Assert.NotNull(await TableAsync(db, "pc_build_templates"));

        await db.Database.MigrateAsync();

        Assert.NotNull(await TableAsync(db, "saved_builds"));
        Assert.Null(await TableAsync(db, "pc_build_templates"));
        Assert.Null(await TableAsync(db, "pc_build_slots"));
        Assert.Null(await TableAsync(db, "pc_build_slot_options"));
    }

    private static Task<string?> TableAsync(PcBuilderDbContext db, string table) =>
        db.Database.SqlQuery<string?>($"SELECT to_regclass({table})::text AS \"Value\"").SingleAsync();
}
