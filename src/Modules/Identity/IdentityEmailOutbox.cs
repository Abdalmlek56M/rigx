using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HardwareStore.Modules.Identity;

public interface IIdentityEmailDispatcher
{
    ValueTask EnqueueVerificationAsync(
        Guid userAccountId,
        EmailVerificationMessage message,
        CancellationToken cancellationToken = default);

    ValueTask EnqueuePasswordResetAsync(
        Guid userAccountId,
        PasswordResetMessage message,
        CancellationToken cancellationToken = default);

    ValueTask EnqueueEmailOtpAsync(
        Guid userAccountId,
        EmailOtpMessage message,
        CancellationToken cancellationToken = default);

    ValueTask EnqueueSecurityNotificationAsync(
        Guid userAccountId,
        IdentitySecurityNotificationMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record OutboxVerificationPayload(string Email, string FullName, string VerificationUrl);
public sealed record OutboxEmailChangePayload(string NewEmail, string FullName, string VerificationUrl);
public sealed record OutboxPasswordResetPayload(string Email, string FullName, string ResetUrl);
public sealed record OutboxEmailOtpPayload(string Email, string FullName, string Code, DateTimeOffset ExpiresAtUtc, string Purpose);
public sealed record OutboxSecurityNotificationPayload(
    string Email,
    string FullName,
    IdentitySecurityNotificationKind Kind,
    DateTimeOffset OccurredAtUtc);

/// <summary>
/// Durable, encrypted identity email queue. Tokens and action URLs never appear
/// in the database in plaintext and are erased after successful delivery.
/// </summary>
public sealed class IdentityEmailOutboxWriter(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IDataProtectionProvider dataProtectionProvider,
    TimeProvider timeProvider)
{
    public const string VerificationKind = "EmailVerification";
    public const string PasswordResetKind = "PasswordReset";
    public const string SecurityNotificationKind = "SecurityNotification";
    public const string EmailChangeKind = "EmailChangeVerification";
    public const string EmailLoginCodeKind = "EmailLoginCode";
    public const string PasswordResetCodeKind = "PasswordResetCode";

    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("HardwareStore.IdentityEmailOutbox.v1");

    public async Task EnqueueAsync(
        string kind,
        Guid? accountId,
        object payload,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        Enqueue(db, kind, accountId, payload);
        await db.SaveChangesAsync(cancellationToken);
    }

    public void Enqueue(
        IdentityDbContext db,
        string kind,
        Guid? accountId,
        object payload)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);
        db.EmailOutbox.Add(new IdentityEmailOutboxMessage
        {
            Id = Guid.CreateVersion7(),
            UserAccountId = accountId,
            Kind = kind,
            ProtectedPayload = _protector.Protect(json),
            Status = IdentityEmailOutboxStatus.Pending,
            Attempts = 0,
            NextAttemptAtUtc = now,
            CreatedAtUtc = now
        });
    }


    internal T Unprotect<T>(byte[] protectedPayload)
    {
        var bytes = _protector.Unprotect(protectedPayload);
        return JsonSerializer.Deserialize<T>(bytes)
            ?? throw new InvalidOperationException("Identity email payload is invalid.");
    }
}

public sealed class IdentityEmailOutboxDispatcher(IdentityEmailOutboxWriter writer) : IIdentityEmailDispatcher
{
    public ValueTask EnqueueVerificationAsync(
        Guid userAccountId,
        EmailVerificationMessage message,
        CancellationToken cancellationToken = default) =>
        new(writer.EnqueueAsync(
            IdentityEmailOutboxWriter.VerificationKind,
            userAccountId,
            new OutboxVerificationPayload(message.Email, message.FullName, message.VerificationUrl.ToString()),
            cancellationToken));

