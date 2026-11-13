using System.Security.Claims;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Inventory;
using Microsoft.AspNetCore.Http;

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

    private static async Task<IResult> GetWarehousesAsync(
        string? search,
        bool? isActive,
        int? page,
        int? pageSize,
        HttpContext context,
        WarehouseDirectoryService warehouses,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await warehouses.GetAsync(
            search,
            isActive,
            page ?? 1,
            pageSize ?? 25,
            cancellationToken));
    }

    private static async Task<IResult> GetWarehouseAsync(
        Guid id,
        HttpContext context,
        WarehouseDirectoryService warehouses,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        var warehouse = await warehouses.GetByIdAsync(id, cancellationToken);
        return warehouse is null ? Results.NotFound() : Results.Ok(warehouse);
    }

    private static async Task<IResult> CreateWarehouseAsync(
        WarehouseCreateRequest request,
        HttpContext context,
        WarehouseDirectoryService warehouses,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await warehouses.CreateAsync(request, actorId, cancellationToken));
        }
        catch (WarehouseDirectoryException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static async Task<IResult> UpdateWarehouseAsync(
        Guid id,
        WarehouseUpdateRequest request,
        HttpContext context,
        WarehouseDirectoryService warehouses,
        CancellationToken cancellationToken)
    {
        if (!TryGetActor(context, out var actorId))
            return Results.Unauthorized();
        try
        {
            return Results.Ok(await warehouses.UpdateAsync(id, request, actorId, cancellationToken));
        }
        catch (WarehouseDirectoryException exception)
        {
            return Results.Problem(statusCode: exception.StatusCode, title: exception.Message);
        }
    }

    private static bool TryGetActor(HttpContext context, out Guid actorId) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId) &&
        actorId != Guid.Empty;
}
