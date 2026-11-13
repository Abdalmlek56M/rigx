using System.Security.Cryptography;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class IdentityAdministrationPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly FirstOwnerBootstrapRequest OwnerRequest = new(
        "rigx-owner",
        "owner@example.test",
        "RIGX Owner");

    [Fact]
    public async Task Admin_can_create_a_staff_role_assign_it_and_audit_every_change()
    {
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = (await database.CreateService().BootstrapAsync(OwnerRequest)).UserAccountId;
        var admin = database.CreateAdministrationService();

        var owner = await admin.GetUserAsync(ownerId);
        Assert.NotNull(owner);
        Assert.Contains("Owner", owner.RoleCodes);

        var createRole = await admin.CreateRoleAsync(
            new CreateAdminIdentityRoleRequest(
                "StoreManager",
                "Store Manager",
                ["Catalog.View", "Catalog.ManageProducts"]),
            ownerId);
        Assert.Equal("StoreManager", createRole.Code);
        Assert.Equal(2, createRole.PermissionCodes.Count);

        var staffId = await CreateUserAsync(database, "staff-user", "staff@example.test");
        var assigned = await admin.AssignRolesAsync(
            staffId,
            new AssignAdminIdentityRolesRequest([createRole.Id]),
            ownerId);
        Assert.Contains("StoreManager", assigned.RoleCodes);
        Assert.True(assigned.AuthorizationVersion > 0);

        var edited = await admin.UpdateRolePermissionsAsync(
            createRole.Id,
            new UpdateAdminIdentityRolePermissionsRequest(["Catalog.View"]),
            ownerId);
        Assert.Equal(new[] { "Catalog.View" }, edited.PermissionCodes);

        var users = await admin.GetUsersAsync("staff@example.test", null, 1, 10);
        Assert.Single(users.Items);
        Assert.Equal(staffId, users.Items[0].Id);

        await using var db = database.CreateIdentityContext();
        Assert.True((await db.AuthorizationStates.SingleAsync()).RbacRevision > 0);

        await using var audit = database.CreateAuditContext();
        var eventTypes = await audit.SecurityAuditEvents
            .Select(entry => entry.EventType)
            .ToListAsync();
        Assert.Contains("Identity.RoleCreated", eventTypes);
        Assert.Contains("Identity.UserRolesChanged", eventTypes);
        Assert.Contains("Identity.RolePermissionsChanged", eventTypes);
    }

    [Fact]
    public async Task Last_active_owner_is_protected_and_concurrent_deactivation_leaves_one_owner()
    {
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = (await database.CreateService().BootstrapAsync(OwnerRequest)).UserAccountId;
        var admin = database.CreateAdministrationService();
        var direct = await Assert.ThrowsAsync<IdentityException>(() =>
            admin.UpdateUserAsync(
                ownerId,
                new UpdateAdminIdentityUserRequest("RIGX Owner", null, false),
                ownerId));
        Assert.Equal(409, direct.StatusCode);

        var secondOwnerId = await CreateUserAsync(database, "second-owner", "second-owner@example.test");
        await AddOwnerRoleAsync(database, secondOwnerId);

        var attempts = await Task.WhenAll(
            CaptureAsync(admin, ownerId, ownerId),
            CaptureAsync(admin, secondOwnerId, ownerId));
        Assert.Equal(1, attempts.Count(attempt => attempt is null));
        Assert.Equal(1, attempts.Count(attempt => attempt?.StatusCode == 409));

        await using var db = database.CreateIdentityContext();
        var activeOwners = await db.UserRoles.CountAsync(
            item => item.Role.Code == "Owner" && item.UserAccount.IsActive);
        Assert.Equal(1, activeOwners);

        var current = await admin.GetUserAsync(ownerId);
        var roleRemoval = await Assert.ThrowsAsync<IdentityException>(() =>
            admin.AssignRolesAsync(
                ownerId,
                new AssignAdminIdentityRolesRequest([]),
                ownerId));
        Assert.Equal(400, roleRemoval.StatusCode);
        Assert.NotNull(current);
        Assert.NotEmpty(current.RoleCodes);
    }

    [Fact]
    public async Task Manager_cannot_escalate_beyond_own_permissions_or_touch_owners()
    {
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = (await database.CreateService().BootstrapAsync(OwnerRequest)).UserAccountId;
        var ownerAdmin = database.CreateAdministrationService();
        var managerRole = await ownerAdmin.CreateRoleAsync(
            new CreateAdminIdentityRoleRequest("UserManager", "User Manager", ["System.ManageUsers"]),
            ownerId);
        var powerRole = await ownerAdmin.CreateRoleAsync(
            new CreateAdminIdentityRoleRequest("PowerRole", "Power Role", ["Catalog.ManageProducts"]),
            ownerId);
        var managerId = await CreateUserAsync(database, "manager-user", "manager@example.test");
        await ownerAdmin.AssignRolesAsync(
            managerId,
            new AssignAdminIdentityRolesRequest([managerRole.Id]),
            ownerId);
        var powerUserId = await CreateUserAsync(database, "power-user", "power@example.test");
        await ownerAdmin.AssignRolesAsync(
            powerUserId,
            new AssignAdminIdentityRolesRequest([powerRole.Id]),
            ownerId);

        var managerAdmin = database.CreateAdministrationService();
        var grantDenied = await Assert.ThrowsAsync<IdentityException>(() =>
            managerAdmin.CreateRoleAsync(
                new CreateAdminIdentityRoleRequest("Escalated", "Escalated", ["Catalog.View"]),
                managerId));
        Assert.Equal(403, grantDenied.StatusCode);

        var ownerRole = (await ownerAdmin.GetRolesAsync()).Single(role => role.Code == "Owner");
        var ownerAssignmentDenied = await Assert.ThrowsAsync<IdentityException>(() =>
            managerAdmin.AssignRolesAsync(
                managerId,
                new AssignAdminIdentityRolesRequest([ownerRole.Id]),
                managerId));
        Assert.Equal(403, ownerAssignmentDenied.StatusCode);

        var ownerManagementDenied = await Assert.ThrowsAsync<IdentityException>(() =>
            managerAdmin.UpdateUserAsync(
                ownerId,
                new UpdateAdminIdentityUserRequest("RIGX Owner", null, false),
                managerId));
        Assert.Equal(403, ownerManagementDenied.StatusCode);

        var powerRoleEditDenied = await Assert.ThrowsAsync<IdentityException>(() =>
            managerAdmin.UpdateRolePermissionsAsync(
                powerRole.Id,
                new UpdateAdminIdentityRolePermissionsRequest(["Catalog.View"]),
                managerId));
        Assert.Equal(403, powerRoleEditDenied.StatusCode);

        var morePrivilegedTargetDenied = await Assert.ThrowsAsync<IdentityException>(() =>
            managerAdmin.UpdateUserAsync(
                powerUserId,
                new UpdateAdminIdentityUserRequest("Power User", null, false),
                managerId));
        Assert.Equal(403, morePrivilegedTargetDenied.StatusCode);

        var selfEscalationDenied = await Assert.ThrowsAsync<IdentityException>(() =>
            managerAdmin.AssignRolesAsync(
                managerId,
                new AssignAdminIdentityRolesRequest([managerRole.Id, powerRole.Id]),
                managerId));
        Assert.Equal(403, selfEscalationDenied.StatusCode);

        var selfDeactivationDenied = await Assert.ThrowsAsync<IdentityException>(() =>
            managerAdmin.UpdateUserAsync(
                managerId,
                new UpdateAdminIdentityUserRequest("Manager User", null, false),
                managerId));
        Assert.Equal(409, selfDeactivationDenied.StatusCode);

        await using var audit = database.CreateAuditContext();
        Assert.Contains(
            "Identity.AdministrationRejected",
            await audit.SecurityAuditEvents.Select(entry => entry.EventType).ToListAsync());
    }

    [Fact]
    public async Task Last_owner_cannot_replace_their_role_and_rejection_preserves_state()
    {
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = (await database.CreateService().BootstrapAsync(OwnerRequest)).UserAccountId;
        var admin = database.CreateAdministrationService();
        var supportRole = await admin.CreateRoleAsync(
            new CreateAdminIdentityRoleRequest("Support", "Support", []),
            ownerId);

        await using (var db = database.CreateIdentityContext())
        {
            var state = await db.AuthorizationStates.SingleAsync();
            db.AuthSessions.Add(new AuthSession
            {
                SessionKeyHash = RandomNumberGenerator.GetBytes(32),
                UserAccountId = ownerId,
                ProtectedTicket = [1, 2, 3],
                CreatedAtUtc = DateTimeOffset.UtcNow,
                LastActivityAtUtc = DateTimeOffset.UtcNow,
                IdleExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
                AbsoluteExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(8),
                IdleTimeoutSeconds = 1800,
                AuthorizationVersion = 1,
                RbacRevision = state.RbacRevision,
                RowVersion = 1
            });
            await db.SaveChangesAsync();
        }

        await using (var db = database.CreateIdentityContext())
        {
            var before = await db.UserAccounts.SingleAsync(account => account.Id == ownerId);
            var beforeVersion = before.AuthorizationVersion;
            var beforeRevision = (await db.AuthorizationStates.SingleAsync()).RbacRevision;

            var rejected = await Assert.ThrowsAsync<IdentityException>(() =>
                admin.AssignRolesAsync(
                    ownerId,
                    new AssignAdminIdentityRolesRequest([supportRole.Id]),
                    ownerId));
            Assert.Equal(409, rejected.StatusCode);

            var after = await db.UserAccounts.SingleAsync(account => account.Id == ownerId);
            Assert.Equal(beforeVersion, after.AuthorizationVersion);
            Assert.Equal(beforeRevision, (await db.AuthorizationStates.SingleAsync()).RbacRevision);
            Assert.True(await db.UserRoles.AnyAsync(role => role.UserAccountId == ownerId && role.Role.Code == "Owner"));
            Assert.True(await db.AuthSessions.AnyAsync(session => session.UserAccountId == ownerId && session.RevokedAtUtc == null));
        }
    }

    [Fact]
    public async Task Concurrent_manager_permission_removal_preserves_owner_admin_access()
    {
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = (await database.CreateService().BootstrapAsync(OwnerRequest)).UserAccountId;
        var roleA = await CreateRoleWithManageUsersAsync(database, "ManagerA");
        var roleB = await CreateRoleWithManageUsersAsync(database, "ManagerB");
        var managerA = await CreateUserAsync(database, "manager-a", "manager-a@example.test", roleA.Id);
        var managerB = await CreateUserAsync(database, "manager-b", "manager-b@example.test", roleB.Id);
        var admin = database.CreateAdministrationService();

        var attempts = await Task.WhenAll(
            CaptureRoleUpdateAsync(admin, roleB.Id, [], managerA),
            CaptureRoleUpdateAsync(admin, roleA.Id, [], managerB));

        Assert.Contains(attempts, attempt => attempt is null);

        await using var db = database.CreateIdentityContext();
        Assert.True(await db.RolePermissions.AnyAsync(
            permission => permission.PermissionCode == "System.ManageUsers" &&
                          permission.Role.Code == "Owner"));
        Assert.True(await db.UserRoles.AnyAsync(
            userRole => userRole.UserAccountId == ownerId &&
                        userRole.Role.Code == "Owner" &&
                        userRole.UserAccount.IsActive));
    }

    [Fact]
    public async Task Reassigning_the_same_role_is_idempotent()
    {
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = (await database.CreateService().BootstrapAsync(OwnerRequest)).UserAccountId;
        var admin = database.CreateAdministrationService();
        var role = await admin.CreateRoleAsync(
            new CreateAdminIdentityRoleRequest("Support", "Support", []),
            ownerId);
        var userId = await CreateUserAsync(
            database,
            "support-user",
            "support-user@example.test",
            role.Id);

        var first = await admin.AssignRolesAsync(
            userId,
            new AssignAdminIdentityRolesRequest([role.Id]),
            ownerId);
        Assert.Contains(role.Id, first.RoleIds);

        var second = await admin.AssignRolesAsync(
            userId,
            new AssignAdminIdentityRolesRequest([role.Id]),
            ownerId);
        Assert.Contains(role.Id, second.RoleIds);
        Assert.Single(second.RoleIds);
    }

    [Fact]
    public async Task Admin_invites_staff_by_email_without_password_or_link_token()
    {
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = (await database.CreateService().BootstrapAsync(OwnerRequest)).UserAccountId;
        var dispatcher = new CapturingEmailDispatcher();
        var admin = database.CreateAdministrationService(dispatcher);
        var role = await admin.CreateRoleAsync(
            new CreateAdminIdentityRoleRequest("Support", "Support", []),
            ownerId);

        var result = await admin.CreateUserAsync(
            new CreateAdminIdentityUserRequest(
                "staff-user",
                "staff-user@example.test",
                "Staff User",
                "+201000000000",
                [role.Id]),
            ownerId);

        Assert.True(result.EmailDispatchQueued);
        Assert.False(result.User.IsEmailVerified);
        Assert.Contains("Support", result.User.RoleCodes);
        await using (var db = database.CreateIdentityContext())
        {
            var person = await db.Persons.SingleAsync(item => item.FullName == "Staff User");
            Assert.True(person.IsEmployee);
            Assert.False(person.IsCustomer);
            Assert.False(person.IsSupplier);
        }
        var message = Assert.Single(dispatcher.Invitations);
        Assert.Equal("staff-user@example.test", message.Email);
        Assert.Equal(["Support"], message.Roles);
        Assert.Equal(IdentityLinks.Local.SignIn, message.SignInUrl);
        Assert.Empty(message.SignInUrl.Fragment);

        var duplicate = await Assert.ThrowsAsync<IdentityException>(() =>
            admin.CreateUserAsync(
                new CreateAdminIdentityUserRequest(
                    "staff-user-2",
                    "staff-user@example.test",
                    "Staff User 2",
                    null,
                    [role.Id]),
                ownerId));
        Assert.Equal(409, duplicate.StatusCode);
    }

    [Fact]
    public async Task Invalid_permissions_and_reserved_roles_are_rejected()
    {
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = (await database.CreateService().BootstrapAsync(OwnerRequest)).UserAccountId;
        var admin = database.CreateAdministrationService();

        var unknown = await Assert.ThrowsAsync<IdentityException>(() =>
            admin.CreateRoleAsync(
                new CreateAdminIdentityRoleRequest("BadRole", "Bad role", ["Not.A.Permission"]),
                ownerId));
        Assert.Equal(400, unknown.StatusCode);

        var reserved = await Assert.ThrowsAsync<IdentityException>(() =>
            admin.CreateRoleAsync(
                new CreateAdminIdentityRoleRequest("Owner", "Another owner", []),
                ownerId));
        Assert.Equal(409, reserved.StatusCode);

        var roles = await admin.GetRolesAsync();
        var ownerRole = roles.Single(role => role.Code == "Owner");
        var ownerEdit = await Assert.ThrowsAsync<IdentityException>(() =>
            admin.UpdateRolePermissionsAsync(
                ownerRole.Id,
                new UpdateAdminIdentityRolePermissionsRequest(["Catalog.View"]),
                ownerId));
        Assert.Equal(409, ownerEdit.StatusCode);
    }

    private static async Task<IdentityException?> CaptureAsync(
        IdentityAdministrationService admin,
        Guid targetUserId,
        Guid actorUserId)
    {
        try
        {
            await admin.UpdateUserAsync(
                targetUserId,
                new UpdateAdminIdentityUserRequest("Owner", null, false),
                actorUserId);
            return null;
        }
        catch (IdentityException exception)
        {
            return exception;
        }
    }

    private static async Task<Guid> CreateUserAsync(
        OwnerBootstrapDatabase database,
        string username,
        string email,
        Guid? roleId = null)
    {
        await using var db = database.CreateIdentityContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var person = new Person
        {
            Id = Guid.CreateVersion7(),
            FullName = username,
            Email = email,
            IsEmployee = true,
            IsActive = true
        };
        await new PersonCodeGenerator(db, TimeProvider.System, TimeZoneInfo.Utc)
            .CreatePersonInCurrentTransactionAsync(person);
        var account = new UserAccount
        {
            Id = Guid.CreateVersion7(),
            PersonId = person.Id,
            Person = person,
            Username = username,
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
            Email = email,
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier(email),
            IsEmailVerified = false,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.UserAccounts.Add(account);
        if (roleId.HasValue)
            db.UserRoles.Add(new UserRole { UserAccountId = account.Id, RoleId = roleId.Value });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return account.Id;
    }

    private static async Task<Role> CreateRoleWithManageUsersAsync(
        OwnerBootstrapDatabase database,
        string code)
    {
        await using var db = database.CreateIdentityContext();
        var role = new Role
        {
            Id = Guid.CreateVersion7(),
            Code = code,
            Name = code
        };
        db.Roles.Add(role);
        db.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            PermissionCode = "System.ManageUsers"
        });
        await db.SaveChangesAsync();
        return role;
    }

    private static async Task<IdentityException?> CaptureRoleUpdateAsync(
        IdentityAdministrationService admin,
        Guid roleId,
        string[] permissions,
        Guid actorUserId)
    {
        try
        {
            await admin.UpdateRolePermissionsAsync(
                roleId,
                new UpdateAdminIdentityRolePermissionsRequest(permissions),
                actorUserId);
            return null;
        }
        catch (IdentityException exception)
        {
            return exception;
        }
    }

    private static async Task AddOwnerRoleAsync(OwnerBootstrapDatabase database, Guid userId)
    {
        await using var db = database.CreateIdentityContext();
        var ownerRole = await db.Roles.SingleAsync(role => role.Code == "Owner");
        db.UserRoles.Add(new UserRole { UserAccountId = userId, RoleId = ownerRole.Id });
        await db.SaveChangesAsync();
    }
}

internal sealed class CapturingEmailDispatcher : IIdentityEmailDispatcher
{
    public List<StaffInvitationMessage> Invitations { get; } = [];

    public ValueTask EnqueueStaffInvitationAsync(
        Guid userAccountId,
        StaffInvitationMessage message,
        CancellationToken cancellationToken = default)
    {
        Invitations.Add(message);
        return ValueTask.CompletedTask;
    }
}
