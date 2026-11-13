using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;

public static class IdentityAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapIdentityAdministrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin/identity");

        group.MapGet("/users", GetUsersAsync)
            .RequirePermission("System.ManageUsers");
        group.MapGet("/users/{id:guid}", GetUserAsync)
            .RequirePermission("System.ManageUsers");
        group.MapPost("/users", CreateUserAsync)
            .RequirePermission("System.ManageUsers");
        group.MapPatch("/users/{id:guid}", UpdateUserAsync)
            .RequirePermission("System.ManageUsers");
        group.MapPut("/users/{id:guid}/roles", AssignRolesAsync)
            .RequirePermission("System.ManageUsers")
            .RequireStepUp();
        group.MapGet("/roles", GetRolesAsync)
            .RequirePermission("System.ManageUsers");
        group.MapGet("/permissions", GetPermissionsAsync)
            .RequirePermission("System.ManageUsers");
        group.MapPost("/roles", CreateRoleAsync)
            .RequirePermission("System.ManageUsers")
            .RequireStepUp();
        group.MapPut("/roles/{id:guid}/permissions", UpdateRolePermissionsAsync)
            .RequirePermission("System.ManageUsers")
            .RequireStepUp();
        return endpoints;
    }

    private static async Task<IResult> GetUsersAsync(
        string? search,
        bool? isActive,
        int? page,
        int? pageSize,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await service.GetUsersAsync(
            search,
            isActive,
            page ?? 1,
            pageSize ?? 25,
            cancellationToken));
    }

    private static async Task<IResult> GetUserAsync(
        Guid id,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        var user = await service.GetUserAsync(id, cancellationToken);
        return user is null ? Results.NotFound() : Results.Ok(user);
    }

    private static async Task<IResult> CreateUserAsync(
        CreateAdminIdentityUserRequest request,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        var result = await service.CreateUserAsync(request, actorId, cancellationToken);
        IdentityHttp.NoStore(context);
        return Results.Created((string?)null, result);
    }

    private static async Task<IResult> UpdateUserAsync(
        Guid id,
        UpdateAdminIdentityUserRequest request,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        return Results.Ok(await service.UpdateUserAsync(id, request, actorId, cancellationToken));
    }

    private static async Task<IResult> AssignRolesAsync(
        Guid id,
        AssignAdminIdentityRolesRequest request,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        return Results.Ok(await service.AssignRolesAsync(id, request, actorId, cancellationToken));
    }

    private static async Task<IResult> GetRolesAsync(
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await service.GetRolesAsync(cancellationToken));
    }

    private static async Task<IResult> GetPermissionsAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out _))
            return Results.Unauthorized();
        context.Response.Headers.CacheControl = "no-store";
        await Task.CompletedTask;
        return Results.Ok(PermissionCatalog.All.OrderBy(code => code, StringComparer.Ordinal).ToArray());
    }

    private static async Task<IResult> CreateRoleAsync(
        CreateAdminIdentityRoleRequest request,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        return Results.Ok(await service.CreateRoleAsync(request, actorId, cancellationToken));
    }

    private static async Task<IResult> UpdateRolePermissionsAsync(
        Guid id,
        UpdateAdminIdentityRolePermissionsRequest request,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        if (!context.TryGetUserId(out var actorId))
            return Results.Unauthorized();
        return Results.Ok(await service.UpdateRolePermissionsAsync(id, request, actorId, cancellationToken));
    }
}
