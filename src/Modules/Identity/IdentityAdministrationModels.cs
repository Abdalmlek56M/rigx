public sealed record AdminIdentityUser(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string? PhoneNumber,
    bool IsActive,
    bool IsEmailVerified,
    int AuthorizationVersion,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<string> RoleCodes);

public sealed record AdminIdentityRole(
    Guid Id,
    string Code,
    string Name,
    int UserCount,
    IReadOnlyList<string> PermissionCodes);

public sealed record UpdateAdminIdentityUserRequest(
    string? FullName,
    string? PhoneNumber,
    bool IsActive);

public sealed record CreateAdminIdentityUserRequest(
    string? Username,
    string? Email,
    string? FullName,
    string? PhoneNumber,
    IReadOnlyList<Guid>? RoleIds);

public sealed record AdminIdentityUserInvitationResult(
    AdminIdentityUser User,
    bool EmailDispatchQueued);

public sealed record AssignAdminIdentityRolesRequest(IReadOnlyList<Guid>? RoleIds);

public sealed record UpdateAdminIdentityRolePermissionsRequest(IReadOnlyList<string>? PermissionCodes);

public sealed record CreateAdminIdentityRoleRequest(
    string? Code,
    string? Name,
    IReadOnlyList<string>? PermissionCodes);

