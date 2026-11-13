using System.Security.Claims;
using System.Text.Json;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public static class RetailEndpoints
{
    public static IEndpointRouteBuilder MapRetailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/v1/admin/retail");
        admin.MapGet("/units", ListUnitsAsync).RequirePermission("Inventory.View");
        admin.MapPost("/receipts/from-draft/{draftId:guid}", ReceiveFromDraftAsync).RequirePermission("Purchasing.ReceiveGoods");
        admin.MapGet("/assemblies", ListAssembliesAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies", CreateAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/assemblies/{id:guid}", UpdateAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/assemblies/{id:guid}/allocations", SetAllocationsAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/publish", PublishAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/unpublish", UnpublishAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapDelete("/assemblies/{id:guid}/units/{unitId:guid}", RemoveUnitAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/swap", SwapAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/disassemble", DisassembleAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapGet("/orders", ListOrdersAsync).RequirePermission("Sales.ViewOrders");
        admin.MapGet("/orders/{id:guid}", GetOrderDetailAsync).RequirePermission("Sales.ViewOrders");
        admin.MapPost("/orders", CreateManualOrderAsync).RequirePermission("Sales.CreatePOS");
        admin.MapPost("/orders/{id:guid}/status", UpdateStatusAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapPost("/orders/{id:guid}/complete", CompleteOrderAsync).RequirePermission("Sales.CompleteOrder");
        admin.MapPost("/orders/{id:guid}/payment", UpdatePaymentAsync).RequirePermission("Finance.VerifyPayment");
        admin.MapPost("/orders/{id:guid}/payment/waive", WaivePaymentAsync).RequirePermission("Finance.VerifyPayment");
        admin.MapGet("/returns", ListReturnsAsync).RequirePermission("Sales.ViewOrders");
        admin.MapPost("/returns", CreateReturnAsync).RequirePermission("Sales.ProcessReturn");
        admin.MapPost("/returns/{id:guid}/inspect", InspectReturnAsync).RequirePermission("Sales.ProcessReturn");
        admin.MapPost("/returns/{id:guid}/reject", RejectReturnAsync).RequirePermission("Sales.ProcessReturn");
        admin.MapGet("/partner/earnings", ListEarningsAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/expenses", ListExpensesAsync).RequirePermission("Finance.ViewReports");
        admin.MapPost("/expenses", CreateExpenseAsync).RequirePermission("Finance.ManageExpenses");
        admin.MapGet("/reports/business", BusinessReportAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partner/adjustments", ListAdjustmentsAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partner/ledger", ListPartnerLedgerAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partner/allocations", ListAllocationsAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partner/allocations/preview", PreviewAllocationAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partner/allocations/{id:guid}/history", AllocationHistoryAsync).RequirePermission("Finance.ViewReports");
        admin.MapPost("/partner/allocations", UpsertAllocationAsync).RequirePermission("Finance.ManagePartnerAgreements");
        admin.MapPost("/partner/allocations/{id:guid}/deactivate", DeactivateAllocationAsync).RequirePermission("Finance.ManagePartnerAgreements");
        admin.MapGet("/partner/agreements", ListAgreementsAsync).RequirePermission("Finance.ViewReports");
        admin.MapPost("/partner/agreements", UpsertAgreementAsync).RequirePermission("Finance.ManagePartnerAgreements");
        admin.MapPost("/partner/settlements", SettleAsync).RequirePermission("Finance.PostPartnerSettlements");

        endpoints.MapGet("/api/v1/store/assemblies", ListPublishedAssembliesAsync).AllowAnonymous();
        endpoints.MapPost("/api/v1/orders", CreateCustomerOrderAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/orders/mine", ListMyOrdersAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/orders/mine/{id:guid}", GetMyOrderAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> ListUnitsAsync(HttpContext context, RetailService retail, CancellationToken ct, string? status = null, Guid? productId = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var includeCosts = HasPermission(context, "Inventory.ViewCosts");
        var includeSerials = HasPermission(context, "Inventory.ViewSerials");
        return Results.Ok(await retail.ListUnitsAsync(status, productId, includeCosts, includeSerials, ct));
    }

    private static async Task<IResult> ReceiveFromDraftAsync(
        Guid draftId, ReceiveNotes request, HttpContext context, RetailService retail,
        IDbContextFactory<PurchasingDbContext> purchasingFactory,
        IDbContextFactory<CatalogDbContext> catalogFactory, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        await using var purchasing = await purchasingFactory.CreateDbContextAsync(ct);
        var draft = await purchasing.PurchaseDrafts.AsNoTracking().Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == draftId, ct);
        if (draft is null) return Results.NotFound();
        var productIds = draft.Items.Where(item => item.ProductId != Guid.Empty).Select(item => item.ProductId).Distinct().ToList();
        await using var catalog = await catalogFactory.CreateDbContextAsync(ct);
        var prices = await catalog.Products.AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new { product.Id, product.BasePrice })
            .ToDictionaryAsync(item => item.Id, item => item.BasePrice, ct);
        var images = await catalog.ProductImages.AsNoTracking()
            .Where(image => productIds.Contains(image.ProductId))
            .OrderByDescending(image => image.IsPrimary).ThenBy(image => image.SortOrder)
            .Select(image => new { image.ProductId, image.StorageKey, image.ThumbnailStorageKey, image.OriginalStorageKey })
            .ToListAsync(ct);
        var imageKeys = images.GroupBy(image => image.ProductId)
            .ToDictionary(group => group.Key, group => JsonSerializer.Serialize(group.First()));
        var sourceName = draft.SupplierId is Guid supplierId
            ? await purchasing.SupplierProfiles.AsNoTracking().Where(supplier => supplier.Id == supplierId)
                .Select(supplier => supplier.CompanyName).SingleOrDefaultAsync(ct)
            : null;
        sourceName ??= draft.MarketplaceName ?? draft.ExternalSellerName;
        var lines = draft.Items.Select(item =>
        {
            var productId = item.ProductId == Guid.Empty ? (Guid?)null : item.ProductId;
            var standalone = productId is not null && prices.TryGetValue(productId.Value, out var price) ? price : (decimal?)null;
            var imageJson = productId is not null && imageKeys.TryGetValue(productId.Value, out var json) ? json : null;
            return new ReceiptLine(productId, item.VariantId, item.ItemName, item.Sku, item.Condition ?? "New", null,
                item.UnitPrice, item.Quantity, null, null, standalone, sourceName, item.Brand, item.Model, item.Category,
                item.Description, item.ExternalCode, item.SpecificationsJson, draft.PurchaseKind, draft.SourceType,
                draft.PurchaseDate, imageJson);
        }).ToList();
        try
        {
            var units = await retail.ReceiveAsync(new ReceiveRequest(draftId, request.Notes, lines), actorId, ct);
            await purchasing.PurchaseDrafts.Where(item => item.Id == draftId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, PurchaseDraftStatus.Received), ct);
            return Results.Ok(units);
        }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> ListAssembliesAsync(HttpContext context, RetailService retail, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await retail.ListAssembliesAsync(HasPermission(context, "Inventory.ViewCosts"), ct));
    }

    private static async Task<IResult> CreateAssemblyAsync(AssemblyCreateRequest request, RetailService retail, CancellationToken ct)
    {
        try { return Results.Ok(await retail.CreateAssemblyAsync(request, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> UpdateAssemblyAsync(Guid id, AssemblyUpdateRequest request, RetailService retail, CancellationToken ct)
    {
        try { return Results.Ok(await retail.UpdateAssemblyAsync(id, request, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> SetAllocationsAsync(Guid id, AssemblyAllocationRequest request, RetailService retail, CancellationToken ct)
    {
        try { return Results.Ok(await retail.SetAssemblyAllocationsAsync(id, request, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> PublishAssemblyAsync(Guid id, RetailVersionRequest request, RetailService retail, CancellationToken ct) =>
        await SetAssemblyPublishedAsync(id, true, request, retail, ct);
    private static async Task<IResult> UnpublishAssemblyAsync(Guid id, RetailVersionRequest request, RetailService retail, CancellationToken ct) =>
        await SetAssemblyPublishedAsync(id, false, request, retail, ct);
    private static async Task<IResult> SetAssemblyPublishedAsync(Guid id, bool published, RetailVersionRequest request, RetailService retail, CancellationToken ct)
    {
        try { return Results.Ok(await retail.SetAssemblyPublishedAsync(id, published, request.RowVersion, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> RemoveUnitAsync(Guid id, Guid unitId, RetailService retail, CancellationToken ct, long rowVersion = 0)
    {
        try { return Results.Ok(await retail.RemoveUnitAsync(id, unitId, rowVersion, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> DisassembleAsync(Guid id, RetailVersionRequest request, RetailService retail, CancellationToken ct)
    {
        try { return Results.Ok(await retail.DisassembleAsync(id, request.RowVersion, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> SwapAsync(SwapRequest request, RetailService retail, CancellationToken ct)
    {
        try { return Results.Ok(await retail.SwapUnitAsync(request, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> ListOrdersAsync(HttpContext context, RetailService retail, CancellationToken ct, string? status = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await retail.ListOrdersAsync(status, HasPermission(context, "Sales.ViewCustomerContact"), ct));
    }

    private static async Task<IResult> GetOrderDetailAsync(Guid id, HttpContext context, RetailService retail, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await retail.GetAdminOrderDetailAsync(id, HasPermission(context, "Inventory.ViewSerials"), ct));
        }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> CreateManualOrderAsync(
        OrderCreateRequest request, HttpContext context, RetailService retail,
        IDbContextFactory<CatalogDbContext> catalogFactory, CancellationToken ct)
    {
        if (!TryGetActor(context, out _)) return Results.Unauthorized();
        var lines = new List<OrderLine>();
        await using var catalog = await catalogFactory.CreateDbContextAsync(ct);
        var publishedAssemblies = await retail.ListPublishedAssembliesAsync(ct);
        foreach (var line in request.Items ?? [])
        {
            if (line.AssemblyId is not null)
            {
                var assembly = publishedAssemblies.SingleOrDefault(item => item.Id == line.AssemblyId)
                    ?? throw new RetailException(409, "The selected build is not published or available.");
                lines.Add(new OrderLine(null, assembly.Id, assembly.TitleAr ?? assembly.TitleEn ?? "RIGX build", null, 1, assembly.SalePrice));
                continue;
            }
            if (line.ProductId is null) throw new RetailException(400, "Each order line needs a product or build.");
            var product = await catalog.Products.AsNoTracking().Include(item => item.Images)
                .SingleOrDefaultAsync(
                item => item.Id == line.ProductId && item.Status == CatalogProductStatus.Published, ct)
                ?? throw new RetailException(409, "The selected product is not published.");
            var thumbnail = product.Images.OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.SortOrder).Select(image => image.ThumbnailStorageKey).FirstOrDefault();
            lines.Add(new OrderLine(product.Id, null, product.Name, product.Sku, Math.Clamp(line.Quantity, 1, 20),
                product.BasePrice, null, null, null, null, null, thumbnail));
        }
        try
        {
            return Results.Ok(await retail.CreateOrderAsync(
                request with { Items = lines, LeadSource = request.LeadSource ?? LeadSource.Manual }, null, ct));
        }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> UpdateStatusAsync(Guid id, OrderStatusRequest request, HttpContext context, RetailService retail, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        try { return Results.Ok(await retail.UpdateStatusAsync(id, request, actorId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> CompleteOrderAsync(Guid id, CompleteOrderRequest request, HttpContext context, RetailService retail, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        try { return Results.Ok(await retail.CompleteOrderAsync(id, request, actorId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> UpdatePaymentAsync(Guid id, PaymentStatusRequest request, HttpContext context, RetailService retail, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        try { return Results.Ok(await retail.UpdatePaymentAsync(id, request, actorId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> WaivePaymentAsync(Guid id, WaivePaymentRequest request, HttpContext context, RetailService retail, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        try { return Results.Ok(await retail.WaivePaymentAsync(id, request, actorId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> ListReturnsAsync(HttpContext context, RetailService retail, CancellationToken ct, string? status = null)
    { context.Response.Headers.CacheControl = "no-store"; return Results.Ok(await retail.ListReturnsAsync(status, ct)); }

    private static async Task<IResult> CreateReturnAsync(ReturnCreateRequest request, HttpContext context, RetailService retail, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        try { return Results.Ok(await retail.CreateReturnAsync(request, actorId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> InspectReturnAsync(Guid id, ReturnInspectRequest request, HttpContext context, RetailService retail, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        try { return Results.Ok(await retail.InspectReturnAsync(id, request, actorId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> RejectReturnAsync(Guid id, ReturnRejectRequest request, HttpContext context, RetailService retail, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        try { return Results.Ok(await retail.RejectReturnAsync(id, request, actorId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> ListPublishedAssembliesAsync(HttpContext context, RetailService retail, CancellationToken ct)
    { context.Response.Headers.CacheControl = "public, max-age=60"; return Results.Ok(await retail.ListPublishedAssembliesAsync(ct)); }

    private static async Task<IResult> CreateCustomerOrderAsync(
        OrderCreateRequest request, HttpContext context, RetailService retail,
        IDbContextFactory<CatalogDbContext> catalogFactory, ContentPolicyService policies, CancellationToken ct)
    {
        if (!TryGetActor(context, out var accountId)) return Results.Unauthorized();
        if (request.TermsAccepted != true)
            return Results.BadRequest(new { error = "Terms and Privacy consent is required." });
        string termsVersion;
        try
        {
            var terms = await policies.GetPublicAsync("Terms", ct);
            termsVersion = terms.VersionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (StorefrontException)
        {
            return Results.Problem(statusCode: 409, title: "The Terms policy is not published; ordering is unavailable.");
        }
        var lines = new List<OrderLine>();
        await using var catalog = await catalogFactory.CreateDbContextAsync(ct);
        foreach (var line in request.Items ?? [])
        {
            if (line.AssemblyId is not null)
            {
                var assemblies = await retail.ListPublishedAssembliesAsync(ct);
                var assembly = assemblies.SingleOrDefault(item => item.Id == line.AssemblyId)
                    ?? throw new RetailException(409, "The selected build is no longer available.");
                string? buildThumbnail = null;
                var buildProductId = assembly.Units.FirstOrDefault()?.ProductId;
                if (buildProductId is not null)
                    buildThumbnail = await catalog.ProductImages.AsNoTracking()
                        .Where(image => image.ProductId == buildProductId)
                        .OrderByDescending(image => image.IsPrimary).ThenBy(image => image.SortOrder)
                        .Select(image => image.ThumbnailStorageKey).FirstOrDefaultAsync(ct);
                lines.Add(new OrderLine(null, assembly.Id, assembly.TitleAr ?? assembly.TitleEn ?? "RIGX build", "BUILD", 1,
                    assembly.SalePrice, null, null, null, null, null, buildThumbnail));
                continue;
            }
            if (line.ProductId is null) throw new RetailException(400, "Each order line needs a product or build.");
            var product = await catalog.Products.AsNoTracking()
                .Include(item => item.Variants)
                .Include(item => item.Images)
                .SingleOrDefaultAsync(item => item.Id == line.ProductId && item.Status == CatalogProductStatus.Published, ct)
                ?? throw new RetailException(409, "A product in the cart is no longer available.");
            var price = product.BasePrice;
            var sku = product.Sku;
            string? variantName = null;
            if (line.VariantId is not null)
            {
                var variant = product.Variants.SingleOrDefault(item => item.Id == line.VariantId && item.IsActive)
                    ?? throw new RetailException(409, "The selected product variant is no longer available.");
                price = variant.Price ?? product.BasePrice;
                sku = variant.Sku;
                variantName = variant.Name;
            }
            var thumbnail = product.Images.OrderByDescending(image => image.IsPrimary)
                .ThenBy(image => image.SortOrder).Select(image => image.ThumbnailStorageKey).FirstOrDefault();
            lines.Add(new OrderLine(product.Id, null, product.Name, sku,
                Math.Clamp(line.Quantity, 1, 20), price, null, null, null, line.VariantId, variantName, thumbnail));
        }
        try
        {
            var order = await retail.CreateOrderAsync(request with
            {
                Items = lines,
                TermsAccepted = true,
                TermsPolicyVersion = termsVersion
            }, accountId, ct);
            return Results.Ok(order);
        }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> ListMyOrdersAsync(HttpContext context, RetailService retail, CancellationToken ct)
    {
        if (!TryGetActor(context, out var accountId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await retail.ListMyOrdersAsync(accountId, ct));
    }

    private static async Task<IResult> GetMyOrderAsync(Guid id, HttpContext context, RetailService retail, CancellationToken ct)
    {
        if (!TryGetActor(context, out var accountId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        try { return Results.Ok(await retail.GetMyOrderAsync(id, accountId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static async Task<IResult> ListEarningsAsync(HttpContext context, RetailService retail, CancellationToken ct, string? partner = null)
    { context.Response.Headers.CacheControl = "no-store"; return Results.Ok(await retail.ListEarningsAsync(partner, ct)); }
    private static async Task<IResult> ListExpensesAsync(HttpContext context, RetailService retail, CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    { context.Response.Headers.CacheControl = "no-store"; return Results.Ok(await retail.ListExpensesAsync(from, to, ct)); }
    private static async Task<IResult> CreateExpenseAsync(ExpenseCreateRequest request, HttpContext context,
        RetailService retail, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        try { return Results.Ok(await retail.CreateExpenseAsync(request, actorId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }
    private static async Task<IResult> BusinessReportAsync(HttpContext context, RetailService retail, CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-30);
        try { return Results.Ok(await retail.BuildReportAsync(start, end, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }
    private static async Task<IResult> ListAdjustmentsAsync(HttpContext context, RetailService retail, CancellationToken ct, string? partner = null)
    { context.Response.Headers.CacheControl = "no-store"; return Results.Ok(await retail.ListAdjustmentsAsync(partner, ct)); }
    private static async Task<IResult> ListPartnerLedgerAsync(HttpContext context, RetailService retail, CancellationToken ct)
    { context.Response.Headers.CacheControl = "no-store"; return Results.Ok(await retail.ListPartnerLedgerAsync(ct)); }
    private static async Task<IResult> ListAgreementsAsync(RetailService retail, CancellationToken ct) => Results.Ok(await retail.ListAgreementsAsync(ct));
    private static async Task<IResult> ListAllocationsAsync(HttpContext context, RetailService retail, CancellationToken ct)
    { context.Response.Headers.CacheControl = "no-store"; return Results.Ok(await retail.ListAllocationsAsync(ct)); }
    private static async Task<IResult> UpsertAllocationAsync(PartnerAllocationRequest request, HttpContext context,
        RetailService retail, CancellationToken ct)
    {
        TryGetActor(context, out var actorId);
        try { return Results.Ok(await retail.UpsertAllocationAsync(request, actorId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }
    private static async Task<IResult> DeactivateAllocationAsync(Guid id, AllocationDeactivateRequest request,
        HttpContext context, RetailService retail, CancellationToken ct)
    {
        TryGetActor(context, out var actorId);
        try { return Results.Ok(await retail.DeactivateAllocationAsync(id, request.RowVersion, request.Reason, actorId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }
    private static async Task<IResult> AllocationHistoryAsync(Guid id, RetailService retail, CancellationToken ct) =>
        Results.Ok(await retail.ListAllocationHistoryAsync(id, ct));
    private static async Task<IResult> PreviewAllocationAsync(HttpContext context, RetailService retail, CancellationToken ct,
        Guid? unitId = null, Guid? productId = null, string? category = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            return Results.Ok(await retail.PreviewAllocationAsync(unitId, productId, category, ct));
        }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }
    private static async Task<IResult> UpsertAgreementAsync(PartnerAgreementRequest request, RetailService retail, CancellationToken ct)
    {
        try { return Results.Ok(await retail.UpsertAgreementAsync(request, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }
    private static async Task<IResult> SettleAsync(SettlementRequest request, HttpContext context, RetailService retail, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        try { return Results.Ok(await retail.SettleAsync(request, actorId, ct)); }
        catch (RetailException ex) { return Results.Problem(statusCode: ex.StatusCode, title: ex.Message); }
    }

    private static bool TryGetActor(HttpContext context, out Guid actorId) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId) && actorId != Guid.Empty;

    private static bool HasPermission(HttpContext context, string permission) =>
        context.User.Claims.Any(claim =>
            claim.Type == PermissionAuthorizationHandler.ClaimType &&
            string.Equals(claim.Value, permission, StringComparison.Ordinal));
}

public sealed record RetailVersionRequest(long RowVersion);
public sealed record ReceiveNotes(string? Notes);
public sealed record AllocationDeactivateRequest(long RowVersion, string? Reason);
