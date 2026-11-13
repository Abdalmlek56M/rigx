using System.Security.Cryptography;
using System.Text;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity.Authentication;

/// <summary>What an email code is for. Stored as text; the database checks the list.</summary>
public static class EmailCodePurpose
{
    public const string SignIn = "SignIn";
    public const string StepUp = "StepUp";
}

/// <summary>When the sent code expires and when the user may ask for another one (the UI's resend timer).</summary>
public sealed record EmailCodeTicket(DateTimeOffset ExpiresAtUtc, DateTimeOffset ResendAvailableAtUtc);

/// <summary>Result of checking a code: success, or how many tries are left on the current code.</summary>
public sealed record EmailCodeCheck(bool Succeeded, int AttemptsLeft);

/// <summary>
/// The one rule set for 6-digit email codes (business rules §10): 5 minutes, 5 attempts, single use,
/// stored protected, and a new code revokes the previous one. Requests per email address are
/// throttled (one per minute, five per hour) whether or not an account exists, so the response
/// never reveals which emails are registered.
/// </summary>
public sealed class EmailCodeService(
    IDataProtectionProvider dataProtectionProvider,
    IdentityEmailOutboxWriter outboxWriter,
    TimeProvider timeProvider)
{
    public const int MaxAttempts = 5;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan HourlyWindow = TimeSpan.FromHours(1);
    private const int HourlyLimit = 5;

    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("HardwareStore.Identity.EmailCode.v1");

    /// <summary>
    /// Issues a new code and queues the email in the same transaction, unless the address is still
    /// inside its resend cooldown or hourly limit; then nothing is sent and the ticket says when to retry.
    /// </summary>
    public async Task<EmailCodeTicket> IssueAsync(
        IdentityDbContext db,
        EmailCodeRecipient recipient,
        string purpose,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockAddressAsync(db, recipient.NormalizedEmail, cancellationToken);

        var recent = await db.EmailCodes.AsNoTracking()
            .Where(code => code.NormalizedEmail == recipient.NormalizedEmail &&
                           code.Purpose == purpose &&
                           code.CreatedAtUtc > now - HourlyWindow)
            .OrderBy(code => code.CreatedAtUtc)
            .Select(code => new { code.CreatedAtUtc, code.ExpiresAtUtc })
            .ToListAsync(cancellationToken);
        var throttledUntil = ThrottledUntil(recent.Select(code => code.CreatedAtUtc).ToList(), now);
        if (throttledUntil is { } retryAt)
        {
            await transaction.CommitAsync(cancellationToken);
            return new EmailCodeTicket(recent[^1].ExpiresAtUtc, retryAt);
        }

        await db.EmailCodes
            .Where(code => code.NormalizedEmail == recipient.NormalizedEmail && code.Purpose == purpose &&
                           code.UsedAtUtc == null && code.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(code => code.RevokedAtUtc, now), cancellationToken);

        var plainCode = RandomNumberGenerator.GetInt32(100000, 1000000)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var entity = new EmailCode
        {
            Id = Guid.CreateVersion7(),
            NormalizedEmail = recipient.NormalizedEmail,
            Purpose = purpose,
            ProtectedCode = _protector.Protect(Encoding.UTF8.GetBytes(plainCode)),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(Lifetime)
        };
        db.EmailCodes.Add(entity);
        outboxWriter.Enqueue(db, IdentityEmailOutboxWriter.EmailCodeKind, recipient.AccountId,
            new OutboxEmailCodePayload(recipient.Email, recipient.DisplayName, plainCode, entity.ExpiresAtUtc, purpose));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new EmailCodeTicket(entity.ExpiresAtUtc, now.Add(ResendCooldown));
    }

    /// <summary>
    /// Checks a code against the newest active code for the address. The row is locked, so parallel
    /// guesses cannot exceed <see cref="MaxAttempts"/>; the fifth wrong guess burns the code.
    /// </summary>
    public async Task<EmailCodeCheck> VerifyAsync(
        IdentityDbContext db,
        string normalizedEmail,
        string? code,
        string purpose,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !code.All(char.IsAsciiDigit))
            return new EmailCodeCheck(false, 0);

        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var active = await db.EmailCodes
            .FromSqlInterpolated($"""
                SELECT * FROM email_codes
                WHERE normalized_email = {normalizedEmail} AND purpose = {purpose}
                  AND used_at_utc IS NULL AND revoked_at_utc IS NULL
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
        if (active is null || active.ExpiresAtUtc <= now || active.Attempts >= MaxAttempts)
        {
            await transaction.CommitAsync(cancellationToken);
            return new EmailCodeCheck(false, 0);
        }

        if (!CryptographicOperations.FixedTimeEquals(_protector.Unprotect(active.ProtectedCode), Encoding.UTF8.GetBytes(code)))
        {
            active.Attempts++;
            if (active.Attempts >= MaxAttempts)
                active.RevokedAtUtc = now;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new EmailCodeCheck(false, MaxAttempts - active.Attempts);
        }

        active.UsedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new EmailCodeCheck(true, MaxAttempts - active.Attempts);
    }

    private static DateTimeOffset? ThrottledUntil(IReadOnlyList<DateTimeOffset> createdInWindow, DateTimeOffset now)
    {
        if (createdInWindow.Count >= HourlyLimit)
            return createdInWindow[^HourlyLimit].Add(HourlyWindow);
        if (createdInWindow.Count > 0 && now - createdInWindow[^1] < ResendCooldown)
            return createdInWindow[^1].Add(ResendCooldown);
        return null;
    }

    /// <summary>Serializes concurrent requests for one address so the throttle cannot be raced.</summary>
    private static Task LockAddressAsync(IdentityDbContext db, string normalizedEmail, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({normalizedEmail}, 0))", cancellationToken);
}

/// <summary>Where a code goes. <see cref="AccountId"/> is null when the address has no account yet.</summary>
public sealed record EmailCodeRecipient(string Email, string NormalizedEmail, string DisplayName, Guid? AccountId);
