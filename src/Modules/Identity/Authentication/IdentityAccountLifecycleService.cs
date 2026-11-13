using System.ComponentModel.DataAnnotations;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HardwareStore.Modules.Identity.Authentication;

public enum IdentityActionStatus
{
    Succeeded,
    InvalidInput,
    InvalidToken,
    InvalidPassword,
    InvalidCredentials,
    Conflict,
    Failed
}

public sealed record IdentityActionResult(IdentityActionStatus Status, string? InvalidField = null);

public sealed class IdentityAccountLifecycleService
{
    public const string EmailVerificationPurpose = "EmailVerification";
    public const string PasswordResetPurpose = "PasswordReset";

    private static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan AccountEmailCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AccountEmailWindow = TimeSpan.FromHours(24);
    private const int AccountEmailLimit = 5;

    private readonly IDbContextFactory<IdentityDbContext> _contextFactory;
    private readonly IPasswordHashingService _passwordHashing;
    private readonly IPasswordPolicyValidator _passwordPolicy;
    private readonly ISecurityAuditTransactionWriter _auditWriter;
    private readonly IIdentityEmailDispatcher _emailDispatcher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IdentityAccountLifecycleService> _logger;
    private readonly Uri _emailVerificationBaseUrl;
    private readonly Uri _passwordResetBaseUrl;

    public IdentityAccountLifecycleService(
        IDbContextFactory<IdentityDbContext> contextFactory,
        IPasswordHashingService passwordHashing,
        IPasswordPolicyValidator passwordPolicy,
        ISecurityAuditTransactionWriter auditWriter,
        IIdentityEmailDispatcher emailDispatcher,
        TimeProvider timeProvider,
        IConfiguration configuration,
        ILogger<IdentityAccountLifecycleService> logger)
    {
        _contextFactory = contextFactory;
        _passwordHashing = passwordHashing;
        _passwordPolicy = passwordPolicy;
        _auditWriter = auditWriter;
        _emailDispatcher = emailDispatcher;
        _timeProvider = timeProvider;
        _logger = logger;
        _emailVerificationBaseUrl = ReadBaseUrl(
            configuration,
            "Identity:EmailVerificationBaseUrl",
            "http://localhost:3000/verify-email");
        _passwordResetBaseUrl = ReadBaseUrl(
            configuration,
            "Identity:PasswordResetBaseUrl",
            "http://localhost:3000/reset-password");
    }

    public async Task<IdentityActionResult> VerifyEmailAsync(
        string? rawToken,
        string? newPassword,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!IdentityTokenFactory.TryHashRawToken(rawToken, out var tokenHash))
            return RejectedToken();

        var precheckNow = UtcNow();
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var candidate = await FindVerificationCandidateAsync(db, tokenHash, precheckNow, cancellationToken);
        if (candidate is null)
        {
            await TryWriteAuditAsync(new SecurityAuditEntry(
                "Identity.EmailVerification.Rejected",
                "Rejected",
                "Anonymous",
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"TokenRejected\"}"), cancellationToken);
            return RejectedToken();
        }

        var policyFailure = _passwordPolicy.Validate(newPassword);
        if (policyFailure != PasswordPolicyFailure.None)
            return InvalidPassword(policyFailure);

        var newPasswordHash = _passwordHashing.HashPassword(candidate.Account, newPassword!);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var account = await LockAccountAsync(db, candidate.Account.Id, cancellationToken);
            var token = await LockTokenAsync(db, tokenHash, cancellationToken);
            var now = UtcNow();
            if (account is null || token is null || account.Id != token.UserAccountId ||
                !account.IsActive || account.IsEmailVerified ||
                token.Purpose != EmailVerificationPurpose ||
                token.UsedAtUtc is not null || token.RevokedAtUtc is not null ||
                token.ExpiresAtUtc <= now)
            {
                await WriteAuditAsync(db, new SecurityAuditEntry(
                    "Identity.EmailVerification.Rejected",
                    "Rejected",
                    "Anonymous",
                    SubjectUserAccountId: account?.Id ?? candidate.Account.Id,
                    CorrelationId: correlationId,
                    SafeMetadata: "{\"reason\":\"TokenRejected\"}"), cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return RejectedToken();
            }

            token.UsedAtUtc = now;
            account.IsEmailVerified = true;
            account.PasswordHash = newPasswordHash;
            account.AuthorizationVersion = checked(account.AuthorizationVersion + 1);
            await db.SaveChangesAsync(cancellationToken);

            await RevokeAllSessionsAsync(db, account.Id, "EmailVerified", now, cancellationToken);
            await WriteAuditAsync(db, new SecurityAuditEntry(
                "Identity.EmailVerification.Succeeded",
                "Succeeded",
                "Anonymous",
                SubjectUserAccountId: account.Id,
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"PasswordSet\"}"), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new IdentityActionResult(IdentityActionStatus.Succeeded);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(exception, "Unable to complete email verification.");
            return Failed();
        }
    }

