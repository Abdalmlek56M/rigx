using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;

public static partial class RetailEndpoints
{
    private static async Task<IResult> ListPartnersAsync(HttpContext context, PartnerService partners, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await partners.ListAsync(ct));
    }

    private static async Task<IResult> GetPartnerAsync(Guid id, HttpContext context, PartnerService partners, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await partners.GetAsync(id, ct));
    }

    private static async Task<IResult> CreatePartnerAsync(PartnerRequest request, PartnerService partners, CancellationToken ct) =>
        Results.Created((string?)null, await partners.CreateAsync(request, ct));

    private static async Task<IResult> UpdatePartnerAsync(Guid id, PartnerRequest request, PartnerService partners, CancellationToken ct) =>
        Results.Ok(await partners.UpdateAsync(id, request, ct));

    private static async Task<IResult> ListPartnerCapitalAsync(Guid id, HttpContext context, PartnerCapitalService capital, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await capital.ListAsync(id, ct));
    }

    private static async Task<IResult> AddPartnerCapitalAsync(Guid id, PartnerCapitalRequest request, HttpContext context,
        PartnerCapitalService capital, CancellationToken ct) =>
        context.TryGetUserId(out var actorId) ? Results.Ok(await capital.AddAsync(id, request, actorId, ct)) : Results.Unauthorized();

    private static async Task<IResult> PartnerStatementAsync(Guid id, HttpContext context, PartnerStatementService statements, CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return Results.Ok(await statements.GetAsync(id, from ?? end.AddDays(-30), end, ct));
    }

    private static async Task<IResult> ListEarningsAsync(HttpContext context, PartnerLedgerService ledger, CancellationToken ct, Guid? partnerId = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await ledger.ListEarningsAsync(partnerId, ct));
    }

    private static async Task<IResult> ListAdjustmentsAsync(HttpContext context, PartnerLedgerService ledger, CancellationToken ct, Guid? partnerId = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await ledger.ListAdjustmentsAsync(partnerId, ct));
    }

    private static async Task<IResult> ListPartnerPayoutsAsync(Guid id, HttpContext context, PartnerLedgerService ledger, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await ledger.ListPayoutsAsync(id, ct));
    }

    private static async Task<IResult> PayoutPartnerAsync(Guid id, PartnerPayoutRequest request, HttpContext context, PartnerLedgerService ledger,
        CancellationToken ct) =>
        context.TryGetUserId(out var actorId) ? Results.Ok(await ledger.PayoutAsync(id, request, actorId, ct)) : Results.Unauthorized();

    /// <summary>Posts the whole-shop partners' shares of a month that has ended (business time zone).</summary>
    private static async Task<IResult> CloseMonthAsync(PartnerMonthCloseRequest request, PartnerMonthCloseService months,
        TimeZoneInfo businessTimeZone, CancellationToken ct) =>
        Results.Ok(await months.CloseMonthAsync(request, businessTimeZone, ct));

    private static async Task<IResult> AssignUnitPartnerAsync(Guid id, UnitPartnerRequest request, UnitPartnerService units, CancellationToken ct) =>
        Results.Ok(await units.AssignAsync(id, request, ct));
}
