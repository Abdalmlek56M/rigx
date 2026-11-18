using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Store expenses (direct or general overhead): each one is paid from a money account or from a partner's own money,
/// with a summary against the previous period and an export table.
/// </summary>
public sealed class ExpenseService(IDbContextFactory<RetailDbContext> contextFactory, BusinessReportService reports, TimeProvider timeProvider,
    TimeZoneInfo businessZone)
{
    public async Task<ExpenseView> CreateExpenseAsync(ExpenseCreateRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var amount = ExpenseRules.Amount(request.Amount);
        var classification = ExpenseRules.Classification(request.Classification);
        ExpenseRules.EnsureDirectLink(classification, request.OrderId, request.UnitId, request.AssemblyId);
        ExpenseRules.EnsureSinglePayer(request.MoneyAccountId, request.PaidByPartnerId);
        var note = OrderRules.Optional(request.Note, 1000, "Note");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var types = (await db.ExpenseTypes.AsNoTracking().Where(item => item.IsActive).Select(item => item.Name).ToListAsync(cancellationToken)).ToHashSet();
            var entity = new RetailExpense
            {
                Id = Guid.CreateVersion7(), ExpenseDate = ExpenseRules.Date(request.ExpenseDate, BusinessDay.Of(now, businessZone)), Amount = amount,
                Category = ExpenseRules.Category(request.Category, types), Classification = classification, Note = note,
                OrderId = request.OrderId, UnitId = request.UnitId, AssemblyId = request.AssemblyId,
                MoneyAccountId = request.MoneyAccountId, PaidByPartnerId = request.PaidByPartnerId,
                CreatedByUserAccountId = actorId, CreatedAtUtc = now
            };
            await EnsureLinksExistAsync(db, entity, cancellationToken);
            await ExpensePosting.AddAsync(db, entity, actorId, now, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ExpensePosting.View(entity);
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    /// <summary>Sets the receipt photo key and returns the key it replaced (null when there was none), so the caller can delete that file.</summary>
    public async Task<string?> SetReceiptAsync(Guid id, string storageKey, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var expense = await db.Expenses.SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw ExpenseNotFound();
        var previous = expense.ReceiptStorageKey;
        expense.ReceiptStorageKey = storageKey;
        expense.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return previous;
    }

    public async Task<string> GetReceiptKeyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var key = await db.Expenses.AsNoTracking().Where(item => item.Id == id).Select(item => new { item.ReceiptStorageKey })
            .SingleOrDefaultAsync(cancellationToken) ?? throw ExpenseNotFound();
        return key.ReceiptStorageKey ?? throw new RetailException(404, "The expense has no receipt.");
    }

    private static RetailException ExpenseNotFound() => new(404, "Expense was not found.");

    public async Task<IReadOnlyList<ExpenseView>> ListExpensesAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Expenses.AsNoTracking().AsQueryable();
        if (from is not null) query = query.Where(item => item.ExpenseDate >= from);
        if (to is not null) query = query.Where(item => item.ExpenseDate <= to);
        return (await query.OrderByDescending(item => item.ExpenseDate).ThenByDescending(item => item.CreatedAtUtc)
            .Take(1000).ToListAsync(cancellationToken)).Select(ExpensePosting.View).ToList();
    }

    /// <summary>Totals by type, the change against the previous period of the same length, the share of the period's gross sales and the fixed monthly amount.</summary>
    public async Task<ExpenseSummaryView> ExpenseSummaryAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        if (to < from) throw new RetailException(400, "The end date cannot precede the start date.");
        var previousTo = from.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(to.DayNumber - from.DayNumber));
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Expenses.AsNoTracking().Where(item => item.ExpenseDate >= from && item.ExpenseDate <= to)
            .Select(item => new { item.Category, item.Amount }).ToListAsync(cancellationToken);
        var byType = rows.GroupBy(item => item.Category)
            .Select(group => new ExpenseTypeTotal(group.Key, decimal.Round(group.Sum(item => item.Amount), 2), group.Count()))
            .OrderByDescending(item => item.Amount).ToList();
        var total = decimal.Round(byType.Sum(item => item.Amount), 2);
        var previous = decimal.Round(await db.Expenses.AsNoTracking().Where(item => item.ExpenseDate >= previousFrom && item.ExpenseDate <= previousTo)
            .SumAsync(item => item.Amount, cancellationToken), 2);
        var fixedMonthly = decimal.Round(await db.RecurringExpenses.AsNoTracking().Where(item => item.IsActive)
            .SumAsync(item => item.Amount, cancellationToken), 2);
        var sales = (await reports.BuildReportAsync(from, to, cancellationToken)).GrossSales;
        return new ExpenseSummaryView(from, to, total, byType, previous,
            previous == 0 ? null : decimal.Round((total - previous) / previous * 100, 1), sales,
            sales == 0 ? null : decimal.Round(total / sales * 100, 1), fixedMonthly);
    }

    /// <summary>The expenses of a period as an export table (read at most <see cref="ExportRules.ReadLimit"/> rows).</summary>
    public async Task<ExportTable> ExportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var expenses = await db.Expenses.AsNoTracking().Where(item => item.ExpenseDate >= from && item.ExpenseDate <= to)
            .OrderBy(item => item.ExpenseDate).ThenBy(item => item.CreatedAtUtc).Take(ExportRules.ReadLimit).ToListAsync(cancellationToken);
        var accounts = await db.MoneyAccounts.AsNoTracking().ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var partnerIds = expenses.Select(item => item.PaidByPartnerId).OfType<Guid>().Distinct().ToList();
        var partners = await db.Partners.AsNoTracking().Where(item => partnerIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var orderIds = expenses.Select(item => item.OrderId).OfType<Guid>().Distinct().ToList();
        var orders = await db.Orders.AsNoTracking().Where(item => orderIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.Reference, cancellationToken);
        var unitIds = expenses.Select(item => item.UnitId).OfType<Guid>().Distinct().ToList();
        var units = await db.Units.AsNoTracking().Where(item => unitIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.StockItem.Code, cancellationToken);
        var assemblyIds = expenses.Select(item => item.AssemblyId).OfType<Guid>().Distinct().ToList();
        var builds = await db.Assemblies.AsNoTracking().Where(item => assemblyIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.Code, cancellationToken);
        return new ExportTable(["date", "type", "classification", "amount", "paid from", "order reference", "unit code", "build code", "note"],
            [.. expenses.Select(item => (IReadOnlyList<ExportCell>)
            [
                ExportCell.Date(item.ExpenseDate), ExportCell.Text(item.Category), ExportCell.Text(item.Classification), ExportCell.Number(item.Amount),
                ExportCell.Text(PaidFrom(item, accounts, partners)), ExportCell.Text(Lookup(orders, item.OrderId)),
                ExportCell.Text(Lookup(units, item.UnitId)), ExportCell.Text(Lookup(builds, item.AssemblyId)), ExportCell.Text(item.Note)
            ])]);
    }

    private static string? PaidFrom(RetailExpense item, Dictionary<Guid, string> accounts, Dictionary<Guid, string> partners) =>
        item.PaidByPartnerId is not null ? Lookup(partners, item.PaidByPartnerId) : Lookup(accounts, item.MoneyAccountId);

    private static string? Lookup(Dictionary<Guid, string> names, Guid? id) => id is Guid key ? names.GetValueOrDefault(key) : null;

    private static async Task EnsureLinksExistAsync(RetailDbContext db, RetailExpense expense, CancellationToken cancellationToken)
    {
        if (expense.OrderId is Guid orderId && !await db.Orders.AnyAsync(item => item.Id == orderId, cancellationToken))
            throw new RetailException(404, "Order was not found.");
        if (expense.UnitId is Guid unitId && !await db.Units.AnyAsync(item => item.Id == unitId, cancellationToken))
            throw new RetailException(404, "Unit was not found.");
        if (expense.AssemblyId is Guid assemblyId && !await db.Assemblies.AnyAsync(item => item.Id == assemblyId, cancellationToken))
            throw new RetailException(404, "Build was not found.");
    }
}
