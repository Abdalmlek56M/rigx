using System.Net;
using System.Net.Http.Json;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.PcBuilder;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Customers admin: list, 360 view, support actions, merge, view as customer, warranty extension and "my requests" (roadmap 2.18).</summary>
[Trait("Category", "RequiresDocker")]
public sealed class CustomerAdminTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Admin = "/api/v1/admin/customers";
    private const string Support = "support@example.test";
    private const string AlicePhone = "01012345678";

    private static IdentityLoginApiFactory CreateFactory(string connectionString) => AdminApiFactory.Create(connectionString);

    private static Task<Guid> StaffAsync(OwnerBootstrapDatabase database, string email, params string[] permissions) =>
        AdminApiFactory.StaffAsync(database, email, permissions);

    internal static async Task<Guid> CustomerAsync(OwnerBootstrapDatabase database, string name, string? phone = null)
    {
        var id = await EmailCodeSignInTests.CreateAccountAsync(database, $"{name}@example.test", verified: true, active: true, roleCode: "Customer");
        await using var identity = database.CreateIdentityContext();
        var person = await identity.UserAccounts.Where(account => account.Id == id).Select(account => account.Person).SingleAsync();
        person.PhoneNumber = phone;
        person.IsCustomer = true;
        await identity.SaveChangesAsync();
        return id;
    }

    private static Task<HttpClient> SignedInAsync(IdentityLoginApiFactory factory, string email) => AdminApiFactory.SignedInAsync(factory, email);

    private static async Task<HttpClient> SupportAsync(OwnerBootstrapDatabase database, IdentityLoginApiFactory factory)
    {
        await StaffAsync(database, Support, "Customers.View", "Customers.Manage", "Sales.ProcessReturn");
        return await SignedInAsync(factory, Support);
    }

    /// <summary>Sells one item (pickup online order for the account, or a guest POS sale) and delivers it, so the warranty is issued.</summary>
    private static async Task<OrderView> DeliveredAsync(RetailTestServices retail, string model, string phone, Guid? userId = null, bool pos = false,
        string? label = "ضمان المحل", int? months = 6)
    {
        var item = await retail.ReceiveItemAsync(new ReceiptLine(model, "New", 1, 100m, 500m, WarrantyLabel: label, WarrantyMonths: months));
        await retail.PublishAsync(item);
        var order = pos
            ? await retail.PosAsync(RetailSales.Sale([RetailSales.StockLine(item.Id)], phone: phone))
            : await retail.CheckoutAsync(RetailSales.Checkout([RetailSales.StockLine(item.Id)], OrderFulfillment.Pickup, phone: phone), RetailSales.Policy(userId));
        return await retail.StatusesAsync(order, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Delivered);
    }

    private static CustomerRequestSubmission SellSubmission(string phone, string model = "RTX 3060") =>
        new(CustomerRequestType.Sell, "Ahmed", phone, null, "GZ", "Facebook", null, "Boxed, 2 years old", "GPU", model, "2 years", null,
            null, null, null);

    private static async Task<CustomerRequestView> SubmitRequestAsync(IdentityLoginApiFactory factory, CustomerRequestSubmission submission, Guid? userId)
    {
        using var scope = factory.Services.CreateScope();
        var requests = scope.ServiceProvider.GetRequiredService<CustomerRequestService>();
        var receipt = await requests.SubmitAsync(submission, userId);
        var page = await requests.ListAsync(new CustomerRequestQuery(Search: receipt.Code));
        return await requests.GetAsync(page.Items.Single().Id);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        Assert.Equal(expected, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static Task<Dictionary<string, string>> StepUpAsync(HttpClient client, IdentityLoginApiFactory factory, string email) =>
        AdminApiFactory.StepUpAsync(client, factory, email);

    private static async Task<HttpResponseMessage> MergeAsync(HttpClient client, IdentityLoginApiFactory factory, Guid target, Guid source,
        bool stepUp = true) =>
        await TestSignIn.PostJsonAsync(client, $"{Admin}/{target}/merge", new { sourceAccountId = source },
            stepUp ? await StepUpAsync(client, factory, Support) : null);

    private static async Task<SecurityAuditEvent?> AuditAsync(OwnerBootstrapDatabase database, string eventType)
    {
        await using var audit = database.CreateAuditContext();
        return await audit.SecurityAuditEvents.AsNoTracking().SingleOrDefaultAsync(item => item.EventType == eventType);
    }

    // ---------------------------------------------------------------- list

    [Fact]
    public async Task The_list_shows_customers_newest_first_with_order_stats_and_searches_by_name_email_phone_and_code()
    {
        var database = await fixture.CreateDatabaseAsync();
        var alice = await CustomerAsync(database, "alice", AlicePhone);
        await CustomerAsync(database, "bob", "01122223333");
        await CustomerAsync(database, "carol");
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = await SupportAsync(database, factory);
        var retail = database.CreateRetailServices();
        await DeliveredAsync(retail, "Alice GPU", "01055555555", alice);
        await DeliveredAsync(retail, "Guest GPU", AlicePhone, pos: true);
        await DeliveredAsync(retail, "Other GPU", "01199999999", pos: true);

        var all = await ReadAsync<PagedResult<CustomerListItem>>(await client.GetAsync(Admin));
        var code = all.Items.Single(item => item.Customer.Id == alice).Customer.PersonCode;

        Assert.Equal(["carol", "bob", "alice"], all.Items.Select(item => item.Customer.Name));
        Assert.Equal(3, all.Total);
        var row = all.Items.Single(item => item.Customer.Id == alice);
        Assert.Equal((2, 1000m), (row.OrderCount, row.TotalSpent));
        Assert.NotNull(row.LastOrderAtUtc);
        Assert.Equal((0, 0m, null), (all.Items[0].OrderCount, all.Items[0].TotalSpent, all.Items[0].LastOrderAtUtc));
        Assert.Contains("no-store", (await client.GetAsync(Admin)).Headers.CacheControl!.ToString());
        foreach (var search in new[] { "ALI", "alice@example", "1012345", code })
            Assert.Equal(alice, Assert.Single((await ReadAsync<PagedResult<CustomerListItem>>(await client.GetAsync($"{Admin}?search={Uri.EscapeDataString(search)}"))).Items).Customer.Id);
        Assert.Empty((await ReadAsync<PagedResult<CustomerListItem>>(await client.GetAsync($"{Admin}?search=nobody"))).Items);
    }

    // ---------------------------------------------------------------- 360

    [Fact]
    public async Task The_360_view_joins_account_orders_by_account_or_phone_warranties_requests_blacklist_and_stats()
    {
        var database = await fixture.CreateDatabaseAsync();
        var alice = await CustomerAsync(database, "alice", AlicePhone);
        await database.CreateAddressService().CreateAsync(alice, Address("Home", true));
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = await SupportAsync(database, factory);
        var retail = database.CreateRetailServices();
        var linked = await DeliveredAsync(retail, "Linked GPU", "01055555555", alice);
        var guest = await DeliveredAsync(retail, "Guest GPU", AlicePhone, pos: true);
        await DeliveredAsync(retail, "Stranger GPU", "01199999999", pos: true);
        await retail.Blacklist.AddAsync(new BlacklistRequest(null, "Alice", AlicePhone, null, null, "Chargeback"), Guid.NewGuid());
        await SubmitRequestAsync(factory, SellSubmission(AlicePhone), null);
        await SubmitRequestAsync(factory, SellSubmission("01077777777", "RX 6600"), alice);
        await SubmitRequestAsync(factory, SellSubmission("01188888888", "GTX 1060"), null);
        await using (var identity = database.CreateIdentityContext())
        {
            identity.ExternalLogins.Add(new ExternalLogin { Provider = "Google", Subject = "g-alice", UserAccountId = alice, Email = "alice@gmail.test", CreatedAtUtc = DateTimeOffset.UtcNow });
            await identity.SaveChangesAsync();
        }

        var profile = await ReadAsync<CustomerProfile>(await client.GetAsync($"{Admin}/{alice}"));

        Assert.Equal(("alice", "alice@example.test", AlicePhone, true, "القاهرة"),
            (profile.Account.Name, profile.Account.Email, profile.Account.Phone, profile.Account.IsActive, profile.Account.DefaultGovernorate));
        Assert.StartsWith("PER-", profile.Account.PersonCode);
        Assert.Equal(("alice@example.test", true, "alice@gmail.test"), (profile.LoginMethods.Email, profile.LoginMethods.EmailCode, profile.LoginMethods.GoogleEmail));
        Assert.Empty(profile.Restrictions);
        Assert.Equal(2, profile.Sales.Orders.Count);
        Assert.True(profile.Sales.Orders.Single(order => order.Id == linked.Id).LinkedToAccount);
        Assert.False(profile.Sales.Orders.Single(order => order.Id == guest.Id).LinkedToAccount);
        Assert.Equal((2, 1000m), (profile.Sales.Stats.OrderCount, profile.Sales.Stats.TotalSpent));
        Assert.Equal(2, profile.Sales.Warranties.Count);
        Assert.All(profile.Sales.Warranties, warranty => Assert.Equal((WarrantyStatus.Active, 0), (warranty.Status, warranty.ExtendedMonths)));
        Assert.True(profile.Sales.Blacklisted);
        Assert.Equal(2, profile.Requests.Count);
        Assert.All(profile.Requests, request => Assert.Equal(CustomerRequestType.Sell, request.Type));
        Assert.Contains("no-store", (await client.GetAsync($"{Admin}/{alice}")).Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task The_360_view_of_a_staff_account_or_an_unknown_id_is_not_found_and_without_permission_it_is_forbidden()
    {
        var database = await fixture.CreateDatabaseAsync();
        var staff = await StaffAsync(database, "other@example.test", "Requests.View");
        var customer = await CustomerAsync(database, "alice");
        await using var factory = CreateFactory(database.ConnectionString);
        using var admin = await SupportAsync(database, factory);
        using var viewer = await SignedInAsync(factory, "other@example.test");
        using var anonymous = factory.CreatePreflightedClient(new() { HandleCookies = true });

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Admin}/{staff}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Admin}/{Guid.NewGuid()}")).StatusCode);
        foreach (var url in new[] { Admin, $"{Admin}/{customer}", $"{Admin}/{customer}/view-as" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Forbidden,
            (await TestSignIn.SendJsonAsync(viewer, HttpMethod.Put, $"{Admin}/{customer}/note", new { note = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await TestSignIn.PostJsonAsync(viewer, $"{Admin}/{customer}/sign-out-all", new { })).StatusCode);
    }

    // ---------------------------------------------------------------- actions

    [Fact]
    public async Task The_note_is_saved_cleared_limited_and_audited_without_its_text()
    {
        var database = await fixture.CreateDatabaseAsync();
        var customer = await CustomerAsync(database, "alice");
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = await SupportAsync(database, factory);

        using var saved = await TestSignIn.SendJsonAsync(client, HttpMethod.Put, $"{Admin}/{customer}/note", new { note = "  Prefers WhatsApp  " });

        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        Assert.Equal("Prefers WhatsApp", (await ReadAsync<CustomerProfile>(await client.GetAsync($"{Admin}/{customer}"))).Account.Note);
        var audit = await AuditAsync(database, "Customer.NoteChanged");
        Assert.Equal(customer, audit!.SubjectUserAccountId);
        Assert.NotNull(audit.ActorUserAccountId);
        Assert.DoesNotContain("WhatsApp", audit.SafeMetadata);
        using var tooLong = await TestSignIn.SendJsonAsync(client, HttpMethod.Put, $"{Admin}/{customer}/note", new { note = new string('x', 2001) });
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
        using var cleared = await TestSignIn.SendJsonAsync(client, HttpMethod.Put, $"{Admin}/{customer}/note", new { note = "  " });
        Assert.Equal(HttpStatusCode.NoContent, cleared.StatusCode);
        Assert.Null((await ReadAsync<CustomerProfile>(await client.GetAsync($"{Admin}/{customer}"))).Account.Note);
    }

    [Fact]
    public async Task Sign_out_all_revokes_every_session_of_the_customer_with_the_admin_as_actor()
    {
        var database = await fixture.CreateDatabaseAsync();
        var customer = await CustomerAsync(database, "alice");
        await using var factory = CreateFactory(database.ConnectionString);
        using var admin = await SupportAsync(database, factory);
        using var phone = await SignedInAsync(factory, "alice@example.test");
        using var laptop = await SignedInAsync(factory, "alice@example.test");
        Assert.Equal(2, (await ReadAsync<CustomerProfile>(await admin.GetAsync($"{Admin}/{customer}"))).Account.ActiveSessions);

        using var response = await TestSignIn.PostJsonAsync(admin, $"{Admin}/{customer}/sign-out-all", new { });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await phone.GetAsync("/api/v1/identity/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await laptop.GetAsync("/api/v1/identity/me")).StatusCode);
        Assert.Equal(0, (await ReadAsync<CustomerProfile>(await admin.GetAsync($"{Admin}/{customer}"))).Account.ActiveSessions);
        var audit = await AuditAsync(database, "Customer.SignedOutAll");
        Assert.Equal(customer, audit!.SubjectUserAccountId);
        Assert.NotEqual(customer, audit.ActorUserAccountId);
    }

    [Fact]
    public async Task A_login_code_is_emailed_to_the_customer_and_never_returned()
    {
        var database = await fixture.CreateDatabaseAsync();
        var customer = await CustomerAsync(database, "alice");
        await using var factory = CreateFactory(database.ConnectionString);
        using var admin = await SupportAsync(database, factory);

        using var response = await TestSignIn.PostJsonAsync(admin, $"{Admin}/{customer}/login-code", new { });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var code = await TestSignIn.LatestCodeAsync(factory.Services, "alice@example.test");
        Assert.Equal(EmailCodePurpose.SignIn, code.Purpose);
        Assert.DoesNotContain(code.Code, await response.Content.ReadAsStringAsync());
        Assert.Equal(customer, (await AuditAsync(database, "Customer.LoginCodeSent"))!.SubjectUserAccountId);
    }

    [Fact]
    public async Task Unlinking_google_removes_the_link_once_and_a_second_try_is_not_found()
    {
        var database = await fixture.CreateDatabaseAsync();
        var customer = await CustomerAsync(database, "alice");
        await using (var identity = database.CreateIdentityContext())
        {
            identity.ExternalLogins.Add(new ExternalLogin { Provider = "Google", Subject = "g-alice", UserAccountId = customer, Email = "alice@gmail.test", CreatedAtUtc = DateTimeOffset.UtcNow });
            await identity.SaveChangesAsync();
        }
        await using var factory = CreateFactory(database.ConnectionString);
        using var admin = await SupportAsync(database, factory);

        using var first = await TestSignIn.PostJsonAsync(admin, $"{Admin}/{customer}/google/unlink", new { });
        using var second = await TestSignIn.PostJsonAsync(admin, $"{Admin}/{customer}/google/unlink", new { });

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Null((await ReadAsync<CustomerProfile>(await admin.GetAsync($"{Admin}/{customer}"))).LoginMethods.GoogleEmail);
        Assert.NotEqual(customer, (await AuditAsync(database, "Identity.Google.Unlinked"))!.ActorUserAccountId);
    }

    // ---------------------------------------------------------------- merge

    [Fact]
    public async Task Merge_moves_orders_warranties_requests_builds_addresses_and_google_and_closes_the_source()
    {
        var database = await fixture.CreateDatabaseAsync();
        var target = await CustomerAsync(database, "target");
        var source = await CustomerAsync(database, "source", AlicePhone);
        await using (var identity = database.CreateIdentityContext())
        {
            foreach (var (id, note) in new[] { (target, "target note"), (source, "source note") })
                (await identity.Persons.SingleAsync(person => person.UserAccount!.Id == id)).CustomerNote = note;
            identity.ExternalLogins.Add(new ExternalLogin { Provider = "Google", Subject = "g-source", UserAccountId = source, Email = "source@gmail.test", CreatedAtUtc = DateTimeOffset.UtcNow });
            await identity.SaveChangesAsync();
        }
        var addresses = database.CreateAddressService();
        await addresses.CreateAsync(target, Address("Home", true));
        await addresses.CreateAsync(source, Address("Work", true));
        await using (var builder = database.CreatePcBuilderContext())
        {
            builder.SavedBuilds.Add(new SavedBuild { Id = Guid.NewGuid(), Token = "tok-source", UserAccountId = source, Parts = [new SavedBuildPart("RX-0001", 1)], CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow });
            await builder.SaveChangesAsync();
        }
        await using var factory = CreateFactory(database.ConnectionString);
        using var admin = await SupportAsync(database, factory);
        using var sourceClient = await SignedInAsync(factory, "source@example.test");
        await DeliveredAsync(database.CreateRetailServices(), "Source GPU", "01055555555", source);
        await SubmitRequestAsync(factory, SellSubmission("01077777777"), source);

        using var response = await MergeAsync(admin, factory, target, source);

        var profile = await ReadAsync<CustomerProfile>(response);
        Assert.Equal(target, profile.Account.Id);
        Assert.Equal(AlicePhone, profile.Account.Phone);
        Assert.Equal($"target note\n--- {await PersonCodeAsync(database, source)} ---\nsource note", profile.Account.Note);
        Assert.Equal(("source@gmail.test", 1, 1, 1), (profile.LoginMethods.GoogleEmail, profile.Sales.Orders.Count, profile.Sales.Warranties.Count, profile.Requests.Count));
        await using var identityDb = database.CreateIdentityContext();
        var closed = await identityDb.UserAccounts.AsNoTracking().SingleAsync(account => account.Id == source);
        Assert.Equal((false, target), (closed.IsActive, closed.MergedIntoAccountId));
        Assert.Equal(0, await identityDb.AuthSessions.CountAsync(session => session.UserAccountId == source && session.RevokedAtUtc == null));
        Assert.Equal(HttpStatusCode.Unauthorized, (await sourceClient.GetAsync("/api/v1/identity/me")).StatusCode);
        var moved = await identityDb.CustomerAddresses.AsNoTracking().Where(address => address.UserAccountId == target).ToListAsync();
        Assert.Equal(["Home", "Work"], moved.Select(address => address.Label).Order());
        Assert.Equal("Home", Assert.Single(moved, address => address.IsDefault).Label);
        Assert.Empty(await identityDb.CustomerAddresses.Where(address => address.UserAccountId == source).ToListAsync());
        Assert.Equal(target, (await identityDb.ExternalLogins.SingleAsync()).UserAccountId);
        await using var builderDb = database.CreatePcBuilderContext();
        Assert.Equal(target, (await builderDb.SavedBuilds.SingleAsync()).UserAccountId);
        var audit = await AuditAsync(database, "Customer.Merged");
        Assert.Equal(target, audit!.SubjectUserAccountId);
        Assert.Contains(source.ToString(), audit.SafeMetadata);
        Assert.Contains("\"Orders\": 1", audit.SafeMetadata);
        var list = await ReadAsync<PagedResult<CustomerListItem>>(await admin.GetAsync(Admin));
        Assert.Equal([target], list.Items.Select(item => item.Customer.Id));
        Assert.Equal(HttpStatusCode.Conflict, (await MergeAsync(admin, factory, target, source)).StatusCode);
    }

    [Fact]
    public async Task Merge_refuses_staff_accounts_self_merge_and_a_missing_step_up_and_changes_nothing()
    {
        var database = await fixture.CreateDatabaseAsync();
        var target = await CustomerAsync(database, "target");
        var source = await CustomerAsync(database, "source");
        await using var factory = CreateFactory(database.ConnectionString);
        var staff = await StaffAsync(database, "staff@example.test", "Requests.View");
        using var admin = await SupportAsync(database, factory);
        await DeliveredAsync(database.CreateRetailServices(), "Source GPU", "01055555555", source);

        Assert.Equal(HttpStatusCode.Forbidden, (await MergeAsync(admin, factory, target, source, stepUp: false)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await MergeAsync(admin, factory, target, staff)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await MergeAsync(admin, factory, staff, source)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await MergeAsync(admin, factory, target, target)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await MergeAsync(admin, factory, target, Guid.NewGuid())).StatusCode);

        await using var identityDb = database.CreateIdentityContext();
        Assert.Equal(0, await identityDb.UserAccounts.CountAsync(account => account.MergedIntoAccountId != null));
        await using var retailDb = database.CreateRetailContext();
        Assert.Equal(source, (await retailDb.Orders.SingleAsync()).UserAccountId);
        Assert.Null(await AuditAsync(database, "Customer.Merged"));
    }

    private static async Task<string> PersonCodeAsync(OwnerBootstrapDatabase database, Guid accountId)
    {
        await using var identity = database.CreateIdentityContext();
        return await identity.UserAccounts.Where(account => account.Id == accountId).Select(account => account.Person.PersonCode).SingleAsync();
    }

    // ---------------------------------------------------------------- view as customer

    [Fact]
    public async Task View_as_returns_what_the_customer_sees_hides_internal_fields_and_is_audited()
    {
        var database = await fixture.CreateDatabaseAsync();
        var customer = await CustomerAsync(database, "alice", AlicePhone);
        await database.CreateAddressService().CreateAsync(customer, Address("Home", true));
        await using (var builder = database.CreatePcBuilderContext())
        {
            builder.SavedBuilds.Add(new SavedBuild { Id = Guid.NewGuid(), Token = "tok-alice", UserAccountId = customer, Name = "My build", Parts = [new SavedBuildPart("RX-0001", 1)], CreatedAtUtc = DateTimeOffset.UtcNow, UpdatedAtUtc = DateTimeOffset.UtcNow });
            await builder.SaveChangesAsync();
        }
        await using var factory = CreateFactory(database.ConnectionString);
        using var admin = await SupportAsync(database, factory);
        var retail = database.CreateRetailServices();
        await DeliveredAsync(retail, "Alice GPU", "01055555555", customer);
        await DeliveredAsync(retail, "Guest GPU", AlicePhone, pos: true);
        var request = await SubmitRequestAsync(factory, SellSubmission("01077777777"), customer);
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<RequestFollowUpService>().OfferAsync(request.Id, 4000m, 9000m, request.RowVersion);
        using var customerClient = await SignedInAsync(factory, "alice@example.test");

        using var response = await admin.GetAsync($"{Admin}/{customer}/view-as");
        var raw = await response.Content.ReadAsStringAsync();
        var view = System.Text.Json.JsonSerializer.Deserialize<CustomerViewAs>(raw, System.Text.Json.JsonSerializerOptions.Web)!;
        var own = await ReadAsync<List<CustomerOrderView>>(await customerClient.GetAsync("/api/v1/orders/mine"));

        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal(own.Select(order => order.Id), view.Orders.Select(order => order.Id));
        Assert.Single(view.Orders);
        Assert.Equal((1, 1, 1, 1, 1), (view.Addresses.Count, view.Warranties.Count, view.Invoices.Count, view.Requests.Count, view.Builds.Count));
        Assert.Equal(4000m, view.Requests[0].OfferAmount);
        Assert.Equal("alice@example.test", view.Profile.Email);
        Assert.DoesNotContain("expectedResale", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internalNote", raw, StringComparison.OrdinalIgnoreCase);
        var audit = await AuditAsync(database, "Customer.ViewedAs");
        Assert.Equal(customer, audit!.SubjectUserAccountId);
        Assert.NotEqual(customer, audit.ActorUserAccountId);
    }

    // ---------------------------------------------------------------- warranty extension

    [Fact]
    public async Task Extending_a_warranty_adds_months_keeps_the_history_and_shows_in_the_customer_lookup()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = CreateFactory(database.ConnectionString);
        using var admin = await SupportAsync(database, factory);
        var retail = database.CreateRetailServices();
        await DeliveredAsync(retail, "Extend GPU", AlicePhone, pos: true);
        var warranty = await FirstWarrantyAsync(database);

        using var response = await TestSignIn.PostJsonAsync(admin, $"/api/v1/admin/retail/warranties/{warranty.Id}/extend", new { months = 3, reason = "  Goodwill  " });

        var view = await ReadAsync<WarrantyAdminView>(response);
        Assert.Equal(warranty.EndsAtUtc!.Value.AddMonths(3), view.EndsAtUtc);
        Assert.Equal((3, warranty.RowVersion + 1), (view.ExtendedMonths, view.RowVersion));
        await using var db = database.CreateRetailContext();
        var history = await db.WarrantyExtensions.SingleAsync();
        Assert.Equal((warranty.Id, 3, warranty.EndsAtUtc, view.EndsAtUtc, "Goodwill"), (history.WarrantyId, history.Months, history.PreviousEndUtc, history.NewEndUtc, history.Reason));
        Assert.NotEqual(Guid.Empty, history.ActorUserAccountId);
        var lookup = await retail.Warranties.LookupAsync(warranty.Code);
        Assert.Equal(view.EndsAtUtc, lookup.Groups.Single().Items.Single().EndsAtUtc);
        using var again = await TestSignIn.PostJsonAsync(admin, $"/api/v1/admin/retail/warranties/{warranty.Id}/extend", new { months = 2, reason = "More" });
        Assert.Equal(5, (await ReadAsync<WarrantyAdminView>(again)).ExtendedMonths);
    }

    [Fact]
    public async Task Extending_refuses_void_and_manufacturer_warranties_bad_input_and_staff_without_the_permission()
    {
        var database = await fixture.CreateDatabaseAsync();
        await StaffAsync(database, "viewer@example.test", "Sales.ViewOrders");
        await using var factory = CreateFactory(database.ConnectionString);
        using var admin = await SupportAsync(database, factory);
        using var viewer = await SignedInAsync(factory, "viewer@example.test");
        var retail = database.CreateRetailServices();
        await DeliveredAsync(retail, "Void GPU", AlicePhone, pos: true);
        await DeliveredAsync(retail, "Agent GPU", AlicePhone, pos: true, label: "ضمان الوكيل", months: null);
        var warranties = await AllWarrantiesAsync(database);
        var open = warranties.Single(item => item.EndsAtUtc is not null);
        var manufacturer = warranties.Single(item => item.EndsAtUtc is null);
        var voided = await retail.Warranties.VoidAsync(open.Id, new WarrantyVoidRequest("Sticker removed", open.RowVersion), Guid.NewGuid());
        Assert.NotNull(voided.VoidedAtUtc);

        string Url(Guid id) => $"/api/v1/admin/retail/warranties/{id}/extend";
        Assert.Equal(HttpStatusCode.Conflict, (await TestSignIn.PostJsonAsync(admin, Url(open.Id), new { months = 3, reason = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await TestSignIn.PostJsonAsync(admin, Url(manufacturer.Id), new { months = 3, reason = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await TestSignIn.PostJsonAsync(admin, Url(Guid.NewGuid()), new { months = 3, reason = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await TestSignIn.PostJsonAsync(viewer, Url(open.Id), new { months = 3, reason = "x" })).StatusCode);
        await using var db = database.CreateRetailContext();
        Assert.Empty(await db.WarrantyExtensions.ToListAsync());
    }

    [Fact]
    public async Task Extending_needs_valid_months_and_a_reason()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = CreateFactory(database.ConnectionString);
        using var admin = await SupportAsync(database, factory);
        await DeliveredAsync(database.CreateRetailServices(), "Extend GPU", AlicePhone, pos: true);
        var warranty = await FirstWarrantyAsync(database);
        var url = $"/api/v1/admin/retail/warranties/{warranty.Id}/extend";

        Assert.Equal(HttpStatusCode.BadRequest, (await TestSignIn.PostJsonAsync(admin, url, new { months = 0, reason = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await TestSignIn.PostJsonAsync(admin, url, new { months = 37, reason = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await TestSignIn.PostJsonAsync(admin, url, new { months = 3, reason = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await TestSignIn.PostJsonAsync(admin, url, new { months = 36, reason = "Max" })).StatusCode);
    }

    private static async Task<Warranty> FirstWarrantyAsync(OwnerBootstrapDatabase database) => Assert.Single(await AllWarrantiesAsync(database));

    private static async Task<List<Warranty>> AllWarrantiesAsync(OwnerBootstrapDatabase database)
    {
        await using var db = database.CreateRetailContext();
        return await db.Warranties.AsNoTracking().OrderBy(warranty => warranty.CreatedAtUtc).ToListAsync();
    }

    // ---------------------------------------------------------------- my requests

    [Fact]
    public async Task My_requests_lists_only_my_requests_shows_the_offer_once_made_and_hides_internal_fields()
    {
        var database = await fixture.CreateDatabaseAsync();
        var alice = await CustomerAsync(database, "alice");
        var bob = await CustomerAsync(database, "bob");
        await using var factory = CreateFactory(database.ConnectionString);
        var offered = await SubmitRequestAsync(factory, SellSubmission("01077777777", "RX 6600"), alice);
        var plain = await SubmitRequestAsync(factory, SellSubmission("01088888888", "GTX 1060"), alice);
        await SubmitRequestAsync(factory, SellSubmission("01099999999", "RTX 4090"), bob);
        using (var scope = factory.Services.CreateScope())
        {
            var followUp = scope.ServiceProvider.GetRequiredService<RequestFollowUpService>();
            await followUp.OfferAsync(offered.Id, 4000m, 9000m, offered.RowVersion);
            await followUp.SetNoteAsync(plain.Id, "private note", plain.RowVersion);
        }
        using var customer = await SignedInAsync(factory, "alice@example.test");
        using var anonymous = factory.CreatePreflightedClient(new() { HandleCookies = true });

        using var response = await customer.GetAsync("/api/v1/account/requests");

        var mine = await ReadAsync<List<MyRequestView>>(response);
        Assert.Equal(["GTX 1060", "RX 6600"], mine.Select(request => request.Item));
        Assert.Equal((null, 4000m), (mine[0].OfferAmount, mine[1].OfferAmount));
        Assert.Equal((CustomerRequestStatus.New, CustomerRequestStatus.Offered), (mine[0].Status, mine[1].Status));
        var raw = await response.Content.ReadAsStringAsync();
        foreach (var hidden in new[] { "internalNote", "expectedResale", "private note", "phone", "purchase" })
            Assert.DoesNotContain(hidden, raw, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/account/requests")).StatusCode);
    }

    private static CustomerAddressCreateRequest Address(string label, bool isDefault) =>
        new(label, "Recipient", "1000000000", "EG", "+20", "القاهرة", "Cairo", "Nasr City", "Street 1", "12", "3", "5", "Near the mall", "11511",
            "Call before arrival", isDefault);
}
