using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace HardwareStore.Modules.Identity;

/// <summary>
/// Shared transaction, audit, lock, delegation and normalization primitives for
/// the identity administration use cases. Keeping them here avoids duplicating
/// security-sensitive code between user and role command services.
/// </summary>
public sealed class IdentityAdministrationSupport(
    ISecurityAuditTransactionWriter auditWriter,
    TimeProvider timeProvider,
    ILogger<IdentityAdministrationSupport> logger)
{
    public async Task<IdentityActor> RequireActorAsync(
        IdentityDbContext db,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var data = await db.UserAccounts.AsNoTracking()
            .Where(account => account.Id == actorUserId && account.IsActive)
            .Select(account => new
            {
                RoleCodes = account.UserRoles.Select(userRole => userRole.Role.Code).ToList(),
                Permissions = account.UserRoles
                    .SelectMany(userRole => userRole.Role.RolePermissions)
                    .Select(permission => permission.PermissionCode)
                    .Distinct()
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (data is null)
            throw new IdentityAdministrationException(403, "The acting account is not active or does not exist.");
        return new IdentityActor(
            actorUserId,
            data.RoleCodes.Contains("Owner", StringComparer.Ordinal),
            data.Permissions.ToHashSet(StringComparer.Ordinal));
    }

    public async Task<UserAuthority?> GetUserAuthorityAsync(
        IdentityDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var data = await db.UserAccounts.AsNoTracking()
            .Where(account => account.Id == userId)
            .Select(account => new
            {
                RoleCodes = account.UserRoles.Select(userRole => userRole.Role.Code).ToList(),
                Permissions = account.UserRoles
                    .SelectMany(userRole => userRole.Role.RolePermissions)
                    .Select(permission => permission.PermissionCode)
                    .Distinct()
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
        return data is null
            ? null
            : new UserAuthority(
                data.RoleCodes.Contains("Owner", StringComparer.Ordinal),
                data.Permissions.ToHashSet(StringComparer.Ordinal));
    }

    public static async Task LockAuthorizationStateAsync(
        IdentityDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            "SELECT 1 FROM authorization_state WHERE id = 1 FOR UPDATE",
            Array.Empty<object>(),
            cancellationToken);
    }

    public static async Task EnsureAdministrativeAccessAsync(
        IdentityDbContext db,
        CancellationToken cancellationToken)
    {
        var activeOwners = await db.UserRoles.CountAsync(
            userRole => userRole.Role.Code == "Owner" && userRole.UserAccount.IsActive,
            cancellationToken);
        if (activeOwners == 0)
            throw new IdentityAdministrationException(409, "The last active Owner cannot be removed or deactivated.");

        var activeManagers = await db.UserAccounts.CountAsync(
            account => account.IsActive &&
                       account.UserRoles.Any(userRole =>
                           userRole.Role.RolePermissions.Any(permission =>
                               permission.PermissionCode == "System.ManageUsers")),
            cancellationToken);
        if (activeManagers == 0)
            throw new IdentityAdministrationException(409, "At least one active user must retain System.ManageUsers.");
    }

    public static async Task EnsureNotLastOwnerAsync(
        IdentityDbContext db,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var isOwner = await db.UserRoles.AnyAsync(
            item => item.UserAccountId == accountId && item.Role.Code == "Owner",
            cancellationToken);
        if (!isOwner)
            return;

        var otherActiveOwners = await db.UserRoles.CountAsync(
            item => item.Role.Code == "Owner" &&
                    item.UserAccountId != accountId &&
                    item.UserAccount.IsActive,
            cancellationToken);
        if (otherActiveOwners == 0)
            throw new IdentityAdministrationException(409, "The last active Owner cannot be removed or deactivated.");
    }

    public async Task RevokeSessionsAsync(
        IdentityDbContext db,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await db.AuthSessions
            .Where(session => session.UserAccountId == accountId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.RevokedAtUtc, now)
                    .SetProperty(session => session.RevokeReason, "IdentityAdministration")
                    .SetProperty(session => session.RowVersion, session => session.RowVersion + 1),
                cancellationToken);
    }

    public async Task WriteAuditAsync(
        IdentityDbContext db,
        IDbContextTransaction transaction,
        SecurityAuditEntry entry,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            entry,
            db.Database.GetDbConnection(),
            transaction.GetDbTransaction(),
            cancellationToken);
    }

    public async Task TryWriteRejectionAuditAsync(
        Guid actorUserId,
        string operation,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not IdentityAdministrationException { StatusCode: 403 or 409 } denied)
            return;
        try
        {
            await auditWriter.WriteAsync(
                new SecurityAuditEntry(
                    "Identity.AdministrationRejected",
                    "Rejected",
                    "User",
                    ActorUserAccountId: actorUserId,
                    SafeMetadata: JsonSerializer.Serialize(new
                    {
                        operation,
                        status = denied.StatusCode
                    })),
                cancellationToken);
        }
        catch (Exception auditException)
        {
            // The rejected change is already rolled back; never mask the original failure,
            // but do not silently hide a failure to record the rejection.
            logger.LogWarning(auditException, "Unable to persist identity administration rejection audit for {Operation}.", operation);
        }
    }

    public static async Task SafeRollbackAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch
        {
            // Preserve the original administration error.
        }
    }

    public static string[] ValidatePermissions(IReadOnlyList<string>? permissionCodes)
    {
        var requested = (permissionCodes ?? [])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
        var unknown = requested
            .Where(code => !PermissionCatalog.All.Contains(code))
            .ToArray();
        if (unknown.Length > 0)
            throw new IdentityAdministrationException(400, "One or more permissions are not in the permission catalog.");
        return requested;
    }

    public static string NormalizeRequired(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new IdentityAdministrationException(400, $"{label} is required.");
        var normalized = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (normalized.Length > maxLength)
            throw new IdentityAdministrationException(400, $"{label} is too long.");
        return normalized;
    }

    public static string? NormalizeOptional(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (normalized.Length > maxLength)
            throw new IdentityAdministrationException(400, $"{label} is too long.");
        return normalized;
    }
}

public sealed record UserAuthority(bool IsOwner, IReadOnlySet<string> Permissions);
