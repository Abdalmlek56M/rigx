using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity;

/// <summary>Resolves account ids to the person's full name in one query (for screens that list who did something).</summary>
public sealed class UserDisplayNames(IDbContextFactory<IdentityDbContext> contextFactory)
{
    public async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken = default)
    {
        if (accountIds.Count == 0) return new Dictionary<Guid, string>();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking().Where(account => accountIds.Contains(account.Id))
            .Select(account => new { account.Id, account.Person.FullName })
            .ToDictionaryAsync(account => account.Id, account => account.FullName, cancellationToken);
    }
}
