using System.Text.Json;
using HardwareStore.Modules.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Purchasing;

/// <summary>
/// Draft-only purchase documents for registered suppliers, marketplaces, and
/// individual sellers. Nothing here posts stock, cost, ledger, or supplier debt.
/// </summary>
public sealed class PurchaseDraftService(
    IDbContextFactory<PurchasingDbContext> contextFactory,
    ISecurityAuditTransactionWriter auditWriter,
    TimeProvider timeProvider)
{
    public async Task<PurchaseDraftPage> GetAsync(
        string? sourceType,
        string? status,
        string? search,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.PurchaseDrafts.AsNoTracking().Include(draft => draft.Items).AsQueryable();
        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            var source = sourceType.Trim();
            query = query.Where(draft => draft.SourceType == source);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            var value = status.Trim();
            query = query.Where(draft => draft.Status == value);
        }
        if (fromDate is not null)
            query = query.Where(draft => draft.PurchaseDate != null && draft.PurchaseDate >= fromDate);
        if (toDate is not null)
            query = query.Where(draft => draft.PurchaseDate != null && draft.PurchaseDate <= toDate);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(draft =>
                (draft.MarketplaceName != null && EF.Functions.ILike(draft.MarketplaceName, term)) ||
                (draft.ExternalSellerName != null && EF.Functions.ILike(draft.ExternalSellerName, term)) ||
                (draft.Notes != null && EF.Functions.ILike(draft.Notes, term)) ||
                draft.Items.Any(item => EF.Functions.ILike(item.ItemName, term) || EF.Functions.ILike(item.Sku, term)));
        }
        var total = await query.CountAsync(cancellationToken);
        var drafts = await query
            .OrderByDescending(draft => draft.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var supplierIds = drafts.Where(draft => draft.SupplierId != null)
            .Select(draft => draft.SupplierId!.Value).Distinct().ToArray();
        var suppliers = supplierIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await db.SupplierProfiles.AsNoTracking()
                .Where(supplier => supplierIds.Contains(supplier.Id))
                .ToDictionaryAsync(supplier => supplier.Id, supplier => supplier.CompanyName, cancellationToken);
        return new PurchaseDraftPage(
            drafts.Select(draft => Map(draft, draft.SupplierId is null ? null : suppliers.GetValueOrDefault(draft.SupplierId.Value))).ToList(),
            page,
            pageSize,
            total);
    }

    public async Task<PurchaseDraftView> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var draft = await db.PurchaseDrafts.AsNoTracking().Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new PurchaseDraftException(404, "Purchase draft was not found.");
        var supplier = draft.SupplierId is null
            ? null
            : await db.SupplierProfiles.AsNoTracking()
                .Where(item => item.Id == draft.SupplierId)
                .Select(item => item.CompanyName)
                .SingleOrDefaultAsync(cancellationToken);
        return Map(draft, supplier);
    }

    public async Task<PurchaseDraftView> CreateAsync(
        PurchaseDraftCreateRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var normalized = await NormalizeAsync(db, request, cancellationToken);
            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var draft = new PurchaseDraft
            {
                Id = Guid.CreateVersion7(),
                SourceType = normalized.SourceType,
                PurchaseKind = normalized.PurchaseKind,
                SupplierId = normalized.SupplierId,
                MarketplaceName = normalized.MarketplaceName,
                ExternalSellerName = normalized.ExternalSellerName,
                ExternalSellerContact = normalized.ExternalSellerContact,
                ListingUrl = normalized.ListingUrl,
                PurchaseDate = normalized.PurchaseDate,
                Notes = normalized.Notes,
                Status = PurchaseDraftStatus.Draft,
                TotalAmount = normalized.TotalAmount,
                CreatedByUserAccountId = actorId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                RowVersion = 1
            };
            foreach (var item in normalized.Items)
            {
                draft.Items.Add(new PurchaseDraftItem
                {
                    Id = Guid.CreateVersion7(),
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    ItemName = item.ItemName,
                    Sku = item.Sku,
                    ItemKind = item.ItemKind,
                    Brand = item.Brand,
                    Model = item.Model,
                    Category = item.Category,
                    Condition = item.Condition,
                    Description = item.Description,
                    ExternalCode = item.ExternalCode,
                    SpecificationsJson = item.SpecificationsJson,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.LineTotal,
                    SortOrder = item.SortOrder
                });
            }
            db.PurchaseDrafts.Add(draft);
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, actorId, "Purchasing.DraftCreated", draft, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(draft, await SupplierNameAsync(db, draft.SupplierId, cancellationToken));
        }
        catch (PurchaseDraftException)
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<PurchaseDraftView> UpdateAsync(
        Guid id,
        PurchaseDraftUpdateRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM purchase_drafts WHERE id = {0} FOR UPDATE",
                new object[] { id },
                cancellationToken);
            var draft = await db.PurchaseDrafts.Include(item => item.Items)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new PurchaseDraftException(404, "Purchase draft was not found.");
            if (draft.Status != PurchaseDraftStatus.Draft)
                throw new PurchaseDraftException(409, "Only a draft purchase can be edited.");
            if (request.RowVersion <= 0 || request.RowVersion != draft.RowVersion)
                throw new PurchaseDraftException(409, "The purchase draft was changed by another user.");
            var normalized = await NormalizeAsync(db, new PurchaseDraftCreateRequest(
                request.SourceType,
                request.SupplierId,
                request.MarketplaceName,
                request.ExternalSellerName,
                request.ExternalSellerContact,
                request.ListingUrl,
                request.PurchaseDate,
                request.Notes,
                request.Items,
                request.PurchaseKind), cancellationToken);

            draft.SourceType = normalized.SourceType;
            draft.PurchaseKind = normalized.PurchaseKind;
            draft.SupplierId = normalized.SupplierId;
            draft.MarketplaceName = normalized.MarketplaceName;
            draft.ExternalSellerName = normalized.ExternalSellerName;
            draft.ExternalSellerContact = normalized.ExternalSellerContact;
            draft.ListingUrl = normalized.ListingUrl;
            draft.PurchaseDate = normalized.PurchaseDate;
            draft.Notes = normalized.Notes;
            draft.TotalAmount = normalized.TotalAmount;
            draft.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
            draft.RowVersion++;
            db.PurchaseDraftItems.RemoveRange(draft.Items);
            draft.Items.Clear();
            foreach (var item in normalized.Items)
            {
                draft.Items.Add(new PurchaseDraftItem
                {
                    Id = Guid.CreateVersion7(),
                    DraftId = draft.Id,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    ItemName = item.ItemName,
                    Sku = item.Sku,
                    ItemKind = item.ItemKind,
                    Brand = item.Brand,
                    Model = item.Model,
                    Category = item.Category,
                    Condition = item.Condition,
                    Description = item.Description,
                    ExternalCode = item.ExternalCode,
                    SpecificationsJson = item.SpecificationsJson,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.LineTotal,
                    SortOrder = item.SortOrder
                });
            }
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, actorId, "Purchasing.DraftUpdated", draft, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(draft, await SupplierNameAsync(db, draft.SupplierId, cancellationToken));
        }
        catch (PurchaseDraftException)
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAsync(transaction, cancellationToken);
            throw new PurchaseDraftException(409, "The purchase draft was changed by another user.");
        }
        catch
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<PurchaseDraftView> CancelAsync(
        Guid id,
        long rowVersion,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var draft = await db.PurchaseDrafts.Include(item => item.Items)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new PurchaseDraftException(404, "Purchase draft was not found.");
            if (draft.Status != PurchaseDraftStatus.Draft)
                throw new PurchaseDraftException(409, "Only a draft purchase can be cancelled.");
            if (rowVersion <= 0 || rowVersion != draft.RowVersion)
                throw new PurchaseDraftException(409, "The purchase draft was changed by another user.");
            draft.Status = PurchaseDraftStatus.Cancelled;
            draft.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
            draft.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, actorId, "Purchasing.DraftCancelled", draft, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(draft, await SupplierNameAsync(db, draft.SupplierId, cancellationToken));
        }
        catch (PurchaseDraftException)
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch
        {
            await RollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    private static async Task<NormalizedDraft> NormalizeAsync(
        PurchasingDbContext db,
        PurchaseDraftCreateRequest request,
        CancellationToken cancellationToken)
    {
        var source = request.SourceType?.Trim() ?? string.Empty;
        if (!PurchaseSourceType.All.Contains(source, StringComparer.Ordinal))
            throw new PurchaseDraftException(400, "A supported purchase source type is required.");

        Guid? supplierId = null;
        if (source == PurchaseSourceType.RegisteredSupplier)
        {
            if (request.SupplierId is null)
                throw new PurchaseDraftException(400, "A registered supplier is required for this source type.");
            if (!await db.SupplierProfiles.AnyAsync(item => item.Id == request.SupplierId && item.IsActive, cancellationToken))
                throw new PurchaseDraftException(400, "The selected supplier is not active or does not exist.");
            supplierId = request.SupplierId;
        }
        else if (request.SupplierId is not null)
        {
            throw new PurchaseDraftException(400, "A registered supplier can only be used with the RegisteredSupplier source.");
        }

        var marketplace = Optional(request.MarketplaceName, 160, "Marketplace name");
        var seller = Optional(request.ExternalSellerName, 160, "External seller name");
        var contact = Optional(request.ExternalSellerContact, 160, "External seller contact");
        if (source == PurchaseSourceType.Marketplace && marketplace is null)
            throw new PurchaseDraftException(400, "A marketplace name is required for marketplace purchases.");
        if (source == PurchaseSourceType.IndividualSeller && seller is null)
            throw new PurchaseDraftException(400, "An external seller name is required for individual seller purchases.");

        var listingUrl = Optional(request.ListingUrl, 512, "Listing URL");
        if (listingUrl is not null &&
            (!Uri.TryCreate(listingUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
            throw new PurchaseDraftException(400, "Listing URL must be an absolute http(s) URL.");

        var notes = Optional(request.Notes, 2000, "Notes");
        var itemInputs = request.Items ?? [];
        if (itemInputs.Count is < 1 or > 100)
            throw new PurchaseDraftException(400, "A purchase draft needs between 1 and 100 items.");

        var productIds = itemInputs.Where(item => item.ProductId != Guid.Empty)
            .Select(item => item.ProductId).Distinct().ToArray();
        var products = await db.Set<SupplierCatalogProductReference>().AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, product => new { product.Name, product.Sku }, cancellationToken);
        var variantIds = itemInputs.Where(item => item.VariantId != null)
            .Select(item => item.VariantId!.Value).Distinct().ToArray();
        var variants = variantIds.Length == 0
            ? new Dictionary<Guid, SupplierVariantReference>()
            : await db.SupplierVariants.AsNoTracking()
                .Where(variant => variantIds.Contains(variant.Id))
                .ToDictionaryAsync(variant => variant.Id, cancellationToken);

        var items = new List<NormalizedDraftItem>();
        var sort = 0;
        var kind = request.PurchaseKind?.Trim() ?? "Component";
        if (kind is not ("Component" or "Components" or "Prebuilt" or "Bundle" or "Other"))
            throw new PurchaseDraftException(400, "A supported purchase kind is required.");
        foreach (var item in itemInputs)
        {
            if (item.ProductId == Guid.Empty)
            {
                var manualName = Optional(item.ItemName, 255, "Item name")
                    ?? throw new PurchaseDraftException(400, "A manual purchase item needs a name.");
                if (item.Quantity is < 1 or > 100000)
                    throw new PurchaseDraftException(400, "Item quantity must be between 1 and 100000.");
                if (item.UnitPrice <= 0 || item.UnitPrice > 1_000_000_000m)
                    throw new PurchaseDraftException(400, "Item unit price must be a positive planning amount.");
                var manualLineTotal = Math.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero);
                items.Add(new NormalizedDraftItem(
                    Guid.Empty,
                    null,
                    manualName,
                    Optional(item.ExternalCode, 128, "External code") ?? "MANUAL",
                    NormalizeItemKind(item.ItemKind),
                    Optional(item.Brand, 160, "Brand"),
                    Optional(item.Model, 160, "Model"),
                    Optional(item.Category, 160, "Category"),
                    Optional(item.Condition, 32, "Condition"),
                    Optional(item.Description, 4000, "Description"),
                    Optional(item.ExternalCode, 128, "External code"),
                    Optional(item.Specifications, 8000, "Specifications"),
                    item.Quantity,
                    item.UnitPrice,
                    manualLineTotal,
                    sort++));
                continue;
            }
            if (!products.TryGetValue(item.ProductId, out var product))
                throw new PurchaseDraftException(400, "A purchase item references a product that does not exist.");
            if (item.Quantity is < 1 or > 100000)
                throw new PurchaseDraftException(400, "Item quantity must be between 1 and 100000.");
            if (item.UnitPrice <= 0 || item.UnitPrice > 1_000_000_000m)
                throw new PurchaseDraftException(400, "Item unit price must be a positive planning amount.");
            var variantName = (string?)null;
            var sku = product.Sku;
            if (item.VariantId is not null)
            {
                if (!variants.TryGetValue(item.VariantId.Value, out var variant) ||
                    variant.ProductId != item.ProductId || !variant.IsActive)
                    throw new PurchaseDraftException(400, "A purchase item references an invalid or inactive variant.");
                variantName = variant.Name;
                sku = variant.Sku;
            }
            var lineTotal = Math.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero);
            items.Add(new NormalizedDraftItem(
                item.ProductId,
                item.VariantId,
                variantName is null ? product.Name : $"{product.Name} — {variantName}",
                sku,
                NormalizeItemKind(item.ItemKind),
                Optional(item.Brand, 160, "Brand"),
                Optional(item.Model, 160, "Model"),
                Optional(item.Category, 160, "Category"),
                Optional(item.Condition, 32, "Condition"),
                Optional(item.Description, 4000, "Description"),
                Optional(item.ExternalCode, 128, "External code"),
                Optional(item.Specifications, 8000, "Specifications"),
                item.Quantity,
                item.UnitPrice,
                lineTotal,
                sort++));
        }

        return new NormalizedDraft(
            source,
            kind,
            supplierId,
            marketplace,
            seller,
            contact,
            listingUrl,
            request.PurchaseDate,
            notes,
            items.Sum(item => item.LineTotal),
            items);
    }

    private async Task WriteAuditAsync(
        PurchasingDbContext db,
        IDbContextTransaction transaction,
        Guid actorId,
        string eventType,
        PurchaseDraft draft,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new SecurityAuditEntry(
                eventType,
                "Succeeded",
                "User",
                ActorUserAccountId: actorId,
                SafeMetadata: JsonSerializer.Serialize(new
                {
                    draftId = draft.Id,
                    source = draft.SourceType,
                    itemCount = draft.Items.Count,
                    status = draft.Status
                })),
            db.Database.GetDbConnection(),
            transaction.GetDbTransaction(),
            cancellationToken);
    }

    private static async Task<string?> SupplierNameAsync(
        PurchasingDbContext db,
        Guid? supplierId,
        CancellationToken cancellationToken) =>
        supplierId is null
            ? null
            : await db.SupplierProfiles.AsNoTracking()
                .Where(item => item.Id == supplierId)
                .Select(item => item.CompanyName)
                .SingleOrDefaultAsync(cancellationToken);

    private static PurchaseDraftView Map(PurchaseDraft draft, string? supplierName) => new(
        draft.Id,
        draft.SourceType,
        draft.PurchaseKind,
        draft.SupplierId,
        supplierName,
        draft.MarketplaceName,
        draft.ExternalSellerName,
        draft.ExternalSellerContact,
        draft.ListingUrl,
        draft.PurchaseDate,
        draft.Notes,
        draft.Status,
        draft.TotalAmount,
        draft.CreatedByUserAccountId,
        draft.CreatedAtUtc,
        draft.UpdatedAtUtc,
        draft.RowVersion,
        draft.Items.OrderBy(item => item.SortOrder).Select(item => new PurchaseDraftItemView(
            item.Id,
            item.ProductId,
            item.VariantId,
            item.ItemName,
            item.Sku,
            item.ItemKind,
            item.Brand,
            item.Model,
            item.Category,
            item.Condition,
            item.Description,
            item.ExternalCode,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal)).ToList());

    private static string? Optional(string? value, int max, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var text = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (text.Length > max)
            throw new PurchaseDraftException(400, $"{field} is too long.");
        if (text.Contains('<') || text.Contains('>'))
            throw new PurchaseDraftException(400, $"{field} must be plain text.");
        return text;
    }

    private static async Task RollbackAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch
        {
            // Preserve the original failure.
        }
    }

    private sealed record NormalizedDraft(
        string SourceType,
        string PurchaseKind,
        Guid? SupplierId,
        string? MarketplaceName,
        string? ExternalSellerName,
        string? ExternalSellerContact,
        string? ListingUrl,
        DateOnly? PurchaseDate,
        string? Notes,
        decimal TotalAmount,
        IReadOnlyList<NormalizedDraftItem> Items);

    private sealed record NormalizedDraftItem(
        Guid ProductId,
        Guid? VariantId,
        string ItemName,
        string Sku,
        string ItemKind,
        string? Brand,
        string? Model,
        string? Category,
        string? Condition,
        string? Description,
        string? ExternalCode,
        string? SpecificationsJson,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        int SortOrder);

    private static string NormalizeItemKind(string? value) =>
        value?.Trim() is "Prebuilt" or "Bundle" or "Other" ? value.Trim() : "Component";
}

public sealed class PurchaseDraftException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
