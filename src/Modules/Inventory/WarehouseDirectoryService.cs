using System.Text.Json;
using HardwareStore.Modules.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace HardwareStore.Modules.Inventory;

/// <summary>
/// M5 warehouse master-data slice. It records warehouse identity, address, and
/// activation only; it does not create stock items, balances, or movements.
/// </summary>
public sealed class WarehouseDirectoryService(
    IDbContextFactory<InventoryDbContext> contextFactory,
    ISecurityAuditTransactionWriter auditWriter,
    TimeProvider timeProvider)
{
    private const long AdvisoryLockKey = 0x57415245484F5553; // WAREHOUS

    public async Task<WarehousePage> GetAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Warehouses.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 100)
                term = term[..100];
            query = query.Where(item =>
                EF.Functions.ILike(item.Code, $"%{term}%") ||
                EF.Functions.ILike(item.Name, $"%{term}%") ||
                (item.Address != null && EF.Functions.ILike(item.Address, $"%{term}%")));
        }
        if (isActive.HasValue)
            query = query.Where(item => item.IsActive == isActive.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new WarehousePage(items.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<WarehouseView?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.Warehouses.AsNoTracking()
            .SingleOrDefaultAsync(warehouse => warehouse.Id == id, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<WarehouseView> CreateAsync(
        WarehouseCreateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var input = NormalizeAndValidate(
            request.Code, request.Name, request.Address,
            request.ContactPhone, request.ManagerName, request.Notes, request.IsActive);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0})",
                new object[] { AdvisoryLockKey },
                cancellationToken);
            if (await db.Warehouses.AnyAsync(item => item.Code == input.Code, cancellationToken))
                throw new WarehouseDirectoryException(409, "A warehouse with this code already exists.");

            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var warehouse = new Warehouse
            {
                Id = Guid.CreateVersion7(),
                Code = input.Code,
                Name = input.Name,
                Address = input.Address,
                ContactPhone = input.ContactPhone,
                ManagerName = input.ManagerName,
                Notes = input.Notes,
                IsActive = input.IsActive,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                RowVersion = 1
            };
            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, actorUserId, "Inventory.WarehouseCreated", warehouse, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(warehouse);
        }
        catch (WarehouseDirectoryException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new WarehouseDirectoryException(409, "A warehouse with this code already exists.");
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<WarehouseView> UpdateAsync(
        Guid id,
        WarehouseUpdateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.RowVersion <= 0)
            throw new WarehouseDirectoryException(400, "A positive warehouse row version is required.");
        var input = NormalizeAndValidate(
            request.Code, request.Name, request.Address,
            request.ContactPhone, request.ManagerName, request.Notes, request.IsActive);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM warehouses WHERE id = {0} FOR UPDATE",
                new object[] { id },
                cancellationToken);
            var warehouse = await db.Warehouses
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new WarehouseDirectoryException(404, "Warehouse was not found.");
            if (request.RowVersion != warehouse.RowVersion)
                throw new WarehouseDirectoryException(409, "The warehouse was changed by another user.");
            if (await db.Warehouses.AnyAsync(
                    item => item.Code == input.Code && item.Id != id,
                    cancellationToken))
                throw new WarehouseDirectoryException(409, "A warehouse with this code already exists.");

            warehouse.Code = input.Code;
            warehouse.Name = input.Name;
            warehouse.Address = input.Address;
            warehouse.ContactPhone = input.ContactPhone;
            warehouse.ManagerName = input.ManagerName;
            warehouse.Notes = input.Notes;
            warehouse.IsActive = input.IsActive;
            warehouse.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
            warehouse.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(
                db,
                transaction,
                actorUserId,
                input.IsActive ? "Inventory.WarehouseUpdated" : "Inventory.WarehouseDeactivated",
                warehouse,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(warehouse);
        }
        catch (WarehouseDirectoryException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new WarehouseDirectoryException(409, "A warehouse with this code already exists.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new WarehouseDirectoryException(409, "The warehouse was changed by another user.");
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    private async Task WriteAuditAsync(
        InventoryDbContext db,
        IDbContextTransaction transaction,
        Guid actorUserId,
        string eventType,
        Warehouse warehouse,
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
                    warehouseId = warehouse.Id,
                    active = warehouse.IsActive
                })),
            db.Database.GetDbConnection(),
            transaction.GetDbTransaction(),
            cancellationToken);
    }

    private static WarehouseView Map(Warehouse item) => new(
        item.Id,
        item.Code,
        item.Name,
        item.Address,
        item.ContactPhone,
        item.ManagerName,
        item.Notes,
        item.IsActive,
        item.CreatedAtUtc,
        item.UpdatedAtUtc,
        item.RowVersion);

    private static NormalizedWarehouseInput NormalizeAndValidate(
        string? rawCode,
        string? rawName,
        string? rawAddress,
        string? rawContactPhone,
        string? rawManagerName,
        string? rawNotes,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
            throw new WarehouseDirectoryException(400, "Warehouse code is required.");
        var code = rawCode.Trim().Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant();
        if (code.Length > 32)
            throw new WarehouseDirectoryException(400, "Warehouse code is too long.");
        if (string.IsNullOrWhiteSpace(rawName))
            throw new WarehouseDirectoryException(400, "Warehouse name is required.");
        var name = rawName.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (name.Length > 128)
            throw new WarehouseDirectoryException(400, "Warehouse name is too long.");
        var address = string.IsNullOrWhiteSpace(rawAddress)
            ? null
            : rawAddress.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (address?.Length > 255)
            throw new WarehouseDirectoryException(400, "Warehouse address is too long.");
        var contactPhone = NormalizeOptional(rawContactPhone, 32, "Warehouse contact phone", allowPhoneOnly: true);
        var managerName = NormalizeOptional(rawManagerName, 128, "Warehouse manager name");
        var notes = NormalizeOptional(rawNotes, 1000, "Warehouse notes", allowNewlines: true);
        return new NormalizedWarehouseInput(code, name, address, contactPhone, managerName, notes, isActive);
    }

    private static string? NormalizeOptional(
        string? value,
        int maxLength,
        string label,
        bool allowPhoneOnly = false,
        bool allowNewlines = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var text = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (text.Length > maxLength)
            throw new WarehouseDirectoryException(400, $"{label} is too long.");
        if (text.Contains('<') || text.Contains('>'))
            throw new WarehouseDirectoryException(400, $"{label} must be plain text.");
        if (allowPhoneOnly && text.Any(character =>
                !(char.IsAsciiDigit(character) || character is '+' or ' ' or '-' or '(' or ')')))
            throw new WarehouseDirectoryException(400, "Warehouse contact phone contains unsupported characters.");
        if (!allowNewlines && text.Any(character => char.IsControl(character)))
            throw new WarehouseDirectoryException(400, $"{label} contains unsupported characters.");
        if (allowNewlines && text.Any(character =>
                char.IsControl(character) && character is not ('\n' or '\r' or '\t')))
            throw new WarehouseDirectoryException(400, $"{label} contains unsupported characters.");
        return text;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static async Task SafeRollbackAsync(
        IDbContextTransaction transaction,
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

    private sealed record NormalizedWarehouseInput(
        string Code,
        string Name,
        string? Address,
        string? ContactPhone,
        string? ManagerName,
        string? Notes,
        bool IsActive);
}