    public async Task<IdentityActionResult> ResendEmailVerificationAsync(
        string? email,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeEmail(email, out var normalizedEmail))
            return InvalidInput("email");

        var now = UtcNow();
        var generatedToken = IdentityTokenFactory.Create(
            Guid.CreateVersion7(), now, EmailVerificationPurpose, EmailVerificationLifetime);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await FindAccountByEmailAsync(db, normalizedEmail, cancellationToken);
        if (account is null || !account.IsActive || account.IsEmailVerified)
        {
            await TryWriteAuditAsync(new SecurityAuditEntry(
                "Identity.EmailVerification.ResendRequested",
                "Rejected",
                "Anonymous",
                SubjectUserAccountId: account?.Id,
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"AccountUnavailable\"}"), cancellationToken);
            return new IdentityActionResult(IdentityActionStatus.Succeeded);
        }

        generatedToken.PersistenceRecord.UserAccountId = account.Id;
        bool issued;
        try
        {
            issued = await TryIssueAccountTokenAsync(
                db,
                account.Id,
                EmailVerificationPurpose,
                generatedToken,
                now,
                "Identity.EmailVerification.ResendRequested",
                "Identity.EmailVerification.ResendThrottled",
                correlationId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Unable to issue an email verification resend token.");
            return new IdentityActionResult(IdentityActionStatus.Succeeded);
        }
        if (!issued)
            return new IdentityActionResult(IdentityActionStatus.Succeeded);

        await TryEnqueueVerificationAsync(account, generatedToken.RawToken, correlationId, CancellationToken.None);
        return new IdentityActionResult(IdentityActionStatus.Succeeded);
    }

    public async Task<IdentityActionResult> RequestPasswordResetAsync(
        string? email,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeEmail(email, out var normalizedEmail))
            return InvalidInput("email");

        var now = UtcNow();
        var generatedToken = IdentityTokenFactory.Create(
            Guid.CreateVersion7(), now, PasswordResetPurpose, PasswordResetLifetime);
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await FindAccountByEmailAsync(db, normalizedEmail, cancellationToken);
        if (account is null || !account.IsActive || !account.IsEmailVerified)
        {
            await TryWriteAuditAsync(new SecurityAuditEntry(
                "Identity.PasswordReset.Requested",
                "Rejected",
                "Anonymous",
                SubjectUserAccountId: account?.Id,
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"AccountUnavailable\"}"), cancellationToken);
            return new IdentityActionResult(IdentityActionStatus.Succeeded);
        }

        generatedToken.PersistenceRecord.UserAccountId = account.Id;
        bool issued;
        try
        {
            issued = await TryIssueAccountTokenAsync(
                db,
                account.Id,
                PasswordResetPurpose,
                generatedToken,
                now,
                "Identity.PasswordReset.Requested",
                "Identity.PasswordReset.Throttled",
                correlationId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Unable to issue a password reset token.");
            return new IdentityActionResult(IdentityActionStatus.Succeeded);
        }
        if (!issued)
            return new IdentityActionResult(IdentityActionStatus.Succeeded);

        await TryEnqueuePasswordResetAsync(account, generatedToken.RawToken, correlationId, CancellationToken.None);
        return new IdentityActionResult(IdentityActionStatus.Succeeded);
    }

    public async Task<IdentityActionResult> ResetPasswordAsync(
        string? rawToken,
        string? newPassword,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!IdentityTokenFactory.TryHashRawToken(rawToken, out var tokenHash))
            return RejectedToken();

        var precheckNow = UtcNow();
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var candidate = await FindPasswordResetCandidateAsync(db, tokenHash, precheckNow, cancellationToken);
        if (candidate is null)
        {
            await TryWriteAuditAsync(new SecurityAuditEntry(
                "Identity.PasswordReset.Rejected",
                "Rejected",
                "Anonymous",
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"TokenRejected\"}"), cancellationToken);
            return RejectedToken();
        }

        var policyFailure = _passwordPolicy.Validate(newPassword);
        if (policyFailure != PasswordPolicyFailure.None)
            return InvalidPassword(policyFailure);

