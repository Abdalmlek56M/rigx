using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HardwareStore.Modules.Identity;

/// <summary>User queries and commands for the identity administration surface.</summary>
public sealed class IdentityUserAdministrationService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IdentityAdministrationSupport support,
    TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone,
    IIdentityEmailDispatcher? emailDispatcher,
    IdentityLinks links,
    ILogger<IdentityUserAdministrationService>? logger)
{
    public async Task<PagedResult<AdminIdentityUser>> GetUsersAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (page, pageSize) = PageRequest.Of(page, pageSize);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = BuildUserQuery(db, search, isActive);
        var total = await query.CountAsync(cancellationToken);
        var accounts = await query
            .Include(account => account.Person)
            .Include(account => account.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .OrderBy(account => account.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<AdminIdentityUser>(
            accounts.Select(MapUser).ToList(),
            page,
            pageSize,
            total);
    }

    public async Task<AdminIdentityUser?> GetUserAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.UserAccounts.AsNoTracking()
            .Include(item => item.Person)
            .Include(item => item.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return account is null ? null : MapUser(account);
    }

    /// <summary>
    /// Invites a staff member by email: the account is created unverified with its roles, and an email
    /// tells them to sign in with that address (code or Google). There is no password or link token.
    /// </summary>
    public async Task<AdminIdentityUserInvitationResult> CreateUserAsync(
        CreateAdminIdentityUserRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var email = IdentityAdministrationSupport.NormalizeRequired(request.Email, 255, "Email");
        var username = IdentityAdministrationSupport.NormalizeOptional(request.Username, 128, "Username") ?? email;
        var fullName = IdentityAdministrationSupport.NormalizeRequired(request.FullName, 255, "Full name");
        var phoneNumber = IdentityAdministrationSupport.NormalizeOptional(request.PhoneNumber, 32, "Phone number");
        if (!new EmailAddressAttribute().IsValid(email))
            throw new IdentityException(400, "Email is invalid.");
        if (username.Length > 128)
            throw new IdentityException(400, "Username is too long.");
        var roleIds = (request.RoleIds ?? []).Distinct().ToArray();
        if (roleIds.Length == 0)
            throw new IdentityException(400, "At least one role is required.");

        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        UserAccount account;
        IReadOnlyList<string> roleCodes;
        try
        {
            await IdentityAdministrationSupport.LockAuthorizationStateAsync(db, cancellationToken);
            var actor = await support.RequireActorAsync(db, actorUserId, cancellationToken);
            var roles = await db.Roles
                .Include(role => role.RolePermissions)
                .Where(role => roleIds.Contains(role.Id))
                .ToListAsync(cancellationToken);
            if (roles.Count != roleIds.Length)
                throw new IdentityException(400, "One or more roles do not exist.");

            var grants = roles.Select(role => new IdentityRoleGrant(
                role.Id,
                role.Code,
                role.RolePermissions.Select(permission => permission.PermissionCode)
                    .ToHashSet(StringComparer.Ordinal))).ToArray();
            IdentityDelegationPolicy.EnsureCanAssignRoles(actor, false, new HashSet<string>(), grants);

            var normalizedUsername = IdentityLoginService.NormalizeIdentifier(username);
            var normalizedEmail = IdentityLoginService.NormalizeIdentifier(email);
            if (await db.UserAccounts.AnyAsync(
                    item => item.NormalizedUsername == normalizedUsername || item.NormalizedEmail == normalizedEmail,
                    cancellationToken))
                throw new IdentityException(409, "The username or email is already in use.");

            var person = new Person
            {
                Id = Guid.CreateVersion7(),
                FullName = fullName,
                PhoneNumber = phoneNumber,
                Email = email,
                IsActive = true,
                CreatedAtUtc = now
            };
            PersonClassificationPolicy.ApplyForRoles(person, roles.Select(role => role.Code));
            await new PersonCodeGenerator(db, timeProvider, businessTimeZone)
                .CreatePersonInCurrentTransactionAsync(person, cancellationToken);
            account = new UserAccount
            {
                Id = Guid.CreateVersion7(),
                PersonId = person.Id,
                Person = person,
                Username = username,
                NormalizedUsername = normalizedUsername,
                Email = email,
                NormalizedEmail = normalizedEmail,
                IsEmailVerified = false,
                IsActive = true,
                AuthorizationVersion = 1,
                CreatedAtUtc = now
            };
            db.UserAccounts.Add(account);
            foreach (var role in roles)
                db.UserRoles.Add(new UserRole { UserAccountId = account.Id, RoleId = role.Id });
            await db.SaveChangesAsync(cancellationToken);
            roleCodes = roles.Select(role => role.Code).OrderBy(code => code, StringComparer.Ordinal).ToList();
            await support.WriteAuditAsync(
                db,
                transaction,
                new SecurityAuditEntry(
                    "Identity.UserInvited",
                    "Succeeded",
                    "User",
                    ActorUserAccountId: actorUserId,
                    SubjectUserAccountId: account.Id,
                    SafeMetadata: JsonSerializer.Serialize(new { roles = roleCodes })),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            await IdentityAdministrationSupport.SafeRollbackAsync(transaction, cancellationToken);
            await support.TryWriteRejectionAuditAsync(actorUserId, "CreateUser", exception, cancellationToken);
            throw;
        }

        var dispatchQueued = await TryQueueInvitationAsync(account, fullName, roleCodes, cancellationToken);
        var user = await GetUserAsync(account.Id, cancellationToken)
            ?? throw new IdentityException(404, "User was not found.");
        return new AdminIdentityUserInvitationResult(user, dispatchQueued);
    }

    /// <summary>The account already exists; a failed queue write is reported, and the invite can simply be re-sent by signing in.</summary>
    private async Task<bool> TryQueueInvitationAsync(
        UserAccount account,
        string fullName,
        IReadOnlyList<string> roleCodes,
        CancellationToken cancellationToken)
    {
        if (emailDispatcher is null)
            return false;
        try
        {
            await emailDispatcher.EnqueueStaffInvitationAsync(
                account.Id,
                new StaffInvitationMessage(account.Email, fullName, roleCodes, links.SignIn),
                cancellationToken);
            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger?.LogWarning(exception, "Staff invitation email could not be queued after the account was created.");
            return false;
        }
    }

    public async Task<AdminIdentityUser> UpdateUserAsync(
        Guid id,
        UpdateAdminIdentityUserRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var fullName = IdentityAdministrationSupport.NormalizeRequired(request.FullName, 255, "Full name");
        var phoneNumber = IdentityAdministrationSupport.NormalizeOptional(request.PhoneNumber, 32, "Phone number");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await IdentityAdministrationSupport.LockAuthorizationStateAsync(db, cancellationToken);
            var actor = await support.RequireActorAsync(db, actorUserId, cancellationToken);
            var targetAuthority = await support.GetUserAuthorityAsync(db, id, cancellationToken)
                ?? throw new IdentityException(404, "User was not found.");
            IdentityDelegationPolicy.EnsureCanManageUser(actor, id, targetAuthority.IsOwner, targetAuthority.Permissions);
            if (id == actorUserId && !request.IsActive)
                throw new IdentityException(409, "You cannot deactivate your own account.");

            var account = await db.UserAccounts
                .Include(item => item.Person)
                .Include(item => item.UserRoles)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new IdentityException(404, "User was not found.");

            var deactivating = account.IsActive && !request.IsActive;
            if (deactivating)
                await IdentityAdministrationSupport.EnsureNotLastOwnerAsync(db, account.Id, cancellationToken);

            var profileChanged =
                !string.Equals(account.Person.FullName, fullName, StringComparison.Ordinal) ||
                !string.Equals(account.Person.PhoneNumber, phoneNumber, StringComparison.Ordinal);
            account.Person.FullName = fullName;
            account.Person.PhoneNumber = phoneNumber;
            account.IsActive = request.IsActive;
            if (deactivating)
            {
                account.AuthorizationVersion++;
                await support.RevokeSessionsAsync(db, account.Id, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            await IdentityAdministrationSupport.EnsureAdministrativeAccessAsync(db, cancellationToken);
            await support.WriteAuditAsync(
                db,
                transaction,
                new SecurityAuditEntry(
                    deactivating ? "Identity.UserDeactivated" : "Identity.UserUpdated",
                    "Succeeded",
                    "User",
                    ActorUserAccountId: actorUserId,
                    SubjectUserAccountId: account.Id,
                    SafeMetadata: JsonSerializer.Serialize(new
                    {
                        profileChanged,
                        active = account.IsActive
                    })),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetUserAsync(account.Id, cancellationToken)
                ?? throw new IdentityException(404, "User was not found.");
        }
        catch (Exception exception)
        {
            await IdentityAdministrationSupport.SafeRollbackAsync(transaction, cancellationToken);
            await support.TryWriteRejectionAuditAsync(actorUserId, "UpdateUser", exception, cancellationToken);
            throw;
        }
    }

    public async Task<AdminIdentityUser> AssignRolesAsync(
        Guid id,
        AssignAdminIdentityRolesRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var requestedRoleIds = (request.RoleIds ?? [])
            .Distinct()
            .ToArray();
        if (requestedRoleIds.Length == 0)
            throw new IdentityException(400, "At least one role is required.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await IdentityAdministrationSupport.LockAuthorizationStateAsync(db, cancellationToken);
            var actor = await support.RequireActorAsync(db, actorUserId, cancellationToken);
            var targetAuthority = await support.GetUserAuthorityAsync(db, id, cancellationToken)
                ?? throw new IdentityException(404, "User was not found.");
            var account = await db.UserAccounts
                .Include(item => item.UserRoles)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new IdentityException(404, "User was not found.");

            var roles = await db.Roles
                .Include(role => role.RolePermissions)
                .Where(role => requestedRoleIds.Contains(role.Id))
                .ToListAsync(cancellationToken);
            if (roles.Count != requestedRoleIds.Length)
                throw new IdentityException(400, "One or more roles do not exist.");

            var grants = roles
                .Select(role => new IdentityRoleGrant(
                    role.Id,
                    role.Code,
                    role.RolePermissions.Select(permission => permission.PermissionCode)
                        .ToHashSet(StringComparer.Ordinal)))
                .ToArray();
            IdentityDelegationPolicy.EnsureCanAssignRoles(
                actor,
                targetAuthority.IsOwner,
                targetAuthority.Permissions,
                grants);

            var ownerRoleId = roles.SingleOrDefault(role => role.Code == "Owner")?.Id;
            var currentlyOwner = ownerRoleId.HasValue &&
                                 account.UserRoles.Any(userRole => userRole.RoleId == ownerRoleId.Value);
            var willBeOwner = roles.Any(role => role.Code == "Owner");
            if (account.IsActive && currentlyOwner && !willBeOwner)
                await IdentityAdministrationSupport.EnsureNotLastOwnerAsync(db, account.Id, cancellationToken);

            var currentRoleIds = account.UserRoles
                .Select(userRole => userRole.RoleId)
                .ToHashSet();
            var targetRoleIds = roles
                .Select(role => role.Id)
                .ToHashSet();
            db.UserRoles.RemoveRange(
                account.UserRoles.Where(userRole => !targetRoleIds.Contains(userRole.RoleId)));
            foreach (var role in roles.Where(role => !currentRoleIds.Contains(role.Id)))
                db.UserRoles.Add(new UserRole { UserAccountId = account.Id, RoleId = role.Id });

            account.AuthorizationVersion++;
            await support.RevokeSessionsAsync(db, account.Id, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await IdentityAdministrationSupport.EnsureAdministrativeAccessAsync(db, cancellationToken);
            await support.WriteAuditAsync(
                db,
                transaction,
                new SecurityAuditEntry(
                    "Identity.UserRolesChanged",
                    "Succeeded",
                    "User",
                    ActorUserAccountId: actorUserId,
                    SubjectUserAccountId: account.Id,
                    SafeMetadata: JsonSerializer.Serialize(new
                    {
                        roles = roles.Select(role => role.Code).OrderBy(code => code, StringComparer.Ordinal)
                    })),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetUserAsync(account.Id, cancellationToken)
                ?? throw new IdentityException(404, "User was not found.");
        }
        catch (Exception exception)
        {
            await IdentityAdministrationSupport.SafeRollbackAsync(transaction, cancellationToken);
            await support.TryWriteRejectionAuditAsync(actorUserId, "AssignRoles", exception, cancellationToken);
            throw;
        }
    }

    private static IQueryable<UserAccount> BuildUserQuery(
        IdentityDbContext db,
        string? search,
        bool? isActive)
    {
        IQueryable<UserAccount> query = db.UserAccounts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 100)
                term = term[..100];
            var digits = new string(term.Where(char.IsAsciiDigit).ToArray());
            query = query.Where(account =>
                EF.Functions.ILike(account.Username, $"%{term}%") ||
                EF.Functions.ILike(account.Email, $"%{term}%") ||
                EF.Functions.ILike(account.Person.FullName, $"%{term}%") ||
                (account.Person.PhoneNumber != null &&
                 (EF.Functions.ILike(account.Person.PhoneNumber, $"%{term}%") ||
                  (digits.Length >= 4 && EF.Functions.ILike(account.Person.PhoneNumber, $"%{digits}%")))));
        }
        if (isActive.HasValue)
            query = query.Where(account => account.IsActive == isActive.Value);
        return query;
    }

    private static AdminIdentityUser MapUser(UserAccount account) => new(
        account.Id,
        account.Username,
        account.Email,
        account.Person.FullName,
        account.Person.PhoneNumber,
        account.IsActive,
        account.IsEmailVerified,
        account.AuthorizationVersion,
        account.UserRoles.Select(userRole => userRole.RoleId).ToList(),
        account.UserRoles.Select(userRole => userRole.Role.Code)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList());
}
