using System.Security.Cryptography;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

    public IdentitySessionService(
        IDbContextFactory<IdentityDbContext> contextFactory,
        ISecurityAuditTransactionWriter auditWriter,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _auditWriter = auditWriter;
        _timeProvider = timeProvider;
    }

    /// <summary>Signs out the session that made this request.</summary>
    public async Task RevokeCurrentAsync(
        Guid accountId,
        string? rawSessionKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty || !DatabaseTicketStore.TryHashSessionKey(rawSessionKey, out var keyHash))
            throw InvalidSession();

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = await LockAccountAsync(db, accountId, cancellationToken) ?? throw InvalidSession();
        var session = await db.AuthSessions
            .FromSqlInterpolated($"""
                SELECT *
                FROM auth_sessions
                WHERE session_key_hash = {keyHash}
                  AND user_account_id = {accountId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken) ?? throw InvalidSession();

        if (session.RevokedAtUtc is null)
        {
            session.RevokedAtUtc = UtcNow();
            session.RevokeReason = "LogoutCurrent";
            session.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
        }

        await WriteAuditAsync(db, UserEntry(accountId, "Identity.Session.LoggedOut", correlationId,
            "{\"scope\":\"Current\"}"), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>Signs out every session of the account, including the current one.</summary>
    public async Task RevokeAllAsync(
        Guid accountId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = await LockAccountAsync(db, accountId, cancellationToken) ?? throw InvalidSession();
        var revokedCount = await RevokeWhereAsync(db,
            session => session.UserAccountId == accountId && session.RevokedAtUtc == null,
            "LogoutAll", cancellationToken);
        await WriteAuditAsync(db, UserEntry(accountId, "Identity.Session.LoggedOutAll", correlationId,
            JsonSerializer.Serialize(new { scope = "All", revokedCount })), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IdentitySessionView>> ListAsync(
        Guid accountId,
        string? currentRawSessionKey,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            return [];
        byte[]? currentHash = DatabaseTicketStore.TryHashSessionKey(currentRawSessionKey, out var hash)
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

    /// <summary>Signs out one session of the account (from the devices list).</summary>
    public async Task RevokeSessionAsync(
        Guid accountId,
        Guid sessionPublicId,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = await LockAccountAsync(db, accountId, cancellationToken) ?? throw InvalidSession();
        var revoked = await RevokeWhereAsync(db,
            row => row.UserAccountId == accountId && row.PublicId == sessionPublicId && row.RevokedAtUtc == null,
            "AccountSessionRevoked", cancellationToken);
        if (revoked == 0)
            throw new IdentityException(404, "Session was not found.");
        await WriteAuditAsync(db, UserEntry(accountId, "Identity.Session.Revoked", correlationId,
            JsonSerializer.Serialize(new { sessionId = sessionPublicId })), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>Signs out every session of the account except the one that made this request.</summary>
    public async Task RevokeOtherSessionsAsync(
        Guid accountId,
        string? currentRawSessionKey,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (!DatabaseTicketStore.TryHashSessionKey(currentRawSessionKey, out var currentHash))
            throw InvalidSession();

        await using var db = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = await LockAccountAsync(db, accountId, cancellationToken) ?? throw InvalidSession();
        var currentExists = await db.AuthSessions.AsNoTracking().AnyAsync(
            session => session.UserAccountId == accountId &&
                       session.SessionKeyHash == currentHash &&
                       session.RevokedAtUtc == null,
            cancellationToken);
        if (!currentExists)
            throw InvalidSession();

        var revokedCount = await RevokeWhereAsync(db,
            session => session.UserAccountId == accountId &&
                       session.SessionKeyHash != currentHash &&
                       session.RevokedAtUtc == null,
            "AccountOtherSessionsRevoked", cancellationToken);
        await WriteAuditAsync(db, UserEntry(accountId, "Identity.Session.RevokedOthers", correlationId,
            JsonSerializer.Serialize(new { revokedCount })), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private Task<int> RevokeWhereAsync(
        IdentityDbContext db,
        System.Linq.Expressions.Expression<Func<AuthSession, bool>> filter,
        string reason,
        CancellationToken cancellationToken)
    {
        var now = UtcNow();
        return db.AuthSessions.Where(filter)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAtUtc, now)
                .SetProperty(session => session.RevokeReason, reason)
                .SetProperty(session => session.RowVersion, session => session.RowVersion + 1),
                cancellationToken);
    }

    private static SecurityAuditEntry UserEntry(Guid accountId, string eventType, string correlationId, string metadata) =>
        new(eventType, "Succeeded", "User",
            ActorUserAccountId: accountId,
            SubjectUserAccountId: accountId,
            CorrelationId: correlationId,
            SafeMetadata: metadata);

    private static IdentityException InvalidSession() => new(401, "The session is no longer valid.");

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
