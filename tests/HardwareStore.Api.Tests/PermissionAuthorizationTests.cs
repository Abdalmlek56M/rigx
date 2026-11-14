using System.Security.Claims;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class PermissionAuthorizationTests
{
    private static IAuthorizationService CreateService() => new ServiceCollection()
        .AddLogging()
        .AddPermissionAuthorization()
        .BuildServiceProvider()
        .GetRequiredService<IAuthorizationService>();

    private static ClaimsPrincipal User(bool authenticated, params string[] permissions) => new(
        new ClaimsIdentity(permissions.Select(p => new Claim(PermissionAuthorizationHandler.ClaimType, p)),
            authenticated ? "test" : null));

    [Fact]
    public async Task Granted_permission_is_authorized()
    {
        var result = await CreateService().AuthorizeAsync(User(true, "Sales.ViewOrders"), null,
            new PermissionRequirement("Sales.ViewOrders"));
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Catalog_management_permission_is_authorized()
    {
        var result = await CreateService().AuthorizeAsync(User(true, "Catalog.ManageProducts"), null,
            new PermissionRequirement("Catalog.ManageProducts"));
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Missing_permission_is_denied()
    {
        var result = await CreateService().AuthorizeAsync(User(true, "Sales.LogCommunication"), null,
            new PermissionRequirement("Sales.ViewOrders"));
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Anonymous_user_is_denied_even_with_permission_claim()
    {
        var result = await CreateService().AuthorizeAsync(User(false, "Sales.ViewOrders"), null,
            new PermissionRequirement("Sales.ViewOrders"));
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Unknown_permission_is_denied()
    {
        var result = await CreateService().AuthorizeAsync(User(true, "Sales.ViewOrders"), null,
            new PermissionRequirement("Unknown.Bypass"));
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Permission_in_unauthenticated_identity_is_not_trusted_when_another_identity_is_authenticated()
    {
        var principal = new ClaimsPrincipal(new[]
        {
            new ClaimsIdentity(new[] { new Claim(PermissionAuthorizationHandler.ClaimType, "Sales.ViewOrders") }),
            new ClaimsIdentity(authenticationType: "test")
        });
        var result = await CreateService().AuthorizeAsync(principal, null, new PermissionRequirement("Sales.ViewOrders"));
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Fallback_policy_requires_authentication_and_health_endpoints_allow_anonymous()
    {
        var services = new ServiceCollection().AddPermissionAuthorization().BuildServiceProvider();
        var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>().Value;
        Assert.NotNull(options.FallbackPolicy);
        Assert.Contains(options.FallbackPolicy!.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);

        await using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>();
        using var client = factory.CreateClient(); // Starts the TestServer before its dynamic EndpointDataSource is resolved.
        var endpoints = factory.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints;
        foreach (var path in new[] { "/health/live", "/health/ready", "/api/v1/storefront/settings", "/api/v1/storefront/checkout-quote", "/api/v1/storefront/shipping-rates" })
        {
            var endpoint = Assert.Single(endpoints, endpoint =>
                endpoint is Microsoft.AspNetCore.Routing.RouteEndpoint route &&
                route.RoutePattern.RawText == path);
            Assert.NotNull(endpoint.Metadata.GetMetadata<IAllowAnonymous>());
        }
    }

    [Fact]
    public async Task Identity_administration_endpoints_require_authorization()
    {
        await using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var endpoints = factory.Services
            .GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>()
            .Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/v1/admin/identity", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.True(endpoints.Length >= 10);
        Assert.All(endpoints, endpoint =>
            Assert.NotNull(endpoint.Metadata.GetMetadata<IAuthorizeData>()));
    }

    [Fact]
    public async Task Purchasing_endpoints_require_authorization()
    {
        await using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var endpoints = factory.Services
            .GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>()
            .Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/v1/admin/purchasing", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Equal(10, endpoints.Length);
        Assert.All(endpoints, endpoint =>
            Assert.NotNull(endpoint.Metadata.GetMetadata<IAuthorizeData>()));
    }

    [Fact]
    public async Task Inventory_warehouse_endpoints_require_authorization()
    {
        await using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var endpoints = factory.Services
            .GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>()
            .Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/v1/admin/inventory/warehouses", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Equal(4, endpoints.Length);
        Assert.All(endpoints, endpoint =>
            Assert.NotNull(endpoint.Metadata.GetMetadata<IAuthorizeData>()));
    }

    [Fact]
    public async Task Storefront_settings_admin_endpoints_require_authorization()
    {
        await using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var endpoints = factory.Services
            .GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>()
            .Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/api/v1/admin/storefront/settings", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Equal(18, endpoints.Length);
        Assert.All(endpoints, endpoint =>
            Assert.NotNull(endpoint.Metadata.GetMetadata<IAuthorizeData>()));
    }

    [Fact]
    public async Task Each_requested_permission_is_checked_independently()
    {
        var user = User(true, "Sales.ViewOrders", "Sales.LogCommunication");
        var auth = CreateService();
        Assert.True((await auth.AuthorizeAsync(user, null, new PermissionRequirement("Sales.ViewOrders"))).Succeeded);
        Assert.True((await auth.AuthorizeAsync(user, null, new PermissionRequirement("Sales.LogCommunication"))).Succeeded);
        Assert.False((await auth.AuthorizeAsync(user, null, new PermissionRequirement("Sales.ApproveOrder"))).Succeeded);
    }
}
