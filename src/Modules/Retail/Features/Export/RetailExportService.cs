using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Retail datasets for the file export (orders, stock items, money movements). Rows are limited to
/// <see cref="ExportRules.ReadLimit"/> so the caller can tell "too many" from "just enough"; the range is in Cairo business days.
/// </summary>
public sealed class RetailExportService(IDbContextFactory<RetailDbContext> contextFactory, TimeZoneInfo businessZone)
{
    /// <summary>Orders created in the range. Phone, email and address are included only when <paramref name="includeContact"/> is true.</summary>
    public async Task<ExportTable> OrdersAsync(DateOnly from, DateOnly to, bool includeContact, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = BusinessDay.Range(from, to, businessZone);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var orders = await db.Orders.AsNoTracking().Where(order => order.CreatedAtUtc >= fromUtc && order.CreatedAtUtc < toUtc)
            .OrderBy(order => order.CreatedAtUtc).Take(ExportRules.ReadLimit).ToListAsync(cancellationToken);
        List<string> columns = ["reference", "created", "channel", "status", "payment status", "payment method", "fulfillment", "customer"];
        if (includeContact) columns.AddRange(["phone", "email", "governorate", "area", "street", "building"]);
        columns.AddRange(["lead source", "subtotal", "delivery fee", "total", "paid", "refunded", "blacklisted"]);
        return new ExportTable(columns, [.. orders.Select(order => OrderRow(order, includeContact))]);
    }

    private IReadOnlyList<ExportCell> OrderRow(StoreOrder order, bool includeContact)
    {
        List<ExportCell> cells =
        [
            ExportCell.Text(order.Reference), ExportCell.Instant(order.CreatedAtUtc, businessZone), ExportCell.Text(order.Channel),
            ExportCell.Text(order.Status), ExportCell.Text(order.PaymentStatus), ExportCell.Text(order.PaymentMethod),
            ExportCell.Text(order.FulfillmentMethod), ExportCell.Text(order.CustomerName)
        ];
        if (includeContact)
            cells.AddRange([ExportCell.Text(order.PhoneNumber), ExportCell.Text(order.CustomerEmail), ExportCell.Text(order.Governorate),
                ExportCell.Text(order.Area), ExportCell.Text(order.Street), ExportCell.Text(order.Building)]);
        cells.AddRange([ExportCell.Text(order.LeadSource), ExportCell.Number(order.Subtotal), ExportCell.Number(order.DeliveryFee),
            ExportCell.Number(order.Total), ExportCell.Number(order.PaidAmount), ExportCell.Number(order.RefundedAmount),
            ExportCell.Flag(order.IsBlacklisted)]);
        return cells;
    }

    /// <summary>Stock items listed in the range. Cost and profit columns (from the units in stock) only with <paramref name="includeCosts"/>.</summary>
    public async Task<ExportTable> StockItemsAsync(DateOnly from, DateOnly to, bool includeCosts, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = BusinessDay.Range(from, to, businessZone);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var items = await db.StockItems.AsNoTracking().Include(item => item.Units)
            .Where(item => item.CreatedAtUtc >= fromUtc && item.CreatedAtUtc < toUtc)
            .OrderBy(item => item.CreatedAtUtc).Take(ExportRules.ReadLimit).AsSplitQuery().ToListAsync(cancellationToken);
        List<string> columns = ["code", "condition", "model", "price", "price with board", "price alone", "available", "hidden", "warranty", "listed"];
        if (includeCosts) columns.AddRange(["stock cost", "potential profit"]);
        return new ExportTable(columns, [.. items.Select(item => StockItemRow(item, includeCosts))]);
    }

    private IReadOnlyList<ExportCell> StockItemRow(StockItem item, bool includeCosts)
    {
        var available = item.Units.Where(unit => unit.Status == UnitStatus.Available).ToList();
        List<ExportCell> cells =
        [
            ExportCell.Text(item.Code), ExportCell.Text(item.Condition), ExportCell.Text(item.ModelName), ExportCell.Number(item.Price),
            ExportCell.Number(item.WithBoardExtra), ExportCell.Number(item.AloneExtra), ExportCell.Number(available.Count),
            ExportCell.Flag(item.IsHidden), ExportCell.Text(item.WarrantyLabel), ExportCell.Instant(item.CreatedAtUtc, businessZone)
        ];
        if (includeCosts)
        {
            var cost = available.Sum(unit => unit.AcquisitionCost);
            cells.AddRange([ExportCell.Number(cost), ExportCell.Number(item.Price * available.Count - cost)]);
        }
        return cells;
    }

    /// <summary>Every money movement of every account that happened in the range.</summary>
    public async Task<ExportTable> MoneyMovementsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var accounts = await db.MoneyAccounts.AsNoTracking().ToDictionaryAsync(account => account.Id, account => account.Name, cancellationToken);
        var movements = await db.MoneyMovements.AsNoTracking().Where(item => item.OccurredOn >= from && item.OccurredOn <= to)
            .OrderBy(item => item.OccurredOn).ThenBy(item => item.CreatedAtUtc).Take(ExportRules.ReadLimit).ToListAsync(cancellationToken);
        return new ExportTable(["date", "account", "amount", "source", "reference", "note"], [.. movements.Select(item =>
            (IReadOnlyList<ExportCell>)[ExportCell.Date(item.OccurredOn), ExportCell.Text(accounts.GetValueOrDefault(item.AccountId)),
                ExportCell.Number(item.Amount), ExportCell.Text(item.Source), ExportCell.Text(item.Reference), ExportCell.Text(item.Note)])]);
    }
}
