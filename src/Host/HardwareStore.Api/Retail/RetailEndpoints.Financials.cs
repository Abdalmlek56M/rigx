using HardwareStore.Common.Domain;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static partial class RetailEndpoints
{
    private static async Task<Ok<IReadOnlyList<ExpenseView>>> ListExpensesAsync(HttpContext context, ExpenseService expenses, CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await expenses.ListExpensesAsync(from, to, ct));
    }

    private static async Task<Ok<ExpenseView>> CreateExpenseAsync(ExpenseCreateRequest request, HttpContext context,
        ExpenseService expenses, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await expenses.CreateExpenseAsync(request, actorId, ct));
    }

    /// <summary>The receipt photo is private: re-encoded, stored outside the public media routes; a replaced one is deleted after the new one is saved.</summary>
    private static async Task<NoContent> SetExpenseReceiptAsync(Guid id, HttpContext context, ExpenseService expenses,
        ICatalogMediaStorage storage, CatalogMediaOptions options, CancellationToken ct)
    {
        var upload = await CatalogEndpoints.ReadSingleImageAsync(context, options, ct);
        var key = await storage.SavePrivateImageAsync("receipts", id, upload, ct);
        string? previous;
        try { previous = await expenses.SetReceiptAsync(id, key, ct); }
        catch { await storage.DeleteStorageKeyAsync(key, CancellationToken.None); throw; }
        if (previous is not null) await storage.DeleteStorageKeyAsync(previous, ct);
        return TypedResults.NoContent();
    }

    private static async Task<FileStreamHttpResult> GetExpenseReceiptAsync(Guid id, HttpContext context, ExpenseService expenses,
        ICatalogMediaStorage storage, CancellationToken ct) =>
        PrivateMediaResults.Image(context, storage, await expenses.GetReceiptKeyAsync(id, ct));

    private static async Task<Ok<BusinessReportView>> BusinessReportAsync(HttpContext context, BusinessReportService reports, TimeProvider timeProvider, TimeZoneInfo businessZone,
        CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var end = to ?? BusinessDay.Today(timeProvider, businessZone);
        var start = from ?? end.AddDays(-30);
        return TypedResults.Ok(await reports.BuildReportAsync(start, end, ct));
    }
}
