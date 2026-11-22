using HardwareStore.Modules.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Retail HTTP API. Routes and permissions are listed here; handlers live in one partial file per area
/// (inventory, assemblies, sales, partners, financials, money). Business errors are <c>RetailException</c>s
/// mapped once by <c>ProblemDetailsExceptionHandler</c>, so handlers do not catch them.
/// </summary>
public static partial class RetailEndpoints
{
    private const string ReceiveGoods = "Purchasing.ReceiveGoods";
    /// <summary>Coupons sit with offers: the store settings permission.</summary>
    private const string CouponPermission = "System.StoreSettings";

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
        admin.MapPut("/stock-items/{id:guid}/photos/order", ReorderStockItemPhotosAsync).RequirePermission("Inventory.StockAdjust");
        admin.MapGet("/stock-import/template", GetStockImportTemplateAsync).RequirePermission(ReceiveGoods)
            .Produces<Stream>(StatusCodes.Status200OK, XlsxContentType);
        admin.MapPost("/stock-import", ImportStockAsync).RequirePermission(ReceiveGoods);
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
        admin.MapPut("/assemblies/{id:guid}/planned-parts", SetPlannedAssemblyPartsAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/assemblies/{id:guid}/parts/{unitId:guid}", UpdateAssemblyPartAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/parts/{unitId:guid}/remove", RemoveAssemblyPartAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/parts/{unitId:guid}/replace", ReplaceAssemblyPartAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPost("/assemblies/{id:guid}/photos", AddAssemblyPhotoAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapDelete("/assemblies/{id:guid}/photos", RemoveAssemblyPhotoAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapPut("/assemblies/{id:guid}/photos/order", ReorderAssemblyPhotosAsync).RequirePermission("Catalog.ManageProducts");
        admin.MapGet("/orders", ListOrdersAsync).RequirePermission("Sales.ViewOrders");
        admin.MapGet("/orders/{id:guid}", GetOrderDetailAsync).RequirePermission("Sales.ViewOrders");
        admin.MapPost("/pos/preview", PreviewPosSaleAsync).RequirePermission("Sales.CreatePOS");
        admin.MapPost("/pos/sales", CreatePosSaleAsync).RequirePermission("Sales.CreatePOS");
        admin.MapGet("/pos/customers", SearchPosCustomersAsync).RequirePermission("Sales.CreatePOS");
        admin.MapPost("/orders/{id:guid}/status", UpdateStatusAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapPost("/orders/{id:guid}/complete", CompleteOrderAsync).RequirePermission("Sales.CompleteOrder");
        admin.MapPost("/orders/{id:guid}/payment", UpdatePaymentAsync).RequirePermission("Finance.VerifyPayment");
        admin.MapPost("/orders/{id:guid}/payment/waive", WaivePaymentAsync).RequirePermission("Finance.VerifyPayment");
        admin.MapPost("/orders/{id:guid}/deposit/waive", WaiveDepositAsync).RequirePermission("Finance.VerifyPayment");
        admin.MapPost("/orders/{id:guid}/payments", AddPaymentAsync).RequirePermission("Finance.VerifyPayment");
        admin.MapPut("/orders/{id:guid}/shipping", SetShippingAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapPut("/orders/{id:guid}/reservation", SetReservationAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapPost("/orders/{id:guid}/unreachable", MarkCustomerUnreachableAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapGet("/coupons", ListCouponsAsync).RequirePermission(CouponPermission);
        admin.MapPost("/coupons", CreateCouponAsync).RequirePermission(CouponPermission);
        admin.MapPut("/coupons/{id:guid}", UpdateCouponAsync).RequirePermission(CouponPermission);
        admin.MapDelete("/coupons/{id:guid}", DeleteCouponAsync).RequirePermission(CouponPermission);
        admin.MapGet("/blacklist", ListBlacklistAsync).RequirePermission("Sales.ViewCustomerContact");
        admin.MapPost("/blacklist", AddBlacklistAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapDelete("/blacklist/{id:guid}", RemoveBlacklistAsync).RequirePermission("Sales.ApproveOrder");
        admin.MapGet("/returns", ListReturnsAsync).RequirePermission("Sales.ViewOrders");
        admin.MapPost("/returns", CreateReturnAsync).RequirePermission("Sales.ProcessReturn");
        admin.MapPost("/returns/{id:guid}/inspect", InspectReturnAsync).RequirePermission("Sales.ProcessReturn");
        admin.MapPost("/returns/{id:guid}/reject", RejectReturnAsync).RequirePermission("Sales.ProcessReturn");
        admin.MapGet("/warranties", ListWarrantiesAsync).RequirePermission("Sales.ViewOrders");
        admin.MapGet("/orders/{id:guid}/invoice", GetOrderInvoiceAsync).RequirePermission("Sales.ViewOrders");
        admin.MapGet("/invoices", ListInvoicesAsync).RequirePermission("Sales.ViewOrders");
        admin.MapPost("/warranties/{id:guid}/void", VoidWarrantyAsync).RequirePermission("Sales.ProcessReturn");
        admin.MapPost("/warranties/{id:guid}/extend", ExtendWarrantyAsync).RequirePermission("Sales.ProcessReturn");
        admin.MapGet("/partner/earnings", ListEarningsAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/expenses", ListExpensesAsync).RequirePermission("Finance.ViewReports");
        admin.MapPost("/expenses", CreateExpenseAsync).RequirePermission("Finance.ManageExpenses");
        admin.MapPut("/expenses/{id:guid}/receipt", SetExpenseReceiptAsync).RequirePermission("Finance.ManageExpenses");
        admin.MapGet("/expenses/{id:guid}/receipt", GetExpenseReceiptAsync).RequirePermission("Finance.ViewReports")
            .Produces<Stream>(StatusCodes.Status200OK, "image/webp");
        admin.MapGet("/expenses/summary", ExpenseSummaryAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/expense-types", ListExpenseTypesAsync).RequirePermission("Finance.ViewReports");
        admin.MapPost("/expense-types", CreateExpenseTypeAsync).RequirePermission("Finance.ManageExpenses");
        admin.MapPut("/expense-types/{id:guid}", UpdateExpenseTypeAsync).RequirePermission("Finance.ManageExpenses");
        admin.MapGet("/recurring-expenses", ListRecurringExpensesAsync).RequirePermission("Finance.ViewReports");
        admin.MapPost("/recurring-expenses", CreateRecurringExpenseAsync).RequirePermission("Finance.ManageExpenses");
        admin.MapPut("/recurring-expenses/{id:guid}", UpdateRecurringExpenseAsync).RequirePermission("Finance.ManageExpenses");
        admin.MapGet("/money/accounts", ListMoneyAccountsAsync).RequirePermission("Finance.ViewVaults");
        admin.MapGet("/money/accounts/{id:guid}/movements", ListMoneyMovementsAsync).RequirePermission("Finance.ViewVaults");
        admin.MapPost("/money/accounts", CreateMoneyAccountAsync).RequirePermission("Finance.Disburse");
        admin.MapPut("/money/accounts/{id:guid}", UpdateMoneyAccountAsync).RequirePermission("Finance.Disburse");
        admin.MapDelete("/money/accounts/{id:guid}", DeleteMoneyAccountAsync).RequirePermission("Finance.Disburse");
        admin.MapPost("/money/transfers", TransferMoneyAsync).RequirePermission("Finance.Disburse");
        admin.MapGet("/reports/business", BusinessReportAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partner/adjustments", ListAdjustmentsAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partners", ListPartnersAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partners/{id:guid}", GetPartnerAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partners/{id:guid}/capital", ListPartnerCapitalAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partners/{id:guid}/statement", PartnerStatementAsync).RequirePermission("Finance.ViewReports");
        admin.MapGet("/partners/{id:guid}/payouts", ListPartnerPayoutsAsync).RequirePermission("Finance.ViewReports");
        admin.MapPost("/partners", CreatePartnerAsync).RequirePermission("Finance.ManagePartnerAgreements");
        admin.MapPut("/partners/{id:guid}", UpdatePartnerAsync).RequirePermission("Finance.ManagePartnerAgreements");
        admin.MapPost("/partners/{id:guid}/capital", AddPartnerCapitalAsync).RequirePermission("Finance.ManagePartnerAgreements");
        admin.MapPost("/partners/month-close", CloseMonthAsync).RequirePermission("Finance.ManagePartnerAgreements");
        admin.MapPut("/units/{id:guid}/partner", AssignUnitPartnerAsync).RequirePermission("Finance.ManagePartnerAgreements");
        admin.MapPost("/partners/{id:guid}/payouts", PayoutPartnerAsync).RequirePermission("Finance.PostPartnerSettlements");

        endpoints.MapGet("/api/v1/store/items", ListStoreItemsAsync).AllowAnonymous();
        endpoints.MapGet("/api/v1/store/items/{key}", GetStoreItemAsync).AllowAnonymous();
        endpoints.MapGet("/api/v1/store/assemblies", ListPublishedAssembliesAsync).AllowAnonymous();
        endpoints.MapGet("/api/v1/store/assemblies/{code}", GetPublishedAssemblyAsync).AllowAnonymous();
        endpoints.MapPost("/api/v1/orders", CreateCustomerOrderAsync).AllowAnonymous().RequireRateLimiting(IdentityRateLimitPolicies.Checkout);
        endpoints.MapGet("/api/v1/track/{token}", GetTrackingAsync).AllowAnonymous().RequireRateLimiting(IdentityRateLimitPolicies.OrderTracking);
        endpoints.MapPost("/api/v1/track/{token}/rating", RateOrderAsync).AllowAnonymous().RequireRateLimiting(IdentityRateLimitPolicies.OrderTracking);
        endpoints.MapGet("/api/v1/orders/mine", ListMyOrdersAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/orders/mine/coupons", ListMyCouponUsesAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/orders/mine/{id:guid}", GetMyOrderAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/warranty/lookup", LookupWarrantyAsync).AllowAnonymous().RequireRateLimiting(IdentityRateLimitPolicies.WarrantyLookup);
        endpoints.MapGet("/api/v1/warranties/mine", ListMyWarrantiesAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/invoices/mine", ListMyInvoicesAsync).RequireAuthorization();
        endpoints.MapGet("/api/v1/invoices/{token}", GetInvoiceByTokenAsync).AllowAnonymous().RequireRateLimiting(IdentityRateLimitPolicies.OrderTracking);
        endpoints.MapPost("/api/v1/store/coupons/check", CheckCouponAsync).AllowAnonymous().RequireRateLimiting(IdentityRateLimitPolicies.CouponCheck);
        endpoints.MapPost("/api/v1/store/quote", QuoteAsync).AllowAnonymous().RequireRateLimiting(IdentityRateLimitPolicies.OrderTracking);
        endpoints.MapGet("/api/v1/admin/dashboard", GetDashboardAsync).RequirePermission("Finance.ViewReports");
        return endpoints;
    }
}

public sealed record RetailVersionRequest(long RowVersion);
