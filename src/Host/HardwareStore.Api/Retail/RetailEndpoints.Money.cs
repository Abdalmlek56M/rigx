using HardwareStore.Common.Domain;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;

public static partial class RetailEndpoints
{
    private static async Task<IResult> ListMoneyAccountsAsync(HttpContext context, MoneyAccountService accounts, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await accounts.ListAsync(ct));
    }

    private static async Task<IResult> ListMoneyMovementsAsync(Guid id, HttpContext context, MoneyAccountService accounts, CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await accounts.ListMovementsAsync(id, from, to, ct));
    }

    private static async Task<IResult> CreateMoneyAccountAsync(MoneyAccountRequest request, MoneyAccountService accounts, CancellationToken ct) =>
        Results.Created((string?)null, await accounts.CreateAsync(request, ct));

    private static async Task<IResult> UpdateMoneyAccountAsync(Guid id, MoneyAccountRequest request, MoneyAccountService accounts, CancellationToken ct) =>
        Results.Ok(await accounts.UpdateAsync(id, request, ct));

    private static async Task<IResult> TransferMoneyAsync(MoneyTransferRequest request, HttpContext context, MoneyAccountService accounts,
        CancellationToken ct)
    {
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        await accounts.TransferAsync(request, actorId, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListExpenseTypesAsync(HttpContext context, ExpenseTypeService types, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await types.ListAsync(ct));
    }

    private static async Task<IResult> CreateExpenseTypeAsync(ExpenseTypeRequest request, ExpenseTypeService types, CancellationToken ct) =>
        Results.Created((string?)null, await types.CreateAsync(request, ct));

    private static async Task<IResult> UpdateExpenseTypeAsync(Guid id, ExpenseTypeRequest request, ExpenseTypeService types, CancellationToken ct) =>
        Results.Ok(await types.UpdateAsync(id, request, ct));

    private static async Task<IResult> ListRecurringExpensesAsync(HttpContext context, RecurringExpenseService recurring, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await recurring.ListAsync(ct));
    }

    private static async Task<IResult> CreateRecurringExpenseAsync(RecurringExpenseRequest request, HttpContext context,
        RecurringExpenseService recurring, TimeZoneInfo businessTimeZone, CancellationToken ct) =>
        context.TryGetUserId(out var actorId)
            ? Results.Created((string?)null, await recurring.CreateAsync(request, actorId, businessTimeZone, ct))
            : Results.Unauthorized();

    private static async Task<IResult> UpdateRecurringExpenseAsync(Guid id, RecurringExpenseRequest request, RecurringExpenseService recurring,
        CancellationToken ct) =>
        Results.Ok(await recurring.UpdateAsync(id, request, ct));

    private static async Task<IResult> ExpenseSummaryAsync(HttpContext context, ExpenseService expenses, TimeProvider timeProvider, TimeZoneInfo businessZone,
        CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var end = to ?? BusinessDay.Today(timeProvider, businessZone);
        return Results.Ok(await expenses.ExpenseSummaryAsync(from ?? end.AddDays(-30), end, ct));
    }
}
