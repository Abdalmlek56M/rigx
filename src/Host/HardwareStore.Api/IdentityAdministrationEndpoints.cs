using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

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
        group.MapGet("/users/{id:guid}/profile", GetProfileAsync)
            .RequirePermission("System.ManageUsers");
        group.MapPut("/users/{id:guid}/note", SetNoteAsync)
            .RequirePermission("System.ManageUsers");
        group.MapPost("/users/{id:guid}/sign-out-all", SignOutAllAsync)
            .RequirePermission("System.ManageUsers");
        group.MapPut("/users/{id:guid}/roles", AssignRolesAsync)
            .RequirePermission("System.ManageUsers")
            .RequireStepUp();
        group.MapGet("/roles", GetRolesAsync)
            .RequirePermission("System.ManageUsers");
        group.MapGet("/permissions", GetPermissions)
            .RequirePermission("System.ManageUsers");
        group.MapPost("/roles", CreateRoleAsync)
            .RequirePermission("System.ManageUsers")
            .RequireStepUp();
        group.MapPut("/roles/{id:guid}/permissions", UpdateRolePermissionsAsync)
            .RequirePermission("System.ManageUsers")
            .RequireStepUp();
        return endpoints;
    }

    private static async Task<Ok<PagedResult<AdminIdentityUser>>> GetUsersAsync(
        string? search,
        bool? isActive,
        int? page,
        int? pageSize,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await service.GetUsersAsync(
            search,
            isActive,
            page ?? 1,
            pageSize ?? 25,
            cancellationToken));
    }

    private static async Task<Ok<AdminIdentityUser>> GetUserAsync(
        Guid id,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        var user = await service.GetUserAsync(id, cancellationToken);
        return TypedResults.Ok(user ?? throw new IdentityException(StatusCodes.Status404NotFound, "The user was not found."));
    }

    private static async Task<Ok<StaffProfile>> GetProfileAsync(
        Guid id,
        HttpContext context,
        StaffProfileComposer composer,
        CancellationToken cancellationToken)
    {
        context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await composer.GetAsync(id, context.HasPermission("Inventory.ViewSerials"), cancellationToken));
    }

    private static async Task<NoContent> SetNoteAsync(
        Guid id,
        StaffNoteRequest request,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        await service.SetStaffNoteAsync(id, request.Note, actorId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> SignOutAllAsync(
        Guid id,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        await service.SignOutStaffEverywhereAsync(id, actorId, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Created<AdminIdentityUserInvitationResult>> CreateUserAsync(
        CreateAdminIdentityUserRequest request,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        var result = await service.CreateUserAsync(request, actorId, cancellationToken);
        IdentityHttp.NoStore(context);
        return TypedResults.Created((string?)null, result);
    }

    private static async Task<Ok<AdminIdentityUser>> UpdateUserAsync(
        Guid id,
        UpdateAdminIdentityUserRequest request,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await service.UpdateUserAsync(id, request, actorId, cancellationToken));
    }

    private static async Task<Ok<AdminIdentityUser>> AssignRolesAsync(
        Guid id,
        AssignAdminIdentityRolesRequest request,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await service.AssignRolesAsync(id, request, actorId, cancellationToken));
    }

    private static async Task<Ok<IReadOnlyList<AdminIdentityRole>>> GetRolesAsync(
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await service.GetRolesAsync(cancellationToken));
    }

    private static Ok<string[]> GetPermissions(HttpContext context)
    {
        context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(PermissionCatalog.All.OrderBy(code => code, StringComparer.Ordinal).ToArray());
    }

    private static async Task<Ok<AdminIdentityRole>> CreateRoleAsync(
        CreateAdminIdentityRoleRequest request,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await service.CreateRoleAsync(request, actorId, cancellationToken));
    }

    private static async Task<Ok<AdminIdentityRole>> UpdateRolePermissionsAsync(
        Guid id,
        UpdateAdminIdentityRolePermissionsRequest request,
        HttpContext context,
        IdentityAdministrationService service,
        CancellationToken cancellationToken)
    {
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await service.UpdateRolePermissionsAsync(id, request, actorId, cancellationToken));
    }
}