        var newPasswordHash = _passwordHashing.HashPassword(candidate.Account, newPassword!);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var account = await LockAccountAsync(db, candidate.Account.Id, cancellationToken);
            var token = await LockTokenAsync(db, tokenHash, cancellationToken);
            var now = UtcNow();
            if (account is null || token is null || account.Id != token.UserAccountId ||
                !account.IsActive || !account.IsEmailVerified ||
                token.Purpose != PasswordResetPurpose ||
                token.UsedAtUtc is not null || token.RevokedAtUtc is not null ||
                token.ExpiresAtUtc <= now)
            {
                await WriteAuditAsync(db, new SecurityAuditEntry(
                    "Identity.PasswordReset.Rejected",
                    "Rejected",
                    "Anonymous",
                    SubjectUserAccountId: account?.Id ?? candidate.Account.Id,
                    CorrelationId: correlationId,
                    SafeMetadata: "{\"reason\":\"TokenRejected\"}"), cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return RejectedToken();
            }

            token.UsedAtUtc = now;
            account.PasswordHash = newPasswordHash;
            account.AccessFailedCount = 0;
            account.AccessFailedWindowStartedUtc = null;
            account.LockoutEndUtc = null;
            account.AuthorizationVersion = checked(account.AuthorizationVersion + 1);
            await db.SaveChangesAsync(cancellationToken);

            await RevokeAllSessionsAsync(db, account.Id, "PasswordReset", now, cancellationToken);
            await WriteAuditAsync(db, new SecurityAuditEntry(
                "Identity.PasswordReset.Succeeded",
                "Succeeded",
                "Anonymous",
                SubjectUserAccountId: account.Id,
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"PasswordChanged\"}"), cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await TryEnqueueSecurityNotificationAsync(
                account.Id,
                account.Email,
                candidate.FullName,
                IdentitySecurityNotificationKind.PasswordResetCompleted,
                now,
                correlationId,
                CancellationToken.None);
            return new IdentityActionResult(IdentityActionStatus.Succeeded);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(exception, "Unable to complete password reset.");
            return Failed();
        }
    }

    public async Task<IdentityActionResult> ChangePasswordAsync(
        Guid accountId,
        string? currentPassword,
        string? newPassword,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);

