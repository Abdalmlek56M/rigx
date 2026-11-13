using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using HardwareStore.Common.Infrastructure.Persistence;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class PostgreSqlIdentityLoginTests(PostgreSqlIdentityFixture fixture)
    : IClassFixture<PostgreSqlIdentityFixture>
{
    private const string Password = "Correct Test Password 2026!";

    [Fact]
    public async Task Login_uses_normalized_identifier_creates_database_cookie_session_and_safe_audit()
    {
        var accountId = await CreateAccountAsync(Password, "CaseSensitive.User@example.test", emailVerified: true,
            assignCustomerRole: true, addPermission: true);
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = await GetCsrfAsync(client);

        using var request = CreateLoginRequest("casesensitive.user@EXAMPLE.TEST", Password, csrf);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookies = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Contains(cookies, value => value.StartsWith("HardwareStore.Dev.Session=", StringComparison.Ordinal));
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);

        await using var identityDb = fixture.CreateContext();
        var session = await identityDb.AuthSessions.SingleAsync(row => row.UserAccountId == accountId);
        Assert.Equal(32, session.SessionKeyHash.Length);
        Assert.NotEmpty(session.ProtectedTicket);
        var protector = factory.Services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("HardwareStore.AuthenticationTicket.v1");
        var ticket = Microsoft.AspNetCore.Authentication.TicketSerializer.Default.Deserialize(
            protector.Unprotect(session.ProtectedTicket));
        Assert.NotNull(ticket);
        Assert.Contains(ticket.Principal.FindAll(ClaimTypes.Role), claim => claim.Value == "Customer");
        Assert.Contains(ticket.Principal.FindAll(PermissionAuthorizationHandler.ClaimType),
            claim => claim.Value == PermissionCatalog.All.OrderBy(code => code, StringComparer.Ordinal).First());

        await using var auditDb = fixture.CreateAuditContext();
        var audit = await auditDb.SecurityAuditEvents.SingleAsync(item =>
            item.EventType == "Identity.Login.Succeeded" && item.SubjectUserAccountId == accountId);
        Assert.Equal("Succeeded", audit.Outcome);
        Assert.DoesNotContain(Password, audit.SafeMetadata, StringComparison.Ordinal);
        Assert.DoesNotContain("CaseSensitive.User@example.test", audit.SafeMetadata, StringComparison.Ordinal);
        Assert.DoesNotContain(Convert.ToBase64String(session.SessionKeyHash), audit.SafeMetadata, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Successful_login_audit_failure_revokes_request_session_and_removes_cookie()
    {
        var email = $"audit-failure-{Guid.NewGuid():N}@example.test";
        var accountId = await CreateAccountAsync(Password, email, emailVerified: true);
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString, failSuccessAudit: true);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = await GetCsrfAsync(client);
        using var request = CreateLoginRequest(email, Password, csrf);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var setCookies = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.DoesNotContain(setCookies, value =>
        {
            var prefix = "HardwareStore.Dev.Session=";
            if (!value.StartsWith(prefix, StringComparison.Ordinal))
                return false;
            return value[prefix.Length..].Split(';', 2)[0].Length > 0;
        });
        await using var identityDb = fixture.CreateContext();
        Assert.Empty(await identityDb.AuthSessions.Where(session =>
            session.UserAccountId == accountId && session.RevokedAtUtc == null).ToListAsync());

        await using var auditDb = fixture.CreateAuditContext();
        Assert.False(await auditDb.SecurityAuditEvents.AnyAsync(item =>
            item.EventType == "Identity.Login.Succeeded" && item.SubjectUserAccountId == accountId));
    }

    [Fact]
    public async Task Unverified_account_with_legacy_password_hash_cannot_login()
    {
        var email = $"unverified-{Guid.NewGuid():N}@example.test";
        await CreateAccountAsync(Password, email,
            emailVerified: false, assignCustomerRole: true, addPermission: true);
        var service = CreateLoginService(TimeProvider.System);

        var result = await service.AuthenticateAsync(email, Password);

        Assert.Equal(IdentityLoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.Principal);
    }

    [Fact]
    public async Task Pending_account_without_password_hash_cannot_login_or_throw()
    {
        var email = $"pending-{Guid.NewGuid():N}@example.test";
        var accountId = await CreateAccountAsync(Password, email, emailVerified: false, passwordHashAbsent: true);
        var service = CreateLoginService(TimeProvider.System);

        var result = await service.AuthenticateAsync(email, Password);

        Assert.Equal(IdentityLoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.Principal);
        await using var db = fixture.CreateContext();
        Assert.Null((await db.UserAccounts.SingleAsync(account => account.Id == accountId)).PasswordHash);
    }

    [Fact]
    public async Task Tenth_concurrent_failure_locks_for_five_minutes_without_losing_updates()
    {
        var email = $"lock-{Guid.NewGuid():N}@example.test";
        var accountId = await CreateAccountAsync(Password, email, emailVerified: true);
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.Zero));
        var service = CreateLoginService(clock);

        var results = await Task.WhenAll(Enumerable.Range(0, 12)
            .Select(_ => service.AuthenticateAsync(email, "wrong password")));

        Assert.Equal(9, results.Count(result => result.Status == IdentityLoginStatus.InvalidCredentials));
        Assert.Equal(3, results.Count(result => result.Status == IdentityLoginStatus.AccountLocked));
        await using var db = fixture.CreateContext();
        var account = await db.UserAccounts.SingleAsync(user => user.Id == accountId);
        Assert.Equal(10, account.AccessFailedCount);
        Assert.Equal(clock.GetUtcNow(), account.AccessFailedWindowStartedUtc);
        Assert.Equal(clock.GetUtcNow().AddMinutes(5), account.LockoutEndUtc);

        clock.Advance(TimeSpan.FromMinutes(5));
        var successful = await service.AuthenticateAsync(email, Password);
        Assert.Equal(IdentityLoginStatus.Succeeded, successful.Status);
        await db.Entry(account).ReloadAsync();
        Assert.Equal(0, account.AccessFailedCount);
        Assert.Null(account.AccessFailedWindowStartedUtc);
        Assert.Null(account.LockoutEndUtc);
    }

    [Fact]
    public async Task Expired_failure_window_starts_a_new_counter_window()
    {
        var email = $"window-{Guid.NewGuid():N}@example.test";
        var accountId = await CreateAccountAsync(Password, email, emailVerified: true);
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 11, 5, 11, 0, 0, TimeSpan.Zero));
        var service = CreateLoginService(clock);

        _ = await service.AuthenticateAsync(email, "wrong password");
        clock.Advance(TimeSpan.FromMinutes(16));
        var result = await service.AuthenticateAsync(email, "wrong password");

        Assert.Equal(IdentityLoginStatus.InvalidCredentials, result.Status);
        await using var db = fixture.CreateContext();
        var account = await db.UserAccounts.SingleAsync(user => user.Id == accountId);
        Assert.Equal(1, account.AccessFailedCount);
        Assert.Equal(clock.GetUtcNow(), account.AccessFailedWindowStartedUtc);
        Assert.Null(account.LockoutEndUtc);
    }

    [Fact]
    public async Task Successful_login_upgrades_a_legacy_password_hash()
    {
        var email = $"rehash-{Guid.NewGuid():N}@example.test";
        var accountId = await CreateAccountAsync(Password, email, emailVerified: true);
        var oldHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<UserAccount>(
            Microsoft.Extensions.Options.Options.Create(new Microsoft.AspNetCore.Identity.PasswordHasherOptions
            {
                CompatibilityMode = Microsoft.AspNetCore.Identity.PasswordHasherCompatibilityMode.IdentityV3,
                IterationCount = 100_000
            }));
        await using (var db = fixture.CreateContext())
        {
            var account = await db.UserAccounts.SingleAsync(user => user.Id == accountId);
            account.PasswordHash = oldHasher.HashPassword(account, Password);
            await db.SaveChangesAsync();
        }

        var result = await CreateLoginService(TimeProvider.System)
            .AuthenticateAsync(email, Password);

        Assert.Equal(IdentityLoginStatus.Succeeded, result.Status);
        await using var verificationDb = fixture.CreateContext();
        var updatedAccount = await verificationDb.UserAccounts.SingleAsync(user => user.Id == accountId);
        var hashing = new ServiceCollection().AddIdentityPasswordHashing().BuildServiceProvider()
            .GetRequiredService<IPasswordHashingService>();
        Assert.Equal(PasswordHashVerification.Succeeded,
            hashing.VerifyPassword(updatedAccount, updatedAccount.PasswordHash, Password));
    }

    [Fact]
    public async Task Locked_account_receives_the_same_public_failure_response()
    {
        var email = $"locked-api-{Guid.NewGuid():N}@example.test";
        var accountId = await CreateAccountAsync(Password, email, emailVerified: true);
        var directService = CreateLoginService(TimeProvider.System);
        for (var attempt = 0; attempt < 10; attempt++)
            _ = await directService.AuthenticateAsync(email, "wrong password");

        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = await GetCsrfAsync(client);
        using var lockedRequest = CreateLoginRequest(email, Password, csrf);
        using var lockedResponse = await client.SendAsync(lockedRequest);
        using var ordinaryRequest = CreateLoginRequest("unknown-lockout@example.test", "wrong password", csrf);
        using var ordinaryResponse = await client.SendAsync(ordinaryRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, lockedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, ordinaryResponse.StatusCode);
        Assert.Equal(await ordinaryResponse.Content.ReadAsStringAsync(), await lockedResponse.Content.ReadAsStringAsync());
        await using var db = fixture.CreateContext();
        var account = await db.UserAccounts.SingleAsync(user => user.Id == accountId);
        Assert.NotNull(account.LockoutEndUtc);
    }

    [Fact]
    public async Task Unknown_and_wrong_credentials_have_identical_public_responses_and_no_secrets_in_audit()
    {
        var knownEmail = $"known-{Guid.NewGuid():N}@example.test";
        var accountId = await CreateAccountAsync(Password, knownEmail, emailVerified: true);
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = await GetCsrfAsync(client);

        using var wrongRequest = CreateLoginRequest(knownEmail, "wrong password", csrf);
        var wrongResponse = await client.SendAsync(wrongRequest);
        using var unknownRequest = CreateLoginRequest("nobody@example.test", "wrong password", csrf);
        var unknownResponse = await client.SendAsync(unknownRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownResponse.StatusCode);
        Assert.Equal(await wrongResponse.Content.ReadAsStringAsync(), await unknownResponse.Content.ReadAsStringAsync());

        await using var auditDb = fixture.CreateAuditContext();
        var failedAudits = await auditDb.SecurityAuditEvents
            .Where(item => item.EventType == "Identity.Login.Failed" &&
                           (item.SubjectUserAccountId == accountId || item.SubjectUserAccountId == null))
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(2)
            .ToListAsync();
        Assert.Equal(2, failedAudits.Count);
        Assert.All(failedAudits, audit =>
        {
            Assert.DoesNotContain("wrong password", audit.SafeMetadata, StringComparison.Ordinal);
            Assert.DoesNotContain(knownEmail, audit.SafeMetadata, StringComparison.Ordinal);
            Assert.DoesNotContain("nobody@example.test", audit.SafeMetadata, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Login_rate_limit_rejects_sixth_attempt_from_same_ip()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var csrf = await GetCsrfAsync(client);
        var statuses = new List<HttpStatusCode>();

        for (var attempt = 0; attempt < 6; attempt++)
        {
            using var request = CreateLoginRequest("unknown@example.test", "wrong password", csrf);
            using var response = await client.SendAsync(request);
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(Enumerable.Repeat(HttpStatusCode.Unauthorized, 5), statuses.Take(5));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[5]);
        await using var auditDb = fixture.CreateAuditContext();
        Assert.True(await auditDb.SecurityAuditEvents.AnyAsync(item =>
            item.EventType == "Identity.Login.RateLimited" && item.Outcome == "Rejected"));
    }

    private IdentityLoginService CreateLoginService(TimeProvider clock)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(fixture.GetConnectionString())
            .Options;
        var services = new ServiceCollection().AddIdentityPasswordHashing().BuildServiceProvider();
        return new IdentityLoginService(new TestIdentityContextFactory(options),
            services.GetRequiredService<IPasswordHashingService>(), clock);
    }

    private async Task<Guid> CreateAccountAsync(
        string password,
        string email,
        bool emailVerified,
        bool assignCustomerRole = false,
        bool addPermission = false,
        bool passwordHashAbsent = false)
    {
        var accountId = Guid.NewGuid();
        var username = $"account-{accountId:N}";
        await using var db = fixture.CreateContext();
        var person = await new PersonCodeGenerator(db, TimeProvider.System,
                TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"))
            .CreatePersonAsync(new Person { FullName = $"Login {accountId:N}" });
        var services = new ServiceCollection().AddIdentityPasswordHashing().BuildServiceProvider();
        var account = new UserAccount
        {
            Id = accountId,
            PersonId = person.Id,
            Username = username,
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
            Email = email,
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier(email),
            PasswordHash = passwordHashAbsent ? null : services.GetRequiredService<IPasswordHashingService>().HashPassword(
                new UserAccount
                {
                    Id = accountId,
                    PersonId = person.Id,
                    Username = username,
                    NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
                    Email = email,
                    NormalizedEmail = IdentityLoginService.NormalizeIdentifier(email),
                    PasswordHash = string.Empty
                }, password),
            IsEmailVerified = emailVerified,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.UserAccounts.Add(account);
        foreach (var identifier in new[] { account.NormalizedUsername, account.NormalizedEmail }.Distinct(StringComparer.Ordinal))
        {
            db.UserAccountLoginIdentifiers.Add(new UserAccountLoginIdentifier
            {
                NormalizedIdentifier = identifier,
                UserAccountId = accountId
            });
        }
        if (assignCustomerRole)
            db.UserRoles.Add(new UserRole { UserAccountId = accountId, RoleId = PostgreSqlIdentityPersistenceTests.CustomerRoleId });
        if (addPermission)
        {
            var roleId = Guid.NewGuid();
            var permission = PermissionCatalog.All.OrderBy(code => code, StringComparer.Ordinal).First();
            db.Roles.Add(new Role { Id = roleId, Code = $"Login-{accountId:N}", Name = "Login test role" });
            db.UserRoles.Add(new UserRole { UserAccountId = accountId, RoleId = roleId });
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionCode = permission });
            var state = await db.AuthorizationStates.SingleAsync(item => item.Id == 1);
            state.RbacRevision++;
        }
        await db.SaveChangesAsync();
        await services.DisposeAsync();
        return accountId;
    }

    private static async Task<string> GetCsrfAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/identity/antiforgery");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AntiforgeryResponse>();
        return payload?.RequestToken ?? throw new InvalidOperationException("Antiforgery endpoint returned no request token.");
    }

    private static HttpRequestMessage CreateLoginRequest(string identifier, string password, string csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/sessions")
        {
            Content = JsonContent.Create(new IdentityLoginEndpoints.LoginRequest(identifier, password))
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return request;
    }

    private sealed record AntiforgeryResponse(string RequestToken);
}

internal sealed class IdentityLoginApiFactory(
    string connectionString,
    bool failSuccessAudit = false,
    TimeProvider? timeProvider = null,
    Action<IServiceCollection>? configureTestServices = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["DataProtection:KeyRingPath"] = Path.Combine(Directory.GetCurrentDirectory(), ".backups", "m3_2b-test-keyring")
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IDbContextFactory<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.AddDbContextFactory<IdentityDbContext>(options => options.UseNpgsql(connectionString));

            services.RemoveAll<DbContextOptions<SecurityAuditDbContext>>();
            services.RemoveAll<IDbContextFactory<SecurityAuditDbContext>>();
            services.RemoveAll<SecurityAuditDbContext>();
            services.AddDbContextFactory<SecurityAuditDbContext>(options => options.UseNpgsql(connectionString));

            services.RemoveAll<DbContextOptions<HardwareStoreDbContext>>();
            services.RemoveAll<HardwareStoreDbContext>();
            services.AddDbContext<HardwareStoreDbContext>(options => options.UseNpgsql(connectionString));

            services.RemoveAll<IDataProtectionProvider>();
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            if (failSuccessAudit)
            {
                services.RemoveAll<ISecurityAuditWriter>();
                services.AddScoped<ISecurityAuditWriter, FailSuccessfulLoginAuditWriter>();
            }

            if (timeProvider is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(timeProvider);
            }

            configureTestServices?.Invoke(services);
        });
    }

    public HttpClient CreatePreflightedClient(WebApplicationFactoryClientOptions options)
    {
        var expected = new NpgsqlConnectionStringBuilder(connectionString);
        using var identityDb = Services.GetRequiredService<IDbContextFactory<IdentityDbContext>>().CreateDbContext();
        AssertTarget(identityDb.Database.GetDbConnection(), expected);

        using var auditDb = Services.GetRequiredService<IDbContextFactory<SecurityAuditDbContext>>().CreateDbContext();
        AssertTarget(auditDb.Database.GetDbConnection(), expected);

        using var scope = Services.CreateScope();
        using var applicationDb = scope.ServiceProvider.GetRequiredService<HardwareStoreDbContext>();
        AssertTarget(applicationDb.Database.GetDbConnection(), expected);

        return CreateClient(options);
    }

    private static void AssertTarget(System.Data.Common.DbConnection connection, NpgsqlConnectionStringBuilder expected)
    {
        var actual = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
        Assert.Equal(expected.Host, actual.Host);
        Assert.Equal(expected.Port, actual.Port);
        Assert.Equal(expected.Database, actual.Database);
    }
}

internal sealed class FailSuccessfulLoginAuditWriter(
    Microsoft.EntityFrameworkCore.IDbContextFactory<HardwareStore.Modules.Auditing.SecurityAuditDbContext> contextFactory)
    : ISecurityAuditWriter
{
    public Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.EventType == "Identity.Login.Succeeded")
            throw new InvalidOperationException("Injected successful-login audit failure.");
        return new HardwareStore.Modules.Auditing.SecurityAuditWriter(contextFactory).WriteAsync(entry, cancellationToken);
    }
}
