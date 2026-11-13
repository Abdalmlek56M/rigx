using System.Text.Json;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HardwareStore.Modules.Identity;

public sealed record CustomerAddressView(
    Guid Id,
    string Label,
    string RecipientName,
    string PhoneNumber,
    string CountryCode,
    string CountryCallingCode,
    string Governorate,
    string City,
    string? District,
    string Street,
    string? Building,
    string? Floor,
    string? Apartment,
    string? Landmark,
    string? PostalCode,
    string? Instructions,
    bool IsDefault,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long RowVersion);

public sealed record CustomerAddressCreateRequest(
    string? Label,
    string? RecipientName,
    string? PhoneNumber,
    string? CountryCode,
    string? CountryCallingCode,
    string? Governorate,
    string? City,
    string? District,
    string? Street,
    string? Building,
    string? Floor,
    string? Apartment,
    string? Landmark,
    string? PostalCode,
    string? Instructions,
    bool IsDefault);

public sealed record CustomerAddressUpdateRequest(
    string? Label,
    string? RecipientName,
    string? PhoneNumber,
    string? CountryCode,
    string? CountryCallingCode,
    string? Governorate,
    string? City,
    string? District,
    string? Street,
    string? Building,
    string? Floor,
    string? Apartment,
    string? Landmark,
    string? PostalCode,
    string? Instructions,
    bool IsDefault,
    long RowVersion);

