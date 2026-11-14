using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>One posting to a money account. The amount is positive; the direction is the method called.</summary>
internal sealed record MoneyEntry(string Source, Guid SourceId, decimal Amount, string? Reference, string? Note, DateOnly Date,
    Guid ActorId, DateTimeOffset Now);

/// <summary>
/// The only place that adds money movements. Outflows lock the account row and refuse to take it below zero, so the caller must be
/// inside a transaction. Postings are added to the context; the caller saves.
/// </summary>
internal static class MoneyLedger
{
    /// <summary>The account the money goes to or comes from: the chosen active one, else the first active account of the kind.</summary>
    public static async Task<MoneyAccount> ResolveAsync(RetailDbContext db, Guid? accountId, string kind, CancellationToken cancellationToken)
    {
        if (accountId is Guid id)
        {
            var chosen = await db.MoneyAccounts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new RetailException(404, "The money account was not found.");
            return chosen.IsActive ? chosen : throw new RetailException(409, "The money account is not active.");
        }
        return await db.MoneyAccounts.Where(item => item.IsActive && item.Kind == kind)
                   .OrderBy(item => item.SortOrder).ThenBy(item => item.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken)
               ?? throw new RetailException(409, $"There is no active {kind} money account.");
    }

    public static async Task<decimal> BalanceAsync(RetailDbContext db, MoneyAccount account, CancellationToken cancellationToken) =>
        decimal.Round(account.OpeningBalance + await db.MoneyMovements.Where(item => item.AccountId == account.Id)
            .SumAsync(item => item.Amount, cancellationToken), 2);

    /// <summary>Adds money to an account and returns the account used.</summary>
    public static async Task<Guid> PostInAsync(RetailDbContext db, Guid? accountId, string kind, MoneyEntry entry, CancellationToken cancellationToken)
    {
        var account = await ResolveAsync(db, accountId, kind, cancellationToken);
        db.MoneyMovements.Add(Movement(account.Id, entry.Amount, entry));
        return account.Id;
    }

    /// <summary>Takes money out of an account (409 when the balance is not enough) and returns the account used.</summary>
    public static async Task<Guid> PostOutAsync(RetailDbContext db, Guid? accountId, string kind, MoneyEntry entry, CancellationToken cancellationToken)
    {
        var account = await ResolveAsync(db, accountId, kind, cancellationToken);
        await LockAsync(db, account.Id, cancellationToken);
        MoneyRules.EnsureCanPay(await BalanceAsync(db, account, cancellationToken), entry.Amount);
        db.MoneyMovements.Add(Movement(account.Id, -entry.Amount, entry));
        return account.Id;
    }

    private static Task LockAsync(RetailDbContext db, Guid id, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync("SELECT 1 FROM money_accounts WHERE id = {0} FOR UPDATE", new object[] { id }, cancellationToken);

    private static MoneyMovement Movement(Guid accountId, decimal signedAmount, MoneyEntry entry) => new()
    {
        Id = Guid.CreateVersion7(), AccountId = accountId, Amount = signedAmount, Source = entry.Source, SourceId = entry.SourceId,
        Reference = entry.Reference, Note = entry.Note, OccurredOn = entry.Date, ActorUserAccountId = entry.ActorId, CreatedAtUtc = entry.Now
    };
}
