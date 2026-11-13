using System.Security.Cryptography;
using System.Text;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity.Authentication;

public sealed record EmailOtpVerifyResult(bool Succeeded, Guid? UserAccountId, string Reason);

public sealed class EmailOtpService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IDataProtectionProvider dataProtectionProvider,
    IIdentityEmailDispatcher emailDispatcher,
    ISecurityAuditWriter auditWriter,
    TimeProvider timeProvider)
{
    public const string LoginPurpose = "EmailLoginCode";
    public const string PasswordResetPurpose = "PasswordResetCode";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private const int MaxAttempts = 5;
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("HardwareStore.Identity.EmailOtp.v1");

    public async Task RequestAsync(string? email, string purpose, string correlationId, CancellationToken cancellationToken = default)
    {
        if (purpose is not (LoginPurpose or PasswordResetPurpose)) return;
        var normalized = NormalizeEmail(email);
        if (normalized is null) return;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.UserAccounts.AsNoTracking()
            .Where(item => item.NormalizedEmail == normalized && item.IsActive && item.IsEmailVerified)
            .Select(item => new { item.Id, item.Email, item.PersonId })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null) return;
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await db.IdentityTokens
            .Where(token => token.UserAccountId == account.Id && token.Purpose == purpose &&
                            token.UsedAtUtc == null && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, now), cancellationToken);
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var entity = new IdentityToken
        {
            Id = Guid.CreateVersion7(),
            UserAccountId = account.Id,
            Purpose = purpose,
            TokenHash = SHA256.HashData(RandomNumberGenerator.GetBytes(32)),
            ProtectedCode = _protector.Protect(Encoding.UTF8.GetBytes(code)),
            Attempts = 0,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(Lifetime)
        };
        db.IdentityTokens.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        var fullName = await db.Persons.AsNoTracking().Where(person => person.Id == account.PersonId)
            .Select(person => person.FullName).SingleOrDefaultAsync(cancellationToken) ?? account.Email;
        await emailDispatcher.EnqueueEmailOtpAsync(account.Id,
            new EmailOtpMessage(account.Email, fullName, code, entity.ExpiresAtUtc, purpose), cancellationToken);
        await WriteAuditAsync("Identity.EmailOtp.Requested", "Succeeded", account.Id, purpose, correlationId, cancellationToken);
    }

    public async Task<EmailOtpVerifyResult> VerifyAsync(string? email, string? code, string purpose,
        string correlationId, CancellationToken cancellationToken = default)
    {
        if (purpose is not (LoginPurpose or PasswordResetPurpose) || string.IsNullOrWhiteSpace(code) ||
            code.Length != 6 || !code.All(char.IsAsciiDigit))
            return new EmailOtpVerifyResult(false, null, "InvalidCode");
        var normalized = NormalizeEmail(email);
        if (normalized is null) return new EmailOtpVerifyResult(false, null, "InvalidCode");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.UserAccounts.AsNoTracking()
            .Where(item => item.NormalizedEmail == normalized && item.IsActive && item.IsEmailVerified)
            .Select(item => new { item.Id })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null) return new EmailOtpVerifyResult(false, null, "InvalidCode");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var token = await db.IdentityTokens
            .Where(item => item.UserAccountId == account.Id && item.Purpose == purpose &&
                           item.UsedAtUtc == null && item.RevokedAtUtc == null)
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (token is null || token.ProtectedCode is null || token.ExpiresAtUtc <= now || token.Attempts >= MaxAttempts)
            return new EmailOtpVerifyResult(false, account.Id, "ExpiredOrMissing");

        var expected = _protector.Unprotect(token.ProtectedCode);
        var supplied = Encoding.UTF8.GetBytes(code);
        if (!CryptographicOperations.FixedTimeEquals(expected, supplied))
        {
            token.Attempts++;
            if (token.Attempts >= MaxAttempts) token.RevokedAtUtc = now;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync("Identity.EmailOtp.Rejected", "Rejected", account.Id, purpose, correlationId, cancellationToken);
            return new EmailOtpVerifyResult(false, account.Id, "InvalidCode");
        }
        token.UsedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync("Identity.EmailOtp.Verified", "Succeeded", account.Id, purpose, correlationId, cancellationToken);
        return new EmailOtpVerifyResult(true, account.Id, "Verified");
    }

    private async Task WriteAuditAsync(string eventType, string outcome, Guid accountId, string purpose,
        string correlationId, CancellationToken cancellationToken)
    {
        try
        {
            await auditWriter.WriteAsync(new SecurityAuditEntry(
                eventType, outcome, "Anonymous", SubjectUserAccountId: accountId,
                CorrelationId: correlationId,
                SafeMetadata: System.Text.Json.JsonSerializer.Serialize(new { purpose })), cancellationToken);
        }
        catch
        {
            // Audit failure must not turn an OTP request into an error response.
        }
    }

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : IdentityLoginService.NormalizeIdentifier(email);
}
