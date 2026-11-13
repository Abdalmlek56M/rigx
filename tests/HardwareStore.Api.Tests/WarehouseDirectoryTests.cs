using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Inventory;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class WarehouseDirectoryPostgreSqlTests(StoreAdminPostgreSqlFixture fixture)
    : IClassFixture<StoreAdminPostgreSqlFixture>
{
    [Fact]
    public async Task Creates_updates_lists_and_audits_a_warehouse()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateWarehouseService();
        var actorId = await CreateActorAsync(database);

        var created = await service.CreateAsync(
            new WarehouseCreateRequest("main", "Main Warehouse", "Cairo", "+20 100 000 0000", "Warehouse Manager", "Dock 2 - call first", true),
            actorId);
        Assert.Equal("MAIN", created.Code);
        Assert.Equal("Main Warehouse", created.Name);
        Assert.Equal("+20 100 000 0000", created.ContactPhone);
        Assert.Equal("Warehouse Manager", created.ManagerName);
        Assert.Equal("Dock 2 - call first", created.Notes);

        var page = await service.GetAsync("cairo", true, 1, 10);
        Assert.Single(page.Items);
        Assert.Equal(created.Id, page.Items[0].Id);

        var duplicate = await Assert.ThrowsAsync<WarehouseDirectoryException>(() =>
            service.CreateAsync(
                new WarehouseCreateRequest("MAIN", "Duplicate", null, null, null, null, true),
                actorId));
        Assert.Equal(409, duplicate.StatusCode);

        var updated = await service.UpdateAsync(
            created.Id,
            new WarehouseUpdateRequest("MAIN", "Main Warehouse Updated", "Giza", "+20 100 000 0000", "Warehouse Manager", "Updated notes", false, created.RowVersion),
            actorId);
        Assert.Equal("Giza", updated.Address);
        Assert.Equal("Updated notes", updated.Notes);
        Assert.False(updated.IsActive);
        Assert.True(updated.RowVersion > created.RowVersion);

        await using var audit = database.CreateAuditContext();
        var events = await audit.SecurityAuditEvents
            .Where(entry => entry.EventType.StartsWith("Inventory.Warehouse"))
            .Select(entry => entry.EventType)
            .ToListAsync();
        Assert.Contains("Inventory.WarehouseCreated", events);
        Assert.Contains("Inventory.WarehouseDeactivated", events);
    }

    [Fact]
    public async Task Update_requires_row_version_and_rejects_stale_and_concurrent_writes()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateWarehouseService();
        var actorId = await CreateActorAsync(database);
        var created = await service.CreateAsync(
            new WarehouseCreateRequest("WH-1", "Warehouse One", null, null, null, null, true),
            actorId);

        var missing = await Assert.ThrowsAsync<WarehouseDirectoryException>(() =>
            service.UpdateAsync(
                created.Id,
                new WarehouseUpdateRequest("WH-1", "No Version", null, null, null, null, true, 0),
                actorId));
        Assert.Equal(400, missing.StatusCode);

        var first = await service.UpdateAsync(
            created.Id,
            new WarehouseUpdateRequest("WH-1", "First Update", null, null, null, null, true, created.RowVersion),
            actorId);
        var stale = await Assert.ThrowsAsync<WarehouseDirectoryException>(() =>
            service.UpdateAsync(
                created.Id,
                new WarehouseUpdateRequest("WH-1", "Stale Update", null, null, null, null, true, created.RowVersion),
                actorId));
        Assert.Equal(409, stale.StatusCode);
        Assert.Equal("First Update", (await service.GetByIdAsync(created.Id))!.Name);

        var request = new WarehouseUpdateRequest("WH-1", "Concurrent", null, null, null, null, true, first.RowVersion);
        var attempts = await Task.WhenAll(
            CaptureAsync(service, created.Id, request, actorId),
            CaptureAsync(service, created.Id, request, actorId));
        Assert.Equal(1, attempts.Count(attempt => attempt is null));
        Assert.Equal(1, attempts.Count(attempt => attempt?.StatusCode == 409));
    }

    private static async Task<WarehouseDirectoryException?> CaptureAsync(
        WarehouseDirectoryService service,
        Guid id,
        WarehouseUpdateRequest request,
        Guid actorId)
    {
        try
        {
            await service.UpdateAsync(id, request, actorId);
            return null;
        }
        catch (WarehouseDirectoryException exception)
        {
            return exception;
        }
    }

    private static async Task<Guid> CreateActorAsync(StoreAdminDatabase database)
    {
        await using var db = database.CreateIdentityContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var person = new Person
        {
            Id = Guid.CreateVersion7(),
            FullName = "Warehouse Actor",
            Email = "warehouse-actor@example.test",
            IsEmployee = true,
            IsActive = true
        };
        await new PersonCodeGenerator(db, TimeProvider.System, TimeZoneInfo.Utc)
            .CreatePersonInCurrentTransactionAsync(person);
        var account = new UserAccount
        {
            Id = Guid.CreateVersion7(),
            PersonId = person.Id,
            Person = person,
            Username = "warehouse-actor",
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier("warehouse-actor"),
            Email = "warehouse-actor@example.test",
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier("warehouse-actor@example.test"),
            IsEmailVerified = true,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.UserAccounts.Add(account);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return account.Id;
    }
}
