using HardwareStore.Common.Domain;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public static partial class RetailEndpoints
{
    private static async Task<Ok<IReadOnlyList<PartnerView>>> ListPartnersAsync(HttpContext context, PartnerService partners, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await partners.ListAsync(ct));
    }

    private static async Task<Ok<PartnerDetailView>> GetPartnerAsync(Guid id, HttpContext context, PartnerService partners, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await partners.GetAsync(id, ct));
    }

    private static async Task<Created<PartnerView>> CreatePartnerAsync(PartnerRequest request, PartnerService partners, CancellationToken ct) =>
        TypedResults.Created((string?)null, await partners.CreateAsync(request, ct));

    private static async Task<Ok<PartnerView>> UpdatePartnerAsync(Guid id, PartnerRequest request, PartnerService partners, CancellationToken ct) =>
        TypedResults.Ok(await partners.UpdateAsync(id, request, ct));

    private static async Task<Ok<IReadOnlyList<PartnerCapitalEntryView>>> ListPartnerCapitalAsync(Guid id, HttpContext context, PartnerCapitalService capital, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await capital.ListAsync(id, ct));
    }

    private static async Task<Ok<PartnerCapitalEntryView>> AddPartnerCapitalAsync(Guid id, PartnerCapitalRequest request, HttpContext context,
        PartnerCapitalService capital, CancellationToken ct) =>
        TypedResults.Ok(await capital.AddAsync(id, request, context.RequireUserId(), ct));

    private static async Task<Ok<PartnerStatementView>> PartnerStatementAsync(Guid id, HttpContext context, PartnerStatementService statements, TimeProvider timeProvider,
        TimeZoneInfo businessZone, CancellationToken ct,
        DateOnly? from = null, DateOnly? to = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        var end = to ?? BusinessDay.Today(timeProvider, businessZone);
        return TypedResults.Ok(await statements.GetAsync(id, from ?? end.AddDays(-30), end, ct));
    }

    private static async Task<Ok<IReadOnlyList<PartnerEarningView>>> ListEarningsAsync(HttpContext context, PartnerLedgerService ledger, CancellationToken ct, Guid? partnerId = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await ledger.ListEarningsAsync(partnerId, ct));
    }

    private static async Task<Ok<IReadOnlyList<PartnerAdjustmentView>>> ListAdjustmentsAsync(HttpContext context, PartnerLedgerService ledger, CancellationToken ct, Guid? partnerId = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await ledger.ListAdjustmentsAsync(partnerId, ct));
    }

    private static async Task<Ok<IReadOnlyList<PartnerSettlementView>>> ListPartnerPayoutsAsync(Guid id, HttpContext context, PartnerLedgerService ledger, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Ok(await ledger.ListPayoutsAsync(id, ct));
    }

    private static async Task<Ok<PartnerSettlementView>> PayoutPartnerAsync(Guid id, PartnerPayoutRequest request, HttpContext context, PartnerLedgerService ledger,
        CancellationToken ct) =>
        TypedResults.Ok(await ledger.PayoutAsync(id, request, context.RequireUserId(), ct));

    /// <summary>Posts the whole-shop partners' shares of a month that has ended (business time zone).</summary>
    private static async Task<Ok<PartnerMonthCloseView>> CloseMonthAsync(PartnerMonthCloseRequest request, PartnerMonthCloseService months,
        TimeZoneInfo businessTimeZone, CancellationToken ct) =>
        TypedResults.Ok(await months.CloseMonthAsync(request, businessTimeZone, ct));

    private static async Task<Ok<UnitView>> AssignUnitPartnerAsync(Guid id, UnitPartnerRequest request, UnitPartnerService units, CancellationToken ct) =>
        TypedResults.Ok(await units.AssignAsync(id, request, ct));
}
