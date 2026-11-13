using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class ContactPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Contact_submission_is_bounded_deduplicated_and_admin_status_uses_row_version()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = new ContactService(new StorefrontFactory(database.ConnectionString), TimeProvider.System);
        await service.SubmitAsync("Test Customer", "contact@example.test", "+201000000000",
            "Build enquiry", "I would like to ask about a custom gaming build.", null);
        await service.SubmitAsync("Test Customer", "contact@example.test", "+201000000000",
            "Build enquiry", "I would like to ask about a custom gaming build.", null);
        var page = await service.ListAsync(null, 1, 50);
        var message = Assert.Single(page.Items);
        Assert.Equal("New", message.Status);
        Assert.Equal("contact@example.test", message.Email);

        await service.SubmitAsync("Bot", "bot@example.test", null, "Spam", "This should be ignored by honeypot", "https://spam.example");
        Assert.Single((await service.ListAsync(null, 1, 50)).Items);

        await Assert.ThrowsAsync<StorefrontException>(() => service.SubmitAsync(
            "X", "bad-email", null, "Hi", "short", null));
        await Assert.ThrowsAsync<StorefrontException>(() => service.SubmitAsync(
            "Valid Name", "valid@example.test", null, "Topic", new string('x', 4001), null));

        var stale = await Assert.ThrowsAsync<StorefrontException>(() =>
            service.SetStatusAsync(message.Id, "Read", message.RowVersion - 1));
        Assert.Equal(409, stale.StatusCode);
        var updated = await service.SetStatusAsync(message.Id, "Read", message.RowVersion);
        Assert.Equal("Read", updated.Status);
        Assert.Equal(message.RowVersion + 1, updated.RowVersion);
    }

    private sealed class StorefrontFactory(string connectionString) : IDbContextFactory<StorefrontDbContext>
    {
        public StorefrontDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);
    }
}
