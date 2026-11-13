using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Inventory;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Storefront;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class SupplierDirectoryPostgreSqlTests(SupplierDirectoryPostgreSqlFixture fixture)
    : IClassFixture<SupplierDirectoryPostgreSqlFixture>
{
    [Fact]
    public async Task Creates_supplier_party_and_profile_with_audit_and_search()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateService();
        var actorId = await CreateActorAsync(database);

        var created = await service.CreateAsync(
            new SupplierCreateRequest(
                "Cairo Hardware Distribution",
                "+20223456789",
                "supplier@example.test",
                "tax-100",
                "cr-200",
                "Sales Contact",
                "+201111111111",
                30,
                "Weekly delivery",
                true),
            actorId);

        Assert.Equal("Cairo Hardware Distribution", created.CompanyName);
        Assert.Equal("TAX-100", created.TaxIdNumber);
        Assert.Equal("CR-200", created.CommercialRegistration);
        Assert.Equal(30, created.PaymentTermsDays);
        Assert.StartsWith("PER-", created.PersonCode);

        var page = await service.GetAsync("supplier@example.test", true, 1, 10);
        Assert.Single(page.Items);
        Assert.Equal(created.Id, page.Items[0].Id);

        var duplicateTax = await Assert.ThrowsAsync<SupplierDirectoryException>(() =>
            service.CreateAsync(
                new SupplierCreateRequest("Another Supplier", null, null, "TAX-100", null, null, null, 0, null, true),
                actorId));
        Assert.Equal(409, duplicateTax.StatusCode);

        var updated = await service.UpdateAsync(
            created.Id,
            new SupplierUpdateRequest(
                "Cairo Hardware Distribution",
                "+20223456780",
                "supplier@example.test",
                "TAX-100",
                "CR-200",
                "New Contact",
                "+201222222222",
                45,
                "Updated terms",
                false,
                created.RowVersion),
            actorId);

        Assert.Equal(45, updated.PaymentTermsDays);
        Assert.False(updated.IsActive);
        Assert.True(updated.RowVersion > created.RowVersion);

        await using var audit = database.CreateAuditContext();
        var events = await audit.SecurityAuditEvents
            .Where(entry => entry.EventType.StartsWith("Purchasing.Supplier"))
            .Select(entry => entry.EventType)
            .ToListAsync();
        Assert.Contains("Purchasing.SupplierCreated", events);
        Assert.Contains("Purchasing.SupplierDeactivated", events);
    }

    [Fact]
    public async Task Update_requires_a_current_row_version_and_does_not_overwrite_stale_data()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateService();
        var actorId = await CreateActorAsync(database);
        var created = await CreateSupplierAsync(service, actorId, "Stable Supplier");

        var stale = await Assert.ThrowsAsync<SupplierDirectoryException>(() =>
            service.UpdateAsync(
                created.Id,
                new SupplierUpdateRequest("Stale Name", null, null, null, null, null, null, 0, null, true, 0),
                actorId));
        Assert.Equal(400, stale.StatusCode);

        var first = await service.UpdateAsync(
            created.Id,
            new SupplierUpdateRequest("First Name", null, null, null, null, null, null, 0, null, true, created.RowVersion),
            actorId);
        var second = await Assert.ThrowsAsync<SupplierDirectoryException>(() =>
            service.UpdateAsync(
                created.Id,
                new SupplierUpdateRequest("Second Name", null, null, null, null, null, null, 0, null, true, created.RowVersion),
                actorId));
        Assert.Equal(409, second.StatusCode);
        Assert.Equal("First Name", (await service.GetByIdAsync(created.Id))!.CompanyName);
        Assert.Equal(first.RowVersion, (await service.GetByIdAsync(created.Id))!.RowVersion);
    }

    [Fact]
    public async Task Concurrent_updates_with_the_same_row_version_produce_one_conflict()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateService();
        var actorId = await CreateActorAsync(database);
        var created = await CreateSupplierAsync(service, actorId, "Concurrent Supplier");

        var request = new SupplierUpdateRequest(
            "Concurrent Supplier",
            "123",
            null,
            null,
            null,
            null,
            null,
            0,
            "one",
            true,
            created.RowVersion);

        var attempts = await Task.WhenAll(
            CaptureUpdateAsync(service, created.Id, request, actorId),
            CaptureUpdateAsync(service, created.Id, request, actorId));

        Assert.Equal(1, attempts.Count(attempt => attempt is null));
        Assert.Equal(1, attempts.Count(attempt => attempt?.StatusCode == 409));
    }

    [Fact]
    public async Task Audit_failure_rolls_back_both_person_and_supplier_profile()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using (var audit = database.CreateAuditContext())
        {
            await audit.Database.ExecuteSqlRawAsync("DROP TABLE security_audit_events");
        }
        var service = database.CreateService();
        var actorId = await CreateActorAsync(database);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.CreateAsync(
                new SupplierCreateRequest("Atomic Supplier", null, null, "TAX-ROLLBACK", null, null, null, 0, null, true),
                actorId));

        await using var purchasing = database.CreatePurchasingContext();
        await using var identity = database.CreateIdentityContext();
        Assert.Equal(0, await purchasing.SupplierProfiles.CountAsync());
        Assert.Equal(0, await identity.Persons.CountAsync(person => person.IsSupplier));
    }

    [Fact]
    public async Task Financials_are_separate_audited_and_concurrency_protected()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateService();
        var actorId = await CreateActorAsync(database);
        var created = await CreateSupplierAsync(service, actorId, "Financial Supplier");

        var empty = await service.GetFinancialsAsync(created.Id, actorId);
        Assert.Null(empty.BankName);
        Assert.Null(empty.BankAccountNumber);
        Assert.Null(empty.BankIban);

        var updated = await service.UpdateFinancialsAsync(
            created.Id,
            new SupplierFinancialWriteRequest("Bank Misr", "123456789", "EG380001000000000000000000", created.RowVersion),
            actorId);
        Assert.Equal("Bank Misr", updated.BankName);
        Assert.True(updated.RowVersion > created.RowVersion);

        var stale = await Assert.ThrowsAsync<SupplierDirectoryException>(() =>
            service.UpdateFinancialsAsync(
                created.Id,
                new SupplierFinancialWriteRequest("Other Bank", "999", "EG999", created.RowVersion),
                actorId));
        Assert.Equal(409, stale.StatusCode);

        var reads = await service.GetFinancialsAsync(created.Id, actorId);
        Assert.Equal("EG380001000000000000000000", reads.BankIban);

        await using var audit = database.CreateAuditContext();
        var events = await audit.SecurityAuditEvents
            .Where(entry => entry.EventType.StartsWith("Purchasing.SupplierFinancials"))
            .Select(entry => entry.EventType)
            .ToListAsync();
        Assert.Contains("Purchasing.SupplierFinancialsViewed", events);
        Assert.Contains("Purchasing.SupplierFinancialsUpdated", events);
    }

    [Fact]
    public async Task Financial_read_fails_closed_when_audit_cannot_be_written()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateService();
        var actorId = await CreateActorAsync(database);
        var created = await CreateSupplierAsync(service, actorId, "Audit Failure Supplier");

        await using (var audit = database.CreateAuditContext())
        {
            await audit.Database.ExecuteSqlRawAsync("DROP TABLE security_audit_events");
        }

        await Assert.ThrowsAnyAsync<Exception>(() => service.GetFinancialsAsync(created.Id, actorId));
    }

    [Fact]
    public async Task Rejects_invalid_email_and_payment_terms()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateService();
        var actorId = await CreateActorAsync(database);

        var invalidEmail = await Assert.ThrowsAsync<SupplierDirectoryException>(() =>
            service.CreateAsync(
                new SupplierCreateRequest("Bad Email", null, "not-an-email", null, null, null, null, 0, null, true),
                actorId));
        Assert.Equal(400, invalidEmail.StatusCode);

        var invalidTerms = await Assert.ThrowsAsync<SupplierDirectoryException>(() =>
            service.CreateAsync(
                new SupplierCreateRequest("Bad Terms", null, null, null, null, null, null, -1, null, true),
                actorId));
        Assert.Equal(400, invalidTerms.StatusCode);
    }

    private static Task<SupplierView> CreateSupplierAsync(
        SupplierDirectoryService service,
        Guid actorId,
        string companyName) =>
        service.CreateAsync(
            new SupplierCreateRequest(companyName, null, null, null, null, null, null, 0, null, true),
            actorId);

    private static async Task<SupplierDirectoryException?> CaptureUpdateAsync(
        SupplierDirectoryService service,
        Guid id,
        SupplierUpdateRequest request,
        Guid actorId)
    {
        try
        {
            await service.UpdateAsync(id, request, actorId);
            return null;
        }
        catch (SupplierDirectoryException exception)
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
            FullName = "Supplier Actor",
            Email = "actor@example.test",
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
            Username = "supplier-actor",
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier("supplier-actor"),
            Email = "actor@example.test",
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier("actor@example.test"),
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

public sealed class SupplierDirectoryPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("supplier_m5_test")
        .WithUsername("supplier_m5_test")
        .WithPassword("test-only-password")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();
    }

    public async Task<SupplierDirectoryDatabase> CreateDatabaseAsync()
    {
        var databaseName = $"supplier_m5_{Guid.NewGuid():N}";
        await using (var admin = new NpgsqlConnection(ConnectionString))
        {
            await admin.OpenAsync();
            await using var command = admin.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await command.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Database = databaseName
        };
        var database = new SupplierDirectoryDatabase(builder.ConnectionString);
        await using var identity = database.CreateIdentityContext();
        await using var audit = database.CreateAuditContext();
        await using var catalog = database.CreateCatalogContext();
        await using var purchasing = database.CreatePurchasingContext();
        await using var inventory = database.CreateInventoryContext();
        await using var storefront = database.CreateStorefrontContext();
        await identity.Database.MigrateAsync();
        await CreateAuditRolesAsync(builder.ConnectionString);
        await audit.Database.MigrateAsync();
        await catalog.Database.MigrateAsync();
        await purchasing.Database.MigrateAsync();
        await inventory.Database.MigrateAsync();
        await storefront.Database.MigrateAsync();
        return database;
    }

    private static async Task CreateAuditRolesAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        foreach (var role in new[] { "hardware_store_audit_owner", "hardware_store_app", "hardware_store_audit_writer" })
        {
            await using var exists = connection.CreateCommand();
            exists.CommandText = "SELECT 1 FROM pg_roles WHERE rolname = @name";
            exists.Parameters.AddWithValue("name", role);
            if (await exists.ExecuteScalarAsync() is null)
            {
                await using var create = connection.CreateCommand();
                create.CommandText = $"CREATE ROLE \"{role}\"";
                await create.ExecuteNonQueryAsync();
            }
        }
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
}

