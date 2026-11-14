using HardwareStore.Modules.Identity.Authorization;

namespace HardwareStore.Modules.Identity;

/// <summary>
/// Effective permissions of the signed-in administrator. Permissions are read
/// from the database, never from the request body or the client UI.
/// </summary>
public sealed record IdentityActor(
    Guid Id,
    bool IsOwner,
    IReadOnlySet<string> Permissions);

public sealed record IdentityRoleGrant(
    Guid Id,
    string Code,
    IReadOnlySet<string> Permissions);

/// <summary>
/// Delegation rules for M3.3.
///
/// A manager may never grant a permission they do not hold, may never modify a
/// more privileged account, and may never assign/remove the Owner role unless
/// they are an Owner themselves. These checks are deliberately pure so they can
/// be unit-tested independently from EF Core.
/// </summary>
public static class IdentityDelegationPolicy
{
    public static void EnsureCanManageUser(
        IdentityActor actor,
        Guid targetUserId,
        bool targetIsOwner,
        IReadOnlySet<string> targetPermissions)
    {
        if (targetIsOwner && !actor.IsOwner && targetUserId != actor.Id)
            throw Denied("Only an Owner can manage another Owner.");
        if (targetIsOwner && !actor.IsOwner && targetUserId == actor.Id)
            throw Denied("Owner access cannot be managed by a non-owner session.");
        EnsureSubset(actor, targetPermissions, "You cannot manage a user with permissions you do not hold.");
    }

    public static void EnsureCanAssignRoles(
        IdentityActor actor,
        bool targetIsOwner,
        IReadOnlySet<string> targetPermissions,
        IReadOnlyCollection<IdentityRoleGrant> requestedRoles)
    {
        if (targetIsOwner && !actor.IsOwner)
            throw Denied("Only an Owner can change another Owner's roles.");
        if (requestedRoles.Any(role => string.Equals(role.Code, "Owner", StringComparison.Ordinal)) && !actor.IsOwner)
            throw Denied("Only an Owner can assign the Owner role.");
        EnsureSubset(actor, targetPermissions, "You cannot change roles for a user with permissions you do not hold.");
        foreach (var role in requestedRoles)
            EnsureSubset(actor, role.Permissions, "You cannot assign a role that contains permissions you do not hold.");
    }

    public static void EnsureCanCreateRole(
        IdentityActor actor,
        IReadOnlyCollection<string> requestedPermissions)
    {
        EnsureNoOwnerOnly(requestedPermissions);
        EnsureSubset(actor, requestedPermissions, "You cannot create a role with permissions you do not hold.");
    }

    public static void EnsureCanEditRole(
        IdentityActor actor,
        IdentityRoleGrant role,
        IReadOnlyCollection<string> requestedPermissions)
    {
        if (string.Equals(role.Code, "Owner", StringComparison.Ordinal) ||
            string.Equals(role.Code, "Customer", StringComparison.Ordinal))
            throw new IdentityException(409, "Owner and Customer permissions are fixed.");
        EnsureNoOwnerOnly(requestedPermissions);
        EnsureSubset(actor, role.Permissions, "You cannot edit a role with permissions you do not hold.");
        EnsureSubset(actor, requestedPermissions, "You cannot grant permissions you do not hold.");
    }

    private static void EnsureNoOwnerOnly(IEnumerable<string> permissions)
    {
        if (permissions.Any(PermissionCatalog.OwnerOnly.Contains))
            throw new IdentityException(409, "Some permissions belong to the Owner role only.");
    }

    private static void EnsureSubset(
        IdentityActor actor,
        IEnumerable<string> permissions,
        string message)
    {
        if (permissions.Any(permission => !actor.Permissions.Contains(permission)))
            throw Denied(message);
    }

    private static IdentityException Denied(string message) =>
        new(403, message);
}
