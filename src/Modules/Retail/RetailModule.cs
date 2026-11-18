using HardwareStore.Common.Infrastructure.Audit;
using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.Retail;

/// <summary>Retail module registration: one DbContext factory plus one scoped service per feature area.</summary>
public static class RetailRegistration
{
    public static IServiceCollection AddRetailModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        services.AddAuditedDbContextFactory<RetailDbContext>(connectionString);
        services.AddRetailAuditRestore();
        services.AddScoped<InventoryUnitService>();
        services.AddScoped<StockItemAdminService>();
        services.AddScoped<StoreItemService>();
        services.AddScoped<StockModelLookupService>();
        services.AddScoped<BuilderItemService>();
        services.AddScoped<AssemblyService>();
        services.AddScoped<AssemblyPartService>();
        services.AddScoped<OrderService>();
        services.AddScoped<OrderStatusService>();
        services.AddScoped<OrderPaymentService>();
        services.AddScoped<OrderFulfillmentService>();
        services.AddScoped<OrderTrackingService>();
        services.AddScoped<BlacklistService>();
        services.AddScoped<ITrashSource, BlacklistTrashSource>();
        services.AddScoped<IDataResetSource, RetailDataReset>();
        services.AddScoped<PosService>();
        services.AddScoped<ReturnService>();
        services.AddScoped<WarrantyService>();
        services.AddScoped<CustomerSalesService>();
        services.AddScoped<InvoiceService>();
        services.AddScoped<QuoteService>();
        services.AddScoped<UnitPartnerService>();
        services.AddScoped<PartnerService>();
        services.AddScoped<PartnerCapitalService>();
        services.AddScoped<PartnerLedgerService>();
        services.AddScoped<PartnerMonthCloseService>();
        services.AddScoped<PartnerStatementService>();
        services.AddScoped<MoneyAccountService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<ExpenseTypeService>();
        services.AddScoped<RecurringExpenseService>();
        services.AddScoped<BusinessReportService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<RetailExportService>();
        return services;
    }
}
