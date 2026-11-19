using System.Security.Cryptography;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>Staff profile support actions (internal note, sign out everywhere) and the staff ↔ customer link.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class StaffProfileTests(OwnerBootstrapPostgreSqlFixture fixture) : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly FirstOwnerBootstrapRequest OwnerRequest = new("rigx-owner", "owner@example.test", "RIGX Owner");

    private static Task<Guid> StaffAsync(OwnerBootstrapDatabase database, string email, params string[] permissions) =>
        EmailCodeSignInTests.CreateAccountAsync(database, email, verified: true, active: true, roleCode: $"{email}-role", permissions);

    private static async Task SetContactAsync(OwnerBootstrapDatabase database, Guid accountId, string? phone, string? personEmail = null)
    {
        await using var db = database.CreateIdentityContext();
        var person = await db.UserAccounts.Where(account => account.Id == accountId).Select(account => account.Person).SingleAsync();
        person.PhoneNumber = phone;
        if (personEmail is not null) person.Email = personEmail;
        await db.SaveChangesAsync();
    }

    private static async Task AddSessionAsync(OwnerBootstrapDatabase database, Guid accountId)
    {
        await using var db = database.CreateIdentityContext();
        var now = DateTimeOffset.UtcNow;
        db.AuthSessions.Add(new AuthSession
        {
            SessionKeyHash = RandomNumberGenerator.GetBytes(32), UserAccountId = accountId, ProtectedTicket = [1, 2, 3],
            CreatedAtUtc = now, LastActivityAtUtc = now, IdleExpiresAtUtc = now.AddHours(1), AbsoluteExpiresAtUtc = now.AddHours(8),
            IdleTimeoutSeconds = 1800, AuthorizationVersion = 1, RbacRevision = (await db.AuthorizationStates.SingleAsync()).RbacRevision, RowVersion = 1
        });
        await db.SaveChangesAsync();
    }

    private static async Task<List<string>> AuditTypesAsync(OwnerBootstrapDatabase database)
    {
        await using var audit = database.CreateAuditContext();
        return await audit.SecurityAuditEvents.Select(entry => entry.EventType).ToListAsync();
    }

    [Fact]
    public async Task Staff_note_is_trimmed_cleared_limited_and_audited()
    {
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = (await database.CreateService().BootstrapAsync(OwnerRequest)).UserAccountId;
        var admin = database.CreateAdministrationService();
        var staffId = await StaffAsync(database, "note-staff@example.test");

        await admin.SetStaffNoteAsync(staffId, "  covers weekends  ", ownerId);
        Assert.Equal("covers weekends", (await admin.GetStaffAccountAsync(staffId))!.Note);

        await admin.SetStaffNoteAsync(staffId, "   ", ownerId);
        Assert.Null((await admin.GetStaffAccountAsync(staffId))!.Note);

        var tooLong = await Assert.ThrowsAsync<IdentityException>(() => admin.SetStaffNoteAsync(staffId, new string('x', 2001), ownerId));
        Assert.Equal(400, tooLong.StatusCode);
        Assert.Contains("Identity.UserNoteChanged", await AuditTypesAsync(database));
    }

    [Fact]
    public async Task Sign_out_everywhere_revokes_sessions_and_refuses_own_account_and_owner_for_a_manager()
    {
        var database = await fixture.CreateDatabaseAsync();
        var ownerId = (await database.CreateService().BootstrapAsync(OwnerRequest)).UserAccountId;
        var admin = database.CreateAdministrationService();
        var staffId = await StaffAsync(database, "signout-staff@example.test");
        var managerId = await StaffAsync(database, "manager@example.test", "System.ManageUsers");
        await AddSessionAsync(database, staffId);
        await AddSessionAsync(database, staffId);
        Assert.Equal(2, (await admin.GetStaffAccountAsync(staffId))!.ActiveSessions);

        await admin.SignOutStaffEverywhereAsync(staffId, ownerId);

        Assert.Equal(0, (await admin.GetStaffAccountAsync(staffId))!.ActiveSessions);
        await using (var db = database.CreateIdentityContext())
            Assert.Equal(0, await db.AuthSessions.CountAsync(session => session.UserAccountId == staffId && session.RevokedAtUtc == null));
        Assert.Contains("Identity.UserSignedOutAll", await AuditTypesAsync(database));

        var own = await Assert.ThrowsAsync<IdentityException>(() => admin.SignOutStaffEverywhereAsync(ownerId, ownerId));
        Assert.Equal(409, own.StatusCode);
        var owner = await Assert.ThrowsAsync<IdentityException>(() => admin.SignOutStaffEverywhereAsync(ownerId, managerId));
        Assert.Equal(403, owner.StatusCode);
    }

    [Fact]
    public async Task Link_follows_shared_phone_or_email_in_both_directions_and_skips_others_and_merged_customers()
    {
        var database = await fixture.CreateDatabaseAsync();
        await database.CreateService().BootstrapAsync(OwnerRequest);
        var links = new StaffCustomerLinkService(new LinkContextFactory(database));
        var staff = await StaffAsync(database, "link-staff@example.test");
        await SetContactAsync(database, staff, "+20 101 234 5678");
        var byPhone = await CustomerAdminTests.CustomerAsync(database, "by-phone", "01012345678");
        var byEmail = await CustomerAdminTests.CustomerAsync(database, "by-email");
        await SetContactAsync(database, byEmail, null, "LINK-Staff@Example.test");
        var unrelated = await CustomerAdminTests.CustomerAsync(database, "unrelated", "01199999999");
        var merged = await CustomerAdminTests.CustomerAsync(database, "merged", "01012345678");
        await using (var db = database.CreateIdentityContext())
        {
            (await db.UserAccounts.SingleAsync(account => account.Id == merged)).MergedIntoAccountId = byPhone;
            await db.SaveChangesAsync();
        }

        var customers = await links.CustomersOfStaffAsync(staff);
        Assert.Equal(new[] { byPhone, byEmail }.Order(), customers.Select(item => item.Id).Order());
        Assert.DoesNotContain(customers, item => item.Id == unrelated || item.Id == merged);

        Assert.Equal([staff], (await links.StaffOfCustomerAsync(byPhone)).Select(item => item.Id));
        Assert.Equal([staff], (await links.StaffOfCustomerAsync(byEmail)).Select(item => item.Id));
        Assert.Empty(await links.StaffOfCustomerAsync(unrelated));
    }

    private sealed class LinkContextFactory(OwnerBootstrapDatabase database) : IDbContextFactory<IdentityDbContext>
    {
        public IdentityDbContext CreateDbContext() => database.CreateIdentityContext();
    }
}
