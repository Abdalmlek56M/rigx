using HardwareStore.Common.Domain;
using System.Text.Json;
using HardwareStore.Modules.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace HardwareStore.Modules.Purchasing;

/// <summary>
/// Supplier-to-product-variant catalog. It deliberately stores only the
/// supplier's own reference, notes, and activation state; no prices, MOQ, lead
/// times, stock, or purchase-order data are introduced here.
/// </summary>
public sealed class SupplierProductCatalogService(
    IDbContextFactory<PurchasingDbContext> purchasingFactory,
    ISecurityAuditTransactionWriter auditWriter,
    TimeProvider timeProvider)
{
    private const long AdvisoryLockKey = 0x535550504C50524F; // SUPPLPRO

    public async Task<SupplierProductPage> GetAsync(
        Guid supplierId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        await EnsureSupplierExistsAsync(db, supplierId, cancellationToken);

        var query = db.SupplierProducts.AsNoTracking()
            .Include(item => item.Variant)
            .ThenInclude(variant => variant.Product)
            .Where(item => item.SupplierProfileId == supplierId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 100)
                term = term[..100];
            query = query.Where(item =>
                EF.Functions.ILike(item.SupplierSku, $"%{term}%") ||
                EF.Functions.ILike(item.Variant.Sku, $"%{term}%") ||
                EF.Functions.ILike(item.Variant.Name, $"%{term}%") ||
                EF.Functions.ILike(item.Variant.Product.Name, $"%{term}%"));
        }
        if (isActive.HasValue)
            query = query.Where(item => item.IsActive == isActive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.Variant.Product.Name)
            .ThenBy(item => item.Variant.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new SupplierProductPage(items.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<SupplierProductView?> GetByIdAsync(
        Guid supplierId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var db = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.SupplierProducts.AsNoTracking()
            .Include(product => product.Variant)
            .ThenInclude(variant => variant.Product)
            .SingleOrDefaultAsync(product => product.Id == id && product.SupplierProfileId == supplierId, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<IReadOnlyList<SupplierVariantOption>> SearchVariantsAsync(
        Guid supplierId,
        string? search,
        int limit,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        await using var db = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        await EnsureSupplierExistsAsync(db, supplierId, cancellationToken);
        var query = db.SupplierVariants.AsNoTracking()
            .Include(variant => variant.Product)
            .Where(variant => variant.IsActive);
        var linkedVariantIds = db.SupplierProducts
            .Where(item => item.SupplierProfileId == supplierId)
            .Select(item => item.ProductVariantId);
        query = query.Where(variant => !linkedVariantIds.Contains(variant.Id));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 100)
                term = term[..100];
            query = query.Where(variant =>
                EF.Functions.ILike(variant.Sku, $"%{term}%") ||
                EF.Functions.ILike(variant.Name, $"%{term}%") ||
                EF.Functions.ILike(variant.Product.Name, $"%{term}%"));
        }
        return await query
            .OrderBy(variant => variant.Product.Name)
            .ThenBy(variant => variant.Name)
            .Take(limit)
            .Select(variant => new SupplierVariantOption(
                variant.Id,
                variant.ProductId,
                variant.Product.Name,
                variant.Name,
                variant.Sku))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierProductView> CreateAsync(
        Guid supplierId,
        SupplierProductCreateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var supplierSku = NormalizeRequired(request.SupplierSku, 128, "Supplier SKU");
        var notes = NormalizeOptional(request.Notes, 2000, "Notes");
        if (request.ProductVariantId == Guid.Empty)
            throw new PurchasingValidationException(400, "A product variant is required.");

        await using var db = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0})",
                new object[] { AdvisoryLockKey },
                cancellationToken);
            await EnsureSupplierExistsAsync(db, supplierId, cancellationToken);
            var variant = await db.SupplierVariants
                .SingleOrDefaultAsync(item => item.Id == request.ProductVariantId, cancellationToken)
                ?? throw new PurchasingValidationException(400, "Product variant was not found.");
            if (!variant.IsActive)
                throw new PurchasingValidationException(409, "Inactive product variants cannot be linked to a supplier.");
            if (await db.SupplierProducts.AnyAsync(
                    item => item.SupplierProfileId == supplierId &&
                            item.ProductVariantId == request.ProductVariantId,
                    cancellationToken))
                throw new PurchasingValidationException(409, "This product variant is already linked to the supplier.");
            if (await db.SupplierProducts.AnyAsync(
                    item => item.SupplierProfileId == supplierId && item.SupplierSku == supplierSku,
                    cancellationToken))
                throw new PurchasingValidationException(409, "This supplier SKU is already used by the supplier.");

            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var item = new SupplierProduct
            {
                Id = Guid.CreateVersion7(),
                SupplierProfileId = supplierId,
                ProductVariantId = request.ProductVariantId,
                SupplierSku = supplierSku,
                Notes = notes,
                IsActive = request.IsActive,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                RowVersion = 1
            };
            db.SupplierProducts.Add(item);
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(
                db,
                transaction,
                actorUserId,
                "Purchasing.SupplierProductLinked",
                item,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(supplierId, item.Id, cancellationToken)
                ?? throw new PurchasingValidationException(500, "Supplier product was created but could not be reloaded.");
        }
        catch (PurchasingValidationException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new PurchasingValidationException(409, "This supplier product relation or SKU already exists.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new PurchasingValidationException(409, "The supplier product was changed by another user.");
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<SupplierProductView> UpdateAsync(
        Guid supplierId,
        Guid id,
        SupplierProductUpdateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.RowVersion <= 0)
            throw new PurchasingValidationException(400, "A positive supplier product row version is required.");
        var supplierSku = NormalizeRequired(request.SupplierSku, 128, "Supplier SKU");
        var notes = NormalizeOptional(request.Notes, 2000, "Notes");

        await using var db = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM supplier_products WHERE id = {0} AND supplier_profile_id = {1} FOR UPDATE",
                new object[] { id, supplierId },
                cancellationToken);
            var item = await db.SupplierProducts
                .Include(product => product.Variant)
                .ThenInclude(variant => variant.Product)
                .SingleOrDefaultAsync(product => product.Id == id && product.SupplierProfileId == supplierId, cancellationToken)
                ?? throw new PurchasingValidationException(404, "Supplier product was not found.");
            if (request.RowVersion != item.RowVersion)
                throw new PurchasingValidationException(409, "The supplier product was changed by another user.");
            if (await db.SupplierProducts.AnyAsync(
                    other => other.SupplierProfileId == supplierId &&
                             other.Id != id &&
                             other.SupplierSku == supplierSku,
                    cancellationToken))
                throw new PurchasingValidationException(409, "This supplier SKU is already used by the supplier.");

            item.SupplierSku = supplierSku;
            item.Notes = notes;
            item.IsActive = request.IsActive;
            item.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
            item.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(
                db,
                transaction,
                actorUserId,
                request.IsActive ? "Purchasing.SupplierProductUpdated" : "Purchasing.SupplierProductDeactivated",
                item,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(supplierId, item.Id, cancellationToken)
                ?? throw new PurchasingValidationException(404, "Supplier product was not found.");
        }
        catch (PurchasingValidationException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new PurchasingValidationException(409, "This supplier product relation or SKU already exists.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new PurchasingValidationException(409, "The supplier product was changed by another user.");
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    private static async Task EnsureSupplierExistsAsync(
        PurchasingDbContext db,
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        if (!await db.SupplierProfiles.AnyAsync(item => item.Id == supplierId, cancellationToken))
            throw new PurchasingValidationException(404, "Supplier was not found.");
    }

    private async Task WriteAuditAsync(
        PurchasingDbContext db,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        Guid actorUserId,
        string eventType,
        SupplierProduct item,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new SecurityAuditEntry(
                eventType,
                "Succeeded",
                "User",
                ActorUserAccountId: actorUserId,
                SafeMetadata: JsonSerializer.Serialize(new
                {
                    supplierId = item.SupplierProfileId,
                    supplierProductId = item.Id,
                    active = item.IsActive
                })),
            db.Database.GetDbConnection(),
            transaction.GetDbTransaction(),
            cancellationToken);
    }

    private static SupplierProductView Map(SupplierProduct item) => new(
        item.Id,
        item.SupplierProfileId,
        item.ProductVariantId,
        item.Variant.ProductId,
        item.Variant.Product.Name,
        item.Variant.Name,
        item.Variant.Sku,
        item.SupplierSku,
        item.Notes,
        item.IsActive,
        item.CreatedAtUtc,
        item.UpdatedAtUtc,
        item.RowVersion);

    private static string NormalizeRequired(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new PurchasingValidationException(400, $"{label} is required.");
        var normalized = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (normalized.Length > maxLength)
            throw new PurchasingValidationException(400, $"{label} is too long.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (normalized.Length > maxLength)
            throw new PurchasingValidationException(400, $"{label} is too long.");
        return normalized;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static async Task SafeRollbackAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken cancellationToken)
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
}

public sealed class PurchasingValidationException(int statusCode, string message) : AppException(statusCode, message);

