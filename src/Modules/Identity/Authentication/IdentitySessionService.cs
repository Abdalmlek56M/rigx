using System.Security.Cryptography;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace HardwareStore.Modules.Identity.Authentication;

public sealed record IdentitySessionView(
    Guid Id,
    bool IsCurrent,
    string Device,
    string Browser,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastActivityAtUtc,
    DateTimeOffset IdleExpiresAtUtc,
    DateTimeOffset AbsoluteExpiresAtUtc);

public sealed class IdentitySessionService
{
    private readonly IDbContextFactory<IdentityDbContext> _contextFactory;
    private readonly ISecurityAuditTransactionWriter _auditWriter;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IdentitySessionService> _logger;

    public IdentitySessionService(
        IDbContextFactory<IdentityDbContext> contextFactory,
        ISecurityAuditTransactionWriter auditWriter,
        TimeProvider timeProvider,
        ILogger<IdentitySessionService> logger)
    {
        _contextFactory = contextFactory;
        _auditWriter = auditWriter;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IdentityActionResult> RevokeCurrentAsync(
        Guid accountId,
        string? rawSessionKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty || !IdentityTokenFactory.TryHashRawToken(rawSessionKey, out var keyHash))
            return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);

        var now = UtcNow();
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var account = await LockAccountAsync(db, accountId, cancellationToken);
            var session = await db.AuthSessions
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM auth_sessions
                    WHERE session_key_hash = {keyHash}
                      AND user_account_id = {accountId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);
            if (account is null || session is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);
            }

            if (session.RevokedAtUtc is null)
            {
                session.RevokedAtUtc = now;
                session.RevokeReason = "LogoutCurrent";
                session.RowVersion++;
                await db.SaveChangesAsync(cancellationToken);
            }

            await WriteAuditAsync(db, new SecurityAuditEntry(
                "Identity.Session.LoggedOut",
                "Succeeded",
                "User",
                ActorUserAccountId: accountId,
                SubjectUserAccountId: accountId,
                CorrelationId: correlationId,
                SafeMetadata: "{\"scope\":\"Current\"}"), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new IdentityActionResult(IdentityActionStatus.Succeeded);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(exception, "Unable to revoke the current identity session.");
            return new IdentityActionResult(IdentityActionStatus.Failed);
        }
    }

    public async Task<IdentityActionResult> RevokeAllAsync(
        Guid accountId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);

        var now = UtcNow();
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var account = await LockAccountAsync(db, accountId, cancellationToken);
            if (account is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);
            }

            var revokedCount = await db.AuthSessions
                .Where(session => session.UserAccountId == accountId && session.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(session => session.RevokedAtUtc, now)
                    .SetProperty(session => session.RevokeReason, "LogoutAll")
                    .SetProperty(session => session.RowVersion, session => session.RowVersion + 1),
                    cancellationToken);

            await WriteAuditAsync(db, new SecurityAuditEntry(
                "Identity.Session.LoggedOutAll",
                "Succeeded",
                "User",
                ActorUserAccountId: accountId,
                SubjectUserAccountId: accountId,
                CorrelationId: correlationId,
                SafeMetadata: JsonSerializer.Serialize(new { scope = "All", revokedCount })), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new IdentityActionResult(IdentityActionStatus.Succeeded);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(exception, "Unable to revoke all identity sessions.");
            return new IdentityActionResult(IdentityActionStatus.Failed);
        }
    }

    public async Task<IReadOnlyList<IdentitySessionView>> ListAsync(
        Guid accountId,
        string? currentRawSessionKey,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            return [];
        byte[]? currentHash = IdentityTokenFactory.TryHashRawToken(currentRawSessionKey, out var hash)
            ? hash
            : null;
        var now = UtcNow();
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.AuthSessions.AsNoTracking()
            .Where(session => session.UserAccountId == accountId &&
                              session.RevokedAtUtc == null &&
                              session.IdleExpiresAtUtc > now &&
                              session.AbsoluteExpiresAtUtc > now)
            .Select(session => new
            {
                session.PublicId,
                session.SessionKeyHash,
                session.UserAgent,
                session.CreatedAtUtc,
                session.LastActivityAtUtc,
                session.IdleExpiresAtUtc,
                session.AbsoluteExpiresAtUtc
            })
            .ToListAsync(cancellationToken);
        return rows
            .OrderByDescending(row => row.LastActivityAtUtc)
            .Select(row => new IdentitySessionView(
                row.PublicId,
                currentHash is not null && CryptographicOperations.FixedTimeEquals(row.SessionKeyHash, currentHash),
                DeviceLabel(row.UserAgent),
                BrowserLabel(row.UserAgent),
                row.CreatedAtUtc,
                row.LastActivityAtUtc,
                row.IdleExpiresAtUtc,
                row.AbsoluteExpiresAtUtc))
            .ToList();
    }

    public async Task<IdentityActionResult> RevokeSessionAsync(
        Guid accountId,
        Guid sessionPublicId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty || sessionPublicId == Guid.Empty)
            return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);

        var now = UtcNow();
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var account = await LockAccountAsync(db, accountId, cancellationToken);
            if (account is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);
            }

            var session = await db.AuthSessions
                .SingleOrDefaultAsync(row => row.UserAccountId == accountId &&
                                             row.PublicId == sessionPublicId &&
                                             row.RevokedAtUtc == null,
                    cancellationToken);
            if (session is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);
            }

            session.RevokedAtUtc = now;
            session.RevokeReason = "AccountSessionRevoked";
            session.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, new SecurityAuditEntry(
                "Identity.Session.Revoked",
                "Succeeded",
                "User",
                ActorUserAccountId: accountId,
                SubjectUserAccountId: accountId,
                CorrelationId: correlationId,
                SafeMetadata: JsonSerializer.Serialize(new { sessionId = sessionPublicId })), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new IdentityActionResult(IdentityActionStatus.Succeeded);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(exception, "Unable to revoke an identity session by public id.");
            return new IdentityActionResult(IdentityActionStatus.Failed);
        }
    }

    public async Task<IdentityActionResult> RevokeOtherSessionsAsync(
        Guid accountId,
        string? currentRawSessionKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty ||
            !IdentityTokenFactory.TryHashRawToken(currentRawSessionKey, out var currentHash))
            return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);

        var now = UtcNow();
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var account = await LockAccountAsync(db, accountId, cancellationToken);
            if (account is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);
            }
            var currentExists = await db.AuthSessions.AsNoTracking().AnyAsync(
                session => session.UserAccountId == accountId &&
                           session.SessionKeyHash == currentHash &&
                           session.RevokedAtUtc == null,
                cancellationToken);
            if (!currentExists)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new IdentityActionResult(IdentityActionStatus.InvalidCredentials);
            }

            var revokedCount = await db.AuthSessions
                .Where(session => session.UserAccountId == accountId &&
                                  session.SessionKeyHash != currentHash &&
                                  session.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(session => session.RevokedAtUtc, now)
                    .SetProperty(session => session.RevokeReason, "AccountOtherSessionsRevoked")
                    .SetProperty(session => session.RowVersion, session => session.RowVersion + 1),
                    cancellationToken);

            await WriteAuditAsync(db, new SecurityAuditEntry(
                "Identity.Session.RevokedOthers",
                "Succeeded",
                "User",
                ActorUserAccountId: accountId,
                SubjectUserAccountId: accountId,
                CorrelationId: correlationId,
                SafeMetadata: JsonSerializer.Serialize(new { revokedCount })), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new IdentityActionResult(IdentityActionStatus.Succeeded);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogError(exception, "Unable to revoke other identity sessions.");
            return new IdentityActionResult(IdentityActionStatus.Failed);
        }
    }

    private static string DeviceLabel(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";
        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
            return "Tablet";
        if (userAgent.Contains("Mobi", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
            return "Mobile";
        return "Desktop";
    }

    private static string BrowserLabel(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return "Unknown";
        if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase))
            return "Edge";
        if (userAgent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase))
            return "Firefox";
        if (userAgent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("CriOS/", StringComparison.OrdinalIgnoreCase))
            return "Chrome";
        if (userAgent.Contains("Safari/", StringComparison.OrdinalIgnoreCase))
            return "Safari";
        return "Other";
    }

    private static Task<UserAccount?> LockAccountAsync(
        IdentityDbContext db,
        Guid accountId,
        CancellationToken cancellationToken) =>
        db.UserAccounts
            .FromSqlInterpolated($"SELECT * FROM user_accounts WHERE id = {accountId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

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

    private DateTimeOffset UtcNow() => _timeProvider.GetUtcNow().ToUniversalTime();
}