    public ValueTask EnqueuePasswordResetAsync(
        Guid userAccountId,
        PasswordResetMessage message,
        CancellationToken cancellationToken = default) =>
        new(writer.EnqueueAsync(
            IdentityEmailOutboxWriter.PasswordResetKind,
            userAccountId,
            new OutboxPasswordResetPayload(message.Email, message.FullName, message.ResetUrl.ToString()),
            cancellationToken));

    public ValueTask EnqueueEmailOtpAsync(
        Guid userAccountId,
        EmailOtpMessage message,
        CancellationToken cancellationToken = default) =>
        new(writer.EnqueueAsync(
            message.Purpose == "PasswordResetCode"
                ? IdentityEmailOutboxWriter.PasswordResetCodeKind
                : IdentityEmailOutboxWriter.EmailLoginCodeKind,
            userAccountId,
            new OutboxEmailOtpPayload(message.Email, message.FullName, message.Code, message.ExpiresAtUtc, message.Purpose),
            cancellationToken));

    public ValueTask EnqueueSecurityNotificationAsync(
        Guid userAccountId,
        IdentitySecurityNotificationMessage message,
        CancellationToken cancellationToken = default) =>
        new(writer.EnqueueAsync(
            IdentityEmailOutboxWriter.SecurityNotificationKind,
            userAccountId,
            new OutboxSecurityNotificationPayload(
                message.Email, message.FullName, message.Kind, message.OccurredAtUtc),
            cancellationToken));
}

