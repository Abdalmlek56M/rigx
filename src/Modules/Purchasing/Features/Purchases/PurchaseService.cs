using HardwareStore.Common.Domain;
using HardwareStore.Common.Domain.Paging;
using Microsoft.EntityFrameworkCore;

namespace HardwareStore.Modules.Purchasing;

/// <summary>
/// Purchases from sellers. A purchase is edited as a draft and then either cancelled or received into stock
/// (the receipt itself is orchestrated by the Host, which calls <see cref="MarkReceivedAsync"/> at the end).
/// </summary>
public sealed class PurchaseService(IDbContextFactory<PurchasingDbContext> contextFactory, TimeProvider timeProvider, TimeZoneInfo businessZone)
{

    public async Task<PagedResult<PurchaseListItem>> ListAsync(PurchaseQuery query, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var purchases = db.Purchases.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Status)) purchases = purchases.Where(purchase => purchase.Status == query.Status.Trim());
        if (!string.IsNullOrWhiteSpace(query.Search)) purchases = Matching(purchases, query.Search);
        var (page, pageSize) = PageRequest.Of(query.Page, query.PageSize);
        var total = await purchases.CountAsync(cancellationToken);
        var rows = await purchases.OrderByDescending(purchase => purchase.CreatedAtUtc).ThenByDescending(purchase => purchase.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(purchase => new PurchaseListItem(purchase.Id, purchase.Code,
                new SellerSummary(purchase.Seller.Id, purchase.Seller.Kind, purchase.Seller.Name, purchase.Seller.Phone),
                purchase.Kind, purchase.Status, purchase.PurchaseDate, purchase.Total, purchase.Remaining, purchase.Lines.Count,
                purchase.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return new PagedResult<PurchaseListItem>(rows, page, pageSize, total);
    }

    /// <summary>
    /// Purchases dated in the range as an export table. The seller's national ID is never exported.
    /// Reads at most <see cref="ExportRules.ReadLimit"/> rows.
    /// </summary>
    public async Task<ExportTable> ExportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Purchases.AsNoTracking()
            .Where(purchase => purchase.PurchaseDate >= from && purchase.PurchaseDate <= to)
            .OrderBy(purchase => purchase.PurchaseDate).ThenBy(purchase => purchase.CreatedAtUtc).Take(ExportRules.ReadLimit)
            .Select(purchase => new
            {
                purchase.Code, purchase.PurchaseDate, SellerName = purchase.Seller.Name, SellerPhone = purchase.Seller.Phone,
                purchase.Kind, purchase.Status, purchase.PaymentMethod, purchase.Total, purchase.PaidAmount, purchase.TradeInValue,
                purchase.Remaining, Lines = purchase.Lines.Count, purchase.ReceivedAtUtc
            })
            .ToListAsync(cancellationToken);
        return new ExportTable(
            ["code", "date", "seller", "seller phone", "kind", "status", "payment method", "total", "paid", "trade-in", "remaining", "lines", "received"],
            [.. rows.Select(row => (IReadOnlyList<ExportCell>)
            [
                ExportCell.Text(row.Code), ExportCell.Date(row.PurchaseDate), ExportCell.Text(row.SellerName), ExportCell.Text(row.SellerPhone),
                ExportCell.Text(row.Kind), ExportCell.Text(row.Status), ExportCell.Text(row.PaymentMethod), ExportCell.Number(row.Total),
                ExportCell.Number(row.PaidAmount), ExportCell.Number(row.TradeInValue), ExportCell.Number(row.Remaining),
                ExportCell.Number(row.Lines), ExportCell.Instant(row.ReceivedAtUtc, businessZone)
            ])]);
    }

    public async Task<PurchaseView> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return ToView(await LoadAsync(db, id, track: false, cancellationToken));
    }

    /// <summary>The purchase to receive; anything that is not a draft any more (already received or cancelled) is a 409.</summary>
    public async Task<PurchaseView> GetDraftAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var purchase = await LoadAsync(db, id, track: false, cancellationToken);
        purchase.EnsureDraft();
        return ToView(purchase);
    }

    public async Task<PurchaseView> CreateAsync(PurchaseRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var checkedPurchase = PurchaseDraftRules.Check(request, BusinessDay.Of(now, businessZone));
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var seller = await FindSellerAsync(db, request.SellerId, cancellationToken);
        var purchase = new Purchase
        {
            Id = Guid.CreateVersion7(),
            Code = await db.NextPurchaseCodeAsync(cancellationToken),
            Kind = checkedPurchase.Kind,
            PaymentMethod = checkedPurchase.Payment.Method,
            CreatedByUserAccountId = actorId,
            CreatedAtUtc = now,
            Seller = seller
        };
        purchase.Revise(seller.Id, checkedPurchase, now);
        db.Purchases.Add(purchase);
        await db.SaveChangesAsync(cancellationToken);
        return ToView(purchase);
    }

    public async Task<PurchaseView> UpdateAsync(Guid id, PurchaseRequest request, CancellationToken cancellationToken = default)
    {
        var rowVersion = request.RowVersion ?? throw new PurchasingException(400, "The purchase version is required.");
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var checkedPurchase = PurchaseDraftRules.Check(request, BusinessDay.Of(now, businessZone));
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var purchase = await LoadAsync(db, id, track: true, cancellationToken);
        purchase.EnsureDraft();
        EnsureVersion(purchase, rowVersion);
        purchase.Seller = await FindSellerAsync(db, request.SellerId, cancellationToken);
        purchase.Revise(purchase.Seller.Id, checkedPurchase, now);
        purchase.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return ToView(purchase);
    }

    public async Task<PurchaseView> CancelAsync(Guid id, long rowVersion, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var purchase = await LoadAsync(db, id, track: true, cancellationToken);
        purchase.EnsureDraft();
        EnsureVersion(purchase, rowVersion);
        purchase.Status = PurchaseStatus.Cancelled;
        purchase.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
        purchase.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return ToView(purchase);
    }

    /// <summary>Adds a later payment to a received purchase (the money itself is posted by the Host).</summary>
    public async Task<PurchaseView> RecordPaymentAsync(Guid id, decimal amount, long rowVersion, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var purchase = await LoadAsync(db, id, track: true, cancellationToken);
        EnsureVersion(purchase, rowVersion);
        purchase.AddPayment(amount, timeProvider.GetUtcNow().ToUniversalTime());
        purchase.RowVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return ToView(purchase);
    }

    /// <summary>Closes a draft as received. The status check is part of the update, so two receipts cannot both win.</summary>
    public async Task<PurchaseView> MarkReceivedAsync(Guid id, Guid? assemblyId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().ToUniversalTime();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var updated = await db.Purchases.Where(purchase => purchase.Id == id && purchase.Status == PurchaseStatus.Draft)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(purchase => purchase.Status, PurchaseStatus.Received)
                .SetProperty(purchase => purchase.ReceivedAtUtc, now)
                .SetProperty(purchase => purchase.AssemblyId, assemblyId)
                .SetProperty(purchase => purchase.UpdatedAtUtc, now)
                .SetProperty(purchase => purchase.RowVersion, purchase => purchase.RowVersion + 1), cancellationToken);
        if (updated == 0) throw new PurchasingException(409, "This purchase is no longer a draft.");
        return ToView(await LoadAsync(db, id, track: false, cancellationToken));
    }

    /// <summary>A PO code ("po-31") matches that purchase; any other text matches the seller's name or phone.</summary>
    private static IQueryable<Purchase> Matching(IQueryable<Purchase> purchases, string search)
    {
        var isCode = PublicCode.TryNormalize(search, PublicCode.Purchase, out var code);
        var namePattern = LikePattern.Contains(search);
        var digits = string.Concat(search.Where(char.IsAsciiDigit));
        return purchases.Where(purchase => (isCode && purchase.Code == code)
            || EF.Functions.ILike(purchase.Seller.Name, namePattern, LikePattern.Escape)
            || (digits.Length >= 3 && purchase.Seller.Phone.Contains(digits)));
    }

    private static async Task<Seller> FindSellerAsync(PurchasingDbContext db, Guid sellerId, CancellationToken cancellationToken) =>
        await db.Sellers.SingleOrDefaultAsync(seller => seller.Id == sellerId, cancellationToken)
            ?? throw new PurchasingException(400, "Choose an existing seller.");

    private static async Task<Purchase> LoadAsync(PurchasingDbContext db, Guid id, bool track, CancellationToken cancellationToken)
    {
        var purchases = db.Purchases.Include(purchase => purchase.Seller).Include(purchase => purchase.Lines).AsQueryable();
        if (!track) purchases = purchases.AsNoTracking();
        return await purchases.AsSplitQuery().SingleOrDefaultAsync(purchase => purchase.Id == id, cancellationToken)
            ?? throw new PurchasingException(404, "Purchase was not found.");
    }

    private static void EnsureVersion(Purchase purchase, long rowVersion)
    {
        if (purchase.RowVersion != rowVersion) throw new PurchasingException(409, "The purchase was changed by another user.");
    }

    private static PurchaseView ToView(Purchase purchase) => new(purchase.Id, purchase.Code,
        new SellerSummary(purchase.Seller.Id, purchase.Seller.Kind, purchase.Seller.Name, purchase.Seller.Phone),
        purchase.Kind, purchase.KeepAsBuild, purchase.BuildName, purchase.Status, purchase.PurchaseDate, purchase.Notes,
        purchase.Total, purchase.PaymentMethod, purchase.PaidAmount, purchase.TradeInValue, purchase.TradeInNote,
        purchase.Remaining, purchase.ReceivedAtUtc, purchase.AssemblyId, purchase.CreatedByUserAccountId, purchase.CreatedAtUtc,
        purchase.UpdatedAtUtc, purchase.RowVersion,
        purchase.Lines.OrderBy(line => line.SortOrder).Select(line => new PurchaseLineView(line.Id, line.SortOrder, line.CategoryId,
            line.ProductId, line.ModelName, line.Condition, line.Quantity, line.UnitCost, line.LineTotal, line.Price,
            line.WithBoardExtra, line.AloneExtra, line.SerialNumber, line.PartnerPercent, line.PartnerId, line.IsHidden, line.WarrantyLabel,
            line.HasBox, line.InternalNote, line.Specs, line.BrandId, line.CopyPhotosFromItemId)).ToList(), purchase.PayFromCash);
}
