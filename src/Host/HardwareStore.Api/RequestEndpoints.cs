using HardwareStore.Common.Domain;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Storefront;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Customer requests: the public form and the admin inbox. Business errors are <c>StorefrontException</c>s
/// mapped once by <c>ProblemDetailsExceptionHandler</c>, so handlers do not catch them.
/// </summary>
public static class RequestEndpoints
{
    private const string View = "Requests.View";
    private const string Manage = "Requests.Manage";
    private const int DefaultDemandDays = 30;

    public static IEndpointRouteBuilder MapRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/requests", SubmitAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.CustomerRequest);
        endpoints.MapPost("/api/v1/requests/photos", UploadPhotoAsync)
            .AllowAnonymous()
            .RequireRateLimiting(IdentityRateLimitPolicies.RequestPhoto);
        endpoints.MapGet("/api/v1/account/requests", ListMineAsync).RequireAuthorization();
        var admin = endpoints.MapGroup("/api/v1/admin/requests");
        admin.MapGet("", ListAsync).RequirePermission(View);
        admin.MapGet("/back-in-stock", BackInStockAsync).RequirePermission(View);
        admin.MapGet("/demand", DemandAsync).RequirePermission(View);
        admin.MapGet("/{id:guid}", GetAsync).RequirePermission(View);
        admin.MapGet("/{id:guid}/photos/{index:int}", GetPhotoAsync).RequirePermission(View)
            .Produces<Stream>(StatusCodes.Status200OK, "image/webp");
        admin.MapGet("/{id:guid}/whatsapp", WhatsAppAsync).RequirePermission(View);
        admin.MapPut("/{id:guid}/status", SetStatusAsync).RequirePermission(Manage);
        admin.MapPost("/{id:guid}/offer", OfferAsync).RequirePermission(Manage);
        admin.MapPost("/{id:guid}/decision", DecideAsync).RequirePermission(Manage);
        admin.MapPut("/{id:guid}/note", SetNoteAsync).RequirePermission(Manage);
        admin.MapPost("/{id:guid}/back-in-stock/sent", BackInStockSentAsync).RequirePermission(Manage);
        admin.MapPost("/{id:guid}/convert", ConvertAsync).RequirePermission(Manage);
        return endpoints;
    }

    private static async Task<Accepted<CustomerRequestReceipt>> SubmitAsync(CustomerRequestSubmission request, CustomerRequestService requests,
        HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        Guid? userId = context.TryGetUserId(out var id) ? id : null;
        return TypedResults.Accepted((string?)null, await requests.SubmitAsync(request, userId, ct));
    }

    /// <summary>
    /// The customer attaches a photo with the token from the submission receipt. The photo is private and never watermarked; the token and
    /// the photo limit are checked before the image is processed and again when it is attached.
    /// </summary>
    private static async Task<Ok<RequestPhotoReceipt>> UploadPhotoAsync(HttpContext context, RequestPhotoService photos, ICatalogMediaStorage storage,
        CatalogMediaOptions options, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        var (form, upload) = await CatalogEndpoints.ReadImageFormAsync(context, options, ct);
        var token = form["token"].ToString();
        var requestId = await photos.AuthorizeAsync(token, ct);
        var key = await storage.SavePrivateImageAsync("requests", requestId, upload, ct);
        try { return TypedResults.Ok(new RequestPhotoReceipt(await photos.AttachAsync(token, key, ct))); }
        catch { await storage.DeleteStorageKeyAsync(key, CancellationToken.None); throw; }
    }

    private static async Task<FileStreamHttpResult> GetPhotoAsync(Guid id, int index, HttpContext context, RequestPhotoService photos,
        ICatalogMediaStorage storage, CancellationToken ct) =>
        PrivateMediaResults.Image(context, storage, await photos.GetPhotoKeyAsync(id, index, ct));

    private static async Task<Ok<IReadOnlyList<MyRequestView>>> ListMineAsync(HttpContext context, AccountRequestService requests, CancellationToken ct)
    {
        var userId = context.RequireUserId();
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await requests.ListMineAsync(userId, ct));
    }

    private static async Task<Ok<CustomerRequestPage>> ListAsync(HttpContext context, CustomerRequestService requests, CancellationToken ct,
        string? type = null, string? status = null, string? q = null, int page = 1, int pageSize = 25)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await requests.ListAsync(new CustomerRequestQuery(type, status, q, page, pageSize), ct));
    }

    private static async Task<Ok<CustomerRequestView>> GetAsync(Guid id, HttpContext context, CustomerRequestService requests, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await requests.GetAsync(id, ct));
    }

    private static async Task<Ok<WhatsAppReplyView>> WhatsAppAsync(Guid id, HttpContext context, RequestFollowUpService followUp, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await followUp.WhatsAppReplyAsync(id, ct));
    }

    private static async Task<Ok<IReadOnlyList<BackInStockListItem>>> BackInStockAsync(HttpContext context, RequestFollowUpService followUp, OrderLinks links, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await followUp.BackInStockListAsync(links.ItemUrl, ct));
    }

    /// <summary>Default period: the last 30 business days ending today.</summary>
    private static async Task<Ok<IReadOnlyList<NotifyMeDemandRow>>> DemandAsync(HttpContext context, NotifyMeService notifyMe, TimeZoneInfo businessTimeZone,
        TimeProvider timeProvider, CancellationToken ct, DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var end = to ?? BusinessDay.Today(timeProvider, businessTimeZone);
        return TypedResults.Ok(await notifyMe.DemandAsync(from ?? end.AddDays(1 - DefaultDemandDays), end, ct));
    }

    private static async Task<Ok<CustomerRequestView>> BackInStockSentAsync(Guid id, RequestFollowUpService followUp, CancellationToken ct) =>
        TypedResults.Ok(await followUp.MarkBackInStockSentAsync(id, ct));

    private static async Task<Ok<CustomerRequestView>> SetStatusAsync(Guid id, RequestStatusRequest request, RequestFollowUpService followUp, CancellationToken ct) =>
        TypedResults.Ok(await followUp.SetStatusAsync(id, request.Status, request.RowVersion, ct));

    private static async Task<Ok<CustomerRequestView>> OfferAsync(Guid id, RequestOfferRequest request, RequestFollowUpService followUp, CancellationToken ct) =>
        TypedResults.Ok(await followUp.OfferAsync(id, request.OfferAmount, request.ExpectedResale, request.RowVersion, ct));

    private static async Task<Ok<CustomerRequestView>> DecideAsync(Guid id, RequestDecisionRequest request, RequestFollowUpService followUp, CancellationToken ct) =>
        TypedResults.Ok(await followUp.DecideAsync(id, request.Decision, request.RowVersion, ct));

    private static async Task<Ok<CustomerRequestView>> SetNoteAsync(Guid id, RequestNoteRequest request, RequestFollowUpService followUp, CancellationToken ct) =>
        TypedResults.Ok(await followUp.SetNoteAsync(id, request.InternalNote, request.RowVersion, ct));

    private static async Task<Ok<CustomerRequestView>> ConvertAsync(Guid id, RequestConvertRequest request, HttpContext context,
        RequestPurchaseConversion conversion, CancellationToken ct)
    {
        if (!context.HasPermission("Purchasing.CreateOrder"))
            throw new ApiProblemException(StatusCodes.Status403Forbidden, "Creating a purchase needs the purchase permission.");
        var actorId = context.RequireUserId();
        return TypedResults.Ok(await conversion.ConvertAsync(id, request.RowVersion, actorId, ct));
    }
}
