using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>The expense types the owner can pick from. Renaming one does not change expenses already recorded (their category is a snapshot).</summary>
public sealed class ExpenseTypeService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ExpenseTypeView>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return (await db.ExpenseTypes.AsNoTracking().OrderBy(item => item.SortOrder).ThenBy(item => item.Name).ToListAsync(cancellationToken))
            .Select(View).ToList();
    }

    public async Task<ExpenseTypeView> CreateAsync(ExpenseTypeRequest request, CancellationToken cancellationToken = default)
    {
        var type = new ExpenseType
        {
            Id = Guid.CreateVersion7(), Name = ExpenseRules.TypeName(request.Name), IsActive = request.IsActive, SortOrder = request.SortOrder,
            CreatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime()
        };
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.ExpenseTypes.Add(type);
        await SaveAsync(db, cancellationToken);
        return View(type);
    }

    public async Task<ExpenseTypeView> UpdateAsync(Guid id, ExpenseTypeRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var type = await db.ExpenseTypes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RetailException(404, "The expense type was not found.");
        if (request.RowVersion != type.RowVersion) throw new RetailException(409, "The expense type was changed by another user.");
        type.Name = ExpenseRules.TypeName(request.Name);
        type.IsActive = request.IsActive;
        type.SortOrder = request.SortOrder;
        type.RowVersion++;
        await SaveAsync(db, cancellationToken);
        return View(type);
    }

    private static ExpenseTypeView View(ExpenseType item) => new(item.Id, item.Name, item.IsActive, item.SortOrder, item.RowVersion);

    private static async Task SaveAsync(RetailDbContext db, CancellationToken cancellationToken)
    {
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            throw new RetailException(409, "An expense type with this name already exists.");
        }
    }
}
