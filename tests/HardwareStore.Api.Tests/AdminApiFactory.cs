using System.Net;
using HardwareStore.Modules.Catalog;
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

/// <summary>The real API host with every module pointed at one test database, plus signed-in staff clients and step-up codes.</summary>
internal static class AdminApiFactory
{
    public static IdentityLoginApiFactory Create(string connectionString) =>
        new(connectionString, configureTestServices: services =>
        {
            services.RemoveAll<IDbContextFactory<RetailDbContext>>();
            services.AddDbContextFactory<RetailDbContext>(options => options.UseNpgsql(connectionString));
            services.RemoveAll<IDbContextFactory<StorefrontDbContext>>();
            services.AddDbContextFactory<StorefrontDbContext>(options => options.UseNpgsql(connectionString));
            services.RemoveAll<IDbContextFactory<PcBuilderDbContext>>();
            services.AddDbContextFactory<PcBuilderDbContext>(options => options.UseNpgsql(connectionString));
            services.RemoveAll<IDbContextFactory<CatalogDbContext>>();
            services.AddDbContextFactory<CatalogDbContext>(options => options.UseNpgsql(connectionString));
            services.RemoveAll<IDbContextFactory<PurchasingDbContext>>();
            services.AddDbContextFactory<PurchasingDbContext>(options => options.UseNpgsql(connectionString));
        });

    public static Task<Guid> StaffAsync(OwnerBootstrapDatabase database, string email, params string[] permissions) =>
        EmailCodeSignInTests.CreateAccountAsync(database, email, verified: true, active: true, roleCode: $"{email}-role", permissions);

    public static async Task<HttpClient> SignedInAsync(IdentityLoginApiFactory factory, string email)
    {
        var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await TestSignIn.SignInAsync(client, factory.Services, email);
        return client;
    }

    /// <summary>Asks for a step-up code the way the admin UI does and returns the header that carries it.</summary>
    public static async Task<Dictionary<string, string>> StepUpAsync(HttpClient client, IdentityLoginApiFactory factory, string email)
    {
        await using (var db = await factory.Services.GetRequiredService<IDbContextFactory<IdentityDbContext>>().CreateDbContextAsync())
        {
            var normalized = IdentityLoginService.NormalizeIdentifier(email);
            await db.EmailCodes.Where(code => code.NormalizedEmail == normalized).ExecuteDeleteAsync();
        }
        using var request = await TestSignIn.PostJsonAsync(client, "/api/v1/identity/step-up/request", new { });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var code = await TestSignIn.LatestCodeAsync(factory.Services, email);
        return new Dictionary<string, string> { [StepUpService.HeaderName] = code.Code };
    }
}
