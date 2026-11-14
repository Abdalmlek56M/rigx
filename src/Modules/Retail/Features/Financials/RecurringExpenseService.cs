using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Fixed monthly expenses (rent, salaries, internet): each template is posted as a general-overhead expense on its day of the month
/// (business time zone), automatically by the hourly job. A template whose account cannot pay stays due and is retried.
/// </summary>
public sealed class RecurringExpenseService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    private enum Outcome { Posted, Skipped, Blocked }

    public async Task<IReadOnlyList<RecurringExpenseView>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return (await db.RecurringExpenses.AsNoTracking().OrderBy(item => item.Category).ThenBy(item => item.DayOfMonth).ToListAsync(cancellationToken))
            .Select(View).ToList();
    }

    public async Task<RecurringExpenseView> CreateAsync(RecurringExpenseRequest request, Guid actorId, TimeZoneInfo businessZone,
        CancellationToken cancellationToken = default)
    {
        var template = new RecurringExpense
        {
            Id = Guid.CreateVersion7(), Category = string.Empty, CreatedByUserAccountId = actorId,
            StartMonth = ExpenseRules.MonthStart(request.StartMonth ?? Today(businessZone)), CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
        };
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await ApplyAsync(db, template, request, cancellationToken);
        db.RecurringExpenses.Add(template);
        await db.SaveChangesAsync(cancellationToken);
        return View(template);
    }

    public async Task<RecurringExpenseView> UpdateAsync(Guid id, RecurringExpenseRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var template = await db.RecurringExpenses.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RetailException(404, "The recurring expense was not found.");
        if (request.RowVersion != template.RowVersion) throw new RetailException(409, "The recurring expense was changed by another user.");
        await ApplyAsync(db, template, request, cancellationToken);
        template.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return View(template);
    }

    /// <summary>Posts every month that is due (day reached, not yet posted) for each active template; a template that cannot pay stops and counts as blocked.</summary>
    public async Task<RecurringPostResult> PostDueAsync(TimeZoneInfo businessZone, CancellationToken cancellationToken = default)
    {
        var today = Today(businessZone);
        var posted = 0;
        var blocked = 0;
        foreach (var template in await ActiveAsync(cancellationToken))
            foreach (var date in ExpenseRules.DueDates(template.StartMonth, template.LastPostedMonth, template.DayOfMonth, today))
            {
                var outcome = await PostOneAsync(template.Id, date, cancellationToken);
                if (outcome == Outcome.Posted) posted++;
                if (outcome != Outcome.Blocked) continue;
                blocked++;
                break;
            }
        return new RecurringPostResult(posted, blocked);
    }

    /// <summary>How many active templates have a month waiting to be posted (the dashboard shows it).</summary>
    public async Task<int> CountDueAsync(TimeZoneInfo businessZone, CancellationToken cancellationToken = default)
    {
        var today = Today(businessZone);
        return (await ActiveAsync(cancellationToken))
            .Count(item => ExpenseRules.DueDates(item.StartMonth, item.LastPostedMonth, item.DayOfMonth, today).Count > 0);
    }

    private async Task<Outcome> PostOneAsync(Guid templateId, DateOnly date, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var template = await db.RecurringExpenses.SingleAsync(item => item.Id == templateId, cancellationToken);
            var month = ExpenseRules.MonthStart(date);
            if (!template.IsActive || template.LastPostedMonth >= month) return Outcome.Skipped;
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            await ExpensePosting.AddAsync(db, new RetailExpense
            {
                Id = Guid.CreateVersion7(), ExpenseDate = date, Amount = template.Amount, Category = template.Category,
                Classification = ExpenseClassification.GeneralOverhead, Note = template.Note, MoneyAccountId = template.MoneyAccountId,
                RecurringExpenseId = template.Id, CreatedByUserAccountId = template.CreatedByUserAccountId, CreatedAtUtc = now
            }, template.CreatedByUserAccountId, now, cancellationToken);
            template.LastPostedMonth = month;
            template.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Outcome.Posted;
        }
        catch (RetailException)
        {
            await transaction.RollbackQuietlyAsync();
            return Outcome.Blocked;
        }
        catch (DbUpdateException exception) when (exception is DbUpdateConcurrencyException || exception.IsUniqueViolation())
        {
            await transaction.RollbackQuietlyAsync();
            return Outcome.Skipped;
        }
        catch { await transaction.RollbackQuietlyAsync(); throw; }
    }

    private async Task<List<RecurringExpense>> ActiveAsync(CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.RecurringExpenses.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    private static async Task ApplyAsync(RetailDbContext db, RecurringExpense template, RecurringExpenseRequest request, CancellationToken cancellationToken)
    {
        var types = (await db.ExpenseTypes.AsNoTracking().Where(item => item.IsActive).Select(item => item.Name).ToListAsync(cancellationToken)).ToHashSet();
        template.Category = ExpenseRules.Category(request.Category, types);
        template.Amount = ExpenseRules.Amount(request.Amount);
        template.DayOfMonth = ExpenseRules.DayOfMonth(request.DayOfMonth);
        template.Note = OrderRules.Optional(request.Note, 1000, "Note");
        template.IsActive = request.IsActive;
        if (request.MoneyAccountId is Guid accountId && !await db.MoneyAccounts.AnyAsync(item => item.Id == accountId, cancellationToken))
            throw new RetailException(404, "The money account was not found.");
        template.MoneyAccountId = request.MoneyAccountId;
    }

    private DateOnly Today(TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow().ToUniversalTime(), zone).DateTime);

    private static RecurringExpenseView View(RecurringExpense item) => new(item.Id, item.Category, item.Amount, item.DayOfMonth,
        item.MoneyAccountId, item.Note, item.IsActive, item.StartMonth, item.LastPostedMonth, item.CreatedAtUtc, item.RowVersion);
}
