using HardwareStore.Modules.Retail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>M33 turns name-based partner agreements, allocations and ledger rows into partner records on a database that already has rows.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class PartnerMigrationTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private const string BeforePartnersV2 = "20261113195018_M29_OrderDeliveryTime";

    [Fact]
    public async Task Old_agreements_allocations_and_ledger_rows_become_partners_with_unit_financing()
    {
        var name = $"partners_migration_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(fixture.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{name}\"";
            await create.ExecuteNonQueryAsync();
        }
        var connectionString = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = name }.ConnectionString;
        await using var retail = new RetailDbContext(new DbContextOptionsBuilder<RetailDbContext>().UseNpgsql(connectionString).Options);
        await retail.GetService<IMigrator>().MigrateAsync(BeforePartnersV2);
        var (stockItem, bothUnit, inactiveUnit, overrideUnit, plainUnit) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (earningAli, earningGhost, settlement, adjustment) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await retail.Database.ExecuteSqlAsync($"""
            INSERT INTO partner_agreements (id, partner_name, default_percent, loss_sharing_enabled, is_active, effective_from, created_at_utc, updated_at_utc, row_version)
            VALUES (gen_random_uuid(), 'Ali', 5, false, true, '2025-01-01', '2025-01-01T00:00:00Z', '2025-01-01T00:00:00Z', 1),
                   (gen_random_uuid(), 'Ali', 10, true, true, '2026-01-01', '2026-01-01T00:00:00Z', '2026-01-01T00:00:00Z', 1),
                   (gen_random_uuid(), 'Retired', 7.5, false, false, '2024-06-01', '2024-06-01T00:00:00Z', '2024-06-01T00:00:00Z', 1);
            INSERT INTO stock_items (id, code, condition, model_name, model_key, price, has_box, is_hidden, created_at_utc, updated_at_utc)
            VALUES ({stockItem}, 'RX-0001', 'New', 'Old GPU', 'old gpu', 500, false, false, now(), now());
            INSERT INTO inventory_units (id, stock_item_id, name, condition, acquisition_cost, partner_percent_override, status, received_at_utc, created_at_utc)
            VALUES ({bothUnit}, {stockItem}, 'Old GPU', 'New', 100, NULL, 'Available', now(), now()),
                   ({inactiveUnit}, {stockItem}, 'Old GPU', 'New', 100, NULL, 'Available', now(), now()),
                   ({overrideUnit}, {stockItem}, 'Old GPU', 'New', 100, 12.5, 'Available', now(), now()),
                   ({plainUnit}, {stockItem}, 'Old GPU', 'New', 100, NULL, 'Available', now(), now());
            INSERT INTO partner_unit_allocations (id, partner_name, unit_id, percent, loss_sharing_enabled, is_active, created_at_utc, updated_at_utc, row_version)
            VALUES (gen_random_uuid(), 'Sara', {bothUnit}, 40, false, true, now(), now(), 1),
                   (gen_random_uuid(), 'Ali', {bothUnit}, 60, false, true, now(), now(), 1),
                   (gen_random_uuid(), 'Sara', {inactiveUnit}, 30, false, false, now(), now(), 1);
            INSERT INTO partner_earnings (id, order_item_id, order_item_component_id, unit_id, order_id, partner_name, percent, loss_sharing_enabled,
                revenue_amount, discount_amount, direct_expense_amount, cost_amount, gross_profit, amount, paid_amount, offset_amount, status, created_at_utc)
            VALUES ({earningAli}, gen_random_uuid(), gen_random_uuid(), {bothUnit}, gen_random_uuid(), 'Ali', 10, true, 300, 0, 0, 100, 200, 20, 5, 0, 'Outstanding', now()),
                   ({earningGhost}, gen_random_uuid(), gen_random_uuid(), {plainUnit}, gen_random_uuid(), 'Ghost', 10, false, 300, 0, 0, 100, 200, 20, 0, 0, 'Outstanding', now());
            INSERT INTO partner_settlements (id, partner_name, amount, offset_applied, method, actor_user_account_id, created_at_utc)
            VALUES ({settlement}, 'Ali', 5, 0, 'Cash', gen_random_uuid(), now());
            INSERT INTO partner_adjustments (id, partner_name, amount, applied_amount, status, reason, created_at_utc)
            VALUES ({adjustment}, 'Ali', -3, 0, 'Open', 'Return recovery', now());
            """);

        await retail.Database.MigrateAsync();

        var partners = await retail.Partners.AsNoTracking().OrderBy(partner => partner.Name).ToListAsync();
        Assert.Equal(["Ali", "Ghost", "Retired", "Sara"], partners.Select(partner => partner.Name));
        Assert.All(partners, partner => Assert.Equal(PartnerBasis.FinancedParts, partner.Basis));
        var ali = partners[0];
        Assert.Equal((10m, true, true, new DateOnly(2026, 1, 1)), (ali.DefaultPercent, ali.LossSharingEnabled, ali.IsActive, ali.JoinDate));
        Assert.Equal((7.5m, false), (partners[2].DefaultPercent, partners[2].IsActive));
        Assert.Equal((0m, true), (partners[1].DefaultPercent, partners[1].IsActive));
        Assert.Equal(0m, partners[3].DefaultPercent);

        Assert.Equal(ali.Id, (await retail.PartnerEarnings.AsNoTracking().SingleAsync(item => item.Id == earningAli)).PartnerId);
        var ghost = await retail.PartnerEarnings.AsNoTracking().SingleAsync(item => item.Id == earningGhost);
        Assert.Equal((partners[1].Id, (DateOnly?)null), (ghost.PartnerId, ghost.PeriodMonth));
        Assert.Equal(ali.Id, (await retail.PartnerSettlements.AsNoTracking().SingleAsync(item => item.Id == settlement)).PartnerId);
        Assert.Equal(ali.Id, (await retail.PartnerAdjustments.AsNoTracking().SingleAsync(item => item.Id == adjustment)).PartnerId);

        var units = await retail.Units.AsNoTracking().ToDictionaryAsync(unit => unit.Id);
        Assert.Equal((ali.Id, 60m), (units[bothUnit].PartnerId, units[bothUnit].PartnerPercentOverride));
        Assert.Null(units[inactiveUnit].PartnerId);
        Assert.Equal((ali.Id, 12.5m), (units[overrideUnit].PartnerId, units[overrideUnit].PartnerPercentOverride));
        Assert.Null(units[plainUnit].PartnerId);

        var oldTables = await retail.Database.SqlQueryRaw<int>(
            "SELECT count(*)::int AS \"Value\" FROM information_schema.tables WHERE table_name IN ('partner_agreements','partner_unit_allocations','partner_allocation_events')")
            .SingleAsync();
        Assert.Equal(0, oldTables);
    }
}
