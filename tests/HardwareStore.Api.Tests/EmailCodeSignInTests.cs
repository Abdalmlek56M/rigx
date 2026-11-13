using System.Net;
using System.Net.Http.Json;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class EmailCodeSignInTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task First_verified_code_creates_a_customer_and_signs_in()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = new IdentityLoginApiFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });

        using var request = await TestSignIn.PostJsonAsync(client, "/api/v1/identity/email-code/request",
            new { email = "New.Buyer@Example.test" });
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
        var ticket = await request.Content.ReadFromJsonAsync<TicketPayload>();
        Assert.InRange(ticket!.ExpiresInSeconds, 290, 300);
        Assert.InRange(ticket.ResendAfterSeconds, 50, 60);
        var code = await TestSignIn.LatestCodeAsync(factory.Services, "New.Buyer@Example.test");
        Assert.Equal(6, code.Code.Length);

        using var verify = await TestSignIn.PostJsonAsync(client, "/api/v1/identity/email-code/verify",
            new { email = "new.buyer@example.test", code = code.Code });
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        Assert.True((await verify.Content.ReadFromJsonAsync<VerifyPayload>())!.IsNewAccount);

        var me = await client.GetFromJsonAsync<MePayload>("/api/v1/identity/me");
        Assert.Equal("new.buyer@example.test", me!.Email);
        Assert.Equal(["Customer"], me.Roles);
        Assert.True(me.IsEmailVerified);
    }

    [Fact]
    public async Task Wrong_codes_count_down_then_burn_the_code_and_resend_is_throttled()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = new IdentityLoginApiFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        const string email = "tries@example.test";
        (await TestSignIn.PostJsonAsync(client, "/api/v1/identity/email-code/request", new { email })).Dispose();
        var first = await TestSignIn.LatestCodeAsync(factory.Services, email);
        var wrong = first.Code == "000000" ? "111111" : "000000";

        for (var expectedLeft = 4; expectedLeft >= 0; expectedLeft--)
        {
            using var attempt = await TestSignIn.PostJsonAsync(client, "/api/v1/identity/email-code/verify",
                new { email, code = wrong });
            Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
            Assert.Equal(expectedLeft, (await attempt.Content.ReadFromJsonAsync<FailurePayload>())!.AttemptsLeft);
        }
        using var burned = await TestSignIn.PostJsonAsync(client, "/api/v1/identity/email-code/verify",
            new { email, code = first.Code });
        Assert.Equal(HttpStatusCode.Unauthorized, burned.StatusCode);

        using var resend = await TestSignIn.PostJsonAsync(client, "/api/v1/identity/email-code/request", new { email });
        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.True((await resend.Content.ReadFromJsonAsync<TicketPayload>())!.ResendAfterSeconds > 0);
        await TestSignIn.DeliverOutboxAsync(factory.Services);
        Assert.Single(factory.Services.GetRequiredService<InMemoryTestEmailProvider>().EmailCodes,
            item => item.Email == email);
    }

    [Fact]
    public async Task Invited_staff_account_is_verified_by_its_first_code_and_keeps_its_roles()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateAccountAsync(database, "staff@example.test", verified: false, active: true, roleCode: "Sales");
        await using var factory = new IdentityLoginApiFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });

        await TestSignIn.SignInAsync(client, factory.Services, "staff@example.test");

        var me = await client.GetFromJsonAsync<MePayload>("/api/v1/identity/me");
        Assert.Equal(["Sales"], me!.Roles);
        Assert.True(me.IsEmailVerified);
    }

    [Fact]
    public async Task Deactivated_account_gets_the_same_response_but_no_email()
    {
        var database = await fixture.CreateDatabaseAsync();
        await CreateAccountAsync(database, "gone@example.test", verified: true, active: false, roleCode: "Gone");
        await using var factory = new IdentityLoginApiFactory(database.ConnectionString);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });

        using var response = await TestSignIn.PostJsonAsync(client, "/api/v1/identity/email-code/request",
            new { email = "gone@example.test" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(300, (await response.Content.ReadFromJsonAsync<TicketPayload>())!.ExpiresInSeconds);
        await TestSignIn.DeliverOutboxAsync(factory.Services);
        Assert.Empty(factory.Services.GetRequiredService<InMemoryTestEmailProvider>().EmailCodes);
    }

    [Fact]
    public async Task Remember_this_device_keeps_a_customer_signed_in_for_30_days_otherwise_12_hours()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = new IdentityLoginApiFactory(database.ConnectionString);
        using var onceClient = factory.CreatePreflightedClient(new() { HandleCookies = true });
        using var rememberClient = factory.CreatePreflightedClient(new() { HandleCookies = true });

        await TestSignIn.SignInAsync(onceClient, factory.Services, "once@example.test");
        await TestSignIn.SignInAsync(rememberClient, factory.Services, "remember@example.test", rememberDevice: true);

        Assert.InRange(await SessionLifetimeAsync(database, "once@example.test"), TimeSpan.FromHours(11.9), TimeSpan.FromHours(12));
        Assert.InRange(await SessionLifetimeAsync(database, "remember@example.test"), TimeSpan.FromDays(29.9), TimeSpan.FromDays(30));
    }

    [Fact]
    public async Task Success_audit_failure_creates_no_session()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var factory = new IdentityLoginApiFactory(database.ConnectionString, failSuccessAudit: true);
        using var client = factory.CreatePreflightedClient(new() { HandleCookies = true });
        (await TestSignIn.PostJsonAsync(client, "/api/v1/identity/email-code/request", new { email = "audit@example.test" })).Dispose();
        var code = await TestSignIn.LatestCodeAsync(factory.Services, "audit@example.test");

        using var verify = await TestSignIn.PostJsonAsync(client, "/api/v1/identity/email-code/verify",
            new { email = "audit@example.test", code = code.Code });

        Assert.Equal(HttpStatusCode.InternalServerError, verify.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/identity/me")).StatusCode);
        await using var db = database.CreateIdentityContext();
        Assert.False(await db.AuthSessions.AnyAsync());
    }

    private static async Task<TimeSpan> SessionLifetimeAsync(OwnerBootstrapDatabase database, string email)
    {
        await using var db = database.CreateIdentityContext();
        var normalized = IdentityLoginService.NormalizeIdentifier(email);
        var session = await db.AuthSessions.SingleAsync(item =>
            db.UserAccounts.Any(account => account.Id == item.UserAccountId && account.NormalizedEmail == normalized));
        return session.AbsoluteExpiresAtUtc - session.CreatedAtUtc;
    }

    internal static async Task<Guid> CreateAccountAsync(
        OwnerBootstrapDatabase database, string email, bool verified, bool active, string roleCode,
        params string[] permissions)
    {
        await using var db = database.CreateIdentityContext();
        var role = await db.Roles.SingleOrDefaultAsync(item => item.Code == roleCode);
        if (role is null)
        {
            role = new Role { Id = Guid.CreateVersion7(), Code = roleCode, Name = roleCode };
            db.Roles.Add(role);
            foreach (var permission in permissions)
                db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = permission });
            if (permissions.Length > 0)
                (await db.AuthorizationStates.SingleAsync()).RbacRevision++;
        }
        var person = await new PersonCodeGenerator(db, TimeProvider.System,
                TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"))
            .CreatePersonAsync(new Person { FullName = email.Split('@')[0], Email = email });
        var account = new UserAccount
        {
            Id = Guid.CreateVersion7(), PersonId = person.Id, Username = email,
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier(email),
            Email = email, NormalizedEmail = IdentityLoginService.NormalizeIdentifier(email),
            IsEmailVerified = verified, IsActive = active, CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.UserAccounts.Add(account);
        db.UserRoles.Add(new UserRole { UserAccountId = account.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
        return account.Id;
    }

    private sealed record TicketPayload(int ExpiresInSeconds, int ResendAfterSeconds);
    private sealed record VerifyPayload(bool IsNewAccount);
    private sealed record FailurePayload(string Error, int AttemptsLeft);
    private sealed record MePayload(string Email, IReadOnlyList<string> Roles, bool IsEmailVerified);
}
