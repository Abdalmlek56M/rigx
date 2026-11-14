using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Identity;

/// <summary>
/// The Identity part of merging a duplicate customer account into the one to keep. Host runs it last, after the other
/// modules moved their records; everything here happens in one transaction.
/// </summary>
public sealed class CustomerMergeService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    ISecurityAuditTransactionWriter auditWriter,
    TimeProvider timeProvider)
{
    /// <summary>Checks the merge rules without changing anything (Host calls it before it moves any other module's data).</summary>
    public async Task EnsureCanMergeAsync(Guid targetId, Guid sourceId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var (target, source) = await LoadAsync(db, targetId, sourceId, cancellationToken);
        CustomerMergeRules.EnsureCanMerge(Party(target), Party(source));
    }

    public async Task MergeAsync(Guid targetId, Guid sourceId, Guid actorId, CustomerMergeCounts moved,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        Guid[] ids = [targetId, sourceId];
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM user_accounts WHERE id = ANY({ids}) ORDER BY id FOR UPDATE", cancellationToken);
        var (target, source) = await LoadAsync(db, targetId, sourceId, cancellationToken);
        CustomerMergeRules.EnsureCanMerge(Party(target), Party(source));

        var addresses = await MoveAddressesAsync(db, targetId, sourceId, cancellationToken);
        var googleMoved = await MoveGoogleLinkAsync(db, targetId, sourceId, cancellationToken);
        if (string.IsNullOrWhiteSpace(target.Person.PhoneNumber)) target.Person.PhoneNumber = source.Person.PhoneNumber;
        target.Person.CustomerNote = CustomerMergeRules.CombineNotes(target.Person.CustomerNote, source.Person.CustomerNote, source.Person.PersonCode);
        source.IsActive = false;
        source.MergedIntoAccountId = targetId;
        source.AuthorizationVersion++;
        await db.AuthSessions.Where(session => session.UserAccountId == sourceId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(session => session.RevokedAtUtc, now)
                .SetProperty(session => session.RevokeReason, "AccountMerged")
                .SetProperty(session => session.RowVersion, session => session.RowVersion + 1), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await auditWriter.WriteAsync(new SecurityAuditEntry("Customer.Merged", "Succeeded", "User",
                ActorUserAccountId: actorId, SubjectUserAccountId: targetId,
                SafeMetadata: JsonSerializer.Serialize(new
                {
                    sourceAccountId = sourceId, targetAccountId = targetId, moved.Orders, moved.Warranties, moved.Requests,
                    moved.SavedBuilds, addresses, googleMoved
                })),
            db.Database.GetDbConnection(), transaction.GetDbTransaction(), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>The source's default address loses its flag when the target already has a default (one default per account).</summary>
    private static async Task<int> MoveAddressesAsync(IdentityDbContext db, Guid targetId, Guid sourceId, CancellationToken cancellationToken)
    {
        if (await db.CustomerAddresses.AnyAsync(address => address.UserAccountId == targetId && address.IsDefault && !address.IsDeleted, cancellationToken))
            await db.CustomerAddresses.Where(address => address.UserAccountId == sourceId && address.IsDefault)
                .ExecuteUpdateAsync(setters => setters.SetProperty(address => address.IsDefault, false), cancellationToken);
        return await db.CustomerAddresses.Where(address => address.UserAccountId == sourceId && !address.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(address => address.UserAccountId, targetId), cancellationToken);
    }

    /// <summary>The Google link moves only when the target has none; otherwise the source's link is dropped.</summary>
    private static async Task<bool> MoveGoogleLinkAsync(IdentityDbContext db, Guid targetId, Guid sourceId, CancellationToken cancellationToken)
    {
        var targetHasLink = await db.ExternalLogins.AnyAsync(link => link.UserAccountId == targetId && link.Provider == Authentication.GoogleSignInService.Provider, cancellationToken);
        var sourceLinks = db.ExternalLogins.Where(link => link.UserAccountId == sourceId && link.Provider == Authentication.GoogleSignInService.Provider);
        if (targetHasLink)
        {
            await sourceLinks.ExecuteDeleteAsync(cancellationToken);
            return false;
        }
        return await sourceLinks.ExecuteUpdateAsync(setters => setters.SetProperty(link => link.UserAccountId, targetId), cancellationToken) > 0;
    }

    private static async Task<(UserAccount Target, UserAccount Source)> LoadAsync(IdentityDbContext db, Guid targetId, Guid sourceId,
        CancellationToken cancellationToken)
    {
        var accounts = await db.UserAccounts.Include(account => account.Person).Include(account => account.UserRoles).ThenInclude(userRole => userRole.Role)
            .Where(account => account.Id == targetId || account.Id == sourceId).ToListAsync(cancellationToken);
        var target = accounts.SingleOrDefault(account => account.Id == targetId) ?? throw new IdentityException(404, "Customer was not found.");
        var source = accounts.SingleOrDefault(account => account.Id == sourceId) ?? throw new IdentityException(404, "The account to merge was not found.");
        return (target, source);
    }

    private static MergeParty Party(UserAccount account) => new(account.Id, CustomerAccounts.IsCustomer(account.UserRoles.Select(userRole => userRole.Role.Code)),
        account.IsActive, account.MergedIntoAccountId);
}
