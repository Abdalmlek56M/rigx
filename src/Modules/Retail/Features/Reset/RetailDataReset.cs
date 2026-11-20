using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Reset;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// The Retail part of the "business" reset: stock, builds, orders, invoices and counters, warranties, money movements,
/// expenses and the partner ledgers. Kept: money accounts (their balance falls back to the opening balance), partners,
/// expense types, the blacklist and coupons (their results restart from zero with the orders).
/// </summary>
public sealed class RetailDataReset(IDbContextFactory<RetailDbContext> contextFactory) : IDataResetSource
{
    private static readonly string[] BusinessTables =
    [
        "warranty_extensions", "warranties", "order_returns", "order_payments", "order_item_components", "order_items", "order_events",
        "invoices", "invoice_counters", "store_orders", "assembly_units", "assemblies", "inventory_units", "stock_item_old_slugs", "stock_items", "stock_imports", "purchase_receipts",
        "money_movements", "retail_expenses", "recurring_expenses",
        "partner_earnings", "partner_settlements", "partner_capital_entries", "partner_adjustments"
    ];

    public async Task<int> ResetAsync(IReadOnlySet<string> sections, CancellationToken cancellationToken = default)
    {
        if (!sections.Contains(DataResetRules.Business)) return 0;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await TableReset.DeleteAllAsync(db, BusinessTables, cancellationToken);
    }
}
