using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Customer requests: the public form and the admin inbox. Business errors are <c>StorefrontException</c>s
/// mapped once by <c>ProblemDetailsExceptionHandler</c>, so handlers do not catch them.
/// </summary>
public static class RequestEndpoints
{
    private const string View = "Requests.View";
    private const string Manage = "Requests.Manage";

    public static IEndpointRouteBuilder MapRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/requests", SubmitAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.CustomerRequest);
        var admin = endpoints.MapGroup("/api/v1/admin/requests");
        admin.MapGet("", ListAsync).RequirePermission(View);
        admin.MapGet("/{id:guid}", GetAsync).RequirePermission(View);
        admin.MapGet("/{id:guid}/whatsapp", WhatsAppAsync).RequirePermission(View);
        admin.MapPut("/{id:guid}/status", SetStatusAsync).RequirePermission(Manage);
        admin.MapPost("/{id:guid}/offer", OfferAsync).RequirePermission(Manage);
        admin.MapPost("/{id:guid}/decision", DecideAsync).RequirePermission(Manage);
        admin.MapPut("/{id:guid}/note", SetNoteAsync).RequirePermission(Manage);
        admin.MapPost("/{id:guid}/convert", ConvertAsync).RequirePermission(Manage);
        return endpoints;
    }

    private static async Task<IResult> SubmitAsync(CustomerRequestSubmission request, CustomerRequestService requests,
        HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        Guid? userId = context.TryGetUserId(out var id) ? id : null;
        return Results.Accepted(value: await requests.SubmitAsync(request, userId, ct));
    }

    private static async Task<IResult> ListAsync(HttpContext context, CustomerRequestService requests, CancellationToken ct,
        string? type = null, string? status = null, string? q = null, int page = 1, int pageSize = 25)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await requests.ListAsync(new CustomerRequestQuery(type, status, q, page, pageSize), ct));
    }

    private static async Task<IResult> GetAsync(Guid id, HttpContext context, CustomerRequestService requests, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await requests.GetAsync(id, ct));
    }

    private static async Task<IResult> WhatsAppAsync(Guid id, HttpContext context, RequestFollowUpService followUp, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await followUp.WhatsAppReplyAsync(id, ct));
    }

    private static async Task<IResult> SetStatusAsync(Guid id, RequestStatusRequest request, RequestFollowUpService followUp, CancellationToken ct) =>
        Results.Ok(await followUp.SetStatusAsync(id, request.Status, request.RowVersion, ct));

    private static async Task<IResult> OfferAsync(Guid id, RequestOfferRequest request, RequestFollowUpService followUp, CancellationToken ct) =>
        Results.Ok(await followUp.OfferAsync(id, request.OfferAmount, request.ExpectedResale, request.RowVersion, ct));

    private static async Task<IResult> DecideAsync(Guid id, RequestDecisionRequest request, RequestFollowUpService followUp, CancellationToken ct) =>
        Results.Ok(await followUp.DecideAsync(id, request.Decision, request.RowVersion, ct));

    private static async Task<IResult> SetNoteAsync(Guid id, RequestNoteRequest request, RequestFollowUpService followUp, CancellationToken ct) =>
        Results.Ok(await followUp.SetNoteAsync(id, request.InternalNote, request.RowVersion, ct));

    private static async Task<IResult> ConvertAsync(Guid id, RequestConvertRequest request, HttpContext context,
        RequestPurchaseConversion conversion, CancellationToken ct)
    {
        if (!context.HasPermission("Purchasing.CreateOrder"))
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Creating a purchase needs the purchase permission.");
        return context.TryGetUserId(out var actorId)
            ? Results.Ok(await conversion.ConvertAsync(id, request.RowVersion, actorId, ct))
            : Results.Unauthorized();
    }
}