public sealed class CustomerAddressService(
    IDbContextFactory<IdentityDbContext> contextFactory,
    ISecurityAuditTransactionWriter auditWriter,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<CustomerAddressView>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var addresses = await db.CustomerAddresses.AsNoTracking()
            .Where(address => address.UserAccountId == accountId && !address.IsDeleted)
            .OrderByDescending(address => address.IsDefault)
            .ThenByDescending(address => address.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
        return addresses.Select(Map).ToList();
    }

    public async Task<CustomerAddressView> CreateAsync(
        Guid accountId,
        CustomerAddressCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var input = Normalize(request);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await LockAccountAsync(db, accountId, cancellationToken);
            if (input.IsDefault)
                await ClearDefaultAsync(db, accountId, null, cancellationToken);

            var now = timeProvider.GetUtcNow().ToUniversalTime();
            var address = new CustomerAddress
            {
                Id = Guid.CreateVersion7(),
                UserAccountId = accountId,
                Label = input.Label,
                RecipientName = input.RecipientName,
                PhoneNumber = input.PhoneNumber,
                CountryCode = input.CountryCode,
                CountryCallingCode = input.CountryCallingCode,
                Governorate = input.Governorate,
                City = input.City,
                District = input.District,
                Street = input.Street,
                Building = input.Building,
                Floor = input.Floor,
                Apartment = input.Apartment,
                Landmark = input.Landmark,
                PostalCode = input.PostalCode,
                Instructions = input.Instructions,
                IsDefault = input.IsDefault,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                RowVersion = 1
            };
            db.CustomerAddresses.Add(address);
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, accountId, "Identity.AddressCreated", address, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(address);
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task<CustomerAddressView> UpdateAsync(
        Guid accountId,
        Guid addressId,
        CustomerAddressUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RowVersion <= 0)
            throw new IdentityAdministrationException(400, "A positive address row version is required.");
        var input = Normalize(request);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await LockAccountAsync(db, accountId, cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                "SELECT 1 FROM customer_addresses WHERE id = {0} AND user_account_id = {1} FOR UPDATE",
                new object[] { addressId, accountId },
                cancellationToken);
            var address = await db.CustomerAddresses.SingleOrDefaultAsync(
                item => item.Id == addressId && item.UserAccountId == accountId && !item.IsDeleted,
                cancellationToken) ?? throw new IdentityAdministrationException(404, "Address was not found.");
            if (address.RowVersion != request.RowVersion)
                throw new IdentityAdministrationException(409, "The address was changed by another user.");
            if (input.IsDefault && !address.IsDefault)
                await ClearDefaultAsync(db, accountId, address.Id, cancellationToken);

            address.Label = input.Label;
            address.RecipientName = input.RecipientName;
            address.PhoneNumber = input.PhoneNumber;
            address.CountryCode = input.CountryCode;
            address.CountryCallingCode = input.CountryCallingCode;
            address.Governorate = input.Governorate;
            address.City = input.City;
            address.District = input.District;
            address.Street = input.Street;
            address.Building = input.Building;
            address.Floor = input.Floor;
            address.Apartment = input.Apartment;
            address.Landmark = input.Landmark;
            address.PostalCode = input.PostalCode;
            address.Instructions = input.Instructions;
            address.IsDefault = input.IsDefault;
            address.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
            address.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, accountId, "Identity.AddressUpdated", address, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Map(address);
        }
        catch (IdentityAdministrationException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw new IdentityAdministrationException(409, "The address was changed by another user.");
        }
        catch
        {
            await SafeRollbackAsync(transaction, cancellationToken);
            throw;
        }
    }

    public async Task DeleteAsync(
        Guid accountId,
        Guid addressId,
        long rowVersion,
        CancellationToken cancellationToken = default)
    {
        if (rowVersion <= 0)
            throw new IdentityAdministrationException(400, "A positive address row version is required.");
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await LockAccountAsync(db, accountId, cancellationToken);
            var address = await db.CustomerAddresses.SingleOrDefaultAsync(
                item => item.Id == addressId && item.UserAccountId == accountId && !item.IsDeleted,
                cancellationToken) ?? throw new IdentityAdministrationException(404, "Address was not found.");
            if (address.RowVersion != rowVersion)
                throw new IdentityAdministrationException(409, "The address was changed by another user.");
            address.IsDeleted = true;
            address.IsDefault = false;
            address.UpdatedAtUtc = timeProvider.GetUtcNow().ToUniversalTime();
            address.RowVersion++;
            await db.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(db, transaction, accountId, "Identity.AddressDeleted", address, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (IdentityAdministrationException)
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

    private static async Task LockAccountAsync(
        IdentityDbContext db,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var exists = await db.UserAccounts.AnyAsync(
            account => account.Id == accountId && account.IsActive, cancellationToken);
        if (!exists)
            throw new IdentityAdministrationException(401, "The account is not active or does not exist.");
        await db.Database.ExecuteSqlRawAsync(
            "SELECT 1 FROM user_accounts WHERE id = {0} FOR UPDATE",
            new object[] { accountId },
            cancellationToken);
    }

    private static Task ClearDefaultAsync(
        IdentityDbContext db,
        Guid accountId,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var query = db.CustomerAddresses
            .Where(address => address.UserAccountId == accountId && address.IsDefault);
        if (exceptId.HasValue)
            query = query.Where(address => address.Id != exceptId.Value);
        return query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(address => address.IsDefault, false)
                .SetProperty(address => address.RowVersion, address => address.RowVersion + 1),
            cancellationToken);
    }

    private async Task WriteAuditAsync(
        IdentityDbContext db,
        IDbContextTransaction transaction,
        Guid accountId,
        string eventType,
        CustomerAddress address,
        CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new SecurityAuditEntry(
                eventType,
                "Succeeded",
                "User",
                ActorUserAccountId: accountId,
                SubjectUserAccountId: accountId,
                SafeMetadata: JsonSerializer.Serialize(new
                {
                    addressId = address.Id,
                    label = address.Label,
                    isDefault = address.IsDefault
                })),
            db.Database.GetDbConnection(),
            transaction.GetDbTransaction(),
            cancellationToken);
    }

    private static CustomerAddressView Map(CustomerAddress address) => new(
        address.Id,
        address.Label,
        address.RecipientName,
        address.PhoneNumber,
        address.CountryCode,
        address.CountryCallingCode,
        address.Governorate,
        address.City,
        address.District,
        address.Street,
        address.Building,
        address.Floor,
        address.Apartment,
        address.Landmark,
        address.PostalCode,
        address.Instructions,
        address.IsDefault,
        address.CreatedAtUtc,
        address.UpdatedAtUtc,
        address.RowVersion);

    private static NormalizedAddress Normalize(CustomerAddressCreateRequest request) =>
        Normalize(request.Label, request.RecipientName, request.PhoneNumber, request.CountryCode,
            request.CountryCallingCode, request.Governorate, request.City, request.District,
            request.Street, request.Building, request.Floor, request.Apartment, request.Landmark,
            request.PostalCode, request.Instructions, request.IsDefault);

    private static NormalizedAddress Normalize(CustomerAddressUpdateRequest request) =>
        Normalize(request.Label, request.RecipientName, request.PhoneNumber, request.CountryCode,
            request.CountryCallingCode, request.Governorate, request.City, request.District,
            request.Street, request.Building, request.Floor, request.Apartment, request.Landmark,
            request.PostalCode, request.Instructions, request.IsDefault);

    private static NormalizedAddress Normalize(
        string? label,
        string? recipientName,
        string? phoneNumber,
        string? countryCode,
        string? countryCallingCode,
        string? governorate,
        string? city,
        string? district,
        string? street,
        string? building,
        string? floor,
        string? apartment,
        string? landmark,
        string? postalCode,
        string? instructions,
        bool isDefault)
    {
        var country = string.IsNullOrWhiteSpace(countryCode) ? "EG" : countryCode.Trim().ToUpperInvariant();
        if (country.Length != 2)
            throw new IdentityAdministrationException(400, "Country code must contain two letters.");
        var calling = string.IsNullOrWhiteSpace(countryCallingCode) ? "+20" : countryCallingCode.Trim();
        if (!calling.StartsWith('+') || calling.Length > 8 || calling[1..].Any(character => !char.IsAsciiDigit(character)))
            throw new IdentityAdministrationException(400, "Country calling code is invalid.");
        return new NormalizedAddress(
            Required(label, 32, "Label"),
            Required(recipientName, 255, "Recipient name"),
            Required(phoneNumber, 32, "Phone number"),
            country,
            calling,
            Required(governorate, 128, "Governorate"),
            Required(city, 128, "City"),
            Optional(district, 128),
            Required(street, 255, "Street"),
            Optional(building, 64),
            Optional(floor, 32),
            Optional(apartment, 32),
            Optional(landmark, 255),
            Optional(postalCode, 32),
            Optional(instructions, 1000),
            isDefault);
    }

    private static string Required(string? value, int max, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new IdentityAdministrationException(400, $"{label} is required.");
        var normalized = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (normalized.Length > max)
            throw new IdentityAdministrationException(400, $"{label} is too long.");
        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim().Normalize(System.Text.NormalizationForm.FormC);
        if (normalized.Length > max)
            throw new IdentityAdministrationException(400, "Address field is too long.");
        return normalized;
    }

    private static async Task SafeRollbackAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
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

    private sealed record NormalizedAddress(
        string Label,
        string RecipientName,
        string PhoneNumber,
        string CountryCode,
        string CountryCallingCode,
        string Governorate,
        string City,
        string? District,
        string Street,
        string? Building,
        string? Floor,
        string? Apartment,
        string? Landmark,
        string? PostalCode,
        string? Instructions,
        bool IsDefault);
}
