using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class StoreSettingsPostgreSqlTests(SupplierDirectoryPostgreSqlFixture fixture)
    : IClassFixture<SupplierDirectoryPostgreSqlFixture>
{
    [Fact]
    public async Task Reads_defaults_and_updates_public_settings_with_audit()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateStoreSettingsService();
        var actorId = await CreateActorAsync(database);

        var initial = await service.GetAsync();
        Assert.Null(initial.WhatsAppNumber);
        Assert.Null(initial.FacebookUrl);
        Assert.Equal(1L, initial.RowVersion);

        var updated = await service.UpdateAsync(
            new StoreSettingsUpdateRequest(
                "+20 100 000 0000",
                "Hello RIGX",
                "https://facebook.com/rigx",
                "https://t.me/rigx",
                null,
                null,
                "https://instagram.com/rigx",
                "Cairo",
                "10:00 - 20:00",
                "Bring your order number",
                "Warranty details are confirmed per product.",
                "Support is available during store hours.",
                "Free consultation is available on request.",
                initial.RowVersion),
            actorId);

        Assert.Equal("201000000000", updated.WhatsAppNumber);
        Assert.Equal("https://facebook.com/rigx", updated.FacebookUrl);
        Assert.Null(updated.TikTokUrl);
        Assert.True(updated.RowVersion > initial.RowVersion);

        await using var audit = database.CreateAuditContext();
        Assert.Contains(
            "Storefront.SettingsUpdated",
            await audit.SecurityAuditEvents.Select(entry => entry.EventType).ToListAsync());
    }

    [Fact]
    public async Task Rejects_invalid_values_and_stale_or_concurrent_writes()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateStoreSettingsService();
        var actorId = await CreateActorAsync(database);
        var initial = await service.GetAsync();

        var invalidUrl = await Assert.ThrowsAsync<StorefrontException>(() =>
            service.UpdateAsync(
                new StoreSettingsUpdateRequest(null, null, "not-a-url", null, null, null, null, null, null, null, null, null, null, initial.RowVersion),
                actorId));
        Assert.Equal(400, invalidUrl.StatusCode);

        var invalidWhatsApp = await Assert.ThrowsAsync<StorefrontException>(() =>
            service.UpdateAsync(
                new StoreSettingsUpdateRequest("123", null, null, null, null, null, null, null, null, null, null, null, null, initial.RowVersion),
                actorId));
        Assert.Equal(400, invalidWhatsApp.StatusCode);

        var first = await service.UpdateAsync(
            new StoreSettingsUpdateRequest("201000000000", null, null, null, null, null, null, "Cairo", null, null, null, null, null, initial.RowVersion),
            actorId);
        var stale = await Assert.ThrowsAsync<StorefrontException>(() =>
            service.UpdateAsync(
                new StoreSettingsUpdateRequest("201000000001", null, null, null, null, null, null, "Giza", null, null, null, null, null, initial.RowVersion),
                actorId));
        Assert.Equal(409, stale.StatusCode);

        var request = new StoreSettingsUpdateRequest(
            "201000000002", null, null, null, null, null, null, "Alexandria", null, null, null, null, null, first.RowVersion);
        var attempts = await Task.WhenAll(
            CaptureAsync(service, request, actorId),
            CaptureAsync(service, request, actorId));
        Assert.Equal(1, attempts.Count(attempt => attempt is null));
        Assert.Equal(1, attempts.Count(attempt => attempt?.StatusCode == 409));
    }

    private static async Task<StorefrontException?> CaptureAsync(
        StoreSettingsService service,
        StoreSettingsUpdateRequest request,
        Guid actorId)
    {
        try
        {
            await service.UpdateAsync(request, actorId);
            return null;
        }
        catch (StorefrontException exception)
        {
            return exception;
        }
    }

    private static async Task<Guid> CreateActorAsync(SupplierDirectoryDatabase database)
    {
        await using var db = database.CreateIdentityContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var person = new Person
        {
            Id = Guid.CreateVersion7(),
            FullName = "Settings Actor",
            Email = "settings-actor@example.test",
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
            Username = "settings-actor",
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier("settings-actor"),
            Email = "settings-actor@example.test",
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier("settings-actor@example.test"),
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
