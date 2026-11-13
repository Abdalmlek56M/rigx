using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Purchasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>M19 turns old suppliers and drafts into sellers and purchases on a database that already has rows.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class PurchaseIntakeMigrationTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string BeforeIntake = "20261109200329_M8_PurchaseReceiptStatus";

    [Fact]
    public async Task Old_suppliers_and_drafts_become_sellers_and_purchases_with_the_same_ids()
    {
        var name = $"intake_migration_{Guid.NewGuid():N}";
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
        await using var purchasing = new PurchasingDbContext(new DbContextOptionsBuilder<PurchasingDbContext>().UseNpgsql(connectionString).Options);
        await purchasing.GetService<IMigrator>().MigrateAsync(BeforeIntake);
        var (supplierDraft, externalDraft, itemId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await purchasing.Database.ExecuteSqlAsync($"""
            INSERT INTO persons (id, person_code, full_name, phone_number, created_at_utc)
            VALUES ('00000000-0000-0000-0000-0000000000a1', 'PER-202611-00001', 'Supplier Person', '+20 100 111 2222', now());
            INSERT INTO supplier_profiles (id, person_id, company_name, created_at_utc, updated_at_utc)
            VALUES ('00000000-0000-0000-0000-0000000000b1', '00000000-0000-0000-0000-0000000000a1', 'Old Supplier Co', now(), now());
            INSERT INTO purchase_drafts (id, source_type, supplier_id, status, total_amount, created_by_user_account_id, created_at_utc, updated_at_utc)
            VALUES ({supplierDraft}, 'RegisteredSupplier', '00000000-0000-0000-0000-0000000000b1', 'Draft', 500, gen_random_uuid(), now() - interval '1 day', now());
            INSERT INTO purchase_drafts (id, source_type, external_seller_name, status, total_amount, created_by_user_account_id, created_at_utc, updated_at_utc)
            VALUES ({externalDraft}, 'IndividualSeller', 'Walk-in Seller', 'Received', 900, gen_random_uuid(), now(), now());
            INSERT INTO purchase_draft_items (id, draft_id, product_id, item_name, sku, quantity, unit_price, line_total)
            VALUES ({itemId}, {externalDraft}, gen_random_uuid(), 'RTX  3060', 'SKU-1', 2, 450, 900);
            """);

        await purchasing.Database.MigrateAsync();

        var sellers = await purchasing.Sellers.AsNoTracking().OrderBy(seller => seller.Name).ToListAsync();
        Assert.Equal(["Old Supplier Co", "Walk-in Seller"], sellers.Select(seller => seller.Name));
        Assert.Equal("01001112222", sellers[0].Phone);
        Assert.Equal(SellerKind.Company, sellers[0].Kind);
        Assert.StartsWith("000", sellers[1].Phone);
        Assert.Equal(SellerKind.Person, sellers[1].Kind);
        var purchases = await purchasing.Purchases.AsNoTracking().Include(purchase => purchase.Lines).OrderBy(purchase => purchase.Code).ToListAsync();
        Assert.Equal(["PO-0001", "PO-0002"], purchases.Select(purchase => purchase.Code));
        Assert.Equal([supplierDraft, externalDraft], purchases.Select(purchase => purchase.Id));
        Assert.Equal(PurchaseStatus.Received, purchases[1].Status);
        Assert.Equal(900m, purchases[1].Remaining);
        var line = Assert.Single(purchases[1].Lines);
        Assert.Equal(itemId, line.Id);
        Assert.Equal("RTX 3060", line.ModelName);
        Assert.Equal("Used", line.Condition);
        var next = await purchasing.Database.SqlQueryRaw<long>("SELECT nextval('purchase_code_seq') AS \"Value\"").SingleAsync();
        Assert.Equal(3, next);
    }
}
