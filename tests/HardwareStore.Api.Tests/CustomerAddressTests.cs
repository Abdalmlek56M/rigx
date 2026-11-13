using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class CustomerAddressPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Creates_defaults_updates_and_deletes_owned_addresses()
    {
        var database = await fixture.CreateDatabaseAsync();
        var accountId = await CreateAccountAsync(database, "address-a");
        var otherAccountId = await CreateAccountAsync(database, "address-b");
        var service = database.CreateAddressService();

        var first = await service.CreateAsync(accountId, CreateRequest("Home", true));
        var second = await service.CreateAsync(accountId, CreateRequest("Work", false));
        var list = await service.ListAsync(accountId);
        Assert.Equal(2, list.Count);
        Assert.True(list[0].IsDefault);
        Assert.Equal(first.Id, list[0].Id);

        var promoted = await service.UpdateAsync(
            accountId,
            second.Id,
            UpdateRequest("Work", true, second.RowVersion));
        Assert.True(promoted.IsDefault);
        list = await service.ListAsync(accountId);
        Assert.Single(list, address => address.IsDefault);
        Assert.Equal(second.Id, list.Single(address => address.IsDefault).Id);

        var stale = await Assert.ThrowsAsync<IdentityException>(() =>
            service.UpdateAsync(
                accountId,
                second.Id,
                UpdateRequest("Stale", true, second.RowVersion)));
        Assert.Equal(409, stale.StatusCode);

        var foreign = await Assert.ThrowsAsync<IdentityException>(() =>
            service.UpdateAsync(
                otherAccountId,
                first.Id,
                UpdateRequest("Foreign", false, first.RowVersion)));
        Assert.Equal(404, foreign.StatusCode);

        await service.DeleteAsync(accountId, first.Id,
            (await service.ListAsync(accountId)).Single(address => address.Id == first.Id).RowVersion);
        await service.DeleteAsync(accountId, second.Id, promoted.RowVersion);
        Assert.Empty(await service.ListAsync(accountId));

        await using var audit = database.CreateAuditContext();
        var events = await audit.SecurityAuditEvents
            .Where(entry => entry.EventType.StartsWith("Identity.Address"))
            .Select(entry => entry.EventType)
            .ToListAsync();
        Assert.Contains("Identity.AddressCreated", events);
        Assert.Contains("Identity.AddressUpdated", events);
        Assert.Contains("Identity.AddressDeleted", events);
    }

    [Fact]
    public async Task Rejects_invalid_address_fields()
    {
        var database = await fixture.CreateDatabaseAsync();
        var accountId = await CreateAccountAsync(database, "address-invalid");
        var service = database.CreateAddressService();

        var invalid = await Assert.ThrowsAsync<IdentityException>(() =>
            service.CreateAsync(accountId, CreateRequest("Home", false) with { CountryCallingCode = "20" }));
        Assert.Equal(400, invalid.StatusCode);

        var missing = await Assert.ThrowsAsync<IdentityException>(() =>
            service.CreateAsync(accountId, CreateRequest("Home", false) with { Street = " " }));
        Assert.Equal(400, missing.StatusCode);
    }

    private static CustomerAddressCreateRequest CreateRequest(string label, bool isDefault) =>
        new(
            label,
            "Recipient",
            "1000000000",
            "EG",
            "+20",
            "Cairo",
            "Cairo",
            "Nasr City",
            "Street 1",
            "12",
            "3",
            "5",
            "Near the mall",
            "11511",
            "Call before arrival",
            isDefault);

    private static CustomerAddressUpdateRequest UpdateRequest(string label, bool isDefault, long rowVersion) =>
        new(
            label,
            "Recipient",
            "1000000000",
            "EG",
            "+20",
            "Cairo",
            "Cairo",
            "Nasr City",
            "Street 1",
            "12",
            "3",
            "5",
            "Near the mall",
            "11511",
            "Call before arrival",
            isDefault,
            rowVersion);

    private static async Task<Guid> CreateAccountAsync(OwnerBootstrapDatabase database, string prefix)
    {
        await using var db = database.CreateIdentityContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var person = new Person
        {
            Id = Guid.CreateVersion7(),
            FullName = prefix,
            Email = $"{prefix}-{Guid.NewGuid():N}@example.test",
            IsCustomer = true,
            IsActive = true
        };
        await new PersonCodeGenerator(db, TimeProvider.System, TimeZoneInfo.Utc)
            .CreatePersonInCurrentTransactionAsync(person);
        var username = $"{prefix}-{Guid.NewGuid():N}";
        var account = new UserAccount
        {
            Id = Guid.CreateVersion7(),
            PersonId = person.Id,
            Person = person,
            Username = username,
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
            Email = person.Email,
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier(person.Email),
            IsEmailVerified = true,
            IsActive = true,
            PasswordHash = "not-used",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.UserAccounts.Add(account);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return account.Id;
    }
}
