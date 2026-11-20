using System.Net;
using System.Net.Http.Json;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using static HardwareStore.Api.Tests.RetailReceipts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class RetailAuthorizationMatrixTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Finance_report_reader_cannot_manage_partners_or_post_payouts()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "finance-reader", ["Finance.ViewReports"]);
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(client, factory, "finance-reader");

        var partnerUrl = $"/api/v1/admin/retail/partners/{Guid.NewGuid()}";
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/retail/partner/earnings")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/retail/partners")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(client, "/api/v1/admin/retail/partners", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(client, $"{partnerUrl}/capital", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(client, "/api/v1/admin/retail/partners/month-close", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(client, $"{partnerUrl}/payouts", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/admin/retail/partner/allocations")).StatusCode);
    }

    [Fact]
    public async Task Retail_business_errors_are_returned_as_problem_details()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "build-editor", ["Catalog.ManageProducts"]);
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(client, factory, "build-editor");

        using var response = await PostAsync(client, "/api/v1/admin/retail/assemblies", new { name = "", unitIds = Array.Empty<Guid>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.Equal(400, problem!.Status);
        Assert.Equal("Assembly name is required.", problem.Title);
    }

    [Fact]
    public async Task Inventory_and_customer_permissions_mask_sensitive_fields_at_the_api()
    {
        var database = await fixture.CreateDatabaseAsync();
        var retail = database.CreateRetailServices();
        var productId = Guid.NewGuid();
        var partner = await retail.CreateAsync("Matrix Partner", 10m);
        await retail.ReceiveUnitsAsync(new ReceiveRequest(Guid.NewGuid(), null,
            new ReceiptSource("Secret Supplier", "Supplier", "Component", new DateOnly(2026, 11, 1)),
            [new ReceiptLine("Matrix GPU", "New", 1, 700m, 1200m, ProductId: productId, SerialNumber: "SERIAL-SECRET", PartnerPercentOverride: 8m,
                PartnerId: partner.Id)]),
            Guid.NewGuid());
        var customerId = await CreateUserAsync(database, "inventory-viewer", ["Inventory.View"]);
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(client, factory, "inventory-viewer");

        var response = await client.GetAsync("/api/v1/admin/retail/units");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var units = await response.Content.ReadFromJsonAsync<List<UnitView>>();
        var unit = Assert.Single(units!);
        Assert.Null(unit.AcquisitionCost);
        Assert.Null(unit.SerialNumber);
        Assert.Null(unit.SourceName);
        Assert.Null(unit.PartnerId);
        Assert.Equal("Matrix GPU", unit.Name);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/retail/assemblies")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/retail/orders")).StatusCode);

        await using var identity = database.CreateIdentityContext();
        var role = await identity.Roles.SingleAsync(item => item.Code == "inventory-viewer-role");
        identity.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = "Inventory.ViewCosts" });
        await identity.SaveChangesAsync();

        await using var costFactory = CreateFactory(database.ConnectionString);
        using var costClient = costFactory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(costClient, costFactory, "inventory-viewer");
        var withCosts = await (await costClient.GetAsync("/api/v1/admin/retail/units"))
            .Content.ReadFromJsonAsync<List<UnitView>>();
        Assert.Equal(700m, Assert.Single(withCosts!).AcquisitionCost);
    }

    [Fact]
    public async Task Sales_routes_follow_their_permissions_and_the_manual_order_route_is_gone()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "pos-seller", ["Sales.CreatePOS"]);
        await CreateUserAsync(database, "order-approver", ["Sales.ApproveOrder"]);
        await using var factory = CreateFactory(database.ConnectionString);
        var orderUrl = $"/api/v1/admin/retail/orders/{Guid.NewGuid()}";

        using var seller = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(seller, factory, "pos-seller");
        Assert.Equal(HttpStatusCode.OK, (await seller.GetAsync("/api/v1/admin/retail/pos/customers?search=01000000000")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(seller, "/api/v1/admin/retail/pos/preview", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(seller, "/api/v1/admin/retail/pos/sales", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.GetAsync("/api/v1/admin/retail/blacklist")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.GetAsync("/api/v1/admin/retail/warranties")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(seller, $"/api/v1/admin/retail/warranties/{Guid.NewGuid()}/void", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(seller, "/api/v1/admin/retail/blacklist", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(seller, $"{orderUrl}/payments", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(seller, HttpMethod.Put, $"{orderUrl}/shipping", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(seller, HttpMethod.Put, $"{orderUrl}/reservation", new { })).StatusCode);
        Assert.Contains((await PostAsync(seller, "/api/v1/admin/retail/orders", new { })).StatusCode,
            new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });

        using var approver = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(approver, factory, "order-approver");
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(approver, "/api/v1/admin/retail/pos/sales", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await approver.GetAsync("/api/v1/admin/retail/blacklist")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(approver, "/api/v1/admin/retail/blacklist", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(approver, HttpMethod.Delete, $"/api/v1/admin/retail/blacklist/{Guid.NewGuid()}", null)).StatusCode);
    }

    [Fact]
    public async Task Warranty_routes_follow_their_permissions_and_the_lookup_is_public()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "order-viewer", ["Sales.ViewOrders"]);
        await CreateUserAsync(database, "return-clerk", ["Sales.ProcessReturn"]);
        await using var factory = CreateFactory(database.ConnectionString);
        var voidUrl = $"/api/v1/admin/retail/warranties/{Guid.NewGuid()}/void";

        using var anonymous = factory.CreatePreflightedClient(new() { HandleCookies = true });
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.GetAsync("/api/v1/warranty/lookup")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/api/v1/warranty/lookup?q=RX-0001")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/warranties/mine")).StatusCode);

        using var viewer = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(viewer, factory, "order-viewer");
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/v1/admin/retail/warranties")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/v1/warranties/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(viewer, voidUrl, new { })).StatusCode);

        using var clerk = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(clerk, factory, "return-clerk");
        Assert.Equal(HttpStatusCode.Forbidden, (await clerk.GetAsync("/api/v1/admin/retail/warranties")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(clerk, voidUrl, new { reason = "Sticker removed", rowVersion = 1 })).StatusCode);
    }

    [Fact]
    public async Task Invoice_routes_follow_their_permissions_and_the_public_link_and_quote_are_anonymous()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "invoice-viewer", ["Sales.ViewOrders"]);
        await CreateUserAsync(database, "invoice-clerk", ["Sales.ProcessReturn"]);
        await using var factory = CreateFactory(database.ConnectionString);
        var orderInvoiceUrl = $"/api/v1/admin/retail/orders/{Guid.NewGuid()}/invoice";

        using var anonymous = factory.CreatePreflightedClient(new() { HandleCookies = true });
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync("/api/v1/invoices/unknown-token")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/invoices/mine")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(anonymous, "/api/v1/store/quote", new { items = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(orderInvoiceUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/admin/retail/invoices")).StatusCode);

        using var viewer = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(viewer, factory, "invoice-viewer");
        Assert.Equal(HttpStatusCode.NotFound, (await viewer.GetAsync(orderInvoiceUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/v1/admin/retail/invoices")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync("/api/v1/invoices/mine")).StatusCode);

        using var clerk = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(clerk, factory, "invoice-clerk");
        Assert.Equal(HttpStatusCode.Forbidden, (await clerk.GetAsync(orderInvoiceUrl)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await clerk.GetAsync("/api/v1/admin/retail/invoices")).StatusCode);
    }

    [Fact]
    public async Task Dashboard_money_and_expense_routes_follow_their_permissions()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "report-reader", ["Finance.ViewReports"]);
        await CreateUserAsync(database, "vault-viewer", ["Finance.ViewVaults"]);
        await CreateUserAsync(database, "disburser", ["Finance.Disburse"]);
        await CreateUserAsync(database, "expense-manager", ["Finance.ManageExpenses"]);
        await using var factory = CreateFactory(database.ConnectionString);
        const string retail = "/api/v1/admin/retail";

        using var anonymous = factory.CreatePreflightedClient(new() { HandleCookies = true });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/admin/dashboard")).StatusCode);

        using var reader = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(reader, factory, "report-reader");
        var dashboard = await reader.GetAsync("/api/v1/admin/dashboard?from=2026-10-01&to=2026-10-31");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Contains("\"snapshot\"", await dashboard.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync("/api/v1/admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await reader.GetAsync("/api/v1/admin/dashboard?from=2026-11-02&to=2026-11-01")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync($"{retail}/expense-types")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync($"{retail}/recurring-expenses")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync($"{retail}/expenses/summary?from=2026-10-01&to=2026-10-31")).StatusCode);
        var export = await reader.GetAsync("/api/v1/admin/system/export/expenses?from=2026-10-01&to=2026-10-31");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        var bytes = await export.Content.ReadAsByteArrayAsync();
        Assert.Equal("EFBBBF", Convert.ToHexString(bytes, 0, 3));
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync($"{retail}/money/accounts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(reader, $"{retail}/expense-types", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(reader, $"{retail}/money/transfers", new { })).StatusCode);

        using var vaults = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(vaults, factory, "vault-viewer");
        var accounts = await vaults.GetAsync($"{retail}/money/accounts");
        Assert.Equal(HttpStatusCode.OK, accounts.StatusCode);
        Assert.Equal(4, (await accounts.Content.ReadFromJsonAsync<List<MoneyAccountView>>())!.Count);
        Assert.Equal(HttpStatusCode.NotFound, (await vaults.GetAsync($"{retail}/money/accounts/{Guid.NewGuid()}/movements")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await vaults.GetAsync("/api/v1/admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(vaults, $"{retail}/money/transfers", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(vaults, $"{retail}/money/accounts", new { })).StatusCode);

        using var disburser = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(disburser, factory, "disburser");
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(disburser, $"{retail}/money/transfers", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(disburser, $"{retail}/money/accounts", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await disburser.GetAsync($"{retail}/money/accounts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(disburser, $"{retail}/expense-types", new { })).StatusCode);

        using var manager = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(manager, factory, "expense-manager");
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(manager, $"{retail}/expense-types", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(manager, $"{retail}/recurring-expenses", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync($"{retail}/expense-types")).StatusCode);
    }

    [Fact]
    public async Task Tracking_is_public_and_unknown_tokens_are_not_found()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/track/unknown-token")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(client, "/api/v1/track/unknown-token/rating", new { score = 5 })).StatusCode);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, object? body)
    {
        var csrf = await GetCsrfAsync(client);
        using var request = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request);
    }

    private static IdentityLoginApiFactory CreateFactory(string connectionString) =>
        new(connectionString, configureTestServices: services =>
        {
            services.RemoveAll<IDbContextFactory<RetailDbContext>>();
            services.AddDbContextFactory<RetailDbContext>(options => options.UseNpgsql(connectionString));
            services.RemoveAll<IDbContextFactory<StorefrontDbContext>>();
            services.AddDbContextFactory<StorefrontDbContext>(options => options.UseNpgsql(connectionString));
            services.RemoveAll<IDbContextFactory<CatalogDbContext>>();
            services.AddDbContextFactory<CatalogDbContext>(options => options.UseNpgsql(connectionString));
        });

    private static Task<Guid> CreateUserAsync(OwnerBootstrapDatabase database, string username, string[] permissions) =>
        EmailCodeSignInTests.CreateAccountAsync(database, $"{username}@example.test", verified: true, active: true,
            roleCode: $"{username}-role", permissions);

    private static Task LoginAsync(HttpClient client, IdentityLoginApiFactory factory, string username) =>
        TestSignIn.SignInAsync(client, factory.Services, $"{username}@example.test");

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string url, object body)
    {
        var csrf = await GetCsrfAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request);
    }

    private static async Task<string> GetCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/identity/antiforgery");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CsrfPayload>();
        return payload!.RequestToken;
    }

    private sealed record CsrfPayload(string RequestToken);
    private sealed record ProblemPayload(int Status, string Title);
}
