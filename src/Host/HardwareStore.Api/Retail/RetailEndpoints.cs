using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

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
        admin.MapGet("/assemblies", ListAssembliesAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapGet("/assemblies/{id:guid}", GetAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies", CreateAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/assemblies/{id:guid}", UpdateAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/assemblies/{id:guid}/allocations", SetAllocationsAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/publish", PublishAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/unpublish", UnpublishAssemblyAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/disassemble", DisassembleAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/parts", AddAssemblyPartAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/parts/new", AddNewAssemblyPartAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/assemblies/{id:guid}/parts/{unitId:guid}", UpdateAssemblyPartAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/parts/{unitId:guid}/remove", RemoveAssemblyPartAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/parts/{unitId:guid}/replace", ReplaceAssemblyPartAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/photos", AddAssemblyPhotoAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapDelete("/assemblies/{id:guid}/photos", RemoveAssemblyPhotoAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapGet("/orders", ListOrdersAsync).RequirePermission("Sales.ViewOrders");
        admin.MapGet("/orders/{id:guid}", GetOrderDetailAsync).RequirePermission("Sales.ViewOrders");
        admin.MapPost("/pos/preview", PreviewPosSaleAsync).RequirePermission("Sales.CreatePOS");
        admin.MapPost("/pos/sales", CreatePosSaleAsync).RequirePermission("Sales.CreatePOS");
        admin.MapGet("/pos/customers", FindPosCustomerAsync).RequirePermission("Sales.CreatePOS");
        admin.MapPost("/orders/{id:guid}/status", UpdateStatusAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapPost("/orders/{id:guid}/complete", CompleteOrderAsync).RequirePermission("Sales.CompleteOrder");
        admin.MapPost("/orders/{id:guid}/payment", UpdatePaymentAsync).RequirePermission("Finance.VerifyPayment");
        admin.MapPost("/orders/{id:guid}/payment/waive", WaivePaymentAsync).RequirePermission("Finance.VerifyPayment");
        admin.MapPost("/orders/{id:guid}/payments", AddPaymentAsync).RequirePermission("Finance.VerifyPayment");
        admin.MapPut("/orders/{id:guid}/shipping", SetShippingAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapPut("/orders/{id:guid}/reservation", SetReservationAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapGet("/blacklist", ListBlacklistAsync).RequirePermission("Sales.ViewCustomerContact");
        admin.MapPost("/blacklist", AddBlacklistAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapDelete("/blacklist/{id:guid}", RemoveBlacklistAsync).RequirePermission("Sales.ApproveOrder");
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
        endpoints.MapGet("/api/v1/store/sold", ListSoldFeedAsync).AllowAnonymous();
        endpoints.MapPost("/api/v1/orders", CreateCustomerOrderAsync).AllowAnonymous().RequireRateLimiting(IdentityRateLimitPolicies.Checkout);
        endpoints.MapGet("/api/v1/track/{token}", GetTrackingAsync).AllowAnonymous().RequireRateLimiting(IdentityRateLimitPolicies.OrderTracking);
        endpoints.MapPost("/api/v1/track/{token}/rating", RateOrderAsync).AllowAnonymous().RequireRateLimiting(IdentityRateLimitPolicies.OrderTracking);
        endpoints.MapGet("/api/v1/orders/mine", ListMyOrdersAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/orders/mine/{id:guid}", GetMyOrderAsync).RequireAuthorization();
        return endpoints;
    }
}

public sealed record RetailVersionRequest(long RowVersion);
public sealed record AllocationDeactivateRequest(long RowVersion, string? Reason);
