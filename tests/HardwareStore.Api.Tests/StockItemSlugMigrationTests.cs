using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>M49 gives every existing stock item a slug with the same rule as <see cref="StockItemSlug"/>.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class StockItemSlugMigrationTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string BeforeSlug = "20261114113012_M43_BlacklistTrash";

    [Fact]
    public async Task Existing_items_are_backfilled_with_slugs_oldest_first()
    {
        var name = $"slug_migration_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(fixture.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{name}\"";
            await create.ExecuteNonQueryAsync();
        }
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = name }.ConnectionString;
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);
        await identity.Database.MigrateAsync();
        await using var catalog = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString).Options);
        await catalog.Database.MigrateAsync();
        await using var retail = new RetailDbContext(new DbContextOptionsBuilder<RetailDbContext>().UseNpgsql(connectionString).Options);
        await retail.GetService<IMigrator>().MigrateAsync(BeforeSlug);
        await retail.Database.ExecuteSqlAsync($"""
            INSERT INTO stock_items (id, code, condition, model_name, model_key, price, has_box, is_hidden, created_at_utc, updated_at_utc)
            VALUES ('00000000-0000-0000-0000-0000000000c2', 'RX-0002', 'Used', 'RTX 3060', 'rtx3060', 100, false, false, now(), now()),
                   ('00000000-0000-0000-0000-0000000000c1', 'RX-0001', 'Used', 'RTX 3060', 'rtx3060', 100, false, false, now() - interval '1 day', now()),
                   ('00000000-0000-0000-0000-0000000000c3', 'RX-0003', 'Used', 'كيسة جيمنج', 'كيسةجيمنج', 100, false, false, now(), now());
            """);

        await retail.Database.MigrateAsync();

        var slugs = await retail.Database.SqlQueryRaw<string>("SELECT slug AS \"Value\" FROM stock_items ORDER BY code").ToListAsync();
        Assert.Equal(["rtx-3060", "rtx-3060-2", "item"], slugs);
    }
}
