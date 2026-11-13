using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class IdentityRegistrationUnitTests
{
    [Fact]
    public void Verification_token_contains_32_random_bytes_and_persists_only_sha256()
    {
        var now = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        var generated = IdentityTokenFactory.Create(Guid.CreateVersion7(), now,
            CustomerRegistrationService.EmailVerificationPurpose, TimeSpan.FromHours(24));

        var tokenBytes = WebEncoders.Base64UrlDecode(generated.RawToken);
        Assert.Equal(32, tokenBytes.Length);
        Assert.Equal(32, generated.PersistenceRecord.TokenHash.Length);
        Assert.Equal(SHA256.HashData(tokenBytes), generated.PersistenceRecord.TokenHash);
        Assert.Equal(now, generated.PersistenceRecord.CreatedAtUtc);
        Assert.Equal(now.AddHours(24), generated.PersistenceRecord.ExpiresAtUtc);
        Assert.DoesNotContain(generated.RawToken, Convert.ToBase64String(generated.PersistenceRecord.TokenHash), StringComparison.Ordinal);
    }

    [Fact]
    public async Task In_memory_email_provider_captures_message_without_external_delivery()
    {
        var provider = new InMemoryTestEmailProvider();
        var message = new EmailVerificationMessage(
            "customer@example.test", "Test Customer", new Uri("https://shop.example.test/verify#token=sample"));

        await provider.SendEmailVerificationAsync(message);

        Assert.Equal(new[] { message }, provider.Messages);
    }
}

