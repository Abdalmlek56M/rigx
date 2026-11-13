using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Retail;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public static partial class RetailEndpoints
{
    private static async Task<IResult> ListOrdersAsync(HttpContext context, OrderService orders, CancellationToken ct, string? status = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await orders.ListOrdersAsync(status, HasPermission(context, "Sales.ViewCustomerContact"), ct));
    }

    private static async Task<IResult> GetOrderDetailAsync(Guid id, HttpContext context, OrderService orders, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await orders.GetAdminOrderDetailAsync(id, HasPermission(context, "Inventory.ViewSerials"), ct));
    }

    /// <summary>POS/manual order: prices come from the published catalog or build (Catalog + Retail, so it stays in Host).</summary>
    private static async Task<IResult> CreateManualOrderAsync(
        OrderCreateRequest request, HttpContext context, OrderService orders, AssemblyService assemblies,
        IDbContextFactory<CatalogDbContext> catalogFactory, CancellationToken ct)
    {
        if (!TryGetActor(context, out _)) return Results.Unauthorized();
        var lines = new List<OrderLine>();
        await using var catalog = await catalogFactory.CreateDbContextAsync(ct);
        var publishedAssemblies = await assemblies.ListPublishedAssembliesAsync(ct);
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
        return Results.Ok(await orders.CreateOrderAsync(
            request with { Items = lines, LeadSource = request.LeadSource ?? LeadSource.Manual }, null, ct));
    }

    private static async Task<IResult> UpdateStatusAsync(Guid id, OrderStatusRequest request, HttpContext context, OrderStatusService statuses, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        return Results.Ok(await statuses.UpdateStatusAsync(id, request, actorId, ct));
    }

    private static async Task<IResult> CompleteOrderAsync(Guid id, CompleteOrderRequest request, HttpContext context, OrderStatusService statuses, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        return Results.Ok(await statuses.CompleteOrderAsync(id, request, actorId, ct));
    }

    private static async Task<IResult> UpdatePaymentAsync(Guid id, PaymentStatusRequest request, HttpContext context, OrderPaymentService payments, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        return Results.Ok(await payments.UpdatePaymentAsync(id, request, actorId, ct));
    }

    private static async Task<IResult> WaivePaymentAsync(Guid id, WaivePaymentRequest request, HttpContext context, OrderPaymentService payments, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        return Results.Ok(await payments.WaivePaymentAsync(id, request, actorId, ct));
    }

    private static async Task<IResult> ListReturnsAsync(HttpContext context, ReturnService returns, CancellationToken ct, string? status = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await returns.ListReturnsAsync(status, ct));
    }

    private static async Task<IResult> CreateReturnAsync(ReturnCreateRequest request, HttpContext context, ReturnService returns, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        return Results.Ok(await returns.CreateReturnAsync(request, actorId, ct));
    }

    private static async Task<IResult> InspectReturnAsync(Guid id, ReturnInspectRequest request, HttpContext context, ReturnService returns, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        return Results.Ok(await returns.InspectReturnAsync(id, request, actorId, ct));
    }

    private static async Task<IResult> RejectReturnAsync(Guid id, ReturnRejectRequest request, HttpContext context, ReturnService returns, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        return Results.Ok(await returns.RejectReturnAsync(id, request, actorId, ct));
    }

    /// <summary>
    /// Customer checkout: requires accepted, published Terms (Storefront) and re-prices every line from the
    /// published catalog/build (Catalog + Retail), so it stays in Host.
    /// </summary>
    private static async Task<IResult> CreateCustomerOrderAsync(
        OrderCreateRequest request, HttpContext context, OrderService orders, AssemblyService assemblies,
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
                var published = await assemblies.ListPublishedAssembliesAsync(ct);
                var assembly = published.SingleOrDefault(item => item.Id == line.AssemblyId)
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
        var order = await orders.CreateOrderAsync(request with
        {
            Items = lines,
            TermsAccepted = true,
            TermsPolicyVersion = termsVersion
        }, accountId, ct);
        return Results.Ok(order);
    }

    private static async Task<IResult> ListMyOrdersAsync(HttpContext context, OrderService orders, CancellationToken ct)
    {
        if (!TryGetActor(context, out var accountId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await orders.ListMyOrdersAsync(accountId, ct));
    }

    private static async Task<IResult> GetMyOrderAsync(Guid id, HttpContext context, OrderService orders, CancellationToken ct)
    {
        if (!TryGetActor(context, out var accountId)) return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await orders.GetMyOrderAsync(id, accountId, ct));
    }
}
