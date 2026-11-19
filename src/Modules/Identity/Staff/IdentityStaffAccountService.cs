using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity;

/// <summary>The account part of a staff profile: when they joined, sessions, the internal note.</summary>
public sealed record StaffAccountView(Guid Id, string PersonCode, DateTimeOffset CreatedAtUtc, string? Note, int ActiveSessions,
    DateTimeOffset? LastActivityAtUtc);

public sealed record StaffNoteRequest(string? Note);

/// <summary>
/// Staff profile reads and the support actions on a staff account (internal note, sign out of every device). Both actions follow the
/// same delegation rule as editing the user, run under the authorization lock and are audited with the actor.
/// </summary>
public sealed class IdentityStaffAccountService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    IdentityAdministrationSupport support,
    TimeProvider timeProvider)
{
    public async Task<StaffAccountView?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking().Where(account => account.Id == id)
            .Select(account => new StaffAccountView(account.Id, account.Person.PersonCode, account.CreatedAtUtc, account.Person.CustomerNote,
                db.AuthSessions.Count(session => session.UserAccountId == account.Id && session.RevokedAtUtc == null &&
                    session.IdleExpiresAtUtc > now && session.AbsoluteExpiresAtUtc > now),
                db.AuthSessions.Where(session => session.UserAccountId == account.Id)
                    .Max(session => (DateTimeOffset?)session.LastActivityAtUtc)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task SetNoteAsync(Guid id, string? note, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var text = PersonNotes.Normalize(note);
        return RunAsync(id, actorUserId, "SetStaffNote", "Identity.UserNoteChanged", async (db, account) =>
        {
            account.Person.CustomerNote = text;
            await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    /// <summary>Ends every session of the account; the person signs in again with their email code or Google.</summary>
    public Task SignOutAllAsync(Guid id, Guid actorUserId, CancellationToken cancellationToken = default) =>
        RunAsync(id, actorUserId, "SignOutStaff", "Identity.UserSignedOutAll", async (db, account) =>
        {
            if (account.Id == actorUserId)
                throw new IdentityException(409, "Use the account page to sign out your own devices.");
            await support.RevokeSessionsAsync(db, account.Id, cancellationToken);
        }, cancellationToken);

    private async Task RunAsync(Guid id, Guid actorUserId, string operation, string eventType,
        Func<IdentityDbContext, UserAccount, Task> change, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await IdentityAdministrationSupport.LockAuthorizationStateAsync(db, cancellationToken);
            var actor = await support.RequireActorAsync(db, actorUserId, cancellationToken);
            var target = await support.GetUserAuthorityAsync(db, id, cancellationToken)
                ?? throw new IdentityException(404, "User was not found.");
            IdentityDelegationPolicy.EnsureCanManageUser(actor, id, target.IsOwner, target.Permissions);
            var account = await db.UserAccounts.Include(item => item.Person).SingleAsync(item => item.Id == id, cancellationToken);
            await change(db, account);
            await support.WriteAuditAsync(db, transaction,
                new SecurityAuditEntry(eventType, "Succeeded", "User", ActorUserAccountId: actorUserId, SubjectUserAccountId: id),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            await IdentityAdministrationSupport.SafeRollbackAsync(transaction, cancellationToken);
            await support.TryWriteRejectionAuditAsync(actorUserId, operation, exception, cancellationToken);
            throw;
        }
    }
}

/// <summary>The staff-only note kept on a person (customer or staff): trimmed, empty means none, at most 2000 characters.</summary>
public static class PersonNotes
{
    public const int MaxLength = 2000;

    public static string? Normalize(string? note)
    {
        var text = note?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        return text.Length > MaxLength ? throw new IdentityException(400, $"The note must be at most {MaxLength} characters.") : text;
    }
}
