using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class IdentitySessionEndpointTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Sign_out_current_and_sign_out_all_revoke_the_expected_sessions()
    {
        var database = await fixture.CreateDatabaseAsync();
        const string email = "devices@example.test";
        var accountId = await EmailCodeSignInTests.CreateAccountAsync(database, email, true, true, "Customer");
        await using var factory = new IdentityLoginApiFactory(database.ConnectionString);
        using var laptop = factory.CreatePreflightedClient(new() { HandleCookies = true });
        using var phone = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await TestSignIn.SignInAsync(laptop, factory.Services, email);
        await TestSignIn.SignInAsync(phone, factory.Services, email);
        Assert.Equal(2, await ActiveSessionsAsync(database, accountId));

        using var current = await TestSignIn.SendJsonAsync(laptop, HttpMethod.Delete, "/api/v1/identity/sessions/current", null);
        Assert.Equal(HttpStatusCode.NoContent, current.StatusCode);
        Assert.Equal(1, await ActiveSessionsAsync(database, accountId));
        Assert.Equal(HttpStatusCode.Unauthorized, (await laptop.GetAsync("/api/v1/identity/me")).StatusCode);

        using var all = await TestSignIn.SendJsonAsync(phone, HttpMethod.Delete, "/api/v1/identity/sessions", null);
        Assert.Equal(HttpStatusCode.NoContent, all.StatusCode);
        Assert.Equal(0, await ActiveSessionsAsync(database, accountId));
    }

    [Fact]
    public async Task Current_user_endpoint_requires_sign_in_and_returns_only_safe_profile_data()
    {
        var database = await fixture.CreateDatabaseAsync();
        await EmailCodeSignInTests.CreateAccountAsync(database, "me@example.test", true, true, "Viewer", "Catalog.View");
        await using var factory = new IdentityLoginApiFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/identity/me")).StatusCode);

        await TestSignIn.SignInAsync(client, factory.Services, "me@example.test");
        using var response = await client.GetAsync("/api/v1/identity/me");

        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = payload.RootElement;
        Assert.Equal("me@example.test", root.GetProperty("email").GetString());
        Assert.Contains(root.GetProperty("permissions").EnumerateArray(), item => item.GetString() == "Catalog.View");
        Assert.False(root.TryGetProperty("passwordHash", out _));
        Assert.False(root.TryGetProperty("personId", out _));
    }

    [Fact]
    public async Task Sign_in_and_sign_out_endpoints_require_csrf()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = new IdentityLoginApiFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });

        using var request = await client.PostAsJsonAsync("/api/v1/identity/email-code/request", new { email = "csrf@example.test" });
        using var verify = await client.PostAsJsonAsync("/api/v1/identity/email-code/verify", new { email = "csrf@example.test", code = "123456" });
        using var stepUp = await client.PostAsJsonAsync("/api/v1/identity/step-up/request", new { });
        using var signOut = await client.DeleteAsync("/api/v1/identity/sessions");

        Assert.All(new[] { request, verify, stepUp, signOut },
            response => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));
    }

    private static async Task<int> ActiveSessionsAsync(OwnerBootstrapDatabase database, Guid accountId)
    {
        await using var db = database.CreateIdentityContext();
        return await db.AuthSessions.CountAsync(session => session.UserAccountId == accountId && session.RevokedAtUtc == null);
    }
}
