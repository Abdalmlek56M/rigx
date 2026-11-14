using HardwareStore.Common.Infrastructure.Audit;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Modules.Retail;

/// <summary>What the audit log can put back in Retail: listing and descriptive fields only (never stock state, orders, payments, ledgers or partner money).</summary>
internal static class RetailAuditRestore
{
    public static IServiceCollection AddRetailAuditRestore(this IServiceCollection services) => services
        .AddAuditRestoreTarget<RetailDbContext, StockItem>(
            nameof(StockItem.ModelName), nameof(StockItem.ModelKey), nameof(StockItem.Price), nameof(StockItem.WithBoardExtra),
            nameof(StockItem.AloneExtra), nameof(StockItem.WarrantyLabel), nameof(StockItem.WarrantyMonths), nameof(StockItem.HasBox),
            nameof(StockItem.IsHidden), nameof(StockItem.InternalNote), nameof(StockItem.Specs))
        .AddAuditRestoreTarget<RetailDbContext, Assembly>(
            nameof(Assembly.Name), nameof(Assembly.PublicTitleAr), nameof(Assembly.PublicTitleEn), nameof(Assembly.SalePrice), nameof(Assembly.Usage),
            nameof(Assembly.SellMode), nameof(Assembly.WarrantyLabel), nameof(Assembly.WarrantyMonths), nameof(Assembly.SuitableForAr),
            nameof(Assembly.SuitableForEn))
        .AddAuditRestoreTarget<RetailDbContext, ExpenseType>(nameof(ExpenseType.Name), nameof(ExpenseType.IsActive), nameof(ExpenseType.SortOrder))
        .AddAuditRestoreTarget<RetailDbContext, Partner>(nameof(Partner.Name), nameof(Partner.Phone), nameof(Partner.DefaultPercent));
}
