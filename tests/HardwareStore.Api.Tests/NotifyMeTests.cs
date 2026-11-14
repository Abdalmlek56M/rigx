using System.Net;
using System.Net.Http.Json;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Notify-me (roadmap 2.8): subscribe by item or model, back-in-stock matching, the WhatsApp list and the demand report.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class NotifyMeTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Admin = "/api/v1/admin/requests";
    private static readonly Guid Gpus = Guid.NewGuid();

    private sealed class Env(OwnerBootstrapDatabase database, IdentityLoginApiFactory factory) : IAsyncDisposable
    {
        public OwnerBootstrapDatabase Database { get; } = database;
        public IdentityLoginApiFactory Factory { get; } = factory;
        public RetailTestServices Retail { get; } = database.CreateRetailServices();
        public HttpClient Anonymous { get; } = factory.CreatePreflightedClient(new() { HandleCookies = true });

        public static async Task<Env> StartAsync(OwnerBootstrapPostgreSqlFixture fixture)
        {
            var database = await fixture.CreateDatabaseAsync();
            var factory = new IdentityLoginApiFactory(database.ConnectionString, configureTestServices: services =>
            {
                services.RemoveAll<IDbContextFactory<StorefrontDbContext>>();
                services.AddDbContextFactory<StorefrontDbContext>(options => options.UseNpgsql(database.ConnectionString));
                services.RemoveAll<IDbContextFactory<RetailDbContext>>();
                services.AddDbContextFactory<RetailDbContext>(options => options.UseNpgsql(database.ConnectionString));
                services.RemoveAll<IDbContextFactory<CatalogDbContext>>();
                services.AddDbContextFactory<CatalogDbContext>(options => options.UseNpgsql(database.ConnectionString));
            });
            return new Env(database, factory);
        }

        public async Task<HttpClient> SignedInAsync(string name, params string[] permissions)
        {
            await EmailCodeSignInTests.CreateAccountAsync(Database, $"{name}@example.test", verified: true, active: true,
                roleCode: $"{name}-role", permissions);
            var client = Factory.CreatePreflightedClient(new() { HandleCookies = true });
            await TestSignIn.SignInAsync(client, Factory.Services, $"{name}@example.test");
            return client;
        }

        /// <summary>Runs the back-in-stock job once and delivers the emails it queued.</summary>
        public async Task<int> RunJobAsync()
        {
            using var scope = Factory.Services.CreateScope();
            var matched = await scope.ServiceProvider.GetRequiredService<BackInStockNotifier>().RunAsync(CancellationToken.None);
            await TestSignIn.DeliverOutboxAsync(Factory.Services);
            return matched;
        }

        public IReadOnlyCollection<BackInStockEmailMessage> Emails => Factory.Services.GetRequiredService<InMemoryTestEmailProvider>().BackInStock;

        public async Task<StockItemAdminView> PublishedAsync(string model, string condition = "New", Guid? categoryId = null, decimal price = 500m)
        {
            var item = await Retail.ReceiveItemAsync(new ReceiptLine(model, condition, 1, 100m, price, CategoryId: categoryId));
            await Retail.PublishAsync(item);
            return item;
        }

        /// <summary>A used piece that was published and then sold: its RX code never comes back.</summary>
        public async Task<StockItemAdminView> SoldOutAsync(string model, Guid? categoryId = null)
        {
            var item = await PublishedAsync(model, "Used", categoryId);
            await Retail.CompletedSaleAsync(RetailMoney.PaidSale(item, 500m));
            return item;
        }

        public Task<HttpResponseMessage> SubscribeAsync(string? model = null, string? itemCode = null, string phone = "01012345678", string? email = null) =>
            TestSignIn.PostJsonAsync(Anonymous, "/api/v1/requests", new { type = "NotifyMe", name = "Ahmed", phone, email, modelName = model, itemCode });

        /// <summary>The public form's service call (the endpoint allows only five submissions per ten minutes).</summary>
        public async Task<string> SubscribedAsync(string? model = null, string? itemCode = null, string phone = "01012345678", string? email = null)
        {
            using var scope = Factory.Services.CreateScope();
            var receipt = await scope.ServiceProvider.GetRequiredService<CustomerRequestService>().SubmitAsync(
                new CustomerRequestSubmission(CustomerRequestType.NotifyMe, "Ahmed", phone, email, null, null, null, null, null, model, null, itemCode,
                    null, null, null), null);
            return receipt.Code!;
        }

        public async Task<CustomerRequest> StoredAsync(string code)
        {
            await using var db = Database.CreateStorefrontContext();
            return await db.CustomerRequests.AsNoTracking().SingleAsync(request => request.Code == code);
        }

        public async ValueTask DisposeAsync()
        {
            Anonymous.Dispose();
            await Factory.DisposeAsync();
        }
    }

    private sealed record DashboardJson(int NewRequests, List<NotifyMeDemandRow> NotifyMeDemand);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.Equal(expected, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    [Fact]
    public async Task Subscribing_by_an_rx_code_waits_for_its_model_with_its_name_and_category()
    {
        await using var env = await Env.StartAsync(fixture);
        var sold = await env.SoldOutAsync("RX 6600", Gpus);

        using var response = await env.SubscribeAsync(itemCode: sold.Code.ToLowerInvariant().Replace("-", " "), email: "ahmed@example.test");
        var code = (await ReadAsync<CustomerRequestReceipt>(response, HttpStatusCode.Accepted)).Code!;

        var stored = await env.StoredAsync(code);
        Assert.Equal((CustomerRequestType.NotifyMe, CustomerRequestStatus.New, sold.Code, "RX 6600", "rx6600", Gpus, "ahmed@example.test"),
            (stored.Type, stored.Status, stored.ItemCode, stored.ModelName, stored.ModelKey, stored.CategoryId, stored.Email));
        Assert.Null(stored.AvailableItemCode);
    }

    [Fact]
    public async Task Subscribing_by_model_keeps_the_spelling_and_matches_the_key_without_an_email()
    {
        await using var env = await Env.StartAsync(fixture);

        using var response = await env.SubscribeAsync(model: "  rtx   3060-ti ");
        var code = (await ReadAsync<CustomerRequestReceipt>(response, HttpStatusCode.Accepted)).Code!;

        var stored = await env.StoredAsync(code);
        Assert.Equal(("rtx 3060-ti", "rtx3060ti", (string?)null, (Guid?)null, (string?)null),
            (stored.ModelName, stored.ModelKey, stored.ItemCode, stored.CategoryId, stored.Email));
    }

    [Fact]
    public async Task An_unknown_rx_code_a_code_that_is_not_an_item_and_a_missing_target_are_refused()
    {
        await using var env = await Env.StartAsync(fixture);

        using var unknown = await env.SubscribeAsync(itemCode: "RX-9999");
        using var notAnItem = await env.SubscribeAsync(itemCode: "BLD-07");
        using var nothing = await env.SubscribeAsync();
        using var noPhone = await env.SubscribeAsync(model: "RX 6600", phone: "");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notAnItem.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, nothing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, noPhone.StatusCode);
        await using var db = env.Database.CreateStorefrontContext();
        Assert.Empty(await db.CustomerRequests.ToListAsync());
    }

    [Fact]
    public async Task A_model_that_can_be_bought_now_is_a_conflict_by_model_or_by_code_but_an_unpublished_item_does_not_count()
    {
        await using var env = await Env.StartAsync(fixture);
        var live = await env.PublishedAsync("Ryzen 5 5600");
        await env.Retail.ReceiveItemAsync(new ReceiptLine("Hidden GPU", "New", 1, 100m, 500m));

        using var byModel = await env.SubscribeAsync(model: "RYZEN 5 5600");
        using var byCode = await env.SubscribeAsync(itemCode: live.Code);
        using var unpublished = await env.SubscribeAsync(model: "Hidden GPU");

        Assert.Equal(HttpStatusCode.Conflict, byModel.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, byCode.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, unpublished.StatusCode);
        Assert.Contains("available now", await byModel.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_same_phone_asking_again_for_the_same_model_gets_the_same_code_until_it_is_closed()
    {
        await using var env = await Env.StartAsync(fixture);
        var sold = await env.SoldOutAsync("RX 6600");
        var first = await env.SubscribedAsync(model: "RX 6600");

        var spelling = await env.SubscribedAsync(model: "rx-6600");
        var byCode = await env.SubscribedAsync(itemCode: sold.Code);
        var otherPhone = await env.SubscribedAsync(model: "RX 6600", phone: "01111111111");
        var otherModel = await env.SubscribedAsync(model: "RX 6700");
        await using (var db = env.Database.CreateStorefrontContext())
            await db.CustomerRequests.Where(request => request.Code == first)
                .ExecuteUpdateAsync(set => set.SetProperty(request => request.Status, CustomerRequestStatus.Closed));
        var afterClose = await env.SubscribedAsync(model: "RX 6600");

        Assert.Equal((first, first), (spelling, byCode));
        Assert.Equal(4, new HashSet<string> { first, otherPhone, otherModel, afterClose }.Count);
    }

    [Fact]
    public async Task The_job_matches_a_new_item_of_the_model_once_and_emails_only_the_requests_that_gave_an_email()
    {
        await using var env = await Env.StartAsync(fixture);
        var withEmail = await env.SubscribedAsync(model: "Future GPU", email: "buyer@example.test");
        var noEmail = await env.SubscribedAsync(model: "future-gpu", phone: "01111111111");
        var other = await env.SubscribedAsync(model: "Other GPU", phone: "01222222222", email: "other@example.test");
        Assert.Equal(0, await env.RunJobAsync());

        var item = await env.PublishedAsync("FUTURE GPU");
        var matched = await env.RunJobAsync();
        var again = await env.RunJobAsync();

        Assert.Equal((2, 0), (matched, again));
        var first = await env.StoredAsync(withEmail);
        Assert.Equal((item.Code, CustomerRequestStatus.New), (first.AvailableItemCode, first.Status));
        Assert.NotNull(first.AvailableAtUtc);
        Assert.Equal(item.Code, (await env.StoredAsync(noEmail)).AvailableItemCode);
        Assert.Null((await env.StoredAsync(other)).AvailableItemCode);
        var email = Assert.Single(env.Emails);
        Assert.Equal(("buyer@example.test", "Ahmed", "FUTURE GPU", $"http://localhost:3000/items/{item.Code}"),
            (email.Email, email.FullName, email.ItemName, email.ItemUrl));
    }

    [Fact]
    public async Task The_job_waits_for_a_published_item_and_picks_the_cheapest_when_several_match()
    {
        await using var env = await Env.StartAsync(fixture);
        var code = await env.SubscribedAsync(model: "RX 6600");
        await env.Retail.ReceiveItemAsync(new ReceiptLine("RX 6600", "Used", 1, 100m, 300m));
        Assert.Equal(0, await env.RunJobAsync());

        await env.PublishedAsync("RX 6600", "Used", price: 900m);
        var cheaper = await env.PublishedAsync("RX-6600", "Used", price: 700m);
        Assert.Equal(1, await env.RunJobAsync());

        Assert.Equal(cheaper.Code, (await env.StoredAsync(code)).AvailableItemCode);
    }

    [Fact]
    public async Task The_back_in_stock_list_has_a_whatsapp_link_per_matched_request_and_marking_it_sent_closes_it()
    {
        await using var env = await Env.StartAsync(fixture);
        var manager = await env.SignedInAsync("manager", "Requests.View", "Requests.Manage");
        var matched = await env.SubscribedAsync(model: "Future GPU", phone: "+20 101 234 5678");
        await env.SubscribedAsync(model: "Waiting GPU", phone: "01111111111");
        Assert.Empty(await ReadAsync<List<BackInStockListItem>>(await manager.GetAsync($"{Admin}/back-in-stock")));
        var item = await env.PublishedAsync("Future GPU");
        await env.RunJobAsync();

        var list = await ReadAsync<List<BackInStockListItem>>(await manager.GetAsync($"{Admin}/back-in-stock"));

        var entry = Assert.Single(list);
        var itemUrl = $"http://localhost:3000/items/{item.Code}";
        Assert.Equal((matched, "Ahmed", item.Code, "Future GPU"), (entry.Code, entry.Name, entry.ItemCode, entry.ItemName));
        Assert.StartsWith("https://wa.me/201012345678?text=", entry.WhatsApp.Url);
        Assert.Contains(Uri.EscapeDataString(itemUrl), entry.WhatsApp.Url);
        Assert.Contains("Ahmed", entry.WhatsApp.Text);
        Assert.Contains(itemUrl, entry.WhatsApp.Text);

        var sent = await ReadAsync<CustomerRequestView>(
            await TestSignIn.SendJsonAsync(manager, HttpMethod.Post, $"{Admin}/{entry.Id}/back-in-stock/sent", null));

        Assert.Equal(CustomerRequestStatus.Closed, sent.Status);
        Assert.NotNull(sent.NotifiedAtUtc);
        Assert.NotNull(sent.ClosedAtUtc);
        Assert.Empty(await ReadAsync<List<BackInStockListItem>>(await manager.GetAsync($"{Admin}/back-in-stock")));
        using var twice = await TestSignIn.SendJsonAsync(manager, HttpMethod.Post, $"{Admin}/{entry.Id}/back-in-stock/sent", null);
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        using var missing = await TestSignIn.SendJsonAsync(manager, HttpMethod.Post, $"{Admin}/{Guid.NewGuid()}/back-in-stock/sent", null);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Marking_sent_needs_a_match_first()
    {
        await using var env = await Env.StartAsync(fixture);
        var manager = await env.SignedInAsync("manager", "Requests.View", "Requests.Manage");
        var code = await env.SubscribedAsync(model: "Future GPU");
        var id = (await env.StoredAsync(code)).Id;

        using var response = await TestSignIn.SendJsonAsync(manager, HttpMethod.Post, $"{Admin}/{id}/back-in-stock/sent", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task The_demand_report_groups_by_model_counts_open_and_notified_and_respects_the_range()
    {
        await using var env = await Env.StartAsync(fixture);
        var viewer = await env.SignedInAsync("viewer", "Requests.View");
        var sold = await env.SoldOutAsync("RX 6600", Gpus);
        var old = await env.SubscribedAsync(itemCode: sold.Code, phone: "01000000001");
        await env.SubscribedAsync(model: "rx-6600", phone: "01000000002");
        await env.SubscribedAsync(model: "RX6600", phone: "01000000003");
        await env.SubscribedAsync(model: "Ryzen 5 5600", phone: "01000000004");
        await env.PublishedAsync("Ryzen 5 5600");
        await using (var db = env.Database.CreateStorefrontContext())
        {
            await db.CustomerRequests.Where(request => request.Code == old)
                .ExecuteUpdateAsync(set => set.SetProperty(request => request.CreatedAtUtc, DateTimeOffset.UtcNow.AddDays(-60)));
            await db.CustomerRequests.Where(request => request.Phone == "01000000002")
                .ExecuteUpdateAsync(set => set.SetProperty(request => request.NotifiedAtUtc, (DateTimeOffset?)DateTimeOffset.UtcNow)
                    .SetProperty(request => request.AvailableItemCode, "RX-0001").SetProperty(request => request.Status, CustomerRequestStatus.Closed));
        }

        var recent = await ReadAsync<List<NotifyMeDemandRow>>(await viewer.GetAsync($"{Admin}/demand"));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var wide = await ReadAsync<List<NotifyMeDemandRow>>(
            await viewer.GetAsync($"{Admin}/demand?from={today.AddDays(-90):yyyy-MM-dd}&to={today.AddDays(1):yyyy-MM-dd}"));
        var empty = await ReadAsync<List<NotifyMeDemandRow>>(
            await viewer.GetAsync($"{Admin}/demand?from={today.AddDays(-90):yyyy-MM-dd}&to={today.AddDays(-80):yyyy-MM-dd}"));
        using var backwards = await viewer.GetAsync($"{Admin}/demand?from={today:yyyy-MM-dd}&to={today.AddDays(-1):yyyy-MM-dd}");

        Assert.Equal(["rx6600", "ryzen55600"], recent.Select(row => row.ModelKey));
        Assert.Equal((2, 1, 1, false), (recent[0].Total, recent[0].Open, recent[0].Notified, recent[0].AvailableNow));
        Assert.Equal((1, 1, 0, true), (recent[1].Total, recent[1].Open, recent[1].Notified, recent[1].AvailableNow));
        Assert.Equal((3, 2, 1), (wide[0].Total, wide[0].Open, wide[0].Notified));
        Assert.Equal(("RX6600", Gpus), (wide[0].ModelName, wide[0].CategoryId));
        Assert.Empty(empty);
        Assert.Equal(HttpStatusCode.BadRequest, backwards.StatusCode);
    }

    [Fact]
    public async Task The_dashboard_shows_the_top_five_models_people_ask_for()
    {
        await using var env = await Env.StartAsync(fixture);
        var finance = await env.SignedInAsync("finance", "Finance.ViewReports");
        for (var model = 1; model <= 6; model++)
            for (var asker = 0; asker < 7 - model; asker++)
                await env.SubscribedAsync(model: $"Model {model}", phone: $"010000{model}{asker:0000}");

        var dashboard = await ReadAsync<DashboardJson>(await finance.GetAsync("/api/v1/admin/dashboard"));

        Assert.Equal(["Model 1", "Model 2", "Model 3", "Model 4", "Model 5"], dashboard.NotifyMeDemand.Select(row => row.ModelName));
        Assert.Equal([6, 5, 4, 3, 2], dashboard.NotifyMeDemand.Select(row => row.Total));
        Assert.Equal(21, dashboard.NewRequests);
    }

    [Fact]
    public async Task The_new_routes_follow_their_permissions()
    {
        await using var env = await Env.StartAsync(fixture);
        var nobody = await env.SignedInAsync("nobody");

        Assert.Equal(HttpStatusCode.Unauthorized, (await env.Anonymous.GetAsync($"{Admin}/demand")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await env.Anonymous.GetAsync($"{Admin}/back-in-stock")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await nobody.GetAsync($"{Admin}/demand")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await nobody.GetAsync($"{Admin}/back-in-stock")).StatusCode);
        var viewer = await env.SignedInAsync("viewer", "Requests.View");
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Admin}/demand")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Admin}/back-in-stock")).StatusCode);
        using var sent = await TestSignIn.SendJsonAsync(viewer, HttpMethod.Post, $"{Admin}/{Guid.NewGuid()}/back-in-stock/sent", null);
        Assert.Equal(HttpStatusCode.Forbidden, sent.StatusCode);
    }
}