        var policyFailure = _passwordPolicy.Validate(newPassword);
        if (policyFailure != PasswordPolicyFailure.None)
            return InvalidPassword(policyFailure);
        if (string.IsNullOrEmpty(currentPassword))
            return InvalidInput("currentPassword");

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var snapshot = await db.UserAccounts.AsNoTracking()
            .Include(account => account.Person)
            .SingleOrDefaultAsync(account => account.Id == accountId, cancellationToken);
        if (snapshot is null || !snapshot.IsActive || !snapshot.IsEmailVerified || snapshot.PasswordHash is null ||
            _passwordHashing.VerifyPassword(snapshot, snapshot.PasswordHash, currentPassword) ==
            PasswordHashVerification.Failed)
        {
            await TryWriteAuditAsync(new SecurityAuditEntry(
                "Identity.Password.ChangeRejected",
                "Rejected",
                "User",
                ActorUserAccountId: accountId,
                SubjectUserAccountId: accountId,
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"CurrentPasswordRejected\"}"), cancellationToken);
            return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);
        }

        var newHash = _passwordHashing.HashPassword(snapshot, newPassword!);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var account = await LockAccountAsync(db, accountId, cancellationToken);
            var now = UtcNow();
            if (account is null || !account.IsActive || !account.IsEmailVerified ||
                !string.Equals(account.PasswordHash, snapshot.PasswordHash, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new IdentityActionResult(IdentityActionStatus.Conflict);
            }

            account.PasswordHash = newHash;
            account.AccessFailedCount = 0;
            account.AccessFailedWindowStartedUtc = null;
            account.LockoutEndUtc = null;
            account.AuthorizationVersion = checked(account.AuthorizationVersion + 1);
            await db.SaveChangesAsync(cancellationToken);

            await RevokeAllSessionsAsync(db, account.Id, "PasswordChanged", now, cancellationToken);
            await WriteAuditAsync(db, new SecurityAuditEntry(
                "Identity.Password.Changed",
                "Succeeded",
                "User",
                ActorUserAccountId: account.Id,
                SubjectUserAccountId: account.Id,
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"CurrentPasswordVerified\"}"), cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await TryEnqueueSecurityNotificationAsync(
                account.Id,
                account.Email,
                snapshot.Person.FullName,
                IdentitySecurityNotificationKind.PasswordChanged,
                now,
                correlationId,
                CancellationToken.None);
            return new IdentityActionResult(IdentityActionStatus.Succeeded);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(exception, "Unable to change password.");
            return Failed();
        }
    }

    private async Task<bool> TryIssueAccountTokenAsync(
        IdentityDbContext db,
        Guid accountId,
        string purpose,
        GeneratedIdentityToken generatedToken,
        DateTimeOffset now,
        string auditEventType,
        string throttledAuditEventType,
        string correlationId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var account = await LockAccountAsync(db, accountId, cancellationToken);
            if (account is null || !account.IsActive ||
                (purpose == EmailVerificationPurpose && account.IsEmailVerified) ||
                (purpose == PasswordResetPurpose && !account.IsEmailVerified))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            var existingTokens = await db.IdentityTokens.AsNoTracking()
                .Where(token => token.UserAccountId == accountId &&
                                token.Purpose == purpose &&
                                token.CreatedAtUtc > now - AccountEmailWindow)
                .OrderByDescending(token => token.CreatedAtUtc)
                .Select(token => token.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            var throttled = existingTokens.Count >= AccountEmailLimit ||
                            (existingTokens.Count > 0 &&
                             now - existingTokens[0] < AccountEmailCooldown);
            if (throttled)
            {
                await WriteAuditAsync(db, new SecurityAuditEntry(
                    throttledAuditEventType,
                    "Rejected",
                    "Anonymous",
                    SubjectUserAccountId: accountId,
                    CorrelationId: correlationId,
                    SafeMetadata: "{\"reason\":\"AccountRateLimit\"}"), cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return false;
            }

            await db.IdentityTokens
                .Where(token => token.UserAccountId == accountId &&
                                token.Purpose == purpose &&
                                token.UsedAtUtc == null &&
                                token.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(token => token.RevokedAtUtc, now), cancellationToken);

            db.IdentityTokens.Add(generatedToken.PersistenceRecord);
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, new SecurityAuditEntry(
                auditEventType,
                "Succeeded",
                "Anonymous",
                SubjectUserAccountId: accountId,
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"TokenIssued\"}"), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<VerificationCandidate?> FindVerificationCandidateAsync(
        IdentityDbContext db,
        byte[] tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await (from token in db.IdentityTokens.AsNoTracking()
               join account in db.UserAccounts.AsNoTracking() on token.UserAccountId equals account.Id
               where token.TokenHash == tokenHash &&
                     token.Purpose == EmailVerificationPurpose &&
                     token.UsedAtUtc == null &&
                     token.RevokedAtUtc == null &&
                     token.ExpiresAtUtc > now &&
                     account.IsActive &&
                     !account.IsEmailVerified
               select new VerificationCandidate(token.Id, account, account.Person.FullName))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<VerificationCandidate?> FindPasswordResetCandidateAsync(
        IdentityDbContext db,
        byte[] tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await (from token in db.IdentityTokens.AsNoTracking()
               join account in db.UserAccounts.AsNoTracking() on token.UserAccountId equals account.Id
               where token.TokenHash == tokenHash &&
                     token.Purpose == PasswordResetPurpose &&
                     token.UsedAtUtc == null &&
                     token.RevokedAtUtc == null &&
                     token.ExpiresAtUtc > now &&
                     account.IsActive &&
                     account.IsEmailVerified
               select new VerificationCandidate(token.Id, account, account.Person.FullName))
            .SingleOrDefaultAsync(cancellationToken);

    private static Task<UserAccount?> LockAccountAsync(
        IdentityDbContext db,
        Guid accountId,
        CancellationToken cancellationToken) =>
        db.UserAccounts
            .FromSqlInterpolated($"SELECT * FROM user_accounts WHERE id = {accountId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private static Task<IdentityToken?> LockTokenAsync(
        IdentityDbContext db,
        byte[] tokenHash,
        CancellationToken cancellationToken) =>
        db.IdentityTokens
            .FromSqlInterpolated($"SELECT * FROM identity_tokens WHERE token_hash = {tokenHash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private static Task<AccountEmailLookup?> FindAccountByEmailAsync(
        IdentityDbContext db,
        string normalizedEmail,
        CancellationToken cancellationToken) =>
        db.UserAccounts.AsNoTracking()
            .Where(account => account.NormalizedEmail == normalizedEmail)
            .Select(account => new AccountEmailLookup(
                account.Id, account.Email, account.Person.FullName, account.IsActive, account.IsEmailVerified))
            .SingleOrDefaultAsync(cancellationToken);

    private static Task<int> RevokeAllSessionsAsync(
        IdentityDbContext db,
        Guid accountId,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        db.AuthSessions
            .Where(session => session.UserAccountId == accountId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAtUtc, now)
                .SetProperty(session => session.RevokeReason, reason)
                .SetProperty(session => session.RowVersion, session => session.RowVersion + 1),
                cancellationToken);

    private async Task WriteAuditAsync(
        IdentityDbContext db,
        SecurityAuditEntry entry,
        CancellationToken cancellationToken)
    {
        var transaction = db.Database.CurrentTransaction
            ?? throw new InvalidOperationException("A security audit transaction is required.");
        await _auditWriter.WriteAsync(
            entry,
            db.Database.GetDbConnection(),
            transaction.GetDbTransaction(),
            cancellationToken);
    }

    private async Task TryWriteAuditAsync(SecurityAuditEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await _auditWriter.WriteAsync(entry, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Unable to persist an identity security audit event.");
        }
    }

    private async Task TryEnqueueVerificationAsync(
        AccountEmailLookup account,
        string rawToken,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailDispatcher.EnqueueVerificationAsync(
                account.Id,
                new EmailVerificationMessage(
                    account.Email,
                    account.FullName,
                    CreateFragmentUrl(_emailVerificationBaseUrl, rawToken)),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Unable to enqueue email verification.");
            await TryWriteAuditAsync(new SecurityAuditEntry(
                "Identity.EmailVerification.DeliveryFailed",
                "Failed",
                "System",
                SubjectUserAccountId: account.Id,
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"DispatchUnavailable\"}"), cancellationToken);
        }
    }

    private async Task TryEnqueuePasswordResetAsync(
        AccountEmailLookup account,
        string rawToken,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailDispatcher.EnqueuePasswordResetAsync(
                account.Id,
                new PasswordResetMessage(
                    account.Email,
                    account.FullName,
                    CreateFragmentUrl(_passwordResetBaseUrl, rawToken)),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Unable to enqueue password reset email.");
            await TryWriteAuditAsync(new SecurityAuditEntry(
                "Identity.PasswordReset.DeliveryFailed",
                "Failed",
                "System",
                SubjectUserAccountId: account.Id,
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"DispatchUnavailable\"}"), cancellationToken);
        }
    }

    private async Task TryEnqueueSecurityNotificationAsync(
        Guid accountId,
        string email,
        string fullName,
        IdentitySecurityNotificationKind kind,
        DateTimeOffset occurredAtUtc,
        string correlationId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailDispatcher.EnqueueSecurityNotificationAsync(
                accountId,
                new IdentitySecurityNotificationMessage(
                    email,
                    fullName,
                    kind,
                    occurredAtUtc),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Unable to enqueue identity security notification.");
            await TryWriteAuditAsync(new SecurityAuditEntry(
                "Identity.SecurityNotification.DeliveryFailed",
                "Failed",
                "System",
                SubjectUserAccountId: accountId,
                CorrelationId: correlationId,
                SafeMetadata: "{\"reason\":\"DispatchUnavailable\"}"), cancellationToken);
        }
    }

    private DateTimeOffset UtcNow() => _timeProvider.GetUtcNow().ToUniversalTime();

    private static bool TryNormalizeEmail(string? email, out string normalized)
    {
        normalized = string.Empty;
        var trimmed = email?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 255 ||
            !new EmailAddressAttribute().IsValid(trimmed))
            return false;

        normalized = IdentityLoginService.NormalizeIdentifier(trimmed);
        return normalized.Length <= 255;
    }

    private static IdentityActionResult RejectedToken() =>
        new(IdentityActionStatus.InvalidToken);

    private static IdentityActionResult InvalidPassword(PasswordPolicyFailure failure) =>
        new(IdentityActionStatus.InvalidPassword, failure == PasswordPolicyFailure.TooShort
            ? "minimumLength"
            : failure == PasswordPolicyFailure.TooLong
                ? "maximumLength"
                : "commonPassword");

    private static IdentityActionResult InvalidInput(string field) =>
        new(IdentityActionStatus.InvalidInput, field);

    private static IdentityActionResult Failed() =>
        new(IdentityActionStatus.Failed);

    private static Uri ReadBaseUrl(IConfiguration configuration, string key, string fallback)
    {
        var value = configuration[key] ?? fallback;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException($"{key} must be an absolute HTTP or HTTPS URL.");
        return uri;
    }

    private static Uri CreateFragmentUrl(Uri baseUri, string rawToken) =>
        new UriBuilder(baseUri)
        {
            Fragment = $"token={Uri.EscapeDataString(rawToken)}"
        }.Uri;

    private sealed record AccountEmailLookup(
        Guid Id,
        string Email,
        string FullName,
        bool IsActive,
        bool IsEmailVerified);

    private sealed record VerificationCandidate(Guid TokenId, UserAccount Account, string FullName);
}
