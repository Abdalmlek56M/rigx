using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class IdentityAccountLifecycleTests(PostgreSqlIdentityFixture fixture)
    : IClassFixture<PostgreSqlIdentityFixture>
{
    private const string NewPassword = "a sufficiently long password";

    [Fact]
    public async Task Email_verification_sets_password_without_creating_a_session_and_login_then_works()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var account = await RegisterAndVerifyAsync(factory, client, NewPassword);

        await using var db = fixture.CreateContext();
        var persisted = await db.UserAccounts.SingleAsync(value => value.Id == account.AccountId);
        Assert.True(persisted.IsEmailVerified);
        Assert.NotNull(persisted.PasswordHash);
        Assert.Equal(1, persisted.AuthorizationVersion);
        Assert.False(await db.AuthSessions.AnyAsync(value =>
            value.UserAccountId == persisted.Id && value.RevokedAtUtc == null));

        using var login = await LoginAsync(client, account.Username, NewPassword, HttpStatusCode.OK);
        await using var sessionDb = fixture.CreateContext();
        Assert.True(await sessionDb.AuthSessions.AnyAsync(value =>
            value.UserAccountId == persisted.Id && value.RevokedAtUtc == null));
    }

    [Fact]
    public async Task Used_malformed_and_unknown_verification_tokens_return_the_same_public_error()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var account = await RegisterAndVerifyAsync(factory, client, NewPassword);

        using var reused = await VerifyAsync(client, account.VerificationToken, NewPassword);
        using var malformed = await VerifyAsync(client, "not-a-token", NewPassword);
        using var unknown = await VerifyAsync(
            client,
            Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)),
            NewPassword);

        Assert.Equal(HttpStatusCode.BadRequest, reused.StatusCode);
        Assert.Equal(reused.StatusCode, malformed.StatusCode);
        Assert.Equal(reused.StatusCode, unknown.StatusCode);
        Assert.Equal(await PublicProblemSignatureAsync(reused), await PublicProblemSignatureAsync(malformed));
        Assert.Equal(await PublicProblemSignatureAsync(reused), await PublicProblemSignatureAsync(unknown));
    }

    [Fact]
    public async Task Resend_rotates_the_token_only_after_the_account_cooldown()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.Zero));
        await using var factory = new IdentityLoginApiFactory(
            fixture.RuntimeConnectionString,
            timeProvider: clock);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var account = await RegisterAsync(factory, client);
        var provider = factory.Services.GetRequiredService<InMemoryTestEmailProvider>();
        await WaitUntilAsync(() => provider.Messages.Count(message =>
            message.Email == account.Email) == 1);

        using var immediateResend = await ResendAsync(client, account.Email);
        Assert.Equal(HttpStatusCode.Accepted, immediateResend.StatusCode);
        await Task.Delay(100);
        Assert.Single(provider.Messages, message => message.Email == account.Email);

        clock.Advance(TimeSpan.FromSeconds(61));
        using var resend = await ResendAsync(client, account.Email);
        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        await WaitUntilAsync(() => provider.Messages.Count(message =>
            message.Email == account.Email) == 2);

        using var oldToken = await VerifyAsync(client, account.VerificationToken, NewPassword);
        Assert.Equal(HttpStatusCode.BadRequest, oldToken.StatusCode);

        var latest = provider.Messages.Where(message => message.Email == account.Email).Last();
        var latestToken = ExtractFragmentToken(latest.VerificationUrl);
        using var newToken = await VerifyAsync(client, latestToken, NewPassword);
        Assert.Equal(HttpStatusCode.NoContent, newToken.StatusCode);
    }

    [Fact]
    public async Task Password_reset_request_enforces_account_cooldown_and_rotates_token()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));
        await using var factory = new IdentityLoginApiFactory(
            fixture.RuntimeConnectionString,
            timeProvider: clock);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var account = await RegisterAndVerifyAsync(factory, client, NewPassword);
        var provider = factory.Services.GetRequiredService<InMemoryTestEmailProvider>();

        await RequestPasswordResetAsync(client, account.Email);
        var first = await WaitForSingleAsync(() => provider.PasswordResetMessages
            .Where(message => message.Email == account.Email));
        await RequestPasswordResetAsync(client, account.Email);
        await Task.Delay(100);
        Assert.Single(provider.PasswordResetMessages, message => message.Email == account.Email);

        clock.Advance(TimeSpan.FromSeconds(61));
        await RequestPasswordResetAsync(client, account.Email);
        await WaitUntilAsync(() => provider.PasswordResetMessages.Count(message =>
            message.Email == account.Email) == 2);
        var second = provider.PasswordResetMessages.Where(message => message.Email == account.Email).Last();

        using var oldToken = await ResetAsync(
            client, ExtractFragmentToken(first.ResetUrl), "first replacement password");
        Assert.Equal(HttpStatusCode.BadRequest, oldToken.StatusCode);
        using var newToken = await ResetAsync(
            client, ExtractFragmentToken(second.ResetUrl), "second replacement password");
        Assert.Equal(HttpStatusCode.NoContent, newToken.StatusCode);

        for (var expectedCount = 3; expectedCount <= 5; expectedCount++)
        {
            clock.Advance(TimeSpan.FromSeconds(61));
            await RequestPasswordResetAsync(client, account.Email);
            var count = expectedCount;
            await WaitUntilAsync(() => provider.PasswordResetMessages.Count(message =>
                message.Email == account.Email) == count);
        }

        clock.Advance(TimeSpan.FromSeconds(61));
        await RequestPasswordResetAsync(client, account.Email);
        await Task.Delay(100);
        Assert.Equal(5, provider.PasswordResetMessages.Count(message =>
            message.Email == account.Email));
    }

    [Fact]
    public async Task Password_reset_is_atomic_and_revokes_every_existing_session()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var firstClient = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var account = await RegisterAndVerifyAsync(factory, firstClient, NewPassword);
        using var secondClient = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        using var firstLogin = await LoginAsync(firstClient, account.Username, NewPassword, HttpStatusCode.OK);
        using var secondLogin = await LoginAsync(secondClient, account.Username, NewPassword, HttpStatusCode.OK);

        await RequestPasswordResetAsync(firstClient, account.Email);
        var provider = factory.Services.GetRequiredService<InMemoryTestEmailProvider>();
        var resetMessage = await WaitForSingleAsync(() => provider.PasswordResetMessages
            .Where(message => message.Email == account.Email));
        var resetToken = ExtractFragmentToken(resetMessage.ResetUrl);
        const string resetPassword = "a different long password";
        using var reset = await ResetAsync(firstClient, resetToken, resetPassword);
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        await using var db = fixture.CreateContext();
        var persisted = await db.UserAccounts.SingleAsync(value => value.Id == account.AccountId);
        Assert.Equal(2, persisted.AuthorizationVersion);
        Assert.Equal(0, persisted.AccessFailedCount);
        Assert.Null(persisted.LockoutEndUtc);
        Assert.False(await db.AuthSessions.AnyAsync(value =>
            value.UserAccountId == account.AccountId && value.RevokedAtUtc == null));

        using var oldLogin = await LoginAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }),
            account.Username,
            NewPassword,
            HttpStatusCode.Unauthorized);
        using var newLogin = await LoginAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true }),
            account.Username,
            resetPassword,
            HttpStatusCode.OK);
        await WaitUntilAsync(() => provider.SecurityNotifications.Any(message =>
            message.Email == account.Email &&
            message.Kind == IdentitySecurityNotificationKind.PasswordResetCompleted));
    }

    [Fact]
    public async Task Password_change_revokes_all_sessions_and_old_cookie_is_rejected()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var firstClient = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var account = await RegisterAndVerifyAsync(factory, firstClient, NewPassword);
        using var secondClient = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        using var firstLogin = await LoginAsync(firstClient, account.Username, NewPassword, HttpStatusCode.OK);
        using var secondLogin = await LoginAsync(secondClient, account.Username, NewPassword, HttpStatusCode.OK);

        const string changedPassword = "another sufficiently long password";
        var csrf = await GetCsrfAsync(firstClient);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/password-changes")
        {
            Content = JsonContent.Create(new
            {
                currentPassword = NewPassword,
                newPassword = changedPassword
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await firstClient.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var db = fixture.CreateContext();
        var persisted = await db.UserAccounts.SingleAsync(value => value.Id == account.AccountId);
        Assert.Equal(2, persisted.AuthorizationVersion);
        Assert.False(await db.AuthSessions.AnyAsync(value =>
            value.UserAccountId == account.AccountId && value.RevokedAtUtc == null));

        var staleCsrf = await GetCsrfAsync(secondClient);
        using var staleLogout = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/identity/sessions");
        staleLogout.Headers.Add("X-CSRF-TOKEN", staleCsrf);
        using var staleResponse = await secondClient.SendAsync(staleLogout);
        Assert.Equal(HttpStatusCode.Unauthorized, staleResponse.StatusCode);

        var provider = factory.Services.GetRequiredService<InMemoryTestEmailProvider>();
        await WaitUntilAsync(() => provider.SecurityNotifications.Any(message =>
            message.Email == account.Email &&
            message.Kind == IdentitySecurityNotificationKind.PasswordChanged));
    }

    [Fact]
    public async Task Logout_current_and_logout_all_revoke_the_expected_scope()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var firstClient = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var account = await RegisterAndVerifyAsync(factory, firstClient, NewPassword);
        using var secondClient = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        using var firstLogin = await LoginAsync(firstClient, account.Username, NewPassword, HttpStatusCode.OK);
        using var secondLogin = await LoginAsync(secondClient, account.Username, NewPassword, HttpStatusCode.OK);
        await using (var sessionProof = fixture.CreateContext())
            Assert.Equal(2, await sessionProof.AuthSessions.CountAsync(value =>
                value.UserAccountId == account.AccountId && value.RevokedAtUtc == null));

        var currentCsrf = await GetCsrfAsync(firstClient);
        using var currentLogout = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/identity/sessions/current");
        currentLogout.Headers.Add("X-CSRF-TOKEN", currentCsrf);
        using var currentResponse = await firstClient.SendAsync(currentLogout);
        Assert.Equal(HttpStatusCode.NoContent, currentResponse.StatusCode);

        await using (var afterCurrent = fixture.CreateContext())
        {
            Assert.Equal(1, await afterCurrent.AuthSessions.CountAsync(value =>
                value.UserAccountId == account.AccountId && value.RevokedAtUtc == null));
        }

        var allCsrf = await GetCsrfAsync(secondClient);
        using var allLogout = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/identity/sessions");
        allLogout.Headers.Add("X-CSRF-TOKEN", allCsrf);
        using var allResponse = await secondClient.SendAsync(allLogout);
        Assert.Equal(HttpStatusCode.NoContent, allResponse.StatusCode);

        await using var afterAll = fixture.CreateContext();
        Assert.Equal(0, await afterAll.AuthSessions.CountAsync(value =>
            value.UserAccountId == account.AccountId && value.RevokedAtUtc == null));
    }

    [Fact]
    public async Task Current_user_endpoint_requires_authentication_and_returns_only_safe_profile_data()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        using var anonymous = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var account = await RegisterAndVerifyAsync(factory, client, NewPassword);
        var permissionRoleId = Guid.NewGuid();
        await using (var db = fixture.CreateContext())
        {
            db.Roles.Add(new Role
            {
                Id = permissionRoleId,
                Code = $"CurrentUser{Guid.NewGuid():N}",
                Name = "Current User Permissions Test"
            });
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = permissionRoleId,
                PermissionCode = "Catalog.View"
            });
            db.UserRoles.Add(new UserRole
            {
                UserAccountId = account.AccountId,
                RoleId = permissionRoleId
            });
            await db.SaveChangesAsync();
        }

        using var login = await LoginAsync(client, account.Username, NewPassword, HttpStatusCode.OK);
        using var current = await client.GetAsync("/api/v1/identity/me");
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        Assert.Equal("no-store", current.Headers.CacheControl?.ToString());

        using var payload = JsonDocument.Parse(await current.Content.ReadAsStringAsync());
        var root = payload.RootElement;
        Assert.Equal(account.Username, root.GetProperty("username").GetString());
        Assert.Equal(account.Email, root.GetProperty("email").GetString());
        Assert.Equal("Lifecycle Test Customer", root.GetProperty("fullName").GetString());
        Assert.True(root.GetProperty("isEmailVerified").GetBoolean());
        Assert.Contains(root.GetProperty("roles").EnumerateArray(),
            role => role.GetString() == "Customer");
        Assert.Contains(root.GetProperty("permissions").EnumerateArray(),
            permission => permission.GetString() == "Catalog.View");
        Assert.False(root.TryGetProperty("passwordHash", out _));
        Assert.False(root.TryGetProperty("personId", out _));
    }

    [Fact]
    public async Task Password_reset_request_does_not_wait_for_provider_and_unknown_account_has_same_response()
    {
        var emailProvider = new BlockingPasswordEmailProvider();
        await using var factory = new IdentityLoginApiFactory(
            fixture.RuntimeConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<IEmailProvider>();
                services.AddSingleton<IEmailProvider>(emailProvider);
            });
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var account = await RegisterAndVerifyAsync(factory, client, NewPassword, emailProvider);

        using var known = await RequestPasswordResetRawAsync(client, account.Email)
            .WaitAsync(TimeSpan.FromSeconds(10));
        await emailProvider.PasswordResetStarted.Task.WaitAsync(TimeSpan.FromSeconds(12));
        Assert.False(emailProvider.PasswordResetRelease.Task.IsCompleted);

        using var unknown = await RequestPasswordResetRawAsync(client, $"unknown-{Guid.NewGuid():N}@example.test");
        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        emailProvider.PasswordResetRelease.TrySetResult();
    }

    [Fact]
    public async Task Concurrent_password_reset_consumption_allows_exactly_one_writer()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var setupClient = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var account = await RegisterAndVerifyAsync(factory, setupClient, NewPassword);
        await RequestPasswordResetAsync(setupClient, account.Email);
        var provider = factory.Services.GetRequiredService<InMemoryTestEmailProvider>();
        var resetMessage = await WaitForSingleAsync(() => provider.PasswordResetMessages
            .Where(message => message.Email == account.Email));
        var resetToken = ExtractFragmentToken(resetMessage.ResetUrl);

        using var first = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var second = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var firstCsrf = await GetCsrfAsync(first);
        var secondCsrf = await GetCsrfAsync(second);
        const string concurrentPassword = "concurrent password value";

        var results = await Task.WhenAll(
            SendResetAttemptAsync(first, firstCsrf, resetToken, concurrentPassword),
            SendResetAttemptAsync(second, secondCsrf, resetToken, concurrentPassword));
        Assert.Equal(1, results.Count(status => status == HttpStatusCode.NoContent));
        Assert.Equal(1, results.Count(status => status == HttpStatusCode.BadRequest));

        await using var db = fixture.CreateContext();
        var token = await db.IdentityTokens.SingleAsync(value =>
            value.UserAccountId == account.AccountId && value.Purpose == "PasswordReset");
        Assert.NotNull(token.UsedAtUtc);
    }

    [Fact]
    public async Task Sensitive_audit_failure_rolls_back_reset_state()
    {
        await using var setupFactory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var setupClient = setupFactory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var account = await RegisterAndVerifyAsync(setupFactory, setupClient, NewPassword);
        await RequestPasswordResetAsync(setupClient, account.Email);
        var provider = setupFactory.Services.GetRequiredService<InMemoryTestEmailProvider>();
        var resetMessage = await WaitForSingleAsync(() => provider.PasswordResetMessages
            .Where(message => message.Email == account.Email));
        var resetToken = ExtractFragmentToken(resetMessage.ResetUrl);

        var failingWriter = new FailingSecurityAuditTransactionWriter();
        await using var failingFactory = new IdentityLoginApiFactory(
            fixture.RuntimeConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<ISecurityAuditTransactionWriter>();
                services.AddSingleton<ISecurityAuditTransactionWriter>(failingWriter);
            });
        using var failingClient = failingFactory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        using var response = await ResetAsync(failingClient, resetToken, "a replacement long password");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        await using var db = fixture.CreateContext();
        var token = await db.IdentityTokens.SingleAsync(value =>
            value.UserAccountId == account.AccountId && value.Purpose == "PasswordReset");
        Assert.Null(token.UsedAtUtc);
        var persisted = await db.UserAccounts.SingleAsync(value => value.Id == account.AccountId);
        Assert.Equal(1, persisted.AuthorizationVersion);
        Assert.True(_hashingMatches(persisted.PasswordHash, NewPassword));
    }

    [Fact]
    public async Task Sensitive_audit_failure_rolls_back_password_change()
    {
        await using var setupFactory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var setupClient = setupFactory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var account = await RegisterAndVerifyAsync(setupFactory, setupClient, NewPassword);

        var failingWriter = new FailingSecurityAuditTransactionWriter();
        await using var failingFactory = new IdentityLoginApiFactory(
            fixture.RuntimeConnectionString,
            configureTestServices: services =>
            {
                services.RemoveAll<ISecurityAuditTransactionWriter>();
                services.AddSingleton<ISecurityAuditTransactionWriter>(failingWriter);
            });
        using var client = failingFactory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        using var login = await LoginAsync(client, account.Username, NewPassword, HttpStatusCode.OK);
        var csrf = await GetCsrfAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/password-changes")
        {
            Content = JsonContent.Create(new
            {
                currentPassword = NewPassword,
                newPassword = "a failed password replacement"
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        await using var db = fixture.CreateContext();
        var persisted = await db.UserAccounts.SingleAsync(value => value.Id == account.AccountId);
        Assert.Equal(1, persisted.AuthorizationVersion);
        Assert.True(_hashingMatches(persisted.PasswordHash, NewPassword));
        Assert.True(await db.AuthSessions.AnyAsync(value =>
            value.UserAccountId == account.AccountId && value.RevokedAtUtc == null));
    }

    [Fact]
    public async Task M32d_ip_rate_limits_reject_requests_after_the_approved_threshold()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        for (var index = 0; index < 20; index++)
        {
            using var response = await VerifyAsync(client, "not-a-token", NewPassword);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var verificationLimited = await VerifyAsync(client, "not-a-token", NewPassword);
        Assert.Equal((HttpStatusCode)429, verificationLimited.StatusCode);
        Assert.Equal("600", verificationLimited.Headers.GetValues("Retry-After").Single());

        for (var index = 0; index < 10; index++)
        {
            using var response = await RequestPasswordResetRawAsync(
                client,
                $"unknown-{index}-{Guid.NewGuid():N}@example.test");
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        }
        using var resetRequestLimited = await RequestPasswordResetRawAsync(
            client,
            $"unknown-final-{Guid.NewGuid():N}@example.test");
        Assert.Equal((HttpStatusCode)429, resetRequestLimited.StatusCode);
        Assert.Equal("3600", resetRequestLimited.Headers.GetValues("Retry-After").Single());
    }

    [Fact]
    public async Task M32d_mutating_endpoints_require_csrf()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        using var verify = await client.PostAsJsonAsync(
            "/api/v1/identity/email-verifications",
            new { token = "not-a-token", newPassword = NewPassword });
        using var resend = await client.PostAsJsonAsync(
            "/api/v1/identity/email-verification-resends",
            new { email = "csrf@example.test" });
        using var resetRequest = await client.PostAsJsonAsync(
            "/api/v1/identity/password-reset-requests",
            new { email = "csrf@example.test" });
        using var reset = await client.PostAsJsonAsync(
            "/api/v1/identity/password-resets",
            new { token = "not-a-token", newPassword = NewPassword });
        using var change = await client.PostAsJsonAsync(
            "/api/v1/identity/password-changes",
            new { currentPassword = NewPassword, newPassword = NewPassword });
        using var logoutCurrent = await client.DeleteAsync("/api/v1/identity/sessions/current");
        using var logoutAll = await client.DeleteAsync("/api/v1/identity/sessions");

        Assert.All(new[]
        {
            verify,
            resend,
            resetRequest,
            reset,
            change,
            logoutCurrent,
            logoutAll
        }, response => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));
    }

    private static bool _hashingMatches(string? passwordHash, string password)
    {
        if (passwordHash is null || string.IsNullOrWhiteSpace(password))
            return false;
        return PasswordHashVerification.Succeeded == new PasswordHashingService(
            new Microsoft.AspNetCore.Identity.PasswordHasher<UserAccount>()).VerifyPassword(
            new UserAccount
            {
                Username = "verification-test",
                NormalizedUsername = "VERIFICATION-TEST",
                Email = "verification-test@example.test",
                NormalizedEmail = "VERIFICATION-TEST@EXAMPLE.TEST"
            },
            passwordHash,
            password);
    }

    private static async Task<TestAccount> RegisterAndVerifyAsync(
        IdentityLoginApiFactory factory,
        HttpClient client,
        string password,
        IEmailProvider? providerOverride = null)
    {
        var account = await RegisterAsync(factory, client, providerOverride);
        using var verification = await VerifyAsync(client, account.VerificationToken, password);
        Assert.Equal(HttpStatusCode.NoContent, verification.StatusCode);
        return account;
    }

    private static async Task<TestAccount> RegisterAsync(
        IdentityLoginApiFactory factory,
        HttpClient client,
        IEmailProvider? providerOverride = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var username = $"customer-{suffix}";
        var email = $"{username}@example.test";
        var csrf = await GetCsrfAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/registrations")
        {
            Content = JsonContent.Create(new
            {
                fullName = "Lifecycle Test Customer",
                phoneNumber = "+201000000000",
                username,
                email
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        if (providerOverride is BlockingPasswordEmailProvider blocking)
        {
            var verificationMessage = await blocking.VerificationMessage.Task.WaitAsync(TimeSpan.FromSeconds(12));
            return new TestAccount(
                await ReadAccountIdAsync(factory, email),
                username,
                email,
                ExtractFragmentToken(verificationMessage.VerificationUrl));
        }

        var provider = factory.Services.GetRequiredService<InMemoryTestEmailProvider>();
        var message = await WaitForSingleAsync(() => provider.Messages.Where(item => item.Email == email));
        return new TestAccount(
            await ReadAccountIdAsync(factory, email),
            username,
            email,
            ExtractFragmentToken(message.VerificationUrl));
    }

    private static async Task<Guid> ReadAccountIdAsync(IdentityLoginApiFactory factory, string email)
    {
        await using var db = await factory.Services
            .GetRequiredService<IDbContextFactory<IdentityDbContext>>()
            .CreateDbContextAsync();
        var normalizedEmail = IdentityLoginService.NormalizeIdentifier(email);
        return await db.UserAccounts
            .Where(account => account.NormalizedEmail == normalizedEmail)
            .Select(account => account.Id)
            .SingleAsync();
    }

    private static async Task<HttpResponseMessage> VerifyAsync(
        HttpClient client,
        string token,
        string password)
    {
        var csrf = await GetCsrfAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/email-verifications")
        {
            Content = JsonContent.Create(new { token, newPassword = password })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> ResendAsync(HttpClient client, string email)
    {
        var csrf = await GetCsrfAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/email-verification-resends")
        {
            Content = JsonContent.Create(new { email })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request);
    }

    private static async Task RequestPasswordResetAsync(HttpClient client, string email)
    {
        using var response = await RequestPasswordResetRawAsync(client, email);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> RequestPasswordResetRawAsync(
        HttpClient client,
        string email)
    {
        var csrf = await GetCsrfAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/password-reset-requests")
        {
            Content = JsonContent.Create(new { email })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> ResetAsync(
        HttpClient client,
        string token,
        string password)
    {
        var csrf = await GetCsrfAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/password-resets")
        {
            Content = JsonContent.Create(new { token, newPassword = password })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string username,
        string password,
        HttpStatusCode expected)
    {
        var csrf = await GetCsrfAsync(client);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/sessions")
        {
            Content = JsonContent.Create(new { identifier = username, password })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        var response = await client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
        return response;
    }

    private static async Task<HttpStatusCode> SendResetAttemptAsync(
        HttpClient client,
        string csrf,
        string token,
        string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/password-resets")
        {
            Content = JsonContent.Create(new { token, newPassword = password })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static async Task<string> GetCsrfAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/identity/antiforgery");
        response.EnsureSuccessStatusCode();
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("requestToken").GetString()
            ?? throw new InvalidOperationException("No antiforgery token returned.");
    }

    private static string ExtractFragmentToken(Uri uri)
    {
        const string prefix = "#token=";
        if (!uri.Fragment.StartsWith(prefix, StringComparison.Ordinal))
            throw new InvalidOperationException("The email link had no token fragment.");
        return Uri.UnescapeDataString(uri.Fragment[prefix.Length..]);
    }

    private static async Task<string> PublicProblemSignatureAsync(HttpResponseMessage response)
    {
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = payload.RootElement;
        return string.Join('|',
            root.GetProperty("status").GetInt32(),
            root.GetProperty("title").GetString(),
            root.GetProperty("detail").GetString());
    }

    private static async Task<T> WaitForSingleAsync<T>(Func<IEnumerable<T>> source)
    {
        await WaitUntilAsync(() => source().Take(2).Count() == 1);
        return source().Single();
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException("Condition was not met before timeout.");
            await Task.Delay(25);
        }
    }

    private sealed record TestAccount(Guid AccountId, string Username, string Email, string VerificationToken);
}

internal sealed class BlockingPasswordEmailProvider : IEmailProvider
{
    public TaskCompletionSource<EmailVerificationMessage> VerificationMessage { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<PasswordResetMessage> PasswordResetStarted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource PasswordResetRelease { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task SendEmailVerificationAsync(
        EmailVerificationMessage message,
        CancellationToken cancellationToken = default)
    {
        VerificationMessage.TrySetResult(message);
        return Task.CompletedTask;
    }

    public async Task SendPasswordResetAsync(
        PasswordResetMessage message,
        CancellationToken cancellationToken = default)
    {
        PasswordResetStarted.TrySetResult(message);
        await PasswordResetRelease.Task.WaitAsync(cancellationToken);
    }

    public Task SendSecurityNotificationAsync(
        IdentitySecurityNotificationMessage message,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task SendEmailCodeAsync(
        EmailCodeMessage message,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class FailingSecurityAuditTransactionWriter : ISecurityAuditTransactionWriter
{
    public Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Injected transaction audit failure.");

    public Task WriteAsync(
        SecurityAuditEntry entry,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Injected transaction audit failure.");
}
