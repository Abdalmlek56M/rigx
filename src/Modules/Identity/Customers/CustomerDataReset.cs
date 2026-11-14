using HardwareStore.Common.Domain;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Identity;

/// <summary>
/// The Identity part of the "customers" reset: customer accounts (accounts holding only the Customer role) with their sessions,
/// addresses, notifications, restrictions, email-change requests, Google links, pending sign-in codes and person records.
/// Staff and owner accounts, roles, permissions and the person-code counters are kept.
/// </summary>
public sealed class CustomerDataReset(IDbContextFactory<IdentityDbContext> contextFactory) : IDataResetSource
{
    public async Task<int> ResetAsync(IReadOnlySet<string> sections, CancellationToken cancellationToken = default)
    {
        if (!sections.Contains(DataResetRules.Customers)) return 0;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var customers = await db.UserAccounts.Customers().Select(account => new { account.Id, account.PersonId, account.NormalizedEmail })
            .ToListAsync(cancellationToken);
        var accountIds = customers.Select(customer => customer.Id).ToList();
        var personIds = customers.Select(customer => customer.PersonId).ToList();
        var emails = customers.Select(customer => customer.NormalizedEmail).ToList();

        var deleted = 0;
        deleted += await db.AuthSessions.Where(row => accountIds.Contains(row.UserAccountId)).ExecuteDeleteAsync(cancellationToken);
        deleted += await db.CustomerAddresses.Where(row => accountIds.Contains(row.UserAccountId)).ExecuteDeleteAsync(cancellationToken);
        deleted += await db.UserNotifications.Where(row => accountIds.Contains(row.UserAccountId)).ExecuteDeleteAsync(cancellationToken);
        deleted += await db.AccountRestrictions.Where(row => accountIds.Contains(row.UserAccountId)).ExecuteDeleteAsync(cancellationToken);
        deleted += await db.EmailChangeRequests.Where(row => accountIds.Contains(row.UserAccountId)).ExecuteDeleteAsync(cancellationToken);
        deleted += await db.ExternalLogins.Where(row => accountIds.Contains(row.UserAccountId)).ExecuteDeleteAsync(cancellationToken);
        deleted += await db.EmailCodes.Where(row => emails.Contains(row.NormalizedEmail)).ExecuteDeleteAsync(cancellationToken);
        deleted += await db.UserRoles.Where(row => accountIds.Contains(row.UserAccountId)).ExecuteDeleteAsync(cancellationToken);
        deleted += await db.UserAccounts.Where(account => accountIds.Contains(account.Id)).ExecuteDeleteAsync(cancellationToken);
        deleted += await db.Persons.Where(person => personIds.Contains(person.Id)).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return deleted;
    }
}
