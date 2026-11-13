using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Retail;

/// <summary>
/// Warranty registry: the public lookup (phone, order number, item code or serial), a customer's own warranties and the
/// admin list with manual voiding (sticker removed or damaged).
/// </summary>
public sealed class WarrantyService(IDbContextFactory<RetailDbContext> contextFactory, TimeProvider timeProvider)
{
    private const int PageSize = 50;
    private const int LookupLimit = 100;

    public async Task<WarrantyLookupResult> LookupAsync(string? query, CancellationToken cancellationToken = default)
    {
        var (kind, value) = WarrantyRules.ParseLookup(query);
        var now = timeProvider.GetUtcNow();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await Matching(db, kind, value).OrderByDescending(w => w.StartsAtUtc).ThenBy(w => w.Code).Take(LookupLimit)
            .Join(db.Orders, w => w.OrderId, o => o.Id, (w, o) => new { Warranty = w, o.Reference }).ToListAsync(cancellationToken);
        var numbers = await InvoiceNumbersAsync(db, rows.Select(row => row.Warranty.OrderId), cancellationToken);
        var groups = rows.GroupBy(row => row.Warranty.OrderId).Select(group => new WarrantyLookupGroup(
                group.First().Reference, numbers.GetValueOrDefault(group.Key), WarrantyRules.MaskPhone(group.First().Warranty.PhoneNumber), group.Min(row => row.Warranty.StartsAtUtc),
                group.Select(row => PublicView(row.Warranty, now)).ToList()))
            .OrderByDescending(group => group.PurchasedAtUtc).ToList();
        return new WarrantyLookupResult(groups);
    }

