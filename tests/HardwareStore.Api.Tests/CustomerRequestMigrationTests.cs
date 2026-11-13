using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>M30 turns old contact messages into Contact requests on a database that already has rows.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class CustomerRequestMigrationTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string BeforeRequests = "20261113150233_M25_SalesSettings";

    [Fact]
    public async Task Old_contact_messages_become_numbered_contact_requests_and_the_old_table_is_dropped()
    {
        var name = $"requests_migration_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(fixture.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{name}\"";
            await create.ExecuteNonQueryAsync();
        }
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = name }.ConnectionString;
        await using var storefront = new StorefrontDbContext(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);
        await storefront.GetService<IMigrator>().MigrateAsync(BeforeRequests);
        var (older, read, closed) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await storefront.Database.ExecuteSqlAsync($"""
            INSERT INTO contact_messages (id, full_name, email, phone, subject, message, status, created_at_utc, updated_at_utc, row_version)
            VALUES ({closed}, 'Third', 'c@x.test', NULL, 'S3', 'Message three', 'Closed', '2026-11-03T10:00:00Z', '2026-11-04T10:00:00Z', 1),
                   ({older}, 'First', 'a@x.test', '+20 101 234 5678', 'S1', 'Message one', 'New', '2026-11-01T10:00:00Z', '2026-11-01T10:00:00Z', 1),
                   ({read}, 'Second', 'b@x.test', '010-0000-1111', 'S2', 'Message two', 'Read', '2026-11-02T10:00:00Z', '2026-11-02T11:00:00Z', 1);
            """);

        await storefront.Database.MigrateAsync();

        var requests = await storefront.CustomerRequests.AsNoTracking().OrderBy(request => request.Code).ToListAsync();
        Assert.Equal(["REQ-0001", "REQ-0002", "REQ-0003"], requests.Select(request => request.Code));
        Assert.Equal([older, read, closed], requests.Select(request => request.Id));
        Assert.All(requests, request => Assert.Equal(CustomerRequestType.Contact, request.Type));
        Assert.Equal([CustomerRequestStatus.New, CustomerRequestStatus.Following, CustomerRequestStatus.Closed], requests.Select(request => request.Status));
        Assert.Equal(["01012345678", "01000001111", string.Empty], requests.Select(request => request.Phone));
        Assert.Equal(("S1", "Message one", "a@x.test"), (requests[0].Subject, requests[0].Message, requests[0].Email));
        Assert.Equal([(DateTimeOffset?)null, null, requests[2].UpdatedAtUtc], requests.Select(request => request.ClosedAtUtc));
        var next = await storefront.Database.SqlQueryRaw<long>("SELECT nextval('customer_request_code_seq') AS \"Value\"").SingleAsync();
        Assert.Equal(4, next);
        var oldTable = await storefront.Database.SqlQueryRaw<string?>("SELECT to_regclass('contact_messages')::text AS \"Value\"").SingleAsync();
        Assert.Null(oldTable);
    }
}
