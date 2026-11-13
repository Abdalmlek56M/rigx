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
        services.AddDbContextFactory<RetailDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<InventoryUnitService>();
        services.AddScoped<StockItemAdminService>();
        services.AddScoped<StoreItemService>();
        services.AddScoped<BuilderItemService>();
        services.AddScoped<AssemblyService>();
        services.AddScoped<AssemblyPartService>();
        services.AddScoped<OrderService>();
        services.AddScoped<OrderStatusService>();
        services.AddScoped<OrderPaymentService>();
        services.AddScoped<OrderFulfillmentService>();
        services.AddScoped<OrderTrackingService>();
        services.AddScoped<BlacklistService>();
        services.AddScoped<PosService>();
        services.AddScoped<SoldFeedService>();
        services.AddScoped<ReturnService>();
        services.AddScoped<WarrantyService>();
        services.AddScoped<InvoiceService>();
        services.AddScoped<QuoteService>();
        services.AddScoped<PartnerAgreementService>();
        services.AddScoped<PartnerAllocationService>();
        services.AddScoped<PartnerLedgerService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<BusinessReportService>();
        return services;
    }
}
