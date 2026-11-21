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
using static HardwareStore.Api.Tests.RetailSales;

namespace HardwareStore.Api.Tests;

/// <summary>Favourites and the notifications bell of a signed-in customer.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class FavouritesAndNotificationsTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Favourites = "/api/v1/account/favourites";
    private const string Bell = "/api/v1/account/notifications";

    private sealed record FavouriteListJson(List<string> Codes, List<CardJson> Items, List<CardJson> Builds);

    private sealed record CardJson(string Code);

    private sealed class Env(OwnerBootstrapDatabase database, IdentityLoginApiFactory factory) : IAsyncDisposable
    {
        public OwnerBootstrapDatabase Database { get; } = database;
        public IdentityLoginApiFactory Factory { get; } = factory;
        public RetailTestServices Retail { get; } = database.CreateRetailServices();

        public static async Task<Env> StartAsync(OwnerBootstrapPostgreSqlFixture fixture)
        {
            var database = await fixture.CreateDatabaseAsync();
            return new Env(database, new IdentityLoginApiFactory(database.ConnectionString, configureTestServices: services =>
            {
                services.RemoveAll<IDbContextFactory<StorefrontDbContext>>();
                services.AddDbContextFactory<StorefrontDbContext>(options => options.UseNpgsql(database.ConnectionString));
                services.RemoveAll<IDbContextFactory<RetailDbContext>>();
                services.AddDbContextFactory<RetailDbContext>(options => options.UseNpgsql(database.ConnectionString));
                services.RemoveAll<IDbContextFactory<CatalogDbContext>>();
                services.AddDbContextFactory<CatalogDbContext>(options => options.UseNpgsql(database.ConnectionString));
            }));
        }

        public async Task<(HttpClient Client, Guid AccountId)> SignedInAsync(string name, params string[] permissions)
        {
            var id = await EmailCodeSignInTests.CreateAccountAsync(Database, $"{name}@example.test", verified: true, active: true,
                roleCode: $"{name}-role", permissions);
            var client = Factory.CreatePreflightedClient(new() { HandleCookies = true });
            await TestSignIn.SignInAsync(client, Factory.Services, $"{name}@example.test");
            return (client, id);
        }

        public async Task<StockItemAdminView> PublishedAsync(string model)
        {
            var item = await Retail.ReceiveItemAsync(new ReceiptLine(model, "New", 1, 100m, 500m));
            await Retail.PublishAsync(item);
            return item;
        }

        /// <summary>A build of one new part; published (visible on the website) or left as a draft.</summary>
        public async Task<AssemblyView> BuildAsync(string name, bool published)
        {
            var part = await Retail.ReceiveItemAsync(new ReceiptLine($"{name} part", "New", 1, 100m, 500m));
            var build = await Retail.Assemblies.CreateAssemblyAsync(new AssemblyCreateRequest(name, [part.Units[0].Id]));
            var card = await Retail.Assemblies.UpdateAssemblyAsync(build.Id, new AssemblyUpdateRequest(name, "عنوان", "Title", 3000m, build.RowVersion, Usage: "Gaming"));
            return published ? await Retail.Assemblies.SetAssemblyPublishedAsync(card.Id, true, card.RowVersion) : card;
        }

        public async Task<CustomerNotificationFeed> BellAsync(HttpClient client)
        {
            using var response = await client.GetAsync(Bell);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<CustomerNotificationFeed>())!;
        }

        public async Task<int> RunJobAsync()
        {
            using var scope = Factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<BackInStockNotifier>().RunAsync(CancellationToken.None);
        }

        public async ValueTask DisposeAsync() => await Factory.DisposeAsync();
    }

    private static string Code(int number) => $"RX-{number:0000}";

    [Fact]
    public async Task Adding_a_code_twice_keeps_one_row_and_accounts_do_not_see_each_other()
    {
        var service = (await fixture.CreateDatabaseAsync()).CreateFavouriteService();
        var (ann, bob) = (Guid.NewGuid(), Guid.NewGuid());

        await service.AddAsync(ann, "rx 7");
        await service.AddAsync(ann, "RX-0007");
        await service.AddAsync(bob, "RX-0008");
        await service.RemoveAsync(bob, "RX-0099");

        Assert.Equal(["RX-0007"], await service.ListCodesAsync(ann));
        Assert.Equal(["RX-0008"], await service.ListCodesAsync(bob));
        await service.RemoveAsync(ann, "RX-0007");
        Assert.Empty(await service.ListCodesAsync(ann));
        Assert.Equal(400, (await Assert.ThrowsAsync<StorefrontException>(() => service.AddAsync(ann, "PO-0001"))).StatusCode);
        await service.AddAsync(ann, "bld 3");
        Assert.Equal(["BLD-03"], await service.ListCodesAsync(ann));
    }

    [Fact]
    public async Task The_list_is_limited_and_a_merge_skips_invalid_and_saved_codes()
    {
        var service = (await fixture.CreateDatabaseAsync()).CreateFavouriteService();
        var account = Guid.NewGuid();
        await service.AddAsync(account, "RX-0001");

        await service.MergeAsync(account, ["rx-2", "RX0001", "PO-0001", null, "", "RX-2"]);
        Assert.Equal(["RX-0001", "RX-0002"], (await service.ListCodesAsync(account)).Order());

        await service.MergeAsync(account, Enumerable.Range(3, 400).Select(Code));
        Assert.Equal(FavouriteRules.Max, (await service.ListCodesAsync(account)).Count);
        var full = await Assert.ThrowsAsync<StorefrontException>(() => service.AddAsync(account, "RX-9999"));
        Assert.Equal(409, full.StatusCode);
        await service.AddAsync(account, "RX-0001");
        Assert.Equal(FavouriteRules.Max, (await service.ListCodesAsync(account)).Count);
    }

    [Fact]
    public async Task Merging_two_accounts_moves_favourites_and_notifications_to_the_one_kept_and_is_safe_to_repeat()
    {
        var database = await fixture.CreateDatabaseAsync();
        var (favourites, notifications) = (database.CreateFavouriteService(), database.CreateNotificationService());
        var (source, target) = (Guid.NewGuid(), Guid.NewGuid());
        await favourites.AddAsync(source, "RX-0001");
        await favourites.AddAsync(source, "RX-0002");
        await favourites.AddAsync(target, "RX-0002");
        await notifications.AddBackInStockAsync(source, "RX-0001", "RX 6600");

        for (var run = 0; run < 2; run++)
        {
            await favourites.MoveAsync(source, target);
            await notifications.MoveAsync(source, target);
        }

        Assert.Equal(["RX-0001", "RX-0002"], (await favourites.ListCodesAsync(target)).Order());
        Assert.Empty(await favourites.ListCodesAsync(source));
        Assert.Single((await notifications.ListAsync(target)).Items);
        Assert.Empty((await notifications.ListAsync(source)).Items);
    }

    [Fact]
    public async Task The_bell_counts_unread_ignores_other_accounts_ids_and_marks_all_read()
    {
        var notifications = (await fixture.CreateDatabaseAsync()).CreateNotificationService();
        var (mine, other) = (Guid.NewGuid(), Guid.NewGuid());
        await notifications.AddOrderStatusAsync(mine, Guid.NewGuid(), "ORD-1", "Confirmed");
        await notifications.AddBackInStockAsync(mine, "RX-0005", "RX 6600");
        await notifications.AddBackInStockAsync(other, "RX-0005", "RX 6600");

        var feed = await notifications.ListAsync(mine);
        Assert.Equal((2, 2), (feed.Items.Count, feed.UnreadCount));
        Assert.Equal(["BackInStock", "OrderStatus"], feed.Items.Select(item => item.Kind));
        var theirId = (await notifications.ListAsync(other)).Items.Single().Id;

        await notifications.MarkReadAsync(mine, theirId);
        Assert.Equal(1, (await notifications.ListAsync(other)).UnreadCount);
        await notifications.MarkReadAsync(mine, feed.Items[0].Id);
        var afterOne = await notifications.ListAsync(mine);
        Assert.Equal(1, afterOne.UnreadCount);
        Assert.True(afterOne.Items.Single(item => item.Id == feed.Items[0].Id).Read);

        await notifications.MarkAllReadAsync(mine);
        var afterAll = await notifications.ListAsync(mine);
        Assert.Equal((0, 2), (afterAll.UnreadCount, afterAll.Items.Count));
        Assert.Equal(1, (await notifications.ListAsync(other)).UnreadCount);
    }

    [Fact]
    public async Task Favourites_need_sign_in_and_return_codes_with_cards_only_for_published_items()
    {
        await using var env = await Env.StartAsync(fixture);
        using var anonymous = env.Factory.CreatePreflightedClient(new() { HandleCookies = true });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Favourites)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Bell)).StatusCode);
        var (customer, _) = await env.SignedInAsync("customer");
        var item = await env.PublishedAsync("RX 6600");

        using var saved = await TestSignIn.SendJsonAsync(customer, HttpMethod.Put, $"{Favourites}/{item.Code}", null);
        using var unknown = await TestSignIn.SendJsonAsync(customer, HttpMethod.Put, $"{Favourites}/RX-9999", null);
        using var invalid = await TestSignIn.SendJsonAsync(customer, HttpMethod.Put, $"{Favourites}/PO-0001", null);
        using var listResponse = await customer.GetAsync(Favourites);
        var list = (await listResponse.Content.ReadFromJsonAsync<FavouriteListJson>())!;

        Assert.Equal((HttpStatusCode.NoContent, HttpStatusCode.NoContent, HttpStatusCode.BadRequest), (saved.StatusCode, unknown.StatusCode, invalid.StatusCode));
        Assert.Equal([item.Code, "RX-9999"], list.Codes.Order());
        Assert.Equal([item.Code], list.Items.Select(card => card.Code));
        Assert.Empty(list.Builds);
        using var merged = await TestSignIn.PostJsonAsync(customer, $"{Favourites}/merge", new { codes = new[] { "rx-9999", "PO-0001", "RX-0500" } });
        var afterMerge = (await merged.Content.ReadFromJsonAsync<FavouriteListJson>())!;
        Assert.Equal(3, afterMerge.Codes.Count);
        Assert.Equal([item.Code], afterMerge.Items.Select(card => card.Code));
    }

    [Fact]
    public async Task A_build_can_be_hearted_and_its_card_comes_back_only_while_it_is_published()
    {
        await using var env = await Env.StartAsync(fixture);
        var (customer, _) = await env.SignedInAsync("customer");
        var item = await env.PublishedAsync("RX 6700");
        var live = await env.BuildAsync("Live build", published: true);
        var draft = await env.BuildAsync("Draft build", published: false);

        foreach (var code in new[] { item.Code, live.Code, draft.Code, "BLD-99" })
        {
            using var response = await TestSignIn.SendJsonAsync(customer, HttpMethod.Put, $"{Favourites}/{code}", null);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
        using var listResponse = await customer.GetAsync(Favourites);
        var list = (await listResponse.Content.ReadFromJsonAsync<FavouriteListJson>())!;

        Assert.Equal(4, list.Codes.Count);
        Assert.Equal([item.Code], list.Items.Select(card => card.Code));
        Assert.Equal([live.Code], list.Builds.Select(card => card.Code));

        using var removed = await TestSignIn.SendJsonAsync(customer, HttpMethod.Delete, $"{Favourites}/{live.Code}", null);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        using var after = await customer.GetAsync(Favourites);
        Assert.Empty((await after.Content.ReadFromJsonAsync<FavouriteListJson>())!.Builds);
    }

    [Fact]
    public async Task A_signed_in_customers_notify_me_request_gets_a_bell_entry_when_the_model_is_back()
    {
        await using var env = await Env.StartAsync(fixture);
        var (customer, accountId) = await env.SignedInAsync("customer");
        using (var scope = env.Factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<CustomerRequestService>().SubmitAsync(
                new CustomerRequestSubmission(CustomerRequestType.NotifyMe, "Ahmed", "01012345678", null, null, null, null, null, null, "Future GPU",
                    null, null, null, null, null, null), accountId);
        var item = await env.PublishedAsync("Future GPU");

        Assert.Equal(1, await env.RunJobAsync());
        Assert.Equal(0, await env.RunJobAsync());

        var feed = await env.BellAsync(customer);
        var entry = Assert.Single(feed.Items);
        Assert.Equal((CustomerNotificationKind.BackInStock, "Future GPU", item.Code, 1, false),
            (entry.Kind, entry.Subject, entry.Target, feed.UnreadCount, entry.Read));
        using var read = await TestSignIn.SendJsonAsync(customer, HttpMethod.Post, $"{Bell}/{entry.Id}/read", null);
        Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);
        Assert.Equal(0, (await env.BellAsync(customer)).UnreadCount);
        using var readAll = await TestSignIn.SendJsonAsync(customer, HttpMethod.Post, $"{Bell}/read-all", null);
        Assert.Equal(HttpStatusCode.NoContent, readAll.StatusCode);
    }

    [Fact]
    public async Task An_order_of_an_account_gets_a_bell_entry_on_a_status_change_and_a_guest_order_gets_none()
    {
        await using var env = await Env.StartAsync(fixture);
        var (staff, _) = await env.SignedInAsync("staff", "Sales.ViewOrders", "Sales.ApproveOrder");
        var (customer, accountId) = await env.SignedInAsync("customer");
        var order = await env.Retail.CheckoutAsync(Checkout([StockLine((await env.PublishedAsync("Mouse")).Id)], OrderFulfillment.Pickup, phone: "01289186840"),
            Policy(accountId));
        var guest = await env.Retail.CheckoutAsync(Checkout([StockLine((await env.PublishedAsync("Keyboard")).Id)], OrderFulfillment.Pickup, phone: "01289186841"),
            Policy());

        var version = order.RowVersion;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var confirmed = await TestSignIn.PostJsonAsync(staff, $"/api/v1/admin/retail/orders/{order.Id}/status",
                new { status = OrderStatus.Confirmed, rowVersion = version });
            Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
            version = (await confirmed.Content.ReadFromJsonAsync<OrderView>())!.RowVersion;
        }
        using var guestConfirmed = await TestSignIn.PostJsonAsync(staff, $"/api/v1/admin/retail/orders/{guest.Id}/status",
            new { status = OrderStatus.Confirmed, rowVersion = guest.RowVersion });
        Assert.Equal(HttpStatusCode.OK, guestConfirmed.StatusCode);

        var entry = Assert.Single((await env.BellAsync(customer)).Items);
        Assert.Equal((CustomerNotificationKind.OrderStatus, order.Reference, OrderStatus.Confirmed, order.Id.ToString()),
            (entry.Kind, entry.Subject, entry.Status, entry.Target));
        await using var db = env.Database.CreateStorefrontContext();
        Assert.Equal(1, await db.CustomerNotifications.CountAsync());
    }
}
