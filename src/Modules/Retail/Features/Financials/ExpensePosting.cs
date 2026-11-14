namespace HardwareStore.Modules.Retail;

/// <summary>Adds an expense together with how it was paid; used by manual expenses and the recurring posting.</summary>
internal static class ExpensePosting
{
    /// <summary>
    /// Paid by a partner: his dues are reduced like a payout (no account movement). Otherwise the money leaves the chosen account, or the
    /// first active cash account (409 when its balance is not enough). The caller is inside a transaction and saves.
    /// </summary>
    public static async Task AddAsync(RetailDbContext db, RetailExpense expense, Guid actorId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        db.Expenses.Add(expense);
        if (expense.PaidByPartnerId is Guid partnerId)
        {
            await PartnerPayouts.PayAsync(db, partnerId, expense.Amount, PartnerSettlementMethod.PartnerExpense, null, expense.Category, expense.Id,
                actorId, now, cancellationToken);
            return;
        }
        expense.MoneyAccountId = await MoneyLedger.PostOutAsync(db, expense.MoneyAccountId, MoneyAccountKind.Cash,
            new MoneyEntry(MoneySource.Expense, expense.Id, expense.Amount, null, expense.Category, expense.ExpenseDate, actorId, now),
            cancellationToken);
    }

    public static ExpenseView View(RetailExpense item) => new(item.Id, item.ExpenseDate, item.Amount, item.Category, item.Classification,
        item.Note, item.OrderId, item.UnitId, item.AttachmentReference, item.CreatedByUserAccountId, item.CreatedAtUtc, item.RowVersion,
        item.AssemblyId, item.MoneyAccountId, item.PaidByPartnerId, item.RecurringExpenseId);
}