    public async Task<IReadOnlyList<WarrantyAccountView>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Warranties.AsNoTracking().Where(w => w.UserAccountId == userId).OrderByDescending(w => w.StartsAtUtc).ThenBy(w => w.Code)
            .Join(db.Orders, w => w.OrderId, o => o.Id, (w, o) => new { Warranty = w, o.Reference }).ToListAsync(cancellationToken);
        var numbers = await InvoiceNumbersAsync(db, rows.Select(row => row.Warranty.OrderId), cancellationToken);
        return rows.Select(row =>
        {
            var view = PublicView(row.Warranty, now);
            return new WarrantyAccountView(row.Warranty.OrderId, row.Reference, numbers.GetValueOrDefault(row.Warranty.OrderId), view.Code, view.Name, view.Condition, view.BuildCode, view.Label,
                view.StartsAtUtc, view.EndsAtUtc, view.Status, view.DaysLeft);
        }).ToList();
    }

    public async Task<WarrantyAdminPage> ListAdminAsync(string? query, string? status, int page, bool includeContact, bool includeSerial,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var number = Math.Max(1, page);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var warranties = db.Warranties.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var (kind, value) = WarrantyRules.ParseLookup(query);
            warranties = Matching(db, kind, value);
        }
        warranties = FilterStatus(warranties, status, now);
        var total = await warranties.CountAsync(cancellationToken);
        var rows = await warranties.OrderByDescending(w => w.StartsAtUtc).ThenBy(w => w.Code).Skip((number - 1) * PageSize).Take(PageSize)
            .Join(db.Orders, w => w.OrderId, o => o.Id, (w, o) => new { Warranty = w, o.Reference, o.CustomerName }).ToListAsync(cancellationToken);
        var numbers = await InvoiceNumbersAsync(db, rows.Select(row => row.Warranty.OrderId), cancellationToken);
        var items = rows.Select(row => AdminView(row.Warranty, row.Reference, numbers.GetValueOrDefault(row.Warranty.OrderId), row.CustomerName, now, includeContact, includeSerial)).ToList();
        return new WarrantyAdminPage(items, number, PageSize, total);
    }

    /// <summary>Voids one warranty by hand (for example the RIGX sticker was removed or damaged).</summary>
    public async Task<WarrantyAdminView> VoidAsync(Guid id, WarrantyVoidRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var warranty = await db.Warranties.SingleOrDefaultAsync(w => w.Id == id, cancellationToken) ?? throw new RetailException(404, "Warranty was not found.");
        if (warranty.RowVersion != request.RowVersion) throw new RetailException(409, "The warranty was changed by another user.");
        WarrantyRules.Void(warranty, request.Reason, actorId, now);
        await db.SaveChangesAsync(cancellationToken);
        var order = await db.Orders.AsNoTracking().Where(o => o.Id == warranty.OrderId).Select(o => new { o.Reference, o.CustomerName }).SingleAsync(cancellationToken);
        var numbers = await InvoiceNumbersAsync(db, [warranty.OrderId], cancellationToken);
        return AdminView(warranty, order.Reference, numbers.GetValueOrDefault(warranty.OrderId), order.CustomerName, now, includeContact: true, includeSerial: true);
    }

    private static IQueryable<Warranty> Matching(RetailDbContext db, WarrantyLookupKind kind, string value)
    {
        var warranties = db.Warranties.AsNoTracking();
        return kind switch
        {
            WarrantyLookupKind.Code => warranties.Where(w => w.Code == value),
            WarrantyLookupKind.OrderReference => warranties.Where(w => db.Orders.Any(o => o.Id == w.OrderId && o.Reference == value)),
            WarrantyLookupKind.InvoiceNumber => warranties.Where(w => db.Invoices.Any(i => i.OrderId == w.OrderId && i.Number == value)),
            WarrantyLookupKind.Phone => warranties.Where(w => w.PhoneNumber == value),
            _ => warranties.Where(w => w.SerialNumber != null && w.SerialNumber.ToUpper() == value.ToUpper())
        };
    }

    private static async Task<Dictionary<Guid, string>> InvoiceNumbersAsync(RetailDbContext db, IEnumerable<Guid> orderIds, CancellationToken cancellationToken)
    {
        var ids = orderIds.Distinct().ToList();
        return await db.Invoices.AsNoTracking().Where(invoice => ids.Contains(invoice.OrderId)).ToDictionaryAsync(invoice => invoice.OrderId, invoice => invoice.Number, cancellationToken);
    }

    private static IQueryable<Warranty> FilterStatus(IQueryable<Warranty> warranties, string? status, DateTimeOffset now) => status?.Trim() switch
    {
        null or "" => warranties,
        WarrantyStatus.Active => warranties.Where(w => w.VoidedAtUtc == null && (w.EndsAtUtc == null || w.EndsAtUtc > now)),
        WarrantyStatus.Expired => warranties.Where(w => w.VoidedAtUtc == null && w.EndsAtUtc != null && w.EndsAtUtc <= now),
        WarrantyStatus.Void => warranties.Where(w => w.VoidedAtUtc != null),
        _ => throw new RetailException(400, "Status must be Active, Expired or Void.")
    };

    private static WarrantyPublicView PublicView(Warranty w, DateTimeOffset now) => new(w.Code, w.Name, w.Condition, w.BuildCode, w.Label,
        w.StartsAtUtc, w.EndsAtUtc, WarrantyRules.Status(w, now), WarrantyRules.DaysLeft(w, now));

    private static WarrantyAdminView AdminView(Warranty w, string orderReference, string? invoiceNumber, string customerName, DateTimeOffset now, bool includeContact, bool includeSerial) =>
        new(w.Id, w.OrderId, orderReference, invoiceNumber, w.Code, w.Name, w.Condition, w.BuildCode, w.Label, w.StartsAtUtc, w.EndsAtUtc,
            WarrantyRules.Status(w, now), WarrantyRules.DaysLeft(w, now), includeContact ? customerName : null, includeContact ? w.PhoneNumber : null,
            includeSerial ? w.SerialNumber : null, w.VoidedAtUtc, w.VoidReason, w.RowVersion);
}
