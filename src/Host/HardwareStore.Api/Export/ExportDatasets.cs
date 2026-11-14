using HardwareStore.Common.Domain;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;

/// <summary>The range and the sensitive-column permissions of one export request.</summary>
public sealed record ExportQuery(DateOnly From, DateOnly To, bool IncludeCosts, bool IncludeContact);

/// <summary>
/// One exportable dataset: its name in the route, the permission its list page already needs, whether it is customer data
/// (so it needs a fresh email code) and how to build it. Each dataset is built by the module that owns the data.
/// </summary>
public sealed record ExportDataset(string Name, string Permission, bool RequiresStepUp,
    Func<IServiceProvider, ExportQuery, CancellationToken, Task<ExportTable>> Build);

public static class ExportDatasets
{
    public static readonly IReadOnlyList<ExportDataset> All =
    [
        new("orders", "Sales.ViewOrders", false, (services, query, ct) =>
            services.GetRequiredService<RetailExportService>().OrdersAsync(query.From, query.To, query.IncludeContact, ct)),
        new("stock-items", "Inventory.View", false, (services, query, ct) =>
            services.GetRequiredService<RetailExportService>().StockItemsAsync(query.From, query.To, query.IncludeCosts, ct)),
        new("expenses", "Finance.ViewReports", false, (services, query, ct) =>
            services.GetRequiredService<ExpenseService>().ExportAsync(query.From, query.To, ct)),
        new("purchases", "Purchasing.View", false, (services, query, ct) =>
            services.GetRequiredService<PurchaseService>().ExportAsync(query.From, query.To, ct)),
        new("money-movements", "Finance.ViewVaults", false, (services, query, ct) =>
            services.GetRequiredService<RetailExportService>().MoneyMovementsAsync(query.From, query.To, ct)),
        new("audit", "System.ViewAuditLogs", false, (services, query, ct) =>
            services.GetRequiredService<AuditLogService>().ExportAsync(query.From, query.To, ct)),
        new("customers", "Customers.View", true, (services, query, ct) =>
            services.GetRequiredService<CustomerProfileComposer>().ExportAsync(query.From, query.To, ct))
    ];
}
