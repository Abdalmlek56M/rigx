using System.Net;
using System.Net.Http.Json;
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
    public async Task Finance_report_reader_cannot_write_agreements_or_post_settlements()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "finance-reader", ["Finance.ViewReports"]);
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(client, factory, "finance-reader");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/retail/partner/earnings")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/retail/partner/allocations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(client, "/api/v1/admin/retail/partner/allocations", new { partnerName = "X", unitId = Guid.NewGuid(), percent = 10, isActive = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(client, "/api/v1/admin/retail/partner/agreements", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(client, "/api/v1/admin/retail/partner/settlements", new { })).StatusCode);
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
        await retail.ReceiveUnitsAsync(new ReceiveRequest(Guid.NewGuid(), null,
            new ReceiptSource("Secret Supplier", "Supplier", "Component", new DateOnly(2026, 11, 1)),
            [new ReceiptLine("Matrix GPU", "New", 1, 700m, 1200m, ProductId: productId, SerialNumber: "SERIAL-SECRET", PartnerPercentOverride: 8m)]),
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
        Assert.Equal(HttpStatusCode.NotFound, (await seller.GetAsync("/api/v1/admin/retail/pos/customers?phone=01000000000")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(seller, "/api/v1/admin/retail/pos/preview", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostAsync(seller, "/api/v1/admin/retail/pos/sales", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await seller.GetAsync("/api/v1/admin/retail/blacklist")).StatusCode);
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
    public async Task Tracking_and_the_sold_feed_are_public_and_unknown_tokens_are_not_found()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });

        var feed = await client.GetAsync("/api/v1/store/sold");
        Assert.Equal(HttpStatusCode.OK, feed.StatusCode);
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
