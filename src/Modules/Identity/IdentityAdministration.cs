using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HardwareStore.Modules.Identity;

/// <summary>
/// Thin application facade for identity administration.
///
/// User commands/queries and role commands/queries live in separate services;
/// this type keeps one stable contract for the HTTP endpoints and tests. It
/// contains no business logic.
/// </summary>
public sealed class IdentityAdministrationService
{
    private readonly IdentityUserAdministrationService _users;
    private readonly IdentityRoleAdministrationService _roles;

    public IdentityAdministrationService(
        IDbContextFactory<IdentityDbContext> contextFactory,
        ISecurityAuditTransactionWriter auditWriter,
        TimeProvider timeProvider,
        ILoggerFactory? loggerFactory = null,
        IIdentityEmailDispatcher? emailDispatcher = null,
        IdentityLinks? links = null,
        TimeZoneInfo? businessTimeZone = null)
    {
        var support = new IdentityAdministrationSupport(
            auditWriter,
            timeProvider,
            loggerFactory?.CreateLogger<IdentityAdministrationSupport>()
                ?? NullLogger<IdentityAdministrationSupport>.Instance);
        _users = new IdentityUserAdministrationService(
            contextFactory,
            support,
            timeProvider,
            businessTimeZone ?? TimeZoneInfo.Utc,
            emailDispatcher,
            links ?? IdentityLinks.Local,
            loggerFactory?.CreateLogger<IdentityUserAdministrationService>());
        _roles = new IdentityRoleAdministrationService(contextFactory, support, timeProvider);
    }

    public Task<AdminIdentityUserPage> GetUsersAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        _users.GetUsersAsync(search, isActive, page, pageSize, cancellationToken);

    public Task<AdminIdentityUser?> GetUserAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        _users.GetUserAsync(id, cancellationToken);

    public Task<AdminIdentityUserInvitationResult> CreateUserAsync(
        CreateAdminIdentityUserRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        _users.CreateUserAsync(request, actorUserId, cancellationToken);

    public Task<AdminIdentityUser> UpdateUserAsync(
        Guid id,
        UpdateAdminIdentityUserRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        _users.UpdateUserAsync(id, request, actorUserId, cancellationToken);

    public Task<AdminIdentityUser> AssignRolesAsync(
        Guid id,
        AssignAdminIdentityRolesRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        _users.AssignRolesAsync(id, request, actorUserId, cancellationToken);

    public Task<IReadOnlyList<AdminIdentityRole>> GetRolesAsync(
        CancellationToken cancellationToken = default) =>
        _roles.GetRolesAsync(cancellationToken);

    public Task<AdminIdentityRole> CreateRoleAsync(
        CreateAdminIdentityRoleRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        _roles.CreateRoleAsync(request, actorUserId, cancellationToken);

    public Task<AdminIdentityRole> UpdateRolePermissionsAsync(
        Guid roleId,
        UpdateAdminIdentityRolePermissionsRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        _roles.UpdateRolePermissionsAsync(roleId, request, actorUserId, cancellationToken);
}
