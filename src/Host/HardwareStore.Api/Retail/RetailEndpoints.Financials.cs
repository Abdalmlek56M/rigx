using HardwareStore.Common.Domain;
using HardwareStore.Modules.Catalog;
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
        if (!context.TryGetUserId(out var actorId)) return Results.Unauthorized();
        return Results.Ok(await expenses.CreateExpenseAsync(request, actorId, ct));
    }

    /// <summary>The receipt photo is private: re-encoded, stored outside the public media routes; a replaced one is deleted after the new one is saved.</summary>
    private static async Task<IResult> SetExpenseReceiptAsync(Guid id, HttpContext context, ExpenseService expenses,
        ICatalogMediaStorage storage, CatalogMediaOptions options, CancellationToken ct)
    {
        var (upload, problem) = await CatalogEndpoints.ReadSingleImageAsync(context, options, ct);
        if (upload is null) return problem!;
        var key = await storage.SavePrivateImageAsync("receipts", id, upload, ct);
        string? previous;
        try { previous = await expenses.SetReceiptAsync(id, key, ct); }
        catch { await storage.DeleteStorageKeyAsync(key, CancellationToken.None); throw; }
        if (previous is not null) await storage.DeleteStorageKeyAsync(previous, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetExpenseReceiptAsync(Guid id, HttpContext context, ExpenseService expenses,
        ICatalogMediaStorage storage, CancellationToken ct) =>
        PrivateMediaResults.Image(context, storage, await expenses.GetReceiptKeyAsync(id, ct));

    private static async Task<IResult> BusinessReportAsync(HttpContext context, BusinessReportService reports, TimeProvider timeProvider, TimeZoneInfo businessZone,
        CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var end = to ?? BusinessDay.Today(timeProvider, businessZone);
        var start = from ?? end.AddDays(-30);
        return Results.Ok(await reports.BuildReportAsync(start, end, ct));
    }
}
