using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Http;

public static partial class RetailEndpoints
{
    private static async Task<IResult> ListEarningsAsync(HttpContext context, PartnerLedgerService ledger, CancellationToken ct, string? partner = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await ledger.ListEarningsAsync(partner, ct));
    }

    private static async Task<IResult> ListAdjustmentsAsync(HttpContext context, PartnerLedgerService ledger, CancellationToken ct, string? partner = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await ledger.ListAdjustmentsAsync(partner, ct));
    }

    private static async Task<IResult> ListPartnerLedgerAsync(HttpContext context, PartnerLedgerService ledger, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await ledger.ListPartnerLedgerAsync(ct));
    }

    private static async Task<IResult> SettleAsync(SettlementRequest request, HttpContext context, PartnerLedgerService ledger, CancellationToken ct)
    {
        if (!TryGetActor(context, out var actorId)) return Results.Unauthorized();
        return Results.Ok(await ledger.SettleAsync(request, actorId, ct));
    }

    private static async Task<IResult> ListAgreementsAsync(PartnerAgreementService agreements, CancellationToken ct) =>
        Results.Ok(await agreements.ListAgreementsAsync(ct));

    private static async Task<IResult> UpsertAgreementAsync(PartnerAgreementRequest request, PartnerAgreementService agreements, CancellationToken ct) =>
        Results.Ok(await agreements.UpsertAgreementAsync(request, ct));

    private static async Task<IResult> ListAllocationsAsync(HttpContext context, PartnerAllocationService allocations, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await allocations.ListAllocationsAsync(ct));
    }

    private static async Task<IResult> UpsertAllocationAsync(PartnerAllocationRequest request, HttpContext context,
        PartnerAllocationService allocations, CancellationToken ct)
    {
        TryGetActor(context, out var actorId);
        return Results.Ok(await allocations.UpsertAllocationAsync(request, actorId, ct));
    }

    private static async Task<IResult> DeactivateAllocationAsync(Guid id, AllocationDeactivateRequest request,
        HttpContext context, PartnerAllocationService allocations, CancellationToken ct)
    {
        TryGetActor(context, out var actorId);
        return Results.Ok(await allocations.DeactivateAllocationAsync(id, request.RowVersion, request.Reason, actorId, ct));
    }

    private static async Task<IResult> AllocationHistoryAsync(Guid id, PartnerAllocationService allocations, CancellationToken ct) =>
        Results.Ok(await allocations.ListAllocationHistoryAsync(id, ct));

    private static async Task<IResult> PreviewAllocationAsync(HttpContext context, PartnerAllocationService allocations, CancellationToken ct,
        Guid? unitId = null, Guid? productId = null, string? category = null)
    {
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await allocations.PreviewAllocationAsync(unitId, productId, category, ct));
    }
}
