using System.Net;
using System.Net.Http.Json;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class RetailAuthorizationMatrixTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Password = "a sufficiently long matrix password";

    [Fact]
    public async Task Finance_report_reader_cannot_write_agreements_or_post_settlements()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateUserAsync(database, "finance-reader", ["Finance.ViewReports"]);
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(client, "finance-reader");

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
        await LoginAsync(client, "build-editor");

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
        await retail.Units.ReceiveAsync(new ReceiveRequest(Guid.NewGuid(), null,
        [
            new ReceiptLine(productId, null, "Matrix GPU", "MATRIX-1", "New", "SERIAL-SECRET", 700m, 1, 8m, null,
                1200m, "Secret Supplier", "Brand", "Model", "GPU", null, null, null, "Component", "Supplier",
                new DateOnly(2026, 11, 1), "[{\"storageKey\":\"matrix/key.webp\"}]")
        ]), Guid.NewGuid());
        var customerId = await CreateUserAsync(database, "inventory-viewer", ["Inventory.View"]);
        await using var factory = CreateFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await LoginAsync(client, "inventory-viewer");

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
        await LoginAsync(costClient, "inventory-viewer");
        var withCosts = await (await costClient.GetAsync("/api/v1/admin/retail/units"))
            .Content.ReadFromJsonAsync<List<UnitView>>();
        Assert.Equal(700m, Assert.Single(withCosts!).AcquisitionCost);
    }

    private static IdentityLoginApiFactory CreateFactory(string connectionString) =>
        new(connectionString, configureTestServices: services =>
        {
            services.RemoveAll<IDbContextFactory<RetailDbContext>>();
            services.AddDbContextFactory<RetailDbContext>(options => options.UseNpgsql(connectionString));
        });

    private static async Task<Guid> CreateUserAsync(OwnerBootstrapDatabase database, string username, IReadOnlyList<string> permissions)
    {
        await using var db = database.CreateIdentityContext();
        var role = new Role { Id = Guid.CreateVersion7(), Code = $"{username}-role", Name = username };
        db.Roles.Add(role);
        foreach (var permission in permissions)
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = permission });
        var person = await new PersonCodeGenerator(db, TimeProvider.System,
                TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"))
            .CreatePersonAsync(new Person { FullName = username, IsCustomer = true });
        var account = new UserAccount
        {
            Id = Guid.CreateVersion7(), PersonId = person.Id, Username = username,
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
            Email = $"{username}@example.test", NormalizedEmail = IdentityLoginService.NormalizeIdentifier($"{username}@example.test"),
            IsEmailVerified = true, CreatedAtUtc = DateTimeOffset.UtcNow
        };
        account.PasswordHash = new PasswordHasher<UserAccount>(Options.Create(new PasswordHasherOptions()))
            .HashPassword(account, Password);
        db.UserAccounts.Add(account);
        db.UserRoles.Add(new UserRole { UserAccountId = account.Id, RoleId = role.Id });
        db.UserAccountLoginIdentifiers.Add(new UserAccountLoginIdentifier
        {
            NormalizedIdentifier = account.NormalizedUsername, UserAccountId = account.Id
        });
        db.UserAccountLoginIdentifiers.Add(new UserAccountLoginIdentifier
        {
            NormalizedIdentifier = account.NormalizedEmail, UserAccountId = account.Id
        });
        await db.SaveChangesAsync();
        return account.Id;
    }

    private static async Task LoginAsync(HttpClient client, string username)
    {
        var csrf = await GetCsrfAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/sessions")
        {
            Content = JsonContent.Create(new { identifier = username, password = Password })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

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
