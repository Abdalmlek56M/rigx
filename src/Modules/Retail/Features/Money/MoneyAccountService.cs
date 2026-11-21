using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Money accounts (cash drawer, InstaPay, wallet, bank): list with balances, edit, movements and transfers between accounts.</summary>
public sealed class MoneyAccountService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider, TimeZoneInfo businessZone)
{
    public async Task<IReadOnlyList<MoneyAccountView>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var accounts = await db.MoneyAccounts.AsNoTracking().OrderBy(item => item.SortOrder).ThenBy(item => item.Name).ToListAsync(cancellationToken);
        var moved = await db.MoneyMovements.AsNoTracking().GroupBy(item => item.AccountId)
            .Select(group => new { group.Key, Total = group.Sum(item => item.Amount) })
            .ToDictionaryAsync(item => item.Key, item => item.Total, cancellationToken);
        return accounts.Select(item => View(item, item.OpeningBalance + moved.GetValueOrDefault(item.Id))).ToList();
    }

    public async Task<MoneyAccountView> CreateAsync(MoneyAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = new MoneyAccount
        {
            Id = Guid.CreateVersion7(), Name = MoneyRules.Name(request.Name), Kind = MoneyRules.Kind(request.Kind),
            OpeningBalance = MoneyRules.Opening(request.OpeningBalance), OpeningDate = request.OpeningDate ?? Today(),
            IsActive = request.IsActive, SortOrder = request.SortOrder, CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
        };
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.MoneyAccounts.Add(account);
        await SaveAsync(db, cancellationToken);
        return View(account, account.OpeningBalance);
    }

    public async Task<MoneyAccountView> UpdateAsync(Guid id, MoneyAccountRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.MoneyAccounts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RetailException(404, "The money account was not found.");
        if (request.RowVersion != account.RowVersion) throw new RetailException(409, "The account was changed by another user.");
        account.Name = MoneyRules.Name(request.Name);
        account.OpeningBalance = MoneyRules.Opening(request.OpeningBalance);
        account.OpeningDate = request.OpeningDate ?? account.OpeningDate;
        account.IsActive = request.IsActive;
        account.SortOrder = request.SortOrder;
        account.RowVersion++;
        await SaveAsync(db, cancellationToken);
        return View(account, await MoneyLedger.BalanceAsync(db, account, cancellationToken));
    }

    /// <summary>
    /// Deletes an account that was never used (no movement, no expense) and is not the last active account of its kind, since payments
    /// of that kind would have nowhere to go; any other account is deactivated instead (409).
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await db.MoneyAccounts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RetailException(404, "The money account was not found.");
        if (await db.MoneyMovements.AnyAsync(item => item.AccountId == id, cancellationToken))
            throw new RetailException(409, "This account has movements, so it cannot be deleted. Deactivate it instead.");
        if (account.IsActive && !await db.MoneyAccounts.AnyAsync(item => item.Id != id && item.Kind == account.Kind && item.IsActive, cancellationToken))
            throw new RetailException(409, "This is the only active account of its kind, so it cannot be deleted.");
        db.MoneyAccounts.Remove(account);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new RetailException(409, "This account is in use, so it cannot be deleted. Deactivate it instead.");
        }
    }

    public async Task<IReadOnlyList<MoneyMovementView>> ListMovementsAsync(Guid accountId, DateOnly? from, DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.MoneyAccounts.AnyAsync(item => item.Id == accountId, cancellationToken))
            throw new RetailException(404, "The money account was not found.");
        var query = db.MoneyMovements.AsNoTracking().Where(item => item.AccountId == accountId);
        if (from is not null) query = query.Where(item => item.OccurredOn >= from);
        if (to is not null) query = query.Where(item => item.OccurredOn <= to);
        return (await query.OrderByDescending(item => item.OccurredOn).ThenByDescending(item => item.CreatedAtUtc).Take(500)
                .ToListAsync(cancellationToken))
            .Select(item => new MoneyMovementView(item.Id, item.AccountId, item.Amount, item.Source, item.SourceId, item.Reference,
                item.Note, item.OccurredOn, item.ActorUserAccountId, item.CreatedAtUtc)).ToList();
    }

    /// <summary>Moves money between two different active accounts in one transaction; refused when the source balance is too small.</summary>
    public async Task TransferAsync(MoneyTransferRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var amount = MoneyRules.Amount(request.Amount);
        if (request.FromAccountId == request.ToAccountId) throw new RetailException(400, "Choose two different accounts.");
        var note = OrderRules.Optional(request.Note, 500, "Note");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var entry = new MoneyEntry(MoneySource.TransferOut, Guid.CreateVersion7(), amount, null, note, request.Date ?? Today(), actorId, now);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await MoneyLedger.PostOutAsync(db, request.FromAccountId, MoneyAccountKind.Cash, entry, cancellationToken);
            await MoneyLedger.PostInAsync(db, request.ToAccountId, MoneyAccountKind.Cash, entry with { Source = MoneySource.TransferIn }, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    /// <summary>Refuses (409) when the default account of the method kind cannot cover the amount; used before receiving a purchase.</summary>
    public async Task EnsureCanPayAsync(string? method, decimal amount, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var account = await MoneyLedger.ResolveAsync(db, null, MoneyRules.KindForMethod(method), cancellationToken);
        MoneyRules.EnsureCanPay(await MoneyLedger.BalanceAsync(db, account, cancellationToken), decimal.Round(amount, 2));
    }

    /// <summary>Pays the seller of a received purchase from the default account of the method kind; a repeated call does nothing.</summary>
    public async Task PayPurchaseAsync(Guid purchaseId, string code, string? method, decimal amount, DateOnly date, Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.MoneyMovements.AnyAsync(item => item.Source == MoneySource.Purchase && item.SourceId == purchaseId, cancellationToken)) return;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var entry = new MoneyEntry(MoneySource.Purchase, purchaseId, MoneyRules.Amount(amount), code, null, date, actorId,
                timeProvider.GetUtcNow().ToUniversalTime());
            await MoneyLedger.PostOutAsync(db, null, MoneyRules.KindForMethod(method), entry, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    /// <summary>Pays the seller a further amount of a received purchase (each payment is its own movement).</summary>
    public async Task PaySellerAsync(string code, string? method, decimal amount, DateOnly date, Guid actorId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var entry = new MoneyEntry(MoneySource.Purchase, Guid.CreateVersion7(), MoneyRules.Amount(amount), code, null, date, actorId,
                timeProvider.GetUtcNow().ToUniversalTime());
            await MoneyLedger.PostOutAsync(db, null, MoneyRules.KindForMethod(method), entry, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    private DateOnly Today() => BusinessDay.Today(timeProvider, businessZone);

    private static MoneyAccountView View(MoneyAccount item, decimal balance) => new(item.Id, item.Name, item.Kind, item.OpeningBalance,
        item.OpeningDate, item.IsActive, item.SortOrder, decimal.Round(balance, 2), item.RowVersion);

    private static async Task SaveAsync(RetailDbContext db, CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            throw new RetailException(409, "An account with this name already exists.");
        }
    }
}