public sealed class IdentityEmailOutboxProcessor(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IdentityEmailOutboxWriter writer,
    IEmailProvider emailProvider,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<IdentityEmailOutboxProcessor> logger) : BackgroundService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Identity email outbox processing failed.");
            }
            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }
    }

    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var claimDb = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await claimDb.Database.BeginTransactionAsync(cancellationToken);
        await claimDb.EmailOutbox
            .Where(row => row.Status == IdentityEmailOutboxStatus.Processing &&
                          row.ProcessingAtUtc != null &&
                          row.ProcessingAtUtc < now - ProcessingTimeout)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(row => row.Status, IdentityEmailOutboxStatus.Failed)
                .SetProperty(row => row.ProcessingAtUtc, (DateTimeOffset?)null)
                .SetProperty(row => row.NextAttemptAtUtc, now)
                .SetProperty(row => row.LastErrorCode, "ProcessingTimeout"), cancellationToken);

        var due = await claimDb.EmailOutbox
            .FromSqlInterpolated($"""
                SELECT *
                FROM identity_email_outbox
                WHERE status IN ('Pending', 'Failed')
                  AND next_attempt_at_utc <= {now}
                ORDER BY created_at_utc
                LIMIT 10
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);
        foreach (var row in due)
        {
            row.Status = IdentityEmailOutboxStatus.Processing;
            row.ProcessingAtUtc = now;
        }
        await claimDb.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        foreach (var claimed in due)
        {
            await DeliverAsync(claimed.Id, cancellationToken);
        }
        return due.Count;
    }

    private async Task DeliverAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.EmailOutbox.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (row is null || row.Status != IdentityEmailOutboxStatus.Processing)
            return;

        try
        {
            await SendAsync(row, cancellationToken);
            row.Status = IdentityEmailOutboxStatus.Sent;
            row.ProcessedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
            row.ProcessingAtUtc = null;
            row.ProtectedPayload = [];
            row.LastErrorCode = null;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync("Identity.EmailDelivery.Sent", "Succeeded", row.UserAccountId,
                new { kind = row.Kind, attempts = row.Attempts }, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            row.Attempts++;
            row.ProcessingAtUtc = null;
            row.LastErrorCode = exception.GetType().Name;
            var delayMinutes = Math.Min(60, Math.Pow(2, row.Attempts));
            row.NextAttemptAtUtc = timeProvider.GetUtcNow().ToUniversalTime().AddMinutes(delayMinutes);
            row.Status = row.Attempts >= MaxAttempts
                ? IdentityEmailOutboxStatus.DeadLetter
                : IdentityEmailOutboxStatus.Failed;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync("Identity.EmailDelivery.Failed", "Failed", row.UserAccountId,
                new { kind = row.Kind, attempts = row.Attempts, deadLetter = row.Status == IdentityEmailOutboxStatus.DeadLetter },
                cancellationToken);
        }
    }

    private Task SendAsync(IdentityEmailOutboxMessage row, CancellationToken cancellationToken) =>
        row.Kind switch
        {
            IdentityEmailOutboxWriter.VerificationKind => SendVerificationAsync(row, cancellationToken),
            IdentityEmailOutboxWriter.PasswordResetKind => SendPasswordResetAsync(row, cancellationToken),
            IdentityEmailOutboxWriter.SecurityNotificationKind => SendSecurityNotificationAsync(row, cancellationToken),
            IdentityEmailOutboxWriter.EmailChangeKind => SendEmailChangeAsync(row, cancellationToken),
            IdentityEmailOutboxWriter.EmailLoginCodeKind or IdentityEmailOutboxWriter.PasswordResetCodeKind =>
                SendEmailOtpAsync(row, cancellationToken),
            _ => throw new InvalidOperationException("Unknown identity email outbox kind.")
        };

    private Task SendVerificationAsync(IdentityEmailOutboxMessage row, CancellationToken cancellationToken)
    {
        var payload = writer.Unprotect<OutboxVerificationPayload>(row.ProtectedPayload);
        return emailProvider.SendEmailVerificationAsync(
            new EmailVerificationMessage(payload.Email, payload.FullName, new Uri(payload.VerificationUrl)),
            cancellationToken);
    }

    private Task SendPasswordResetAsync(IdentityEmailOutboxMessage row, CancellationToken cancellationToken)
    {
        var payload = writer.Unprotect<OutboxPasswordResetPayload>(row.ProtectedPayload);
        return emailProvider.SendPasswordResetAsync(
            new PasswordResetMessage(payload.Email, payload.FullName, new Uri(payload.ResetUrl)),
            cancellationToken);
    }

    private Task SendSecurityNotificationAsync(IdentityEmailOutboxMessage row, CancellationToken cancellationToken)
    {
        var payload = writer.Unprotect<OutboxSecurityNotificationPayload>(row.ProtectedPayload);
        return emailProvider.SendSecurityNotificationAsync(
            new IdentitySecurityNotificationMessage(
                payload.Email, payload.FullName, payload.Kind, payload.OccurredAtUtc),
            cancellationToken);
    }

    private Task SendEmailChangeAsync(IdentityEmailOutboxMessage row, CancellationToken cancellationToken)
    {
        var payload = writer.Unprotect<OutboxEmailChangePayload>(row.ProtectedPayload);
        return emailProvider.SendEmailChangeVerificationAsync(
            new EmailVerificationMessage(payload.NewEmail, payload.FullName, new Uri(payload.VerificationUrl)),
            cancellationToken);
    }

    private Task SendEmailOtpAsync(IdentityEmailOutboxMessage row, CancellationToken cancellationToken)
    {
        var payload = writer.Unprotect<OutboxEmailOtpPayload>(row.ProtectedPayload);
        return emailProvider.SendEmailOtpAsync(
            new EmailOtpMessage(payload.Email, payload.FullName, payload.Code, payload.ExpiresAtUtc, payload.Purpose),
            cancellationToken);
    }

    private async Task WriteAuditAsync(
        string eventType,
        string outcome,
        Guid? accountId,
        object metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var auditWriter = scope.ServiceProvider.GetRequiredService<ISecurityAuditWriter>();
            await auditWriter.WriteAsync(new SecurityAuditEntry(
                eventType,
                outcome,
                "System",
                SubjectUserAccountId: accountId,
                SafeMetadata: JsonSerializer.Serialize(metadata)), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to write identity email outbox audit event.");
        }
    }
}
