using HardwareStore.Common.Domain;
using HardwareStore.Common.Infrastructure.Audit;
using HardwareStore.Modules.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>The audit interceptor on a real database (roadmap 2.11): same transaction as the save, joins the caller's transaction, append-only table.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class AuditInterceptorTests(StoreAdminPostgreSqlFixture fixture) : IClassFixture<StoreAdminPostgreSqlFixture>
{
    private static IDbContextFactory<InventoryDbContext> Factory(StoreAdminDatabase database) =>
        new ServiceCollection().AddAuditedDbContextFactory<InventoryDbContext>(database.ConnectionString)
            .BuildServiceProvider().GetRequiredService<IDbContextFactory<InventoryDbContext>>();

    private static async Task<List<Modules.Auditing.AuditEntry>> EntriesAsync(StoreAdminDatabase database)
    {
        await using var audit = database.CreateAuditContext();
        return await audit.AuditEntries.AsNoTracking().ToListAsync();
    }

    private static Warehouse NewWarehouse(string code, string name) => new() { Id = Guid.NewGuid(), Code = code, Name = name };

    [Fact]
    public async Task A_failed_save_leaves_no_audit_row_and_a_job_write_is_logged_as_the_system()
    {
        var database = await fixture.CreateDatabaseAsync();
        var factory = Factory(database);
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Warehouses.Add(NewWarehouse("W-1", "Main"));
            await db.SaveChangesAsync();
        }
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Warehouses.AddRange(NewWarehouse("W-2", "Second"), NewWarehouse("W-1", "Duplicate code"));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        var logged = Assert.Single(await EntriesAsync(database));
        Assert.Equal(("Created", "System", "Inventory", "warehouses", null), (logged.Action, logged.ActorKind, logged.Module, logged.Entity, logged.ActorUserId));
        var fields = AuditChanges.Parse(logged.Changes).Select(change => change.Field).ToList();
        Assert.Contains("Code", fields);
        Assert.Contains("Name", fields);
        Assert.Equal("W-1", AuditChanges.Parse(logged.Changes).Single(change => change.Field == "Code").After!.GetValue<string>());
    }

    [Fact]
    public async Task The_interceptor_joins_the_callers_transaction_and_never_commits_it()
    {
        var database = await fixture.CreateDatabaseAsync();
        var factory = Factory(database);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.Warehouses.Add(NewWarehouse("ROLLED", "Rolled back"));
            await db.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        await using (var db = await factory.CreateDbContextAsync())
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.Warehouses.Add(NewWarehouse("KEPT", "Committed"));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var check = database.CreateInventoryContext();
        Assert.Equal(["KEPT"], await check.Warehouses.Select(warehouse => warehouse.Code).ToListAsync());
        Assert.Single(await EntriesAsync(database));
    }

    [Fact]
    public async Task A_restore_scope_logs_the_edit_as_restored_values_linked_to_its_entry()
    {
        var database = await fixture.CreateDatabaseAsync();
        var factory = Factory(database);
        var warehouse = NewWarehouse("W-9", "Old name");
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync();
        }
        var origin = Guid.NewGuid();
        await using (var db = await factory.CreateDbContextAsync())
        {
            using var scope = AuditRestoreScope.Begin(origin);
            db.Warehouses.Single(item => item.Id == warehouse.Id).Name = "New name";
            await db.SaveChangesAsync();
        }

        var restored = Assert.Single(await EntriesAsync(database), entry => entry.Action == AuditAction.ValuesRestored);
        Assert.Equal(origin, restored.RestoredFromEntryId);
        Assert.Equal(["Name"], AuditChanges.Parse(restored.Changes).Select(change => change.Field));
    }

    [Fact]
    public async Task The_database_refuses_to_update_delete_or_truncate_audit_entries()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using (var db = await Factory(database).CreateDbContextAsync())
        {
            db.Warehouses.Add(NewWarehouse("W-1", "Main"));
            await db.SaveChangesAsync();
        }

        foreach (var sql in new[] { "UPDATE audit_entries SET module = 'Hacked'", "DELETE FROM audit_entries", "TRUNCATE audit_entries" })
        {
            await using var connection = new NpgsqlConnection(database.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Contains("append-only", error.MessageText);
        }
        Assert.Equal("Inventory", Assert.Single(await EntriesAsync(database)).Module);
    }
}
