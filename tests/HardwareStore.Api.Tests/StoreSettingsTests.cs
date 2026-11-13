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
    public async Task Fresh_database_reads_the_default_sections()
    {
        var service = (await fixture.CreateDatabaseAsync()).CreateStoreSettingsService();

        var settings = await service.GetAdminAsync();

        Assert.Equal(1L, settings.RowVersion);
        Assert.Equal("RIGX Hardware", settings.StoreInfo.Name);
        Assert.Equal(10_000m, settings.Payment.DepositThreshold);
        Assert.Equal(10m, settings.Payment.DepositPercent);
        Assert.False(settings.Reviews.Enabled);
        Assert.Equal(9, settings.LeadSources.Sources.Count);
        Assert.Equal(4, settings.WarrantyOptions.Options.Count);
        Assert.Equal(HomepageSectionKey.All, settings.Controls.HomepageSections.Select(section => section.Key));
    }

    [Fact]
    public async Task Updating_a_section_persists_it_bumps_the_version_and_audits()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateStoreSettingsService();
        var actorId = await CreateActorAsync(database);
        var initial = await service.GetAdminAsync();

        var shipped = await service.UpdateSectionAsync(
            new ShippingSettings
            {
                DefaultFee = 120m,
                Groups = [new() { Name = "القاهرة الكبرى", Fee = 80m, Governorates = ["c", "GZ"] }]
            },
            initial.RowVersion,
            actorId);
        var contacts = await service.UpdateSectionAsync(
            new ContactLinkSettings { Links = [new() { Platform = ContactPlatform.WhatsApp, Value = "+20 100 000 0000" }] },
            shipped.RowVersion,
            actorId);

        Assert.Equal(initial.RowVersion + 2, contacts.RowVersion);
        var reloaded = await service.GetAdminAsync();
        Assert.Equal(contacts.RowVersion, reloaded.RowVersion);
        Assert.Equal(["C", "GZ"], reloaded.Shipping.Groups[0].Governorates);
        Assert.Equal(120m, reloaded.Shipping.DefaultFee);
        Assert.Equal("201000000000", Assert.Single(reloaded.ContactLinks.Links).Value);
        Assert.Equal(initial.Payment, reloaded.Payment);
        Assert.Equal(initial.StoreInfo, reloaded.StoreInfo);

        await using var audit = database.CreateAuditContext();
        var events = await audit.SecurityAuditEvents.Select(entry => entry.EventType).ToListAsync();
        Assert.Equal(2, events.Count(type => type == "Storefront.SettingsUpdated"));
    }

    [Fact]
    public async Task Public_view_lists_only_links_shown_on_the_site()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateStoreSettingsService();
        var actorId = await CreateActorAsync(database);
        var initial = await service.GetAdminAsync();
        await service.UpdateSectionAsync(
            new ContactLinkSettings
            {
                Links =
                [
                    new() { Platform = ContactPlatform.WhatsApp, Value = "201000000000" },
                    new() { Platform = ContactPlatform.Facebook, Value = "https://facebook.com/rigx", ShowOnSite = false }
                ]
            },
            initial.RowVersion,
            actorId);

        var view = await service.GetPublicAsync();

        Assert.Equal(ContactPlatform.WhatsApp, Assert.Single(view.ContactLinks).Platform);
    }

    [Fact]
    public async Task Checkout_quote_applies_shipping_group_default_pickup_and_deposit()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateStoreSettingsService();
        var actorId = await CreateActorAsync(database);
        var initial = await service.GetAdminAsync();
        await service.UpdateSectionAsync(
            new ShippingSettings
            {
                DefaultFee = 120m,
                Groups = [new() { Name = "القاهرة الكبرى", Fee = 80m, Governorates = ["C", "GZ"] }]
            },
            initial.RowVersion,
            actorId);

        var grouped = await service.QuoteCheckoutAsync("gz", 5_000m, false);
        Assert.Equal(80m, grouped.ShippingFee);
        Assert.Equal(80m, grouped.UpfrontTotal);
        Assert.Equal(0m, grouped.Deposit);

        var other = await service.QuoteCheckoutAsync("ASN", 20_000m, false);
        Assert.Equal(120m, other.ShippingFee);
        Assert.Equal(2_000m, other.Deposit);
        Assert.Equal(2_120m, other.UpfrontTotal);

        var pickup = await service.QuoteCheckoutAsync(null, 20_000m, true);
        Assert.Equal(0m, pickup.ShippingFee);
        Assert.Equal(0m, pickup.UpfrontTotal);

        var unknown = await Assert.ThrowsAsync<StorefrontException>(() => service.QuoteCheckoutAsync("XX", 100m, false));
        Assert.Equal(400, unknown.StatusCode);
    }

    [Fact]
    public async Task Invalid_value_is_rejected_and_nothing_is_saved()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateStoreSettingsService();
        var actorId = await CreateActorAsync(database);
        var initial = await service.GetAdminAsync();

        var invalid = await Assert.ThrowsAsync<StorefrontException>(() =>
            service.UpdateSectionAsync(new InvoiceSettings { NumberPrefix = "IN1" }, initial.RowVersion, actorId));

        Assert.Equal(400, invalid.StatusCode);
        Assert.Equal(initial.RowVersion, (await service.GetAdminAsync()).RowVersion);
    }

    [Fact]
    public async Task Stale_and_concurrent_writes_return_conflict()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateStoreSettingsService();
        var actorId = await CreateActorAsync(database);
        var initial = await service.GetAdminAsync();

        var first = await service.UpdateSectionAsync(new ReviewSettings { Enabled = true }, initial.RowVersion, actorId);
        var stale = await Assert.ThrowsAsync<StorefrontException>(() =>
            service.UpdateSectionAsync(new ReviewSettings(), initial.RowVersion, actorId));
        Assert.Equal(409, stale.StatusCode);

        var attempts = await Task.WhenAll(
            CaptureAsync(service, first.RowVersion, actorId, 5),
            CaptureAsync(service, first.RowVersion, actorId, 6));
        Assert.Equal(1, attempts.Count(attempt => attempt is null));
        Assert.Equal(1, attempts.Count(attempt => attempt?.StatusCode == 409));
    }

    private static async Task<StorefrontException?> CaptureAsync(
        StoreSettingsService service,
        long rowVersion,
        Guid actorId,
        int minimumCount)
    {
        try
        {
            await service.UpdateSectionAsync(
                new ReviewSettings { Enabled = true, MinimumCountToShowStars = minimumCount }, rowVersion, actorId);
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
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        db.UserAccounts.Add(account);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return account.Id;
    }
}
