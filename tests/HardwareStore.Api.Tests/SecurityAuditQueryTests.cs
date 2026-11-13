using HardwareStore.Modules.Auditing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class SecurityAuditQueryPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    [Fact]
    public async Task Query_filters_and_returns_only_whitelisted_metadata()
    {
        var database = await fixture.CreateDatabaseAsync();
        var now = DateTimeOffset.UtcNow;
        await using (var audit = database.CreateAuditContext())
        {
            audit.SecurityAuditEvents.AddRange(
                new SecurityAuditEvent
                {
                    Id = Guid.CreateVersion7(),
                    EventType = "Identity.EmailDelivery.Sent",
                    Outcome = "Succeeded",
                    ActorKind = "System",
                    SafeMetadata =
                        "{\"kind\":\"EmailVerification\",\"attempts\":2,\"roles\":[\"Owner\",\"Support\"]," +
                        "\"token\":\"SUPER-SECRET\",\"email\":\"customer@example.test\",\"ip\":\"10.0.0.1\",\"url\":\"http://x\"}"
                },
                new SecurityAuditEvent
                {
                    Id = Guid.CreateVersion7(),
                    EventType = "Identity.Login.Rejected",
                    Outcome = "Rejected",
                    ActorKind = "Anonymous",
                    SafeMetadata = "{\"reason\":\"InvalidCredentials\"}"
                },
                new SecurityAuditEvent
                {
                    Id = Guid.CreateVersion7(),
                    EventType = "Identity.AddressUpdated",
                    Outcome = "Succeeded",
                    ActorKind = "User",
                    SafeMetadata = "{\"addressId\":\"abc\",\"isDefault\":true,\"label\":\"Home\"}"
                });
            await audit.SaveChangesAsync();
        }

        var service = new SecurityAuditQueryService(new AuditFactory(database.ConnectionString));
        var page = await service.QueryAsync(new SecurityAuditQuery(PageSize: 2));
        Assert.Equal(3, page.Total);
        Assert.Equal(2, page.Items.Count);

        var emailEvent = (await service.QueryAsync(new SecurityAuditQuery(EventType: "emaildelivery")))
            .Items.Single();
        Assert.Equal("Identity.EmailDelivery.Sent", emailEvent.EventType);
        Assert.Equal("2", emailEvent.Metadata["attempts"]);
        Assert.Equal("Owner, Support", emailEvent.Metadata["roles"]);
        Assert.Equal("EmailVerification", emailEvent.Metadata["kind"]);
        Assert.False(emailEvent.Metadata.ContainsKey("token"));
        Assert.False(emailEvent.Metadata.ContainsKey("email"));
        Assert.False(emailEvent.Metadata.ContainsKey("ip"));
        Assert.False(emailEvent.Metadata.ContainsKey("url"));

        var rejected = await service.QueryAsync(new SecurityAuditQuery(Outcome: "Rejected"));
        Assert.Single(rejected.Items);
        Assert.Equal("InvalidCredentials", rejected.Items[0].Metadata["reason"]);

        var search = await service.QueryAsync(new SecurityAuditQuery(Search: "address"));
        Assert.Single(search.Items);
        Assert.Equal("true", search.Items[0].Metadata["isDefault"]);

        var range = await service.QueryAsync(new SecurityAuditQuery(
            FromUtc: now.AddMinutes(-5),
            ToUtc: now.AddMinutes(5)));
        Assert.Equal(3, range.Total);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.QueryAsync(new SecurityAuditQuery(
                FromUtc: now,
                ToUtc: now.AddMinutes(-1))));
    }

    private sealed class AuditFactory(string connectionString)
        : Microsoft.EntityFrameworkCore.IDbContextFactory<SecurityAuditDbContext>
    {
        public SecurityAuditDbContext CreateDbContext() =>
            new(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<SecurityAuditDbContext>()
                .UseNpgsql(connectionString).Options);
    }
}
