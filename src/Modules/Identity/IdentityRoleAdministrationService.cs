using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity;

/// <summary>Role queries and commands for the identity administration surface.</summary>
public sealed class IdentityRoleAdministrationService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IdentityAdministrationSupport support,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AdminIdentityRole>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var roles = await db.Roles.AsNoTracking()
            .Include(role => role.UserRoles)
            .Include(role => role.RolePermissions)
            .OrderBy(role => role.Code)
            .ToListAsync(cancellationToken);
        return roles.Select(role => new AdminIdentityRole(
            role.Id,
            role.Code,
            role.Name,
            role.UserRoles.Count,
            role.RolePermissions
                .Select(permission => permission.PermissionCode)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToList()))
            .ToList();
    }

    public async Task<AdminIdentityRole> CreateRoleAsync(
        CreateAdminIdentityRoleRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var code = IdentityAdministrationSupport.NormalizeRequired(request.Code, 64, "Role code");
        var name = IdentityAdministrationSupport.NormalizeRequired(request.Name, 128, "Role name");
        if (code is "Owner" or "Customer")
            throw new IdentityException(409, "Owner and Customer roles are reserved.");
        if (!code.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-'))
            throw new IdentityException(400, "Role code contains unsupported characters.");
        var requestedPermissions = IdentityAdministrationSupport.ValidatePermissions(request.PermissionCodes);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await IdentityAdministrationSupport.LockAuthorizationStateAsync(db, cancellationToken);
            var actor = await support.RequireActorAsync(db, actorUserId, cancellationToken);
            IdentityDelegationPolicy.EnsureCanCreateRole(actor, requestedPermissions);
            if (await db.Roles.AnyAsync(role => role.Code == code, cancellationToken))
                throw new IdentityException(409, "A role with this code already exists.");

            var role = new Role
            {
                Id = Guid.CreateVersion7(),
                Code = code,
                Name = name
            };
            db.Roles.Add(role);
            foreach (var permission in requestedPermissions)
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionCode = permission
                });
            await db.SaveChangesAsync(cancellationToken);
            await support.WriteAuditAsync(
                db,
                transaction,
                new SecurityAuditEntry(
                    "Identity.RoleCreated",
                    "Succeeded",
                    "User",
                    ActorUserAccountId: actorUserId,
                    SafeMetadata: JsonSerializer.Serialize(new
                    {
                        role = role.Code,
                        permissions = requestedPermissions
                    })),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new AdminIdentityRole(
                role.Id,
                role.Code,
                role.Name,
                0,
                requestedPermissions);
        }
        catch (Exception exception)
        {
            await IdentityAdministrationSupport.SafeRollbackAsync(transaction, cancellationToken);
            await support.TryWriteRejectionAuditAsync(actorUserId, "CreateRole", exception, cancellationToken);
            throw;
        }
    }

    public async Task<AdminIdentityRole> UpdateRolePermissionsAsync(
        Guid roleId,
        UpdateAdminIdentityRolePermissionsRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var requestedPermissions = IdentityAdministrationSupport.ValidatePermissions(request.PermissionCodes);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await IdentityAdministrationSupport.LockAuthorizationStateAsync(db, cancellationToken);
            var actor = await support.RequireActorAsync(db, actorUserId, cancellationToken);
            var role = await db.Roles
                .Include(item => item.RolePermissions)
                .SingleOrDefaultAsync(item => item.Id == roleId, cancellationToken)
                ?? throw new IdentityException(404, "Role was not found.");
            var existingGrant = new IdentityRoleGrant(
                role.Id,
                role.Code,
                role.RolePermissions.Select(permission => permission.PermissionCode)
                    .ToHashSet(StringComparer.Ordinal));
            IdentityDelegationPolicy.EnsureCanEditRole(actor, existingGrant, requestedPermissions);

            db.RolePermissions.RemoveRange(role.RolePermissions);
            foreach (var permission in requestedPermissions)
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionCode = permission
                });
            var state = await db.AuthorizationStates.SingleAsync(cancellationToken);
            state.RbacRevision++;
            state.UpdatedAtUtc = timeProvider.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
            await IdentityAdministrationSupport.EnsureAdministrativeAccessAsync(db, cancellationToken);
            await support.WriteAuditAsync(
                db,
                transaction,
                new SecurityAuditEntry(
                    "Identity.RolePermissionsChanged",
                    "Succeeded",
                    "User",
                    ActorUserAccountId: actorUserId,
                    SafeMetadata: JsonSerializer.Serialize(new
                    {
                        role = role.Code,
                        permissions = requestedPermissions
                    })),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new AdminIdentityRole(
                role.Id,
                role.Code,
                role.Name,
                await db.UserRoles.CountAsync(item => item.RoleId == role.Id, cancellationToken),
                requestedPermissions);
        }
        catch (Exception exception)
        {
            await IdentityAdministrationSupport.SafeRollbackAsync(transaction, cancellationToken);
            await support.TryWriteRejectionAuditAsync(actorUserId, "UpdateRolePermissions", exception, cancellationToken);
            throw;
        }
    }
}
