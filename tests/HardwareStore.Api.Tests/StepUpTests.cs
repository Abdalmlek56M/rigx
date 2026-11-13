using System.Net;
using System.Net.Http.Json;
using HardwareStore.Modules.Identity.Authentication;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class StepUpTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Destructive_admin_action_needs_a_fresh_single_use_email_code()
    {
        var database = await fixture.CreateDatabaseAsync();
        const string email = "admin@example.test";
        await EmailCodeSignInTests.CreateAccountAsync(database, email, verified: true, active: true,
            roleCode: "UserAdmin", "System.ManageUsers", "Catalog.ManageProducts");
        await using var factory = new IdentityLoginApiFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        await TestSignIn.SignInAsync(client, factory.Services, email);
        object Role(string code) => new { code, name = code, permissionCodes = new[] { "Catalog.ManageProducts" } };

        using var withoutCode = await TestSignIn.PostJsonAsync(client, "/api/v1/admin/identity/roles", Role("Editors"));
        Assert.Equal(HttpStatusCode.Forbidden, withoutCode.StatusCode);
        var problem = await withoutCode.Content.ReadFromJsonAsync<StepUpProblem>();
        Assert.True(problem!.Errors.ContainsKey(StepUpService.CodeField));

        using var request = await TestSignIn.PostJsonAsync(client, "/api/v1/identity/step-up/request", new { });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var code = await TestSignIn.LatestCodeAsync(factory.Services, email);
        Assert.Equal(EmailCodePurpose.StepUp, code.Purpose);
        var header = new Dictionary<string, string> { [StepUpService.HeaderName] = code.Code };

        using var confirmed = await TestSignIn.PostJsonAsync(client, "/api/v1/admin/identity/roles", Role("Editors"), header);
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        using var reused = await TestSignIn.PostJsonAsync(client, "/api/v1/admin/identity/roles", Role("Writers"), header);
        Assert.Equal(HttpStatusCode.Forbidden, reused.StatusCode);
    }

    private sealed record StepUpProblem(int Status, Dictionary<string, string[]> Errors);
}
