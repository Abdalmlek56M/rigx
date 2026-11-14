using HardwareStore.Common.Domain.Paging;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// Customer administration: list, 360 view, support actions, merge and "view as customer". Business errors are module
/// exceptions mapped once by <c>ProblemDetailsExceptionHandler</c>, so handlers do not catch them.
/// </summary>
public static class CustomerEndpoints
{
    private const string View = "Customers.View";
    private const string Manage = "Customers.Manage";

    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/v1/admin/customers");
        admin.MapGet("", ListAsync).RequirePermission(View);
        admin.MapGet("/{id:guid}", GetAsync).RequirePermission(View);
        admin.MapGet("/{id:guid}/view-as", ViewAsAsync).RequirePermission(View);
        admin.MapPut("/{id:guid}/note", SetNoteAsync).RequirePermission(Manage);
        admin.MapPost("/{id:guid}/login-code", SendLoginCodeAsync).RequirePermission(Manage);
        admin.MapPost("/{id:guid}/google/unlink", UnlinkGoogleAsync).RequirePermission(Manage);
        admin.MapPost("/{id:guid}/sign-out-all", SignOutAllAsync).RequirePermission(Manage);
        admin.MapPost("/{id:guid}/merge", MergeAsync).RequirePermission(Manage).RequireStepUp();
        return endpoints;
    }

    private static async Task<Ok<PagedResult<CustomerListItem>>> ListAsync(HttpContext context, CustomerProfileComposer composer, CancellationToken ct,
        string? search = null, int page = 1)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await composer.ListAsync(search, page, ct));
    }

    private static async Task<Ok<CustomerProfile>> GetAsync(Guid id, HttpContext context, CustomerProfileComposer composer, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await composer.GetAsync(id, context.HasPermission("Inventory.ViewSerials"), ct));
    }

    private static async Task<Ok<CustomerViewAs>> ViewAsAsync(Guid id, HttpContext context, CustomerProfileComposer composer, CustomerAdminService customers,
        CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        var view = await composer.ViewAsAsync(id, ct);
        await customers.RecordViewedAsAsync(id, actorId, ct);
        return TypedResults.Ok(view);
    }

    private static async Task<NoContent> SetNoteAsync(Guid id, CustomerNoteRequest request, HttpContext context, CustomerAdminService customers,
        CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        await customers.SetNoteAsync(id, request.Note, actorId, ct);
        return TypedResults.NoContent();
    }

    private static async Task<Accepted<EmailCodeTicketView>> SendLoginCodeAsync(Guid id, HttpContext context, CustomerAdminService customers, TimeProvider timeProvider,
        CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        IdentityHttp.NoStore(context);
        return TypedResults.Accepted((string?)null, EmailCodeTicketView.From(await customers.SendLoginCodeAsync(id, actorId, ct), timeProvider));
    }

    private static async Task<NoContent> UnlinkGoogleAsync(Guid id, HttpContext context, CustomerAdminService customers, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        await customers.UnlinkGoogleAsync(id, actorId, ct);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> SignOutAllAsync(Guid id, HttpContext context, CustomerAdminService customers, CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        await customers.SignOutAllAsync(id, actorId, context.TraceIdentifier, ct);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<CustomerProfile>> MergeAsync(Guid id, CustomerMergeRequest request, HttpContext context, CustomerMergeOrchestrator merge,
        CancellationToken ct)
    {
        var actorId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await merge.MergeAsync(id, request.SourceAccountId, actorId, context.HasPermission("Inventory.ViewSerials"), ct));
    }
}
