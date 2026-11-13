using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;

public static partial class RetailEndpoints
{
    /// <summary>Public invoice page (/i/{token}): the customer's own document, opened by an unguessable link.</summary>
    private static async Task<IResult> GetInvoiceByTokenAsync(string token, HttpContext context, InvoiceService invoices, StoreSettingsService settings,
        TaxonomyService taxonomy, OrderLinks links, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(InvoiceDocuments.Build(await invoices.GetByTokenAsync(token, ct), await settings.GetAdminAsync(ct), links,
            await InvoiceDocuments.CategoryNamesAsync(taxonomy, ct)));
    }

    private static async Task<IResult> ListMyInvoicesAsync(HttpContext context, InvoiceService invoices, CancellationToken ct)
    {
        if (!context.TryGetUserId(out var accountId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await invoices.ListMineAsync(accountId, ct));
    }

    /// <summary>Staff view of an order's invoice: phone masked, address and serials hidden without the matching permissions.</summary>
    private static async Task<IResult> GetOrderInvoiceAsync(Guid id, HttpContext context, InvoiceService invoices, StoreSettingsService settings,
        TaxonomyService taxonomy, OrderLinks links, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(InvoiceDocuments.Build(await invoices.GetForOrderAsync(id, ct), await settings.GetAdminAsync(ct), links,
            await InvoiceDocuments.CategoryNamesAsync(taxonomy, ct),
            context.HasPermission("Sales.ViewCustomerContact"), context.HasPermission("Inventory.ViewSerials")));
    }

    private static async Task<IResult> ListInvoicesAsync(HttpContext context, InvoiceService invoices, CancellationToken ct, string? q = null, int page = 1)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await invoices.ListAdminAsync(q, page, ct));
    }

    /// <summary>Shareable price quote: no invoice number and no customer data.</summary>
    private static async Task<IResult> QuoteAsync(QuoteRequest request, HttpContext context, QuoteService quotes, StoreSettingsService settings,
        BuilderCatalogLoader builderCatalogs, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new QuoteDocument(await quotes.QuoteAsync(request, await builderCatalogs.BuildPriceRuleForAsync(request.Items, ct), ct), InvoiceDocuments.Branding(await settings.GetAdminAsync(ct))));
    }
}
