using System.Security.Claims;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Purchasing;
using Microsoft.AspNetCore.Http;

public static class PurchasingEndpoints
{
    public static IEndpointRouteBuilder MapPurchasingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/admin/purchasing/suppliers", GetSuppliersAsync)
            .RequirePermission("Purchasing.View");
        endpoints.MapGet("/api/v1/admin/purchasing/suppliers/{id:guid}", GetSupplierAsync)
            .RequirePermission("Purchasing.View");
        endpoints.MapPost("/api/v1/admin/purchasing/suppliers", CreateSupplierAsync)
            .RequirePermission("Purchasing.ManageSuppliers");
        endpoints.MapPut("/api/v1/admin/purchasing/suppliers/{id:guid}", UpdateSupplierAsync)
            .RequirePermission("Purchasing.ManageSuppliers");
        endpoints.MapGet("/api/v1/admin/purchasing/suppliers/{id:guid}/financials", GetSupplierFinancialsAsync)
            .RequirePermission("Purchasing.ViewSupplierFinancials");
        endpoints.MapPut("/api/v1/admin/purchasing/suppliers/{id:guid}/financials", UpdateSupplierFinancialsAsync)
            .RequirePermission("Purchasing.ManageSupplierFinancials");
        endpoints.MapGet("/api/v1/admin/purchasing/suppliers/{supplierId:guid}/products", GetSupplierProductsAsync)
            .RequirePermission("Purchasing.View");
        endpoints.MapGet("/api/v1/admin/purchasing/suppliers/{supplierId:guid}/products/{id:guid}", GetSupplierProductAsync)
            .RequirePermission("Purchasing.View");
        endpoints.MapPost("/api/v1/admin/purchasing/suppliers/{supplierId:guid}/products", CreateSupplierProductAsync)
            .RequirePermission("Purchasing.ManageSuppliers");
        endpoints.MapPut("/api/v1/admin/purchasing/suppliers/{supplierId:guid}/products/{id:guid}", UpdateSupplierProductAsync)
            .RequirePermission("Purchasing.ManageSuppliers");
        endpoints.MapGet("/api/v1/admin/purchasing/suppliers/{supplierId:guid}/available-variants", GetAvailableVariantsAsync)
            .RequirePermission("Purchasing.View");
        return endpoints;
    }

    private static async Task<IResult> GetSuppliersAsync(
        string? search,
        bool? isActive,
        int? page,
        int? pageSize,
        HttpContext context,
        SupplierDirectoryService suppliers,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await suppliers.GetAsync(
            search,
            isActive,
            page ?? 1,
            pageSize ?? 25,
            cancellationToken));
    }

    private static async Task<IResult> GetSupplierAsync(
        Guid id,
        HttpContext context,
        SupplierDirectoryService suppliers,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        var supplier = await suppliers.GetByIdAsync(id, cancellationToken);
        return supplier is null ? Results.NotFound() : Results.Ok(supplier);
    }

    private static async Task<IResult> CreateSupplierAsync(
        SupplierCreateRequest request,
        HttpContext context,
        SupplierDirectoryService suppliers,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await suppliers.CreateAsync(request, actorId, cancellationToken));
        }
        catch (SupplierDirectoryException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> UpdateSupplierAsync(
        Guid id,
        SupplierUpdateRequest request,
        HttpContext context,
        SupplierDirectoryService suppliers,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await suppliers.UpdateAsync(id, request, actorId, cancellationToken));
        }
        catch (SupplierDirectoryException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> GetSupplierFinancialsAsync(
        Guid id,
        HttpContext context,
        SupplierDirectoryService suppliers,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        try
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(await suppliers.GetFinancialsAsync(id, actorId, cancellationToken));
        }
        catch (SupplierDirectoryException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> UpdateSupplierFinancialsAsync(
        Guid id,
        SupplierFinancialWriteRequest request,
        HttpContext context,
        SupplierDirectoryService suppliers,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await suppliers.UpdateFinancialsAsync(id, request, actorId, cancellationToken));
        }
        catch (SupplierDirectoryException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> GetSupplierProductsAsync(
        Guid supplierId,
        string? search,
        bool? isActive,
        int? page,
        int? pageSize,
        HttpContext context,
        SupplierProductCatalogService catalog,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out _))
            return Results.Unauthorized();
        try
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(await catalog.GetAsync(
                supplierId,
                search,
                isActive,
                page ?? 1,
                pageSize ?? 25,
                cancellationToken));
        }
        catch (PurchasingValidationException exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> GetSupplierProductAsync(
        Guid supplierId,
        Guid id,
        HttpContext context,
        SupplierProductCatalogService catalog,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        var item = await catalog.GetByIdAsync(supplierId, id, cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> CreateSupplierProductAsync(
        Guid supplierId,
        SupplierProductCreateRequest request,
        HttpContext context,
        SupplierProductCatalogService catalog,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await catalog.CreateAsync(supplierId, request, actorId, cancellationToken));
        }
        catch (PurchasingValidationException exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> UpdateSupplierProductAsync(
        Guid supplierId,
        Guid id,
        SupplierProductUpdateRequest request,
        HttpContext context,
        SupplierProductCatalogService catalog,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await catalog.UpdateAsync(supplierId, id, request, actorId, cancellationToken));
        }
        catch (PurchasingValidationException exception)
        {
            return MapException(exception);
        }
    }

    private static async Task<IResult> GetAvailableVariantsAsync(
        Guid supplierId,
        string? search,
        HttpContext context,
        SupplierProductCatalogService catalog,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out _))
            return Results.Unauthorized();
        try
        {
            context.Response.Headers.CacheControl = "no-store";
            return Results.Ok(await catalog.SearchVariantsAsync(supplierId, search, 25, cancellationToken));
        }
        catch (PurchasingValidationException exception)
        {
            return MapException(exception);
        }
    }

    private static IResult MapException(PurchasingValidationException exception) =>
        Results.Problem(statusCode: exception.StatusCode, title: exception.Message);

    private static bool TryGetActor(HttpContext context, out Guid actorId) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId) &&
        actorId != Guid.Empty;
}
