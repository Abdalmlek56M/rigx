using System.Text.Json;
using HardwareStore.Common.Domain;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Identity;

public sealed record UpdateIdentityProfileRequest(string? FullName, string? PhoneNumber);

/// <summary>What a public page shows about an account: its name and avatar.</summary>
public sealed record AccountAuthor(Guid Id, string Name, string? AvatarStorageKey);

public sealed class IdentityProfileService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    ISecurityAuditTransactionWriter auditWriter)
{
    /// <summary>Full name of an active account, or null when the account is unknown (used to copy the seller name onto invoices).</summary>
    public async Task<string?> GetFullNameAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking().Where(item => item.Id == accountId && item.IsActive)
            .Select(item => item.Person.FullName).SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>The name and avatar of the active accounts among <paramref name="accountIds"/>, by account id (unknown or inactive accounts are left out).</summary>
    public async Task<IReadOnlyDictionary<Guid, AccountAuthor>> GetAuthorsAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default)
    {
        var ids = accountIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, AccountAuthor>();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking()
            .Where(account => ids.Contains(account.Id) && account.IsActive)
            .Select(account => new AccountAuthor(account.Id, account.Username, account.AvatarStorageKey))
            .ToDictionaryAsync(author => author.Id, cancellationToken);
    }

    /// <summary>Sets (or, with null, removes) the active account's avatar and returns the key it had before; 401 when the account is not active.</summary>
    public async Task<string?> ReplaceAvatarAsync(Guid accountId, string? storageKey, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.UserAccounts.SingleOrDefaultAsync(item => item.Id == accountId && item.IsActive, cancellationToken)
            ?? throw new UnauthenticatedException();
        var previousKey = account.AvatarStorageKey;
        account.AvatarStorageKey = storageKey;
        await db.SaveChangesAsync(cancellationToken);
        return previousKey;
    }

    public async Task UpdateAsync(
        Guid accountId,
        UpdateIdentityProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (accountId == Guid.Empty)
            throw new IdentityException(401, "The account is not authenticated.");
        var fullName = IdentityAdministrationSupport.NormalizeRequired(request.FullName, 255, "Full name");
        var phoneNumber = IdentityAdministrationSupport.NormalizeOptional(request.PhoneNumber, 32, "Phone number");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var account = await db.UserAccounts
                .Include(item => item.Person)
                .SingleOrDefaultAsync(item => item.Id == accountId && item.IsActive, cancellationToken)
                ?? throw new IdentityException(401, "The account is not active or does not exist.");
            var changed =
                !string.Equals(account.Person.FullName, fullName, StringComparison.Ordinal) ||
                !string.Equals(account.Person.PhoneNumber, phoneNumber, StringComparison.Ordinal);
            account.Person.FullName = fullName;
            account.Person.PhoneNumber = phoneNumber;
            await db.SaveChangesAsync(cancellationToken);
            await auditWriter.WriteAsync(
                new SecurityAuditEntry(
                    "Identity.ProfileUpdated",
                    "Succeeded",
                    "User",
                    ActorUserAccountId: accountId,
                    SubjectUserAccountId: accountId,
                    SafeMetadata: JsonSerializer.Serialize(new { changed })),
                db.Database.GetDbConnection(),
                transaction.GetDbTransaction(),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (IdentityException)
        {
            await IdentityAdministrationSupport.SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch
        {
            await IdentityAdministrationSupport.SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }
}
