using System.Security.Claims;
using System.Security.Cryptography;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class AuthenticationSessionModelTests
{
    [Fact]
    public void Session_and_authorization_state_have_required_constraints()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=none;Password=none")
            .Options;
        using var context = new IdentityDbContext(options);

        var session = context.Model.FindEntityType(typeof(AuthSession))!;
        var designTimeSession = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(AuthSession))!;
        Assert.Equal("auth_sessions", session.GetTableName());
        Assert.Contains(designTimeSession.GetCheckConstraints(), constraint => constraint.Name == "ck_auth_sessions_key_hash_length");
        Assert.Contains(session.GetForeignKeys(), key => key.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Equal("authorization_state", context.Model.FindEntityType(typeof(AuthorizationState))!.GetTableName());
        Assert.Equal("authorization_version", context.Model.FindEntityType(typeof(UserAccount))!
            .FindProperty(nameof(UserAccount.AuthorizationVersion))!.GetColumnName());
    }
}

[Trait("Category", "RequiresDocker")]
public sealed class PostgreSqlAuthenticationSessionTests(PostgreSqlIdentityFixture fixture)
    : IClassFixture<PostgreSqlIdentityFixture>
{
    [Fact]
    public async Task Ticket_store_persists_random_key_hash_and_protected_ticket()
    {
        var accountId = await CreateAccountAsync();
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero));
        var store = CreateStore(clock);
        var ticket = CreateTicket(accountId);

        var key = await store.StoreAsync(ticket);
        Assert.Equal(43, key.Length); // base64url representation of 32 random bytes

        await using var db = fixture.CreateContext();
        var row = await db.AuthSessions.SingleAsync(session => session.SessionKeyHash == SHA256.HashData(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(key)));
        Assert.Equal(SHA256.HashData(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(key)), row.SessionKeyHash);
        Assert.NotEqual(Microsoft.AspNetCore.Authentication.TicketSerializer.Default.Serialize(ticket), row.ProtectedTicket);

        var restored = await store.RetrieveAsync(key);
        Assert.NotNull(restored);
        Assert.Equal(accountId.ToString(), restored!.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    [Fact]
    public async Task Expired_or_logically_revoked_session_is_not_retrieved()
    {
        var accountId = await CreateAccountAsync();
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero));
        var store = CreateStore(clock);
        var key = await store.StoreAsync(CreateTicket(accountId));

        Assert.NotNull(await store.RetrieveAsync(key));
        await store.RemoveAsync(key);
        Assert.Null(await store.RetrieveAsync(key));

        var secondKey = await store.StoreAsync(CreateTicket(accountId));
        clock.Advance(TimeSpan.FromDays(15));
        Assert.Null(await store.RetrieveAsync(secondKey));
    }

    [Fact]
    public async Task Concurrent_renew_and_remove_never_recreates_a_revoked_session()
    {
        var accountId = await CreateAccountAsync();
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero));
        var store = CreateStore(clock);
        var ticket = CreateTicket(accountId);
        var key = await store.StoreAsync(ticket);
        var retrieved = await store.RetrieveAsync(key);
        Assert.NotNull(retrieved);

        await Task.WhenAll(store.RenewAsync(key, retrieved!), store.RemoveAsync(key));

        Assert.Null(await store.RetrieveAsync(key));
        await using var db = fixture.CreateContext();
        var row = await db.AuthSessions.SingleAsync(session => session.SessionKeyHash == SHA256.HashData(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(key)));
        Assert.NotNull(row.RevokedAtUtc);
    }

    [Fact]
    public async Task Repeated_customer_renewal_never_passes_the_absolute_expiry()
    {
        var accountId = await CreateAccountAsync();
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero));
        var store = CreateStore(clock);
        var ticket = CreateTicket(accountId);
        var key = await store.StoreAsync(ticket);

        clock.Advance(TimeSpan.FromDays(13));
        await store.RenewAsync(key, ticket);
        Assert.NotNull(await store.RetrieveAsync(key));

        clock.Advance(TimeSpan.FromDays(13));
        await store.RenewAsync(key, ticket);
        Assert.NotNull(await store.RetrieveAsync(key));

        clock.Advance(TimeSpan.FromDays(5));
        Assert.Null(await store.RetrieveAsync(key));
    }

    [Fact]
    public async Task Admin_session_uses_the_short_idle_window()
    {
        var accountId = await CreateAccountAsync();
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero));
        var store = CreateStore(clock);
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, accountId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, "Owner"));
        identity.AddClaim(new Claim(IdentityLoginService.AuthorizationVersionClaim, "0"));
        identity.AddClaim(new Claim(IdentityLoginService.RbacRevisionClaim, "0"));
        var key = await store.StoreAsync(new AuthenticationTicket(new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true }, "Cookies"));

        clock.Advance(TimeSpan.FromMinutes(31));
        Assert.Null(await store.RetrieveAsync(key));
    }

    private DatabaseTicketStore CreateStore(TimeProvider clock)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(fixture.GetConnectionString())
            .Options;
        return new DatabaseTicketStore(new TestIdentityContextFactory(options),
            new EphemeralDataProtectionProvider(), clock);
    }

    private async Task<Guid> CreateAccountAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        await using var db = fixture.CreateContext();
        var person = await new PersonCodeGenerator(db, TimeProvider.System,
                TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"))
            .CreatePersonAsync(new Person { Id = personId, FullName = $"Session {Guid.NewGuid():N}" });
        var username = $"session-{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@example.test";
        db.UserAccounts.Add(new UserAccount
        {
            Id = accountId,
            PersonId = person.Id,
            Username = username,
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
            Email = email,
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier(email),
            PasswordHash = "test-only-placeholder",
            CreatedAtUtc = now
        });
        await db.SaveChangesAsync();
        return accountId;
    }

    private static AuthenticationTicket CreateTicket(Guid accountId)
    {
        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, accountId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, "Customer"));
        identity.AddClaim(new Claim(IdentityLoginService.AuthorizationVersionClaim, "0"));
        identity.AddClaim(new Claim(IdentityLoginService.RbacRevisionClaim, "0"));
        return new AuthenticationTicket(new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true }, "Cookies");
    }
}

