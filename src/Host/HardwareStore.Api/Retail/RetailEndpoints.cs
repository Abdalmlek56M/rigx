using System.Security.Claims;
using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Retail HTTP API. Routes and permissions are listed here; handlers live in one partial file per area
/// (inventory, assemblies, sales, partners, financials). Business errors are <c>RetailException</c>s
/// mapped once by <c>ProblemDetailsExceptionHandler</c>, so handlers do not catch them.
/// </summary>
public static partial class RetailEndpoints
{
    public static IEndpointRouteBuilder MapRetailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/v1/admin/retail");
        admin.MapGet("/units", ListUnitsAsync).RequirePermission("Inventory.View");
        admin.MapPost("/receipts/from-draft/{draftId:guid}", ReceiveFromDraftAsync).RequirePermission("Purchasing.ReceiveGoods");
        admin.MapGet("/assemblies", ListAssembliesAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies", CreateAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/assemblies/{id:guid}", UpdateAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/assemblies/{id:guid}/allocations", SetAllocationsAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/publish", PublishAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/unpublish", UnpublishAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapDelete("/assemblies/{id:guid}/units/{unitId:guid}", RemoveUnitAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/swap", SwapAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/disassemble", DisassembleAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapGet("/orders", ListOrdersAsync).RequirePermission("Sales.ViewOrders");
        admin.MapGet("/orders/{id:guid}", GetOrderDetailAsync).RequirePermission("Sales.ViewOrders");
        admin.MapPost("/orders", CreateManualOrderAsync).RequirePermission("Sales.CreatePOS");
        admin.MapPost("/orders/{id:guid}/status", UpdateStatusAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapPost("/orders/{id:guid}/complete", CompleteOrderAsync).RequirePermission("Sales.CompleteOrder");
        admin.MapPost("/orders/{id:guid}/payment", UpdatePaymentAsync).RequirePermission("Finance.VerifyPayment");
        admin.MapPost("/orders/{id:guid}/payment/waive", WaivePaymentAsync).RequirePermission("Finance.VerifyPayment");
        admin.MapGet("/returns", ListReturnsAsync).RequirePermission("Sales.ViewOrders");
        admin.MapPost("/returns", CreateReturnAsync).RequirePermission("Sales.ProcessReturn");
        admin.MapPost("/returns/{id:guid}/inspect", InspectReturnAsync).RequirePermission("Sales.ProcessReturn");
        admin.MapPost("/returns/{id:guid}/reject", RejectReturnAsync).RequirePermission("Sales.ProcessReturn");
        admin.MapGet("/partner/earnings", ListEarningsAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/expenses", ListExpensesAsync).RequirePermission("Finance.ViewReports");
        admin.MapPost("/expenses", CreateExpenseAsync).RequirePermission("Finance.ManageExpenses");
        admin.MapGet("/reports/business", BusinessReportAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partner/adjustments", ListAdjustmentsAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partner/ledger", ListPartnerLedgerAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partner/allocations", ListAllocationsAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partner/allocations/preview", PreviewAllocationAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partner/allocations/{id:guid}/history", AllocationHistoryAsync).RequirePermission("Finance.ViewReports");
        admin.MapPost("/partner/allocations", UpsertAllocationAsync).RequirePermission("Finance.ManagePartnerAgreements");
        admin.MapPost("/partner/allocations/{id:guid}/deactivate", DeactivateAllocationAsync).RequirePermission("Finance.ManagePartnerAgreements");
        admin.MapGet("/partner/agreements", ListAgreementsAsync).RequirePermission("Finance.ViewReports");
        admin.MapPost("/partner/agreements", UpsertAgreementAsync).RequirePermission("Finance.ManagePartnerAgreements");
        admin.MapPost("/partner/settlements", SettleAsync).RequirePermission("Finance.PostPartnerSettlements");

        endpoints.MapGet("/api/v1/store/assemblies", ListPublishedAssembliesAsync).AllowAnonymous();
        endpoints.MapPost("/api/v1/orders", CreateCustomerOrderAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/orders/mine", ListMyOrdersAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/orders/mine/{id:guid}", GetMyOrderAsync).RequireAuthorization();
        return endpoints;
    }

    private static bool TryGetActor(HttpContext context, out Guid actorId) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId) && actorId != Guid.Empty;

    private static bool HasPermission(HttpContext context, string permission) =>
        context.User.Claims.Any(claim =>
            claim.Type == PermissionAuthorizationHandler.ClaimType &&
            string.Equals(claim.Value, permission, StringComparison.Ordinal));
}

public sealed record RetailVersionRequest(long RowVersion);
public sealed record ReceiveNotes(string? Notes);
public sealed record AllocationDeactivateRequest(long RowVersion, string? Reason);
