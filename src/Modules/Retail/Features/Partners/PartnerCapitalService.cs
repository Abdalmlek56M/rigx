using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>Capital a partner puts in or takes out. Entries are never edited; the balance is deposits minus withdrawals.</summary>
public sealed class PartnerCapitalService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider, TimeZoneInfo businessZone)
{
    public async Task<IReadOnlyList<PartnerCapitalEntryView>> ListAsync(Guid partnerId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Partners.AnyAsync(partner => partner.Id == partnerId, cancellationToken)) throw new RetailException(404, "The partner was not found.");
        return (await db.PartnerCapitalEntries.AsNoTracking().Where(entry => entry.PartnerId == partnerId)
                .OrderByDescending(entry => entry.EntryDate).ThenByDescending(entry => entry.CreatedAtUtc).ToListAsync(cancellationToken))
            .Select(entry => new PartnerCapitalEntryView(entry.Id, entry.PartnerId, entry.Kind, entry.Amount, entry.EntryDate, entry.Method,
                entry.Note, entry.CreatedAtUtc)).ToList();
    }

    /// <summary>The partner row is locked so two withdrawals cannot both pass the balance check; the money moves in or out of an account.</summary>
    public async Task<PartnerCapitalEntryView> AddAsync(Guid partnerId, PartnerCapitalRequest request, Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var amount = decimal.Round(request.Amount, 2);
        var method = MoneyRules.KindForMethod(request.Method);
        var note = OrderRules.Optional(request.Note, 500, "Note");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.LockPartnerAsync(partnerId, cancellationToken);
            if (!await db.Partners.AnyAsync(partner => partner.Id == partnerId, cancellationToken)) throw new RetailException(404, "The partner was not found.");
            var balance = PartnerRules.CapitalBalance(await db.PartnerCapitalEntries.AsNoTracking()
                .Where(entry => entry.PartnerId == partnerId).ToListAsync(cancellationToken));
            PartnerRules.EnsureCapitalEntry(request.Kind, amount, balance);
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var entry = new PartnerCapitalEntry
            {
                Id = Guid.CreateVersion7(), PartnerId = partnerId, Kind = request.Kind!, Amount = amount,
                EntryDate = request.EntryDate ?? BusinessDay.Of(now, businessZone), Method = method, Note = note,
                ActorUserAccountId = actorId, CreatedAtUtc = now
            };
            db.PartnerCapitalEntries.Add(entry);
            var deposit = request.Kind == PartnerCapitalKind.Deposit;
            var posting = new MoneyEntry(deposit ? MoneySource.CapitalDeposit : MoneySource.CapitalWithdrawal, entry.Id, amount, null, note,
                entry.EntryDate, actorId, now);
            if (deposit) await MoneyLedger.PostInAsync(db, request.MoneyAccountId, method, posting, cancellationToken);
            else await MoneyLedger.PostOutAsync(db, request.MoneyAccountId, method, posting, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new PartnerCapitalEntryView(entry.Id, entry.PartnerId, entry.Kind, entry.Amount, entry.EntryDate, entry.Method, entry.Note, entry.CreatedAtUtc);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }
}
