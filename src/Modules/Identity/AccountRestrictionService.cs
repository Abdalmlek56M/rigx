using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Identity;

public sealed record RestrictionView(
    Guid Id,
    string Type,
    string Reason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool IsActive,
    DateTimeOffset? RevokedAtUtc,
    string? ReinstatementReason,
    long RowVersion);

public sealed record RestrictionRequest(string? Type, string? Reason, DateTimeOffset? ExpiresAtUtc);

public sealed record ReinstatementRequest(string? Reason, long RowVersion);

public sealed class AccountRestrictionService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    ISecurityAuditTransactionWriter auditWriter,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<RestrictionView>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.AccountRestrictions.AsNoTracking()
            .Where(item => item.UserAccountId == accountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        return rows.Select(item => Map(item, now)).ToList();
    }

    public async Task<RestrictionView> ApplyAsync(
        Guid targetAccountId,
        Guid actorId,
        RestrictionRequest request,
        CancellationToken cancellationToken = default)
    {
        var type = request.Type?.Trim();
        if (type is not (AccountRestrictionType.Suspension or AccountRestrictionType.Ban))
            throw new IdentityException(400, "A supported restriction type is required.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
            throw new IdentityException(400, "A restriction reason of at least 5 characters is required.");
        if (request.Reason.Trim().Length > 1000)
            throw new IdentityException(400, "The restriction reason is too long.");
        if (targetAccountId == actorId)
            throw new IdentityException(409, "An administrator cannot restrict their own account.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        if (request.ExpiresAtUtc is not null && request.ExpiresAtUtc <= now)
            throw new IdentityException(400, "The restriction expiry must be in the future.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var target = await db.UserAccounts.Include(item => item.UserRoles).ThenInclude(item => item.Role)
                .SingleOrDefaultAsync(item => item.Id == targetAccountId, cancellationToken)
                ?? throw new IdentityException(404, "Account was not found.");
            if (target.UserRoles.Any(role => role.Role.Code == "Owner"))
                throw new IdentityException(409, "Owner accounts cannot be restricted.");
            var restriction = new AccountRestriction
            {
                Id = Guid.CreateVersion7(),
                UserAccountId = targetAccountId,
                Type = type,
                Reason = request.Reason.Trim(),
                CreatedAtUtc = now,
                EffectiveAtUtc = now,
                ExpiresAtUtc = request.ExpiresAtUtc?.ToUniversalTime(),
                CreatedByUserAccountId = actorId
            };
            db.AccountRestrictions.Add(restriction);
            target.AuthorizationVersion++;
            await db.AuthSessions
                .Where(session => session.UserAccountId == targetAccountId && session.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(session => session.RevokedAtUtc, now)
                    .SetProperty(session => session.RevokeReason, "AccountRestricted")
                    .SetProperty(session => session.RowVersion, session => session.RowVersion + 1),
                    cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, actorId, targetAccountId,
                type == AccountRestrictionType.Ban ? "Identity.AccountBanned" : "Identity.AccountSuspended",
                new { restrictionId = restriction.Id, type }, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(restriction, now);
        }
        catch
        {
            await SafeRollbackAsync(transaction);
            throw;
        }
    }

    public async Task<RestrictionView> ReinstateAsync(
        Guid restrictionId,
        Guid actorId,
        ReinstatementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
            throw new IdentityException(400, "A reinstatement reason of at least 5 characters is required.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var restriction = await db.AccountRestrictions
                .SingleOrDefaultAsync(item => item.Id == restrictionId, cancellationToken)
                ?? throw new IdentityException(404, "Restriction was not found.");
            if (restriction.RevokedAtUtc is not null)
                throw new IdentityException(409, "The restriction is already revoked.");
            if (restriction.RowVersion != request.RowVersion)
                throw new IdentityException(409, "The restriction was changed by another user.");
            restriction.RevokedAtUtc = now;
            restriction.RevokedByUserAccountId = actorId;
            restriction.ReinstatementReason = request.Reason.Trim();
            restriction.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, actorId, restriction.UserAccountId,
                "Identity.AccountReinstated",
                new { restrictionId = restriction.Id }, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(restriction, now);
        }
        catch
        {
            await SafeRollbackAsync(transaction);
            throw;
        }
    }

    public static bool IsActive(AccountRestriction restriction, DateTimeOffset now) =>
        restriction.RevokedAtUtc is null &&
        (restriction.Type == AccountRestrictionType.Ban ||
         (restriction.Type == AccountRestrictionType.Suspension &&
          (restriction.ExpiresAtUtc is null || restriction.ExpiresAtUtc > now)));

    private async Task WriteAuditAsync(
        IdentityDbContext db,
        IDbContextTransaction transaction,
        Guid actorId,
        Guid subjectId,
        string eventType,
        object metadata,
        CancellationToken cancellationToken) =>
        await auditWriter.WriteAsync(
            new SecurityAuditEntry(eventType, "Succeeded", "User",
                ActorUserAccountId: actorId,
                SubjectUserAccountId: subjectId,
                SafeMetadata: JsonSerializer.Serialize(metadata)),
            db.Database.GetDbConnection(),
            transaction.GetDbTransaction(),
            cancellationToken);

    private static RestrictionView Map(AccountRestriction item, DateTimeOffset now) => new(
        item.Id, item.Type, item.Reason, item.CreatedAtUtc, item.EffectiveAtUtc,
        item.ExpiresAtUtc, IsActive(item, now), item.RevokedAtUtc,
        item.ReinstatementReason, item.RowVersion);

    private static async Task SafeRollbackAsync(IDbContextTransaction transaction)
    {
        try { await transaction.RollbackAsync(CancellationToken.None); }
        catch { /* preserve original failure */ }
    }
}
