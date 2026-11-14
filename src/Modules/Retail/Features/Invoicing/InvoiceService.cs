using HardwareStore.Common.Domain.Paging;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Reads invoices: the public link (by token), one order's invoice, a customer's own invoices and the admin search.
/// Invoices are issued by <see cref="InvoiceRegistry"/> when the order is delivered, never from here.
/// </summary>
public sealed class InvoiceService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    private const int PageSize = 50;

    /// <summary>Member-init (not a constructor) so EF can still sort and page on the projected invoice.</summary>
    private sealed class SummaryRow
    {
        public required Invoice Invoice { get; init; }
        public required string Reference { get; init; }
        public required string OrderStatus { get; init; }
        public bool HasBuild { get; init; }
        public decimal Total { get; init; }
    }

    public async Task<InvoiceView> GetByTokenAsync(string? token, CancellationToken cancellationToken = default)
    {
        var value = token?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 64) throw NotFound();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var invoice = await db.Invoices.AsNoTracking().SingleOrDefaultAsync(item => item.AccessToken == value, cancellationToken) ?? throw NotFound();
        return await ViewAsync(db, invoice, cancellationToken);
    }

    public async Task<InvoiceView> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var invoice = await db.Invoices.AsNoTracking().SingleOrDefaultAsync(item => item.OrderId == orderId, cancellationToken) ?? throw NotFound();
        return await ViewAsync(db, invoice, cancellationToken);
    }

    /// <summary>The invoice number and link token of an order, or null while it has no invoice (used for the invoice email).</summary>
    public async Task<(string Number, string AccessToken)?> FindForOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var row = await db.Invoices.AsNoTracking().Where(item => item.OrderId == orderId).Select(item => new { item.Number, item.AccessToken })
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : (row.Number, row.AccessToken);
    }

    public async Task<IReadOnlyList<InvoiceSummaryView>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var mine = db.Invoices.AsNoTracking().Where(invoice => db.Orders.Any(o => o.Id == invoice.OrderId && o.UserAccountId == userId));
        var rows = await Summaries(db, mine).OrderByDescending(row => row.Invoice.IssuedAtUtc).ThenByDescending(row => row.Invoice.Number)
            .ToListAsync(cancellationToken);
        return rows.Select(Summary).ToList();
    }

    /// <summary>Admin list; the search text is an invoice number, an order reference (RIGX-…) or a customer phone number.</summary>
    public async Task<PagedResult<InvoiceSummaryView>> ListAdminAsync(string? query, int page, CancellationToken cancellationToken = default)
    {
        var paging = PageRequest.Of(page, PageSize);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var invoices = db.Invoices.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query)) invoices = Matching(db, invoices, query.Trim());
        var total = await invoices.CountAsync(cancellationToken);
        var rows = await Summaries(db, invoices).OrderByDescending(row => row.Invoice.IssuedAtUtc).ThenByDescending(row => row.Invoice.Number)
            .Skip(paging.Skip).Take(paging.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<InvoiceSummaryView>(rows.Select(Summary).ToList(), paging.Page, paging.PageSize, total);
    }

    private static IQueryable<Invoice> Matching(RetailDbContext db, IQueryable<Invoice> invoices, string text)
    {
        if (InvoiceRules.TryParseNumber(text, out var invoiceNumber)) return invoices.Where(invoice => invoice.Number == invoiceNumber);
        var upper = text.ToUpperInvariant();
        if (upper.StartsWith("RIGX-", StringComparison.Ordinal))
            return invoices.Where(invoice => db.Orders.Any(o => o.Id == invoice.OrderId && o.Reference == upper));
        var phone = OrderRules.Phone(text);
        return invoices.Where(invoice => db.Orders.Any(o => o.Id == invoice.OrderId && o.PhoneNumber == phone));
    }

    private static IQueryable<SummaryRow> Summaries(RetailDbContext db, IQueryable<Invoice> invoices) =>
        invoices.Join(db.Orders, invoice => invoice.OrderId, order => order.Id, (invoice, order) =>
            new SummaryRow
            {
                Invoice = invoice, Reference = order.Reference, OrderStatus = order.Status,
                HasBuild = order.Items.Any(item => item.AssemblyId != null), Total = order.Total
            });

    private static InvoiceSummaryView Summary(SummaryRow row) => new(row.Invoice.Number, InvoiceRules.Kind(row.HasBuild),
        InvoiceRules.Status(row.OrderStatus), row.Invoice.IssuedAtUtc, row.Reference, row.Total, row.Invoice.AccessToken);

    private async Task<InvoiceView> ViewAsync(RetailDbContext db, Invoice invoice, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Items).ThenInclude(item => item.Components).AsSplitQuery()
            .SingleAsync(o => o.Id == invoice.OrderId, cancellationToken);
        var unitIds = order.Items.SelectMany(item => item.Components.Select(c => c.UnitId)).ToList();
        var units = await db.Units.AsNoTracking().Include(unit => unit.StockItem).Where(unit => unitIds.Contains(unit.Id))
            .ToDictionaryAsync(unit => unit.Id, cancellationToken);
        var warranties = (await db.Warranties.AsNoTracking().Where(w => w.OrderId == order.Id).ToListAsync(cancellationToken))
            .ToDictionary(w => (w.OrderItemId, w.UnitId));
        var buildIds = order.Items.Where(item => item.AssemblyId is not null).Select(item => item.AssemblyId!.Value).Distinct().ToList();
        var buildCodes = await db.Assemblies.AsNoTracking().Where(a => buildIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Code, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var lines = order.Items.OrderBy(item => item.Id).Select(item => Line(item, units, warranties, buildCodes, now)).ToList();
        return new InvoiceView(invoice.Number, InvoiceRules.Kind(order), InvoiceRules.Status(order.Status), invoice.IssuedAtUtc, order.Reference,
            order.CustomerName, order.PhoneNumber, OrderMapping.AddressOf(order), order.FulfillmentMethod, order.PaymentMethod, invoice.SellerName,
            lines, lines.Sum(line => line.LineTotal), lines.Sum(line => line.Discount), order.DeliveryFee, order.Total, order.PaidAmount,
            OrderRules.Remaining(order), order.RefundedAmount, invoice.AccessToken);
    }

    private static InvoiceLineView Line(OrderItem item, Dictionary<Guid, InventoryUnit> units, Dictionary<(Guid, Guid), Warranty> warranties,
        Dictionary<Guid, string> buildCodes, DateTimeOffset now)
    {
        var parts = item.Components.Select(component =>
        {
            var unit = units[component.UnitId];
            warranties.TryGetValue((item.Id, component.UnitId), out var warranty);
            return new InvoicePartView(unit.StockItem.Code, unit.Name, unit.Condition, unit.SerialNumber, warranty?.Label ?? component.WarrantyLabel,
                warranty?.EndsAtUtc, warranty is null ? null : WarrantyRules.Status(warranty, now), unit.StockItem.CategoryId, null);
        }).OrderBy(part => part.Code).ToList();
        // A whole build line stores the BLD code as its Code; a part sold alone stores its RX code and points to the build by AssemblyId.
        var buildCode = item.AssemblyId is Guid assemblyId && buildCodes.TryGetValue(assemblyId, out var code) ? code : null;
        return new InvoiceLineView(item.Code, item.Name, buildCode, item.Quantity, item.UnitPrice, item.LineTotal, item.DiscountAmount, parts);
    }

    private static RetailException NotFound() => new(404, "Invoice was not found.");
}
