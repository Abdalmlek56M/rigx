using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Store expenses (direct or general overhead).
/// </summary>
public sealed class ExpenseService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<ExpenseView> CreateExpenseAsync(ExpenseCreateRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            throw new RetailException(400, "Expense amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.Category))
            throw new RetailException(400, "Expense category is required.");
        var classification = request.Classification is "Direct" or "GeneralOverhead" ? request.Classification : "GeneralOverhead";
        var date = request.ExpenseDate ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var entity = new RetailExpense
        {
            Id = Guid.CreateVersion7(), ExpenseDate = date, Amount = decimal.Round(request.Amount, 2),
            Category = request.Category.Trim(), Classification = classification,
            Note = request.Note?.Trim(), OrderId = request.OrderId, UnitId = request.UnitId,
            AttachmentReference = request.AttachmentReference?.Trim(),
            CreatedByUserAccountId = actorId, CreatedAtUtc = now
        };
        db.Expenses.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return MapExpense(entity);
    }

    public async Task<IReadOnlyList<ExpenseView>> ListExpensesAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Expenses.AsNoTracking().AsQueryable();
        if (from is not null) query = query.Where(item => item.ExpenseDate >= from);
        if (to is not null) query = query.Where(item => item.ExpenseDate <= to);
        return (await query.OrderByDescending(item => item.ExpenseDate).ThenByDescending(item => item.CreatedAtUtc)
            .Take(1000).ToListAsync(cancellationToken)).Select(MapExpense).ToList();
    }

    private static ExpenseView MapExpense(RetailExpense item) =>
        new(item.Id, item.ExpenseDate, item.Amount, item.Category, item.Classification, item.Note,
            item.OrderId, item.UnitId, item.AttachmentReference, item.CreatedByUserAccountId,
            item.CreatedAtUtc, item.RowVersion);
}
