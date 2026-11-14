using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/admin/inventory/warehouses", GetWarehousesAsync)
            .RequirePermission("Inventory.View");
        endpoints.MapGet("/api/v1/admin/inventory/warehouses/{id:guid}", GetWarehouseAsync)
            .RequirePermission("Inventory.View");
        endpoints.MapPost("/api/v1/admin/inventory/warehouses", CreateWarehouseAsync)
            .RequirePermission("Inventory.ManageWarehouses");
        endpoints.MapPut("/api/v1/admin/inventory/warehouses/{id:guid}", UpdateWarehouseAsync)
            .RequirePermission("Inventory.ManageWarehouses");
        return endpoints;
    }

    private static async Task<Ok<PagedResult<WarehouseView>>> GetWarehousesAsync(
        string? search,
        bool? isActive,
        int? page,
        int? pageSize,
        HttpContext context,
        WarehouseDirectoryService warehouses,
        CancellationToken cancellationToken)
    {
        context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await warehouses.GetAsync(
            search,
            isActive,
            page ?? 1,
            pageSize ?? 25,
            cancellationToken));
    }

    private static async Task<Ok<WarehouseView>> GetWarehouseAsync(
        Guid id,
        HttpContext context,
        WarehouseDirectoryService warehouses,
        CancellationToken cancellationToken)
    {
        context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        var warehouse = await warehouses.GetByIdAsync(id, cancellationToken);
        return TypedResults.Ok(warehouse ?? throw new WarehouseDirectoryException(StatusCodes.Status404NotFound, "The warehouse was not found."));
    }

    private static async Task<Ok<WarehouseView>> CreateWarehouseAsync(
        WarehouseCreateRequest request,
        HttpContext context,
        WarehouseDirectoryService warehouses,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await warehouses.CreateAsync(request, actorId, cancellationToken));
    }

    private static async Task<Ok<WarehouseView>> UpdateWarehouseAsync(
        Guid id,
        WarehouseUpdateRequest request,
        HttpContext context,
        WarehouseDirectoryService warehouses,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await warehouses.UpdateAsync(id, request, actorId, cancellationToken));
    }
}
