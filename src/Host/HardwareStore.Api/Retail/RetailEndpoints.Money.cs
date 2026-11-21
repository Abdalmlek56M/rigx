using HardwareStore.Common.Domain;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static partial class RetailEndpoints
{
    private static async Task<Ok<IReadOnlyList<MoneyAccountView>>> ListMoneyAccountsAsync(HttpContext context, MoneyAccountService accounts, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await accounts.ListAsync(ct));
    }

    private static async Task<Ok<IReadOnlyList<MoneyMovementView>>> ListMoneyMovementsAsync(Guid id, HttpContext context, MoneyAccountService accounts, CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await accounts.ListMovementsAsync(id, from, to, ct));
    }

    private static async Task<Created<MoneyAccountView>> CreateMoneyAccountAsync(MoneyAccountRequest request, MoneyAccountService accounts, CancellationToken ct) =>
        TypedResults.Created((string?)null, await accounts.CreateAsync(request, ct));

    private static async Task<Ok<MoneyAccountView>> UpdateMoneyAccountAsync(Guid id, MoneyAccountRequest request, MoneyAccountService accounts, CancellationToken ct) =>
        TypedResults.Ok(await accounts.UpdateAsync(id, request, ct));

    private static async Task<NoContent> DeleteMoneyAccountAsync(Guid id, MoneyAccountService accounts, CancellationToken ct)
    {
        await accounts.DeleteAsync(id, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> TransferMoneyAsync(MoneyTransferRequest request, HttpContext context, MoneyAccountService accounts,
        CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        await accounts.TransferAsync(request, actorId, ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<ExpenseTypeView>>> ListExpenseTypesAsync(HttpContext context, ExpenseTypeService types, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await types.ListAsync(ct));
    }

    private static async Task<Created<ExpenseTypeView>> CreateExpenseTypeAsync(ExpenseTypeRequest request, ExpenseTypeService types, CancellationToken ct) =>
        TypedResults.Created((string?)null, await types.CreateAsync(request, ct));

    private static async Task<Ok<ExpenseTypeView>> UpdateExpenseTypeAsync(Guid id, ExpenseTypeRequest request, ExpenseTypeService types, CancellationToken ct) =>
        TypedResults.Ok(await types.UpdateAsync(id, request, ct));

    private static async Task<Ok<IReadOnlyList<RecurringExpenseView>>> ListRecurringExpensesAsync(HttpContext context, RecurringExpenseService recurring, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await recurring.ListAsync(ct));
    }

    private static async Task<Created<RecurringExpenseView>> CreateRecurringExpenseAsync(RecurringExpenseRequest request, HttpContext context,
        RecurringExpenseService recurring, TimeZoneInfo businessTimeZone, CancellationToken ct) =>
        TypedResults.Created((string?)null, await recurring.CreateAsync(request, context.RequireUserId(), businessTimeZone, ct));

    private static async Task<Ok<RecurringExpenseView>> UpdateRecurringExpenseAsync(Guid id, RecurringExpenseRequest request, RecurringExpenseService recurring,
        CancellationToken ct) =>
        TypedResults.Ok(await recurring.UpdateAsync(id, request, ct));

    private static async Task<Ok<ExpenseSummaryView>> ExpenseSummaryAsync(HttpContext context, ExpenseService expenses, TimeProvider timeProvider, TimeZoneInfo businessZone,
        CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var end = to ?? BusinessDay.Today(timeProvider, businessZone);
        return TypedResults.Ok(await expenses.ExpenseSummaryAsync(from ?? end.AddDays(-30), end, ct));
    }
}
