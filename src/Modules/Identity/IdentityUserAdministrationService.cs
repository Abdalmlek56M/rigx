using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HardwareStore.Modules.Identity;

/// <summary>User queries and commands for the identity administration surface.</summary>
public sealed class IdentityUserAdministrationService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IdentityAdministrationSupport support,
    TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone,
    IIdentityEmailDispatcher? emailDispatcher = null,
    IConfiguration? configuration = null,
    ILogger<IdentityUserAdministrationService>? logger = null)
{
    public async Task<AdminIdentityUserPage> GetUsersAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
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
        return new AdminIdentityUserPage(
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

    public async Task<AdminIdentityUserInvitationResult> CreateUserAsync(
        CreateAdminIdentityUserRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var username = IdentityAdministrationSupport.NormalizeRequired(request.Username, 128, "Username");
        var email = IdentityAdministrationSupport.NormalizeRequired(request.Email, 255, "Email");
        var fullName = IdentityAdministrationSupport.NormalizeRequired(request.FullName, 255, "Full name");
        var phoneNumber = IdentityAdministrationSupport.NormalizeOptional(request.PhoneNumber, 32, "Phone number");
        if (!new EmailAddressAttribute().IsValid(email))
            throw new IdentityAdministrationException(400, "Email is invalid.");
        var roleIds = (request.RoleIds ?? []).Distinct().ToArray();
        if (roleIds.Length == 0)
            throw new IdentityAdministrationException(400, "At least one role is required.");

        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await IdentityAdministrationSupport.LockAuthorizationStateAsync(db, cancellationToken);
            var actor = await support.RequireActorAsync(db, actorUserId, cancellationToken);
            var roles = await db.Roles
                .Include(role => role.RolePermissions)
                .Where(role => roleIds.Contains(role.Id))
                .ToListAsync(cancellationToken);
            if (roles.Count != roleIds.Length)
                throw new IdentityAdministrationException(400, "One or more roles do not exist.");

            var grants = roles.Select(role => new IdentityRoleGrant(
                role.Id,
                role.Code,
                role.RolePermissions.Select(permission => permission.PermissionCode)
                    .ToHashSet(StringComparer.Ordinal))).ToArray();
            IdentityDelegationPolicy.EnsureCanAssignRoles(actor, false, new HashSet<string>(), grants);

            var normalizedUsername = IdentityLoginService.NormalizeIdentifier(username);
            var normalizedEmail = IdentityLoginService.NormalizeIdentifier(email);
            var collision = await db.UserAccounts.AnyAsync(
                account => account.NormalizedUsername == normalizedUsername ||
                           account.NormalizedEmail == normalizedEmail,
                cancellationToken) ||
                await db.UserAccountLoginIdentifiers.AnyAsync(
                    identifier => identifier.NormalizedIdentifier == normalizedUsername ||
                                  identifier.NormalizedIdentifier == normalizedEmail,
                    cancellationToken);
            if (collision)
                throw new IdentityAdministrationException(409, "The username or email is already in use.");

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
            var account = new UserAccount
            {
                Id = Guid.CreateVersion7(),
                PersonId = person.Id,
                Person = person,
                Username = username,
                NormalizedUsername = normalizedUsername,
                Email = email,
                NormalizedEmail = normalizedEmail,
                PasswordHash = null,
                IsEmailVerified = false,
                IsActive = true,
                AuthorizationVersion = 1,
                CreatedAtUtc = now
            };
            db.UserAccounts.Add(account);
            await new UserAccountLoginIdentifierSynchronizer(db)
                .SetAsync(account, username, email, cancellationToken);
            foreach (var role in roles)
                db.UserRoles.Add(new UserRole { UserAccountId = account.Id, RoleId = role.Id });

            var token = IdentityTokenFactory.Create(
                account.Id, now, CustomerRegistrationService.EmailVerificationPurpose, TimeSpan.FromHours(24));
            db.IdentityTokens.Add(token.PersistenceRecord);
            await db.SaveChangesAsync(cancellationToken);
            await support.WriteAuditAsync(
                db,
                transaction,
                new SecurityAuditEntry(
                    "Identity.UserInvited",
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

            var dispatchQueued = false;
            if (emailDispatcher is not null && configuration is not null)
            {
                try
                {
                    var baseUrl = configuration["Identity:EmailVerificationBaseUrl"]
                        ?? "http://localhost:3000/verify-email";
                    if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var verificationBase))
                    {
                        var verificationUrl = new UriBuilder(verificationBase)
                        {
                            Fragment = $"token={Uri.EscapeDataString(token.RawToken)}"
                        }.Uri;
                        await emailDispatcher.EnqueueVerificationAsync(
                            account.Id,
                            new EmailVerificationMessage(account.Email, person.FullName, verificationUrl),
                            cancellationToken);
                        dispatchQueued = true;
                    }
                    else
                    {
                        logger?.LogWarning("Admin invitation email base URL is invalid; the account was created without a dispatched email.");
                    }
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested)
                {
                    logger?.LogWarning("Admin invitation email dispatch failed after the account was created.");
                }
            }
            else
            {
                logger?.LogWarning("Admin invitation email dispatcher is unavailable; the account was created without an email.");
            }

            var user = await GetUserAsync(account.Id, cancellationToken)
                ?? throw new IdentityAdministrationException(404, "User was not found.");
            return new AdminIdentityUserInvitationResult(user, dispatchQueued);
        }
        catch (Exception exception)
        {
            await IdentityAdministrationSupport.SafeRollbackAsync(transaction, cancellationToken);
            await support.TryWriteRejectionAuditAsync(actorUserId, "CreateUser", exception, cancellationToken);
            throw;
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
                ?? throw new IdentityAdministrationException(404, "User was not found.");
            IdentityDelegationPolicy.EnsureCanManageUser(actor, id, targetAuthority.IsOwner, targetAuthority.Permissions);
            if (id == actorUserId && !request.IsActive)
                throw new IdentityAdministrationException(409, "You cannot deactivate your own account.");

            var account = await db.UserAccounts
                .Include(item => item.Person)
                .Include(item => item.UserRoles)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new IdentityAdministrationException(404, "User was not found.");

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
                ?? throw new IdentityAdministrationException(404, "User was not found.");
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
            throw new IdentityAdministrationException(400, "At least one role is required.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await IdentityAdministrationSupport.LockAuthorizationStateAsync(db, cancellationToken);
            var actor = await support.RequireActorAsync(db, actorUserId, cancellationToken);
            var targetAuthority = await support.GetUserAuthorityAsync(db, id, cancellationToken)
                ?? throw new IdentityAdministrationException(404, "User was not found.");
            var account = await db.UserAccounts
                .Include(item => item.UserRoles)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new IdentityAdministrationException(404, "User was not found.");

            var roles = await db.Roles
                .Include(role => role.RolePermissions)
                .Where(role => requestedRoleIds.Contains(role.Id))
                .ToListAsync(cancellationToken);
            if (roles.Count != requestedRoleIds.Length)
                throw new IdentityAdministrationException(400, "One or more roles do not exist.");

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
                ?? throw new IdentityAdministrationException(404, "User was not found.");
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
        account.LockoutEndUtc,
        account.AuthorizationVersion,
        account.UserRoles.Select(userRole => userRole.RoleId).ToList(),
        account.UserRoles.Select(userRole => userRole.Role.Code)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList());
}
