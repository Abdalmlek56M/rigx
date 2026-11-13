using HardwareStore.Common.Domain;
public sealed record AdminIdentityUser(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string? PhoneNumber,
    bool IsActive,
    bool IsEmailVerified,
    DateTimeOffset? LockoutEndUtc,
    int AuthorizationVersion,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<string> RoleCodes);

public sealed record AdminIdentityUserPage(
    IReadOnlyList<AdminIdentityUser> Items,
    int Page,
    int PageSize,
    int TotalCount);

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

public sealed class IdentityAdministrationException(int statusCode, string message) : AppException(statusCode, message);
