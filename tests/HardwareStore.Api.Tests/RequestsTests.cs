using System.Net;
using System.Net.Http.Json;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class RequestsTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Admin = "/api/v1/admin/requests";
    private const string LongMessage = "I would like some help with my PC.";

    private static IdentityLoginApiFactory CreateFactory(string connectionString) =>
        new(connectionString, configureTestServices: services =>
        {
            services.RemoveAll<IDbContextFactory<StorefrontDbContext>>();
            services.AddDbContextFactory<StorefrontDbContext>(options => options.UseNpgsql(connectionString));
            services.RemoveAll<IDbContextFactory<PurchasingDbContext>>();
            services.AddDbContextFactory<PurchasingDbContext>(options => options.UseNpgsql(connectionString));
        });

    private static Task<Guid> CreateUserAsync(OwnerBootstrapDatabase database, string username, params string[] permissions) =>
        EmailCodeSignInTests.CreateAccountAsync(database, $"{username}@example.test", verified: true, active: true,
            roleCode: $"{username}-role", permissions);

    private static async Task<HttpClient> SignedInAsync(IdentityLoginApiFactory factory, string username)
    {
        var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await TestSignIn.SignInAsync(client, factory.Services, $"{username}@example.test");
        return client;
    }

    private static object Contact(string phone = "01012345678", string message = LongMessage, string? website = null) =>
        new { type = "Contact", name = "Ahmed", phone, message, website };

    private static CustomerRequestSubmission SellSubmission(string phone = "01012345678", string model = "RTX 3060") =>
        new(CustomerRequestType.Sell, "Ahmed", phone, null, "GZ", "Facebook", null, "Boxed, 2 years old", "GPU", model, "2 years", null,
            null, null, null);

    private static async Task<CustomerRequestView> SeedAsync(IdentityLoginApiFactory factory, CustomerRequestSubmission submission)
    {
        using var scope = factory.Services.CreateScope();
        var receipt = await scope.ServiceProvider.GetRequiredService<CustomerRequestService>().SubmitAsync(submission, null);
        var page = await scope.ServiceProvider.GetRequiredService<CustomerRequestService>()
            .ListAsync(new CustomerRequestQuery(Search: receipt.Code));
        return await scope.ServiceProvider.GetRequiredService<CustomerRequestService>().GetAsync(page.Items.Single().Id);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.Equal(expected, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, Guid id, string action, object body) =>
        TestSignIn.SendJsonAsync(client, method, $"{Admin}/{id}/{action}", body);

    [Fact]
    public async Task Anonymous_submit_returns_a_code_and_honeypot_and_duplicates_store_nothing_new()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });

        using var first = await TestSignIn.PostJsonAsync(client, "/api/v1/requests", Contact("+20 101 234 5678"));
        var receipt = await ReadAsync<CustomerRequestReceipt>(first, HttpStatusCode.Accepted);
        using var bot = await TestSignIn.PostJsonAsync(client, "/api/v1/requests", Contact("01099999999", website: "http://spam.test"));
        var botReceipt = await ReadAsync<CustomerRequestReceipt>(bot, HttpStatusCode.Accepted);
        using var again = await TestSignIn.PostJsonAsync(client, "/api/v1/requests", Contact());
        var duplicate = await ReadAsync<CustomerRequestReceipt>(again, HttpStatusCode.Accepted);

        Assert.Equal("REQ-0001", receipt.Code);
        Assert.Null(botReceipt.Code);
        Assert.Equal(receipt.Code, duplicate.Code);
        Assert.Contains("no-store", first.Headers.CacheControl!.ToString());
        await using var db = database.CreateStorefrontContext();
        var stored = Assert.Single(await db.CustomerRequests.AsNoTracking().ToListAsync());
        Assert.Equal(("01012345678", CustomerRequestStatus.New, (Guid?)null), (stored.Phone, stored.Status, stored.UserAccountId));
    }

    [Fact]
    public async Task An_invalid_submission_is_a_bad_request_and_stores_nothing()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });

        using var response = await TestSignIn.PostJsonAsync(client, "/api/v1/requests", Contact(phone: "123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = database.CreateStorefrontContext();
        Assert.Empty(await db.CustomerRequests.ToListAsync());
    }

    [Fact]
    public async Task A_signed_in_customer_submit_is_linked_to_the_account()
    {
        var database = await fixture.CreateDatabaseAsync();
        var userId = await CreateUserAsync(database, "customer");
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = await SignedInAsync(factory, "customer");

        using var response = await TestSignIn.PostJsonAsync(client, "/api/v1/requests", Contact());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await using var db = database.CreateStorefrontContext();
        Assert.Equal(userId, (await db.CustomerRequests.SingleAsync()).UserAccountId);
    }

    [Fact]
    public async Task The_admin_list_filters_by_type_status_and_search_and_counts_new_requests_per_type()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "viewer", "Requests.View");
        await using var factory = CreateFactory(database.ConnectionString);
        var sell = await SeedAsync(factory, SellSubmission());
        await SeedAsync(factory, SellSubmission("01111111111", "RX 6600"));
        var contact = await SeedAsync(factory, new CustomerRequestSubmission(CustomerRequestType.Contact, "Mona", "01222222222", null, null,
            null, null, LongMessage, null, null, null, null, null, null, null));
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<RequestFollowUpService>().SetStatusAsync(contact.Id, CustomerRequestStatus.Following, contact.RowVersion);
        using var client = await SignedInAsync(factory, "viewer");

        var all = await ReadAsync<CustomerRequestPage>(await client.GetAsync(Admin));
        var sells = await ReadAsync<CustomerRequestPage>(await client.GetAsync($"{Admin}?type=Sell"));
        var following = await ReadAsync<CustomerRequestPage>(await client.GetAsync($"{Admin}?status=Following"));
        var byCode = await ReadAsync<CustomerRequestPage>(await client.GetAsync($"{Admin}?q=req 1"));
        var byPhone = await ReadAsync<CustomerRequestPage>(await client.GetAsync($"{Admin}?q=1111"));
        var byName = await ReadAsync<CustomerRequestPage>(await client.GetAsync($"{Admin}?q=mon"));

        Assert.Equal(3, all.Total);
        Assert.Equal("REQ-0003", all.Items[0].Code);
        Assert.Equal(2, sells.Total);
        Assert.Equal(contact.Id, Assert.Single(following.Items).Id);
        Assert.Equal(sell.Id, Assert.Single(byCode.Items).Id);
        Assert.Equal("RX 6600", Assert.Single(byPhone.Items).ModelName);
        Assert.Equal(contact.Id, Assert.Single(byName.Items).Id);
        Assert.Equal(2, all.NewByType[CustomerRequestType.Sell]);
        Assert.Equal(0, all.NewByType[CustomerRequestType.Contact]);
        Assert.Equal(0, all.NewByType[CustomerRequestType.Service]);
        Assert.Equal(sell.Code, (await ReadAsync<CustomerRequestView>(await client.GetAsync($"{Admin}/{sell.Id}"))).Code);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Admin}/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Admin_routes_follow_their_permissions_and_converting_also_needs_the_purchase_permission()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "nobody");
        await CreateUserAsync(database, "viewer", "Requests.View");
        await CreateUserAsync(database, "manager", "Requests.View", "Requests.Manage");
        await using var factory = CreateFactory(database.ConnectionString);
        var request = await SeedAsync(factory, SellSubmission());
        var note = new { internalNote = "x", rowVersion = request.RowVersion };

        using var anonymous = factory.CreatePreflightedClient(new() { HandleCookies = true });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Admin)).StatusCode);

        using var nobody = await SignedInAsync(factory, "nobody");
        Assert.Equal(HttpStatusCode.Forbidden, (await nobody.GetAsync(Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await nobody.GetAsync($"{Admin}/{request.Id}")).StatusCode);

        using var viewer = await SignedInAsync(factory, "viewer");
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync(Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Admin}/{request.Id}/whatsapp")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(viewer, HttpMethod.Put, request.Id, "note", note)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(viewer, HttpMethod.Post, request.Id, "offer", new { offerAmount = 1, rowVersion = 1 })).StatusCode);

        using var manager = await SignedInAsync(factory, "manager");
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(manager, HttpMethod.Put, request.Id, "note", note)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(manager, HttpMethod.Post, request.Id, "convert", new { rowVersion = 1 })).StatusCode);
    }

    [Fact]
    public async Task A_sell_request_goes_from_offer_to_acceptance_to_a_draft_purchase_from_a_new_seller()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "owner", "Requests.View", "Requests.Manage", "Purchasing.CreateOrder");
        await using var factory = CreateFactory(database.ConnectionString);
        var request = await SeedAsync(factory, SellSubmission());
        using var client = await SignedInAsync(factory, "owner");

        var following = await ReadAsync<CustomerRequestView>(
            await SendAsync(client, HttpMethod.Put, request.Id, "status", new { status = "Following", rowVersion = request.RowVersion }));
        var offered = await ReadAsync<CustomerRequestView>(await SendAsync(client, HttpMethod.Post, request.Id, "offer",
            new { offerAmount = 4000m, expectedResale = 5500m, rowVersion = following.RowVersion }));
        var accepted = await ReadAsync<CustomerRequestView>(await SendAsync(client, HttpMethod.Post, request.Id, "decision",
            new { decision = "Accepted", rowVersion = offered.RowVersion }));
        var converted = await ReadAsync<CustomerRequestView>(await SendAsync(client, HttpMethod.Post, request.Id, "convert",
            new { rowVersion = accepted.RowVersion }));

        Assert.Equal(CustomerRequestStatus.Offered, offered.Status);
        Assert.Equal((4000m, 5500m), (offered.OfferAmount, offered.ExpectedResale));
        Assert.Equal(CustomerRequestStatus.Converted, converted.Status);
        Assert.Equal("PO-0001", converted.PurchaseCode);
        Assert.NotNull(converted.ClosedAtUtc);
        using var scope = factory.Services.CreateScope();
        var purchase = await scope.ServiceProvider.GetRequiredService<PurchaseService>().GetAsync(converted.PurchaseId!.Value);
        Assert.Equal((PurchaseStatus.Draft, "PO-0001"), (purchase.Status, purchase.Code));
        Assert.Contains(request.Code, purchase.Notes);
        var line = Assert.Single(purchase.Lines);
        Assert.Equal(("RTX 3060", "Used", 1, 4000m, 5500m), (line.ModelName, line.Condition, line.Quantity, line.UnitCost, line.Price));
        var seller = Assert.Single(await scope.ServiceProvider.GetRequiredService<SellerService>().ListAsync(request.Phone, null, false));
        Assert.Equal((seller.Id, "Ahmed"), (purchase.Seller.Id, seller.Name));
    }

    [Fact]
    public async Task Converting_reuses_the_seller_with_the_same_phone_and_a_second_convert_or_stale_version_is_a_conflict()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "owner", "Requests.View", "Requests.Manage", "Purchasing.CreateOrder");
        await using var factory = CreateFactory(database.ConnectionString);
        var request = await SeedAsync(factory, SellSubmission());
        using var scope = factory.Services.CreateScope();
        var existing = await scope.ServiceProvider.GetRequiredService<SellerService>()
            .CreateAsync(new SellerRequest("Person", "Old Name", "01012345678", null, null, null, null, null));
        var followUp = scope.ServiceProvider.GetRequiredService<RequestFollowUpService>();
        var offered = await followUp.OfferAsync(request.Id, 3000m, null, request.RowVersion);
        var accepted = await followUp.DecideAsync(request.Id, CustomerRequestStatus.Accepted, offered.RowVersion);
        using var client = await SignedInAsync(factory, "owner");

        var stale = await SendAsync(client, HttpMethod.Post, request.Id, "convert", new { rowVersion = accepted.RowVersion - 1 });
        var converted = await ReadAsync<CustomerRequestView>(
            await SendAsync(client, HttpMethod.Post, request.Id, "convert", new { rowVersion = accepted.RowVersion }));
        var second = await SendAsync(client, HttpMethod.Post, request.Id, "convert", new { rowVersion = converted.RowVersion });

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var sellers = await scope.ServiceProvider.GetRequiredService<SellerService>().ListAsync(null, null, false);
        Assert.Equal(existing.Id, Assert.Single(sellers).Id);
        var purchase = await scope.ServiceProvider.GetRequiredService<PurchaseService>().GetAsync(converted.PurchaseId!.Value);
        Assert.Equal((existing.Id, 3000m), (purchase.Seller.Id, Assert.Single(purchase.Lines).Price));
        await using var purchasing = database.CreatePurchasingContext();
        Assert.Single(await purchasing.Purchases.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Follow_up_rules_are_enforced_over_http_and_a_stale_row_version_is_a_conflict()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "owner", "Requests.View", "Requests.Manage", "Purchasing.CreateOrder");
        await using var factory = CreateFactory(database.ConnectionString);
        var request = await SeedAsync(factory, SellSubmission());
        using var client = await SignedInAsync(factory, "owner");

        var early = await SendAsync(client, HttpMethod.Post, request.Id, "decision", new { decision = "Accepted", rowVersion = request.RowVersion });
        var convertEarly = await SendAsync(client, HttpMethod.Post, request.Id, "convert", new { rowVersion = request.RowVersion });
        var badOffer = await SendAsync(client, HttpMethod.Post, request.Id, "offer", new { offerAmount = 0m, rowVersion = request.RowVersion });
        var badStatus = await SendAsync(client, HttpMethod.Put, request.Id, "status", new { status = "Offered", rowVersion = request.RowVersion });
        var noted = await ReadAsync<CustomerRequestView>(
            await SendAsync(client, HttpMethod.Put, request.Id, "note", new { internalNote = " call him ", rowVersion = request.RowVersion }));
        var stale = await SendAsync(client, HttpMethod.Put, request.Id, "note", new { internalNote = "other", rowVersion = request.RowVersion });
        var closed = await ReadAsync<CustomerRequestView>(
            await SendAsync(client, HttpMethod.Put, request.Id, "status", new { status = "Closed", rowVersion = noted.RowVersion }));

        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, convertEarly.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badOffer.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badStatus.StatusCode);
        Assert.Equal(("call him", request.RowVersion + 1), (noted.InternalNote, noted.RowVersion));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(CustomerRequestStatus.Closed, closed.Status);
        Assert.NotNull(closed.ClosedAtUtc);
    }

    [Fact]
    public async Task The_whatsapp_reply_fills_the_settings_template_and_returns_a_wa_me_link()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "viewer", "Requests.View");
        await using var factory = CreateFactory(database.ConnectionString);
        var request = await SeedAsync(factory, SellSubmission());
        using var client = await SignedInAsync(factory, "viewer");

        var reply = await ReadAsync<WhatsAppReplyView>(await client.GetAsync($"{Admin}/{request.Id}/whatsapp"));

        Assert.Equal("01012345678", reply.Phone);
        Assert.Equal("أهلًا Ahmed، بخصوص طلبك عند RIGX: ", reply.Text);
        Assert.Equal($"https://wa.me/201012345678?text={Uri.EscapeDataString(reply.Text)}", reply.Url);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Admin}/{Guid.NewGuid()}/whatsapp")).StatusCode);
    }
}
