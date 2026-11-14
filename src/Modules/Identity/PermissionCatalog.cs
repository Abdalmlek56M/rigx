using System.Collections.Frozen;

namespace HardwareStore.Modules.Identity.Authorization;

public static class PermissionCatalog
{
    public static readonly IReadOnlySet<string> All = new[]
    {
        "Catalog.View", "Catalog.ManageProducts", "Catalog.ManageTaxonomy", "Catalog.ManageMedia",
        "Sales.ViewOrders", "Sales.LogCommunication", "Sales.ApproveOrder", "Sales.CreatePOS",
        "Sales.OverrideMinimumPrice", "Sales.FulfillStorePickup", "Sales.ProcessReturn", "Sales.CompleteOrder", "Sales.ViewCustomerContact",
        "Inventory.View", "Inventory.Transfer", "Inventory.StockCount", "Inventory.StockAdjust", "Inventory.ManageSerials",
        "Inventory.ManageWarehouses", "Inventory.ViewCosts", "Inventory.ViewSerials",
        "Purchasing.View", "Purchasing.ManageSuppliers", "Purchasing.CreateOrder", "Purchasing.ReceiveGoods", "Purchasing.PostBill",
        "Finance.ViewVaults", "Finance.VerifyPayment", "Finance.Disburse", "Finance.ManageExpenses", "Finance.ViewReports",
        "Finance.ManagePartnerAgreements", "Finance.PostPartnerSettlements",
        "System.ManageUsers", "System.ViewAuditLogs", "System.ViewErrorLogs", "System.ExecuteAllowlistedAction",
        "System.ManageBackups", "System.StoreSettings", "System.Trash", "System.Purge",
        "System.BackupDownload", "System.ResetData",
        "Requests.View", "Requests.Manage",
        "Customers.View", "Customers.Manage"
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Permissions that only the Owner role holds; no other role can be given them.</summary>
    public static readonly IReadOnlySet<string> OwnerOnly = new[] { "System.Purge", "System.BackupDownload", "System.ResetData" }.ToFrozenSet(StringComparer.Ordinal);
}
