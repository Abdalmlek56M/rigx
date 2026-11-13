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
        admin.MapGet("/stock-items", ListStockItemsAsync).RequirePermission("Inventory.View");
        admin.MapGet("/stock-items/{id:guid}", GetStockItemAsync).RequirePermission("Inventory.View");
        admin.MapPut("/stock-items/{id:guid}", UpdateStockItemAsync).RequirePermission("Inventory.StockAdjust");
        admin.MapPost("/stock-items/visibility", SetStockVisibilityAsync).RequirePermission("Inventory.StockAdjust");
        admin.MapPost("/stock-items/{id:guid}/photos", AddStockItemPhotoAsync).RequirePermission("Inventory.StockAdjust");
        admin.MapDelete("/stock-items/{id:guid}/photos", RemoveStockItemPhotoAsync).RequirePermission("Inventory.StockAdjust");
        admin.MapGet("/models/suggest", SuggestModelsAsync).RequirePermission("Inventory.View");
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

        endpoints.MapGet("/api/v1/store/items", ListStoreItemsAsync).AllowAnonymous();
        endpoints.MapGet("/api/v1/store/items/{code}", GetStoreItemAsync).AllowAnonymous();
        endpoints.MapGet("/api/v1/store/assemblies", ListPublishedAssembliesAsync).AllowAnonymous();
        endpoints.MapPost("/api/v1/orders", CreateCustomerOrderAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/orders/mine", ListMyOrdersAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/orders/mine/{id:guid}", GetMyOrderAsync).RequireAuthorization();
        return endpoints;
    }
}

public sealed record RetailVersionRequest(long RowVersion);
public sealed record ReceiveNotes(string? Notes);
public sealed record AllocationDeactivateRequest(long RowVersion, string? Reason);