[Trait("Category", "RequiresDocker")]
public sealed class PostgreSqlIdentitySessionManagementTests(PostgreSqlIdentityFixture fixture)
    : IClassFixture<PostgreSqlIdentityFixture>
{
    [Fact]
    public async Task Lists_marks_current_and_revokes_only_owned_sessions()
    {
        var accountId = await CreateAccountAsync();
        var otherAccountId = await CreateAccountAsync();
        var current = await CreateSessionAsync(accountId, "Mozilla/5.0 (Windows NT 10.0) Chrome/130.0");
        var other = await CreateSessionAsync(accountId, "Mozilla/5.0 (iPhone) Safari/604.1");
        var foreign = await CreateSessionAsync(otherAccountId, "Mozilla/5.0 (X11) Firefox/128.0");
        var service = CreateService();

        var sessions = await service.ListAsync(accountId, current.RawKey);
        Assert.Equal(2, sessions.Count);
        Assert.Single(sessions, session => session.IsCurrent);
        Assert.Contains(sessions, session => session.Id == other.PublicId && session.Browser == "Safari" && session.Device == "Mobile");

        var foreignAttempt = await Assert.ThrowsAsync<IdentityException>(() =>
            service.RevokeSessionAsync(accountId, foreign.PublicId, "test"));
        Assert.Equal(404, foreignAttempt.StatusCode);

        await service.RevokeSessionAsync(accountId, other.PublicId, "test");
        var remaining = await service.ListAsync(accountId, current.RawKey);
        Assert.Single(remaining);
        Assert.True(remaining[0].IsCurrent);
    }

    [Fact]
    public async Task Revokes_all_other_sessions_and_keeps_the_current_one()
    {
        var accountId = await CreateAccountAsync();
        var current = await CreateSessionAsync(accountId, "Mozilla/5.0 (Windows NT 10.0) Chrome/130.0");
        await CreateSessionAsync(accountId, "Mozilla/5.0 (Macintosh) Safari/605.1");
        await CreateSessionAsync(accountId, "Mozilla/5.0 (Linux) Firefox/128.0");
        var service = CreateService();

        await service.RevokeOtherSessionsAsync(accountId, current.RawKey, "test");
        var remaining = await service.ListAsync(accountId, current.RawKey);
        Assert.Single(remaining);
        Assert.True(remaining[0].IsCurrent);
    }

    private IdentitySessionService CreateService()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(fixture.GetConnectionString())
            .Options;
        var auditOptions = new DbContextOptionsBuilder<HardwareStore.Modules.Auditing.SecurityAuditDbContext>()
            .UseNpgsql(fixture.GetConnectionString())
            .Options;
        return new IdentitySessionService(
            new TestIdentityContextFactory(options),
            new HardwareStore.Modules.Auditing.SecurityAuditWriter(
                new PooledDbContextFactory<HardwareStore.Modules.Auditing.SecurityAuditDbContext>(auditOptions)),
            TimeProvider.System);
    }

    private async Task<Guid> CreateAccountAsync()
    {
        var accountId = Guid.NewGuid();
        var username = $"session-{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@example.test";
        await using var db = fixture.CreateContext();
        var person = await new PersonCodeGenerator(db, TimeProvider.System,
                TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"))
            .CreatePersonAsync(new Person { FullName = $"Session {Guid.NewGuid():N}" });
        db.UserAccounts.Add(new UserAccount
        {
            Id = accountId,
            PersonId = person.Id,
            Username = username,
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
            Email = email,
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier(email),
            PasswordHash = "test-only-placeholder",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return accountId;
    }

    private async Task<(Guid PublicId, string RawKey)> CreateSessionAsync(Guid accountId, string userAgent)
    {
        var rawKey = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = DateTimeOffset.UtcNow;
        var session = new AuthSession
        {
            SessionKeyHash = SHA256.HashData(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(rawKey)),
            PublicId = Guid.NewGuid(),
            UserAccountId = accountId,
            ProtectedTicket = [1, 2, 3],
            UserAgent = userAgent,
            CreatedAtUtc = now,
            LastActivityAtUtc = now,
            IdleExpiresAtUtc = now.AddDays(1),
            AbsoluteExpiresAtUtc = now.AddDays(2),
            IdleTimeoutSeconds = 3600,
            AuthorizationVersion = 0,
            RbacRevision = 0,
            RowVersion = 1
        };
        await using var db = fixture.CreateContext();
        db.AuthSessions.Add(session);
        await db.SaveChangesAsync();
        return (session.PublicId, rawKey);
    }
}

[Trait("Category", "RequiresDocker")]
public sealed class PostgreSqlSecurityAuditTests(PostgreSqlIdentityFixture fixture)
    : IClassFixture<PostgreSqlIdentityFixture>
{
    [Fact]
    public async Task Runtime_role_can_append_but_cannot_update_or_delete_security_audit_events()
    {
        var accountId = await CreateAccountAsync();
        var runtimeOptions = new DbContextOptionsBuilder<HardwareStore.Modules.Auditing.SecurityAuditDbContext>()
            .UseNpgsql(fixture.RuntimeConnectionString).Options;
        var writer = new HardwareStore.Modules.Auditing.SecurityAuditWriter(
            new Microsoft.EntityFrameworkCore.Infrastructure.PooledDbContextFactory<
                HardwareStore.Modules.Auditing.SecurityAuditDbContext>(runtimeOptions));
        await writer.WriteAsync(new HardwareStore.Modules.Auditing.SecurityAuditEntry(
            "Bootstrap.Owner.Created", "Succeeded", "System", SubjectUserAccountId: accountId));

        await using (var connection = new Npgsql.NpgsqlConnection(fixture.RuntimeConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE security_audit_events SET outcome = 'Failed'";
            var exception = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal("42501", exception.SqlState);
        }
        await using (var connection = new Npgsql.NpgsqlConnection(fixture.RuntimeConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM security_audit_events";
            var exception = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal("42501", exception.SqlState);
        }
    }

    private async Task<Guid> CreateAccountAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.NewGuid();
        await using var db = fixture.CreateContext();
        var person = await new PersonCodeGenerator(db, TimeProvider.System,
                TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo"))
            .CreatePersonAsync(new Person { FullName = $"Audit {Guid.NewGuid():N}" });
        var username = $"audit-{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@example.test";
        db.UserAccounts.Add(new UserAccount
        {
            Id = accountId,
            PersonId = person.Id,
            Username = username,
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
            Email = email,
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier(email),
            PasswordHash = "test-only-placeholder",
            CreatedAtUtc = now
        });
        await db.SaveChangesAsync();
        return accountId;
    }
}

internal sealed class TestIdentityContextFactory(DbContextOptions<IdentityDbContext> options)
    : IDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext() => new(options);
    public Task<IdentityDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(CreateDbContext());
}

internal sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan duration) => _now = _now.Add(duration);
}