public sealed class SupplierDirectoryDatabase(string connectionString)
{
    public IdentityDbContext CreateIdentityContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);

    public SecurityAuditDbContext CreateAuditContext() =>
        new(new DbContextOptionsBuilder<SecurityAuditDbContext>().UseNpgsql(connectionString).Options);

    public PurchasingDbContext CreatePurchasingContext() =>
        new(new DbContextOptionsBuilder<PurchasingDbContext>().UseNpgsql(connectionString).Options);

    public SupplierDirectoryService CreateService()
    {
        var purchasingFactory = new TestPurchasingFactory(connectionString);
        var auditFactory = new TestAuditFactory(connectionString);
        return new SupplierDirectoryService(
            purchasingFactory,
            new TestIdentityFactory(connectionString),
            new SecurityAuditWriter(auditFactory),
            TimeProvider.System,
            TimeZoneInfo.Utc);
    }

    public SupplierProductCatalogService CreateProductService()
    {
        var purchasingFactory = new TestPurchasingFactory(connectionString);
        var auditFactory = new TestAuditFactory(connectionString);
        return new SupplierProductCatalogService(
            purchasingFactory,
            new SecurityAuditWriter(auditFactory),
            TimeProvider.System);
    }

    public CatalogDbContext CreateCatalogContext() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString).Options);

    public InventoryDbContext CreateInventoryContext() =>
        new(new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(connectionString).Options);

    public StorefrontDbContext CreateStorefrontContext() =>
        new(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);

    public StoreSettingsService CreateStoreSettingsService()
    {
        var auditFactory = new TestAuditFactory(connectionString);
        return new StoreSettingsService(
            new TestStorefrontFactory(connectionString),
            new SecurityAuditWriter(auditFactory),
            TimeProvider.System);
    }

    public WarehouseDirectoryService CreateWarehouseService()
    {
        var auditFactory = new TestAuditFactory(connectionString);
        return new WarehouseDirectoryService(
            new TestInventoryFactory(connectionString),
            new SecurityAuditWriter(auditFactory),
            TimeProvider.System);
    }

    public PurchaseDraftService CreatePurchaseDraftService()
    {
        var auditFactory = new TestAuditFactory(connectionString);
        return new PurchaseDraftService(
            new TestPurchasingFactory(connectionString),
            new SecurityAuditWriter(auditFactory),
            TimeProvider.System);
    }

    private sealed class TestPurchasingFactory(string connectionString) : IDbContextFactory<PurchasingDbContext>
    {
        public PurchasingDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<PurchasingDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class TestIdentityFactory(string connectionString) : IDbContextFactory<IdentityDbContext>
    {
        public IdentityDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class TestAuditFactory(string connectionString) : IDbContextFactory<SecurityAuditDbContext>
    {
        public SecurityAuditDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<SecurityAuditDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class TestInventoryFactory(string connectionString) : IDbContextFactory<InventoryDbContext>
    {
        public InventoryDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class TestStorefrontFactory(string connectionString) : IDbContextFactory<StorefrontDbContext>
    {
        public StorefrontDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);
    }
}
