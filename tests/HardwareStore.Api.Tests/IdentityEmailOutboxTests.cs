using System.Text;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class IdentityEmailOutboxPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Enqueue_stores_only_encrypted_payload_and_sends_once()
    {
        var database = await fixture.CreateDatabaseAsync();
        var now = new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(now);
        var provider = new CapturingEmailProvider();
        var writer = CreateWriter(database, clock);
        Guid? accountId = null;
        const string tokenUrl = "http://localhost/verify-email#token=SUPER-SECRET-TOKEN";

        await writer.EnqueueAsync(
            IdentityEmailOutboxWriter.VerificationKind,
            accountId,
            new OutboxVerificationPayload("customer@example.test", "Test Customer", tokenUrl));

        await using (var db = database.CreateIdentityContext())
        {
            var row = await db.EmailOutbox.SingleAsync();
            Assert.Equal(IdentityEmailOutboxStatus.Pending, row.Status);
            Assert.NotEmpty(row.ProtectedPayload);
            var cipherText = Encoding.UTF8.GetString(row.ProtectedPayload);
            Assert.DoesNotContain("SUPER-SECRET-TOKEN", cipherText, StringComparison.Ordinal);
            Assert.DoesNotContain("verify-email", cipherText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("customer@example.test", cipherText, StringComparison.OrdinalIgnoreCase);
        }

        var processor = CreateProcessor(database, clock, provider, writer);
        Assert.Equal(1, await processor.ProcessDueAsync(CancellationToken.None));

        var delivered = Assert.Single(provider.Verifications);
        Assert.Equal(tokenUrl, delivered.VerificationUrl.ToString());
        await using (var db = database.CreateIdentityContext())
        {
            var row = await db.EmailOutbox.SingleAsync();
            Assert.Equal(IdentityEmailOutboxStatus.Sent, row.Status);
            Assert.Empty(row.ProtectedPayload);
            Assert.Null(row.ProcessingAtUtc);
            Assert.NotNull(row.ProcessedAtUtc);
            Assert.Null(row.LastErrorCode);
        }

        // A sent row is never processed twice.
        Assert.Equal(0, await processor.ProcessDueAsync(CancellationToken.None));
        Assert.Single(provider.Verifications);

        await using var audit = database.CreateAuditContext();
        Assert.True(await audit.SecurityAuditEvents.AnyAsync(entry =>
            entry.EventType == "Identity.EmailDelivery.Sent" && entry.Outcome == "Succeeded"));
    }

    [Fact]
    public async Task Provider_failure_retries_with_backoff_then_succeeds()
    {
        var database = await fixture.CreateDatabaseAsync();
        var now = new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(now);
        var provider = new CapturingEmailProvider { Fail = true };
        var writer = CreateWriter(database, clock);
        await writer.EnqueueAsync(
            IdentityEmailOutboxWriter.PasswordResetKind,
            null,
            new OutboxPasswordResetPayload("customer@example.test", "Test Customer", "http://localhost/reset-password#token=ABC"));

        var processor = CreateProcessor(database, clock, provider, writer);
        Assert.Equal(1, await processor.ProcessDueAsync(CancellationToken.None));

        await using (var db = database.CreateIdentityContext())
        {
            var row = await db.EmailOutbox.SingleAsync();
            Assert.Equal(IdentityEmailOutboxStatus.Failed, row.Status);
            Assert.Equal(1, row.Attempts);
            Assert.Equal(now.AddMinutes(2), row.NextAttemptAtUtc);
            Assert.Equal(nameof(InvalidOperationException), row.LastErrorCode);
            Assert.NotEmpty(row.ProtectedPayload);
        }

        // Not due yet.
        Assert.Equal(0, await processor.ProcessDueAsync(CancellationToken.None));

        clock.Advance(TimeSpan.FromMinutes(3));
        provider.Fail = false;
        Assert.Equal(1, await processor.ProcessDueAsync(CancellationToken.None));

        var delivered = Assert.Single(provider.PasswordResets);
        Assert.Contains("reset-password", delivered.ResetUrl.ToString(), StringComparison.Ordinal);
        await using (var db = database.CreateIdentityContext())
        {
            var row = await db.EmailOutbox.SingleAsync();
            Assert.Equal(IdentityEmailOutboxStatus.Sent, row.Status);
            Assert.Equal(1, row.Attempts);
            Assert.Empty(row.ProtectedPayload);
        }

        await using var audit = database.CreateAuditContext();
        Assert.True(await audit.SecurityAuditEvents.AnyAsync(entry =>
            entry.EventType == "Identity.EmailDelivery.Failed" && entry.Outcome == "Failed"));
    }

    [Fact]
    public async Task Repeated_failures_move_message_to_dead_letter()
    {
        var database = await fixture.CreateDatabaseAsync();
        var now = new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(now);
        var provider = new CapturingEmailProvider { Fail = true };
        var writer = CreateWriter(database, clock);
        await writer.EnqueueAsync(
            IdentityEmailOutboxWriter.VerificationKind,
            null,
            new OutboxVerificationPayload("customer@example.test", "Test Customer", "http://localhost/verify-email#token=ABC"));

        var processor = CreateProcessor(database, clock, provider, writer);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            if (attempt > 1)
                clock.Advance(TimeSpan.FromHours(2));
            Assert.Equal(1, await processor.ProcessDueAsync(CancellationToken.None));
        }

        await using (var db = database.CreateIdentityContext())
        {
            var row = await db.EmailOutbox.SingleAsync();
            Assert.Equal(IdentityEmailOutboxStatus.DeadLetter, row.Status);
            Assert.Equal(5, row.Attempts);
        }

        clock.Advance(TimeSpan.FromDays(1));
        Assert.Equal(0, await processor.ProcessDueAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Message_survives_restart_with_persisted_key_ring()
    {
        var database = await fixture.CreateDatabaseAsync();
        var keyDirectory = Path.Combine(Path.GetTempPath(), $"rigx-outbox-keys-{Guid.NewGuid():N}");
        Directory.CreateDirectory(keyDirectory);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var clock = new MutableTimeProvider(now);
            var firstProtection = DataProtectionProvider.Create(
                new DirectoryInfo(keyDirectory),
                builder => builder.SetApplicationName("HardwareStore"));
            var writer = new IdentityEmailOutboxWriter(
                new OutboxContextFactory(database.ConnectionString), firstProtection, clock);
            await writer.EnqueueAsync(
                IdentityEmailOutboxWriter.VerificationKind,
                null,
                new OutboxVerificationPayload("customer@example.test", "Test Customer", "http://localhost/verify-email#token=RESTART"));

            // Simulate a new application process with a fresh provider that shares the key ring.
            var secondProtection = DataProtectionProvider.Create(
                new DirectoryInfo(keyDirectory),
                builder => builder.SetApplicationName("HardwareStore"));
            var restartedWriter = new IdentityEmailOutboxWriter(
                new OutboxContextFactory(database.ConnectionString), secondProtection, clock);
            var provider = new CapturingEmailProvider();
            var processor = CreateProcessor(database, clock, provider, restartedWriter);

            Assert.Equal(1, await processor.ProcessDueAsync(CancellationToken.None));
            var delivered = Assert.Single(provider.Verifications);
            Assert.Contains("RESTART", delivered.VerificationUrl.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(keyDirectory, recursive: true);
        }
    }

    private static IdentityEmailOutboxWriter CreateWriter(
        OwnerBootstrapDatabase database,
        TimeProvider clock) =>
        new(new OutboxContextFactory(database.ConnectionString),
            new EphemeralDataProtectionProvider(),
            clock);

    private static IdentityEmailOutboxProcessor CreateProcessor(
        OwnerBootstrapDatabase database,
        TimeProvider clock,
        IEmailProvider provider,
        IdentityEmailOutboxWriter writer)
    {
        var factory = new OutboxContextFactory(database.ConnectionString);
        var auditFactory = new OutboxAuditContextFactory(database.ConnectionString);
        var services = new ServiceCollection()
            .AddScoped<ISecurityAuditWriter>(_ => new SecurityAuditWriter(auditFactory))
            .BuildServiceProvider();
        return new IdentityEmailOutboxProcessor(
            factory,
            writer,
            provider,
            services.GetRequiredService<IServiceScopeFactory>(),
            clock,
            NullLogger<IdentityEmailOutboxProcessor>.Instance);
    }

    private sealed class OutboxContextFactory(string connectionString)
        : IDbContextFactory<IdentityDbContext>
    {
        public IdentityDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class OutboxAuditContextFactory(string connectionString)
        : IDbContextFactory<SecurityAuditDbContext>
    {
        public SecurityAuditDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<SecurityAuditDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }

    private sealed class CapturingEmailProvider : IEmailProvider
    {
        public bool Fail { get; set; }
        public List<EmailVerificationMessage> Verifications { get; } = [];
        public List<PasswordResetMessage> PasswordResets { get; } = [];
        public List<EmailCodeMessage> EmailCodes { get; } = [];

        public Task SendEmailVerificationAsync(
            EmailVerificationMessage message,
            CancellationToken cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("Simulated provider outage.");
            Verifications.Add(message);
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(
            PasswordResetMessage message,
            CancellationToken cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("Simulated provider outage.");
            PasswordResets.Add(message);
            return Task.CompletedTask;
        }

        public Task SendSecurityNotificationAsync(
            IdentitySecurityNotificationMessage message,
            CancellationToken cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("Simulated provider outage.");
            return Task.CompletedTask;
        }

        public Task SendEmailCodeAsync(
            EmailCodeMessage message,
            CancellationToken cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("Simulated provider outage.");
            EmailCodes.Add(message);
            return Task.CompletedTask;
        }
    }
}
