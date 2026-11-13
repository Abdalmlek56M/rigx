using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity.Authentication;

public sealed record CurrentIdentityUser(
    string Username,
    string Email,
    string FullName,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    bool IsEmailVerified,
    string? AvatarStorageKey);

public sealed class IdentityCurrentUserService(IDbContextFactory<IdentityDbContext> contextFactory)
{
    public async Task<CurrentIdentityUser?> GetAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            return null;

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking()
            .Where(account => account.Id == accountId && account.IsActive)
            .Select(account => new CurrentIdentityUser(
                account.Username,
                account.Email,
                account.Person.FullName,
                account.Person.PhoneNumber,
                account.UserRoles.Select(userRole => userRole.Role.Code).ToList(),
                account.UserRoles
                    .SelectMany(userRole => userRole.Role.RolePermissions)
                    .Select(rolePermission => rolePermission.PermissionCode)
                    .Distinct()
                    .ToList(),
                account.IsEmailVerified,
                account.AvatarStorageKey))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
