using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace HardwareStore.Modules.Purchasing;

/// <summary>
/// M5 supplier master-data slice.
///
/// This slice deliberately does not touch stock, SKU/barcode scope, reservations,
/// transfers, costs, or receiving. It creates the supplier party in Identity's
/// persons table and the supplier profile in Purchasing in one shared database
/// transaction, with audit in the same transaction.
/// </summary>
public sealed class SupplierDirectoryService(
    IDbContextFactory<PurchasingDbContext> purchasingFactory,
    IDbContextFactory<IdentityDbContext> identityFactory,
    ISecurityAuditTransactionWriter auditWriter,
    TimeProvider timeProvider,
    TimeZoneInfo businessTimeZone)
{
    private const long AdvisoryLockKey = 0x535550504C494552; // SUPPLIER

    public async Task<SupplierPage> GetAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await using var db = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        var query = db.SupplierProfiles.AsNoTracking().Include(item => item.Person).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 100)
                term = term[..100];
            query = query.Where(item =>
                EF.Functions.ILike(item.CompanyName, $"%{term}%") ||
                (item.TaxIdNumber != null && EF.Functions.ILike(item.TaxIdNumber, $"%{term}%")) ||
                (item.CommercialRegistration != null && EF.Functions.ILike(item.CommercialRegistration, $"%{term}%")) ||
                (item.ContactPersonName != null && EF.Functions.ILike(item.ContactPersonName, $"%{term}%")) ||
                EF.Functions.ILike(item.Person.FullName, $"%{term}%") ||
                (item.Person.Email != null && EF.Functions.ILike(item.Person.Email, $"%{term}%")));
        }
        if (isActive.HasValue)
            query = query.Where(item => item.IsActive == isActive.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.CompanyName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(SupplierProjection)
            .ToListAsync(cancellationToken);
        return new SupplierPage(items, page, pageSize, total);
    }

    public async Task<SupplierView?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        return await db.SupplierProfiles.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(SupplierProjection)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<SupplierView> CreateAsync(
        SupplierCreateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var input = NormalizeAndValidate(request);
        await using var purchasing = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        await using var identity = await identityFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await purchasing.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await purchasing.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0})",
                new object[] { AdvisoryLockKey },
                cancellationToken);
            await EnsureUniqueBusinessIdentifiersAsync(
                purchasing,
                input.TaxIdNumber,
                input.CommercialRegistration,
                null,
                cancellationToken);

            identity.Database.SetDbConnection(purchasing.Database.GetDbConnection());
            await identity.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);

            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var person = new Person
            {
                Id = Guid.CreateVersion7(),
                FullName = input.CompanyName,
                PhoneNumber = input.PhoneNumber,
                Email = input.Email,
                IsSupplier = true,
                IsActive = true
            };
            await new PersonCodeGenerator(identity, timeProvider, businessTimeZone)
                .CreatePersonInCurrentTransactionAsync(person, cancellationToken);

            var profile = new SupplierProfile
            {
                Id = Guid.CreateVersion7(),
                PersonId = person.Id,
                CompanyName = input.CompanyName,
                TaxIdNumber = input.TaxIdNumber,
                CommercialRegistration = input.CommercialRegistration,
                ContactPersonName = input.ContactPersonName,
                ContactPersonPhone = input.ContactPersonPhone,
                PaymentTermsDays = input.PaymentTermsDays,
                Notes = input.Notes,
                IsActive = input.IsActive,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                RowVersion = 1
            };
            purchasing.SupplierProfiles.Add(profile);
            await purchasing.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(
                purchasing,
                transaction,
                actorUserId,
                "Purchasing.SupplierCreated",
                profile,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(profile.Id, cancellationToken)
                ?? throw new SupplierDirectoryException(500, "Supplier was created but could not be reloaded.");
        }
        catch (SupplierDirectoryException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new SupplierDirectoryException(409, "A supplier with the same tax ID or commercial registration already exists.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new SupplierDirectoryException(409, "The supplier was changed by another user.");
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<SupplierView> UpdateAsync(
        Guid id,
        SupplierUpdateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.RowVersion <= 0)
            throw new SupplierDirectoryException(400, "A positive supplier row version is required.");
        var input = NormalizeAndValidate(request);
        await using var purchasing = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        await using var identity = await identityFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await purchasing.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await purchasing.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM supplier_profiles WHERE id = {0} FOR UPDATE",
                new object[] { id },
                cancellationToken);
            var profile = await purchasing.SupplierProfiles
                .Include(item => item.Person)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new SupplierDirectoryException(404, "Supplier was not found.");
            if (request.RowVersion != profile.RowVersion)
                throw new SupplierDirectoryException(409, "The supplier was changed by another user.");

            await EnsureUniqueBusinessIdentifiersAsync(
                purchasing,
                input.TaxIdNumber,
                input.CommercialRegistration,
                id,
                cancellationToken);
            identity.Database.SetDbConnection(purchasing.Database.GetDbConnection());
            await identity.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);
            var person = await identity.Persons
                .SingleOrDefaultAsync(item => item.Id == profile.PersonId, cancellationToken)
                ?? throw new SupplierDirectoryException(409, "The supplier party record is missing.");

            var now = timeProvider.GetUtcNow().ToUniversalTime();
            person.FullName = input.CompanyName;
            person.PhoneNumber = input.PhoneNumber;
            person.Email = input.Email;
            person.IsSupplier = true;

            profile.CompanyName = input.CompanyName;
            profile.TaxIdNumber = input.TaxIdNumber;
            profile.CommercialRegistration = input.CommercialRegistration;
            profile.ContactPersonName = input.ContactPersonName;
            profile.ContactPersonPhone = input.ContactPersonPhone;
            profile.PaymentTermsDays = input.PaymentTermsDays;
            profile.Notes = input.Notes;
            profile.IsActive = input.IsActive;
            profile.UpdatedAtUtc = now;
            profile.RowVersion++;

            await identity.SaveChangesAsync(cancellationToken);
            await purchasing.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(
                purchasing,
                transaction,
                actorUserId,
                input.IsActive ? "Purchasing.SupplierUpdated" : "Purchasing.SupplierDeactivated",
                profile,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(profile.Id, cancellationToken)
                ?? throw new SupplierDirectoryException(404, "Supplier was not found.");
        }
        catch (SupplierDirectoryException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new SupplierDirectoryException(409, "A supplier with the same tax ID or commercial registration already exists.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new SupplierDirectoryException(409, "The supplier was changed by another user.");
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<SupplierFinancialView> GetFinancialsAsync(
        Guid id,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        await using var purchasing = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await purchasing.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var profile = await purchasing.SupplierProfiles.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new SupplierDirectoryException(404, "Supplier was not found.");
            await WriteAuditAsync(
                purchasing,
                transaction,
                actorUserId,
                "Purchasing.SupplierFinancialsViewed",
                profile,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapFinancials(profile);
        }
        catch (SupplierDirectoryException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<SupplierFinancialView> UpdateFinancialsAsync(
        Guid id,
        SupplierFinancialWriteRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.RowVersion <= 0)
            throw new SupplierDirectoryException(400, "A positive supplier row version is required.");
        var bankName = NormalizeOptional(request.BankName, 128);
        var bankAccount = NormalizeOptional(request.BankAccountNumber, 64);
        var bankIban = NormalizeOptional(request.BankIban, 64);

        await using var purchasing = await purchasingFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await purchasing.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await purchasing.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM supplier_profiles WHERE id = {0} FOR UPDATE",
                new object[] { id },
                cancellationToken);
            var profile = await purchasing.SupplierProfiles
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
                ?? throw new SupplierDirectoryException(404, "Supplier was not found.");
            if (request.RowVersion != profile.RowVersion)
                throw new SupplierDirectoryException(409, "The supplier was changed by another user.");

            profile.BankName = bankName;
            profile.BankAccountNumber = bankAccount;
            profile.BankIban = bankIban;
            profile.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
            profile.RowVersion++;
            await purchasing.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(
                purchasing,
                transaction,
                actorUserId,
                "Purchasing.SupplierFinancialsUpdated",
                profile,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapFinancials(profile);
        }
        catch (SupplierDirectoryException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new SupplierDirectoryException(409, "The supplier was changed by another user.");
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    private static async Task EnsureUniqueBusinessIdentifiersAsync(
        PurchasingDbContext db,
        string? taxIdNumber,
        string? commercialRegistration,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        if (taxIdNumber is not null)
        {
            var taxQuery = db.SupplierProfiles.Where(item => item.TaxIdNumber == taxIdNumber);
            if (excludingId.HasValue)
                taxQuery = taxQuery.Where(item => item.Id != excludingId.Value);
            if (await taxQuery.AnyAsync(cancellationToken))
                throw new SupplierDirectoryException(409, "A supplier with the same tax ID already exists.");
        }
        if (commercialRegistration is not null)
        {
            var registrationQuery = db.SupplierProfiles
                .Where(item => item.CommercialRegistration == commercialRegistration);
            if (excludingId.HasValue)
                registrationQuery = registrationQuery.Where(item => item.Id != excludingId.Value);
            if (await registrationQuery.AnyAsync(cancellationToken))
                throw new SupplierDirectoryException(409, "A supplier with the same commercial registration already exists.");
        }
    }

    private async Task WriteAuditAsync(
        PurchasingDbContext db,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        Guid actorUserId,
        string eventType,
        SupplierProfile profile,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new SecurityAuditEntry(
                eventType,
                "Succeeded",
                "User",
                ActorUserAccountId: actorUserId,
                SafeMetadata: System.Text.Json.JsonSerializer.Serialize(new
                {
                    supplierId = profile.Id,
                    active = profile.IsActive
                })),
            db.Database.GetDbConnection(),
            transaction.GetDbTransaction(),
            cancellationToken);
    }

    private static readonly Expression<Func<SupplierProfile, SupplierView>> SupplierProjection =
        profile => new SupplierView(
            profile.Id,
            profile.PersonId,
            profile.Person.PersonCode,
            profile.CompanyName,
            profile.Person.PhoneNumber,
            profile.Person.Email,
            profile.TaxIdNumber,
            profile.CommercialRegistration,
            profile.ContactPersonName,
            profile.ContactPersonPhone,
            profile.PaymentTermsDays,
            profile.Notes,
            profile.IsActive,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.RowVersion);

    private static SupplierView Map(SupplierProfile profile) => new(
        profile.Id,
        profile.PersonId,
        profile.Person.PersonCode,
        profile.CompanyName,
        profile.Person.PhoneNumber,
        profile.Person.Email,
        profile.TaxIdNumber,
        profile.CommercialRegistration,
        profile.ContactPersonName,
        profile.ContactPersonPhone,
        profile.PaymentTermsDays,
        profile.Notes,
        profile.IsActive,
        profile.CreatedAtUtc,
        profile.UpdatedAtUtc,
        profile.RowVersion);

    private static SupplierFinancialView MapFinancials(SupplierProfile profile) => new(
        profile.Id,
        profile.BankName,
        profile.BankAccountNumber,
        profile.BankIban,
        profile.RowVersion);

    private static NormalizedSupplierInput NormalizeAndValidate(SupplierCreateRequest request) =>
        NormalizeAndValidate(
            request.CompanyName,
            request.PhoneNumber,
            request.Email,
            request.TaxIdNumber,
            request.CommercialRegistration,
            request.ContactPersonName,
            request.ContactPersonPhone,
            request.PaymentTermsDays,
            request.Notes,
            request.IsActive);

    private static NormalizedSupplierInput NormalizeAndValidate(SupplierUpdateRequest request) =>
        NormalizeAndValidate(
            request.CompanyName,
            request.PhoneNumber,
            request.Email,
            request.TaxIdNumber,
            request.CommercialRegistration,
            request.ContactPersonName,
            request.ContactPersonPhone,
            request.PaymentTermsDays,
            request.Notes,
            request.IsActive);

    private static NormalizedSupplierInput NormalizeAndValidate(
        string? rawCompanyName,
        string? rawPhoneNumber,
        string? rawEmail,
        string? rawTaxIdNumber,
        string? rawCommercialRegistration,
        string? rawContactPersonName,
        string? rawContactPersonPhone,
        int paymentTermsDays,
        string? rawNotes,
        bool isActive)
    {
        var companyName = NormalizeRequired(rawCompanyName, 255, "Company name");
        var phoneNumber = NormalizeOptional(rawPhoneNumber, 32);
        var email = NormalizeOptional(rawEmail, 255);
        if (email is not null && !new EmailAddressAttribute().IsValid(email))
            throw new SupplierDirectoryException(400, "Supplier email is invalid.");
        if (paymentTermsDays is < 0 or > 3650)
            throw new SupplierDirectoryException(400, "Payment terms must be between 0 and 3650 days.");
        return new NormalizedSupplierInput(
            companyName,
            phoneNumber,
            email,
            NormalizeOptional(rawTaxIdNumber, 64)?.ToUpperInvariant(),
            NormalizeOptional(rawCommercialRegistration, 64)?.ToUpperInvariant(),
            NormalizeOptional(rawContactPersonName, 255),
            NormalizeOptional(rawContactPersonPhone, 32),
            paymentTermsDays,
            NormalizeOptional(rawNotes, 2000),
            isActive);
    }

    private static string NormalizeRequired(string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new SupplierDirectoryException(400, $"{label} is required.");
        var normalized = value.Trim().Normalize(NormalizationForm.FormC);
        if (normalized.Length > maxLength)
            throw new SupplierDirectoryException(400, $"{label} is too long.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim().Normalize(NormalizationForm.FormC);
        if (normalized.Length > maxLength)
            throw new SupplierDirectoryException(400, "A supplier field is too long.");
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
            // Preserve the original supplier-directory failure.
        }
    }

    private sealed record NormalizedSupplierInput(
        string CompanyName,
        string? PhoneNumber,
        string? Email,
        string? TaxIdNumber,
        string? CommercialRegistration,
        string? ContactPersonName,
        string? ContactPersonPhone,
        int PaymentTermsDays,
        string? Notes,
        bool IsActive);
}
