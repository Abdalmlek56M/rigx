using System.Net;
using System.Net.Http.Json;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class GoogleSignInTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string Google = "/api/v1/identity/google";
    private const string Methods = "/api/v1/identity/login-methods";

    [Fact]
    public async Task New_verified_google_email_creates_a_customer_and_signs_in()
    {
        await using var host = await StartAsync();
        using var client = host.NewClient();

        using var response = await SignInAsync(client, Token("sub-1", "new@example.test"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<SignInPayload>())!.IsNewAccount);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie => cookie.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase));
        var methods = await client.GetFromJsonAsync<MethodsPayload>(Methods);
        Assert.Equal("new@example.test", methods!.Email);
        Assert.Equal("new@example.test", methods.GoogleEmail);
        Assert.True(methods.EmailCode);
    }

    [Fact]
    public async Task Same_google_subject_signs_into_the_same_account_again()
    {
        await using var host = await StartAsync();
        using var first = host.NewClient();
        using var second = host.NewClient();
        (await SignInAsync(first, Token("sub-1", "again@example.test"))).Dispose();

        using var response = await SignInAsync(second, Token("sub-1", "again@example.test"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<SignInPayload>())!.IsNewAccount);
        Assert.Equal("again@example.test", (await second.GetFromJsonAsync<MethodsPayload>(Methods))!.Email);
    }

    [Fact]
    public async Task Verified_google_email_links_to_the_existing_email_code_customer()
    {
        await using var host = await StartAsync();
        using var emailClient = host.NewClient();
        await TestSignIn.SignInAsync(emailClient, host.Factory.Services, "shopper@example.test");
        using var googleClient = host.NewClient();

        using var response = await SignInAsync(googleClient, Token("sub-9", "Shopper@Example.test"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await response.Content.ReadFromJsonAsync<SignInPayload>())!.IsNewAccount);
        var methods = await googleClient.GetFromJsonAsync<MethodsPayload>(Methods);
        Assert.Equal("shopper@example.test", methods!.Email);
        Assert.Equal("Shopper@Example.test", methods.GoogleEmail);
    }

    [Fact]
    public async Task Unverified_google_email_without_a_link_is_rejected()
    {
        await using var host = await StartAsync();
        using var client = host.NewClient();

        using var response = await SignInAsync(client, Token("sub-1", "unverified@example.test", verified: false));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Methods)).StatusCode);
    }

    [Fact]
    public async Task Invalid_token_is_rejected_with_a_generic_error()
    {
        await using var host = await StartAsync();
        using var client = host.NewClient();

        using var response = await SignInAsync(client, "garbage");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Google sign-in failed", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Other_google_subject_with_the_same_email_cannot_sign_into_a_linked_account()
    {
        await using var host = await StartAsync();
        using var first = host.NewClient();
        (await SignInAsync(first, Token("sub-1", "linked@example.test"))).Dispose();
        using var intruder = host.NewClient();

        using var response = await SignInAsync(intruder, Token("sub-2", "linked@example.test"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await intruder.GetAsync(Methods)).StatusCode);
    }

    [Fact]
    public async Task Linking_needs_step_up_and_a_google_account_belongs_to_one_account_only()
    {
        await using var host = await StartAsync();
        using var owner = host.NewClient();
        await TestSignIn.SignInAsync(owner, host.Factory.Services, "mine@example.test");
        using var other = host.NewClient();
        (await SignInAsync(other, Token("sub-taken", "other@example.test"))).Dispose();

        using var withoutCode = await TestSignIn.PostJsonAsync(owner, Methods + "/google",
            new { idToken = Token("sub-mine", "mine@gmail.test") });
        Assert.Equal(HttpStatusCode.Forbidden, withoutCode.StatusCode);

        using var linked = await TestSignIn.PostJsonAsync(owner, Methods + "/google",
            new { idToken = Token("sub-mine", "mine@gmail.test") }, await StepUpHeaderAsync(owner, host, "mine@example.test"));
        Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
        Assert.Equal("mine@gmail.test", (await linked.Content.ReadFromJsonAsync<MethodsPayload>())!.GoogleEmail);

        using var owner2 = host.NewClient();
        await TestSignIn.SignInAsync(owner2, host.Factory.Services, "second@example.test");
        using var conflict = await TestSignIn.PostJsonAsync(owner2, Methods + "/google",
            new { idToken = Token("sub-taken", "other@example.test") }, await StepUpHeaderAsync(owner2, host, "second@example.test"));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Unlinking_removes_google_but_email_code_sign_in_still_works()
    {
        await using var host = await StartAsync();
        using var client = host.NewClient();
        (await SignInAsync(client, Token("sub-1", "unlink@example.test"))).Dispose();

        using var removed = await TestSignIn.SendJsonAsync(client, HttpMethod.Delete, Methods + "/google", null);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        using var again = await TestSignIn.SendJsonAsync(client, HttpMethod.Delete, Methods + "/google", null);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        Assert.Null((await client.GetFromJsonAsync<MethodsPayload>(Methods))!.GoogleEmail);

        using var fresh = host.NewClient();
        await TestSignIn.SignInAsync(fresh, host.Factory.Services, "unlink@example.test");
        Assert.Equal("unlink@example.test", (await fresh.GetFromJsonAsync<MethodsPayload>(Methods))!.Email);
    }

    [Fact]
    public async Task Admin_unlink_needs_the_permission_and_a_step_up_code()
    {
        await using var host = await StartAsync();
        const string adminEmail = "admin@example.test";
        await EmailCodeSignInTests.CreateAccountAsync(host.Database, adminEmail, verified: true, active: true,
            roleCode: "UserAdmin", "System.ManageUsers");
        using var customer = host.NewClient();
        (await SignInAsync(customer, Token("sub-c", "customer@example.test"))).Dispose();
        var customerId = await host.AccountIdAsync("customer@example.test");
        var url = $"/api/v1/admin/identity/users/{customerId}/google";

        using var plain = host.NewClient();
        await TestSignIn.SignInAsync(plain, host.Factory.Services, "plain@example.test");
        using var denied = await TestSignIn.SendJsonAsync(plain, HttpMethod.Delete, url, null,
            await StepUpHeaderAsync(plain, host, "plain@example.test"));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using var admin = host.NewClient();
        await TestSignIn.SignInAsync(admin, host.Factory.Services, adminEmail);
        using var noCode = await TestSignIn.SendJsonAsync(admin, HttpMethod.Delete, url, null);
        Assert.Equal(HttpStatusCode.Forbidden, noCode.StatusCode);
        Assert.NotNull((await customer.GetFromJsonAsync<MethodsPayload>(Methods))!.GoogleEmail);

        using var removed = await TestSignIn.SendJsonAsync(admin, HttpMethod.Delete, url, null,
            await StepUpHeaderAsync(admin, host, adminEmail));
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Null((await customer.GetFromJsonAsync<MethodsPayload>(Methods))!.GoogleEmail);
    }

    [Fact]
    public async Task Google_sign_in_is_not_available_when_no_client_id_is_configured()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = new IdentityLoginApiFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });

        using var response = await SignInAsync(client, "any-token");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<GoogleHost> StartAsync()
    {
        var database = await fixture.CreateDatabaseAsync();
        var factory = new IdentityLoginApiFactory(database.ConnectionString, configureTestServices: services =>
        {
            services.RemoveAll<IGoogleIdTokenValidator>();
            services.AddSingleton<IGoogleIdTokenValidator, FakeGoogleValidator>();
        });
        return new GoogleHost(database, factory);
    }

    private static string Token(string subject, string email, bool verified = true) => $"{subject}|{email}|{verified}";

    private static Task<HttpResponseMessage> SignInAsync(HttpClient client, string idToken) =>
        TestSignIn.PostJsonAsync(client, Google, new { idToken, rememberDevice = false });

    private static async Task<Dictionary<string, string>> StepUpHeaderAsync(HttpClient client, GoogleHost host, string email)
    {
        (await TestSignIn.PostJsonAsync(client, "/api/v1/identity/step-up/request", new { })).Dispose();
        var code = await TestSignIn.LatestCodeAsync(host.Factory.Services, email);
        Assert.Equal(EmailCodePurpose.StepUp, code.Purpose);
        return new Dictionary<string, string> { [StepUpService.HeaderName] = code.Code };
    }

    private sealed class FakeGoogleValidator : IGoogleIdTokenValidator
    {
        public Task<GoogleIdentity> ValidateAsync(string? idToken, CancellationToken cancellationToken)
        {
            var parts = idToken?.Split('|');
            if (parts is not { Length: 3 })
                throw new IdentityException(401, "Google sign-in failed. Try again.");
            return Task.FromResult(new GoogleIdentity(parts[0], parts[1], bool.Parse(parts[2])));
        }
    }

    private sealed class GoogleHost(OwnerBootstrapDatabase database, IdentityLoginApiFactory factory) : IAsyncDisposable
    {
        public OwnerBootstrapDatabase Database => database;
        public IdentityLoginApiFactory Factory => factory;

        public HttpClient NewClient() => factory.CreatePreflightedClient(new() { HandleCookies = true });

        public async Task<Guid> AccountIdAsync(string email)
        {
            await using var db = database.CreateIdentityContext();
            var normalized = IdentityLoginService.NormalizeIdentifier(email);
            return (await db.UserAccounts.SingleAsync(account => account.NormalizedEmail == normalized)).Id;
        }

        public ValueTask DisposeAsync() => factory.DisposeAsync();
    }

    private sealed record SignInPayload(bool IsNewAccount);
    private sealed record MethodsPayload(string Email, bool EmailCode, string? GoogleEmail);
}