[Trait("Category", "RequiresDocker")]
public sealed class PostgreSqlIdentityRegistrationTests(PostgreSqlIdentityFixture fixture)
    : IClassFixture<PostgreSqlIdentityFixture>
{
    [Fact]
    public async Task Registration_creates_pending_customer_and_hash_only_verification_token()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var response = await RegisterAsync(client, "register@example.test", "customer@example.test",
            includeUntrustedSecurityFields: true);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());

        await using var db = fixture.CreateContext();
        var account = await db.UserAccounts.Include(value => value.Person).Include(value => value.UserRoles)
            .ThenInclude(value => value.Role).SingleAsync(value =>
            value.NormalizedEmail == "CUSTOMER@EXAMPLE.TEST");
        Assert.False(account.IsEmailVerified);
        Assert.Null(account.PasswordHash);
        Assert.True(account.Person.IsCustomer);
        Assert.Equal(account.Email, account.Person.Email);
        Assert.Single(account.UserRoles);
        Assert.Equal("Customer", account.UserRoles.Single().Role.Code);
        Assert.Empty(await db.RolePermissions.Where(permission =>
            permission.RoleId == account.UserRoles.Single().RoleId).ToListAsync());

        var identifiers = await db.UserAccountLoginIdentifiers.Where(item => item.UserAccountId == account.Id)
            .Select(item => item.NormalizedIdentifier).Order().ToListAsync();
        Assert.Equal(new[] { "CUSTOMER@EXAMPLE.TEST", "REGISTER@EXAMPLE.TEST" }, identifiers);
        var token = await db.IdentityTokens.SingleAsync(item => item.UserAccountId == account.Id);
        Assert.Equal("EmailVerification", token.Purpose);
        Assert.Equal(32, token.TokenHash.Length);
        Assert.Equal(TimeSpan.FromHours(24), token.ExpiresAtUtc - token.CreatedAtUtc);

        var outbox = await db.EmailOutbox.Where(message => message.UserAccountId == account.Id).ToListAsync();
        var queued = Assert.Single(outbox);
        Assert.Equal("EmailVerification", queued.Kind);
        Assert.Equal("Pending", queued.Status);
        Assert.DoesNotContain("token=", body, StringComparison.OrdinalIgnoreCase);

        await using var audit = fixture.CreateAuditContext();
        var auditEvent = await audit.SecurityAuditEvents.SingleAsync(item =>
            item.EventType == "Identity.Registration.Created" && item.SubjectUserAccountId == account.Id);
        Assert.DoesNotContain(account.Email, auditEvent.SafeMetadata, StringComparison.Ordinal);
        Assert.DoesNotContain("token=", auditEvent.SafeMetadata, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ClientChosenPassword", auditEvent.SafeMetadata, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cross_field_collision_returns_the_same_public_response_and_creates_no_second_account()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var first = await RegisterAsync(client, "reserved@example.test", "first@example.test");
        var firstBody = await first.Content.ReadAsStringAsync();
        var collision = await RegisterAsync(client, "other-name", "reserved@example.test");
        var collisionBody = await collision.Content.ReadAsStringAsync();

        Assert.Equal(first.StatusCode, collision.StatusCode);
        Assert.Equal(firstBody, collisionBody);
        Assert.Equal(HttpStatusCode.Accepted, collision.StatusCode);
        await using var db = fixture.CreateContext();
        Assert.Equal(1, await db.UserAccounts.CountAsync(account =>
            account.NormalizedEmail == "FIRST@EXAMPLE.TEST" || account.NormalizedUsername == "RESERVED@EXAMPLE.TEST"));
        var firstAccountId = await db.UserAccounts.Where(account => account.NormalizedEmail == "FIRST@EXAMPLE.TEST")
            .Select(account => account.Id).SingleAsync();
        Assert.Equal(1, await db.EmailOutbox.CountAsync(message => message.UserAccountId == firstAccountId));
    }

    [Fact]
    public async Task Equal_username_and_email_for_the_same_account_create_one_login_identifier()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var response = await RegisterAsync(client, "same@example.test", "same@example.test");
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        await using var db = fixture.CreateContext();
        var accountId = await db.UserAccounts.Where(account => account.NormalizedEmail == "SAME@EXAMPLE.TEST")
            .Select(account => account.Id).SingleAsync();
        Assert.Equal("SAME@EXAMPLE.TEST", await db.UserAccountLoginIdentifiers
            .Where(identifier => identifier.UserAccountId == accountId)
            .Select(identifier => identifier.NormalizedIdentifier).SingleAsync());
    }

    [Fact]
    public async Task Concurrent_duplicate_registrations_create_one_account_and_rollback_loser_person()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var beforePeople = await CountPeopleAsync();
        var csrf = await GetCsrfAsync(client);
        using var firstRequest = CreateRegistrationRequest(csrf, "parallel@example.test", "parallel@example.test");
        using var secondRequest = CreateRegistrationRequest(csrf, "parallel@example.test", "parallel@example.test");

        var responses = await Task.WhenAll(client.SendAsync(firstRequest), client.SendAsync(secondRequest));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Accepted, response.StatusCode));
        Assert.Equal(await responses[0].Content.ReadAsStringAsync(), await responses[1].Content.ReadAsStringAsync());
        await using var db = fixture.CreateContext();
        var accountId = await db.UserAccounts.Where(account => account.NormalizedEmail == "PARALLEL@EXAMPLE.TEST")
            .Select(account => account.Id).SingleAsync();
        Assert.Equal(1, await db.UserAccounts.CountAsync(account => account.Id == accountId));
        Assert.Equal(beforePeople + 1, await db.Persons.CountAsync());
        Assert.Equal(1, await db.EmailOutbox.CountAsync(message => message.UserAccountId == accountId));
    }

    [Fact]
    public async Task Email_delivery_failure_keeps_pending_account_and_token_for_later_resend()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(fixture.RuntimeConnectionString)
            .Options;
        var email = $"delivery-failure-{Guid.NewGuid():N}@example.test";
        CustomerRegistrationResult result;
        await using (var db = new IdentityDbContext(options))
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:EmailVerificationBaseUrl"] = "http://localhost:3000/verify-email"
            }).Build();
            var service = new CustomerRegistrationService(db,
                new PersonCodeGenerator(db, TimeProvider.System, TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo")),
                new UserAccountLoginIdentifierSynchronizer(db), new ThrowingEmailDispatcher(), TimeProvider.System,
                config, NullLogger<CustomerRegistrationService>.Instance);
            result = await service.RegisterAsync(new CustomerRegistrationRequest(
                "Delivery Failure", null, $"delivery-{Guid.NewGuid():N}", email));
        }

        Assert.Equal(CustomerRegistrationStatus.Created, result.Status);
        Assert.False(result.EmailDispatchQueued);
        await using var persisted = new IdentityDbContext(options);
        var account = await persisted.UserAccounts.SingleAsync(value => value.NormalizedEmail == email.ToUpperInvariant());
        Assert.False(account.IsEmailVerified);
        Assert.Null(account.PasswordHash);
        Assert.Single(await persisted.IdentityTokens.Where(token => token.UserAccountId == account.Id).ToListAsync());
    }

    [Fact]
    public async Task Registration_rate_limit_allows_ten_requests_per_ip_per_hour()
    {
        await using var factory = new IdentityLoginApiFactory(fixture.RuntimeConnectionString);
        using var client = factory.CreatePreflightedClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var responses = new List<HttpResponseMessage>();
        for (var index = 0; index < 11; index++)
        {
            using var request = await CreateCsrfRequestAsync(client);
            var response = await client.SendAsync(request);
            responses.Add(response);
        }

        Assert.All(responses.Take(10), response => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));
        Assert.Equal((HttpStatusCode)429, responses[^1].StatusCode);
        Assert.Equal("3600", responses[^1].Headers.GetValues("Retry-After").Single());
    }

    private static async Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string username,
        string email,
        bool includeUntrustedSecurityFields = false)
    {
        var csrf = await GetCsrfAsync(client);
        using var request = CreateRegistrationRequest(await GetCsrfAsync(client), username, email,
            includeUntrustedSecurityFields);
        return await client.SendAsync(request);
    }

    private static HttpRequestMessage CreateRegistrationRequest(
        string csrf,
        string username,
        string email,
        bool includeUntrustedSecurityFields = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/registrations")
        {
            Content = JsonContent.Create(includeUntrustedSecurityFields
                ? (object)new
                {
                    fullName = "Test Customer",
                    phoneNumber = "+201000000000",
                    username,
                    email,
                    password = "ClientChosenPassword",
                    role = "Owner",
                    permissions = PermissionCatalog.All
                }
                : new
                {
                    fullName = "Test Customer",
                    phoneNumber = "+201000000000",
                    username,
                    email
                })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return request;
    }

    private static async Task<string> GetCsrfAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/identity/antiforgery");
        response.EnsureSuccessStatusCode();
        using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("requestToken").GetString()!;
    }

    private static async Task<HttpRequestMessage> CreateCsrfRequestAsync(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/registrations")
        {
            Content = JsonContent.Create(new { fullName = "", username = "", email = "" })
        };
        request.Headers.Add("X-CSRF-TOKEN", await GetCsrfAsync(client));
        return request;
    }

    private async Task<int> CountPeopleAsync()
    {
        await using var db = fixture.CreateContext();
        return await db.Persons.CountAsync();
    }

    private sealed class BlockingEmailProvider : IEmailProvider
    {
        public TaskCompletionSource SendStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task SendEmailVerificationAsync(
            EmailVerificationMessage message,
            CancellationToken cancellationToken = default)
        {
            SendStarted.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }

        public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SendSecurityNotificationAsync(
            IdentitySecurityNotificationMessage message,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SendEmailCodeAsync(
            EmailCodeMessage message,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ThrowingEmailDispatcher : IIdentityEmailDispatcher
    {
        public ValueTask EnqueueEmailOtpAsync(
            Guid userAccountId,
            EmailCodeMessage message,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated provider outage.");

        public ValueTask EnqueueVerificationAsync(
            Guid userAccountId,
            EmailVerificationMessage message,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated provider outage.");

        public ValueTask EnqueuePasswordResetAsync(
            Guid userAccountId,
            PasswordResetMessage message,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated provider outage.");

        public ValueTask EnqueueSecurityNotificationAsync(
            Guid userAccountId,
            IdentitySecurityNotificationMessage message,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated provider outage.");
    }
}
