using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;

public static partial class RetailEndpoints
{
    private static async Task<IResult> ListExpensesAsync(HttpContext context, ExpenseService expenses, CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await expenses.ListExpensesAsync(from, to, ct));
    }

    private static async Task<IResult> CreateExpenseAsync(ExpenseCreateRequest request, HttpContext context,
        ExpenseService expenses, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        return Results.Ok(await expenses.CreateExpenseAsync(request, actorId, ct));
    }

    private static async Task<IResult> BusinessReportAsync(HttpContext context, BusinessReportService reports, CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-30);
        return Results.Ok(await reports.BuildReportAsync(start, end, ct));
    }
}
