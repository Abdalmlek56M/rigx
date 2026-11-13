using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Catalog;
using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Storefront;
using HardwareStore.Modules.Purchasing;
using HardwareStore.Modules.Retail;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class OwnerBootstrapPostgreSqlTests(OwnerBootstrapPostgreSqlFixture fixture)
    : IClassFixture<OwnerBootstrapPostgreSqlFixture>
{
    private static readonly FirstOwnerBootstrapRequest ValidRequest = new(
        "rigx-owner",
        "owner@example.test",
        "RIGX Owner",
        "a sufficiently long owner password");

    [Fact]
    public async Task Bootstrap_is_atomic_one_time_and_grants_every_catalog_permission()
    {
        var database = await fixture.CreateDatabaseAsync();
        var service = database.CreateService();

        var attempts = await Task.WhenAll(
            CaptureAsync(service),
            CaptureAsync(service));

        Assert.Single(attempts, attempt => attempt is null);
        Assert.Single(attempts, attempt => attempt?.Failure == FirstOwnerBootstrapFailure.AlreadyInitialized);

        await using var db = database.CreateIdentityContext();
        Assert.Equal(1, await db.Roles.CountAsync(role => role.Code == "Owner"));
        Assert.Equal(1, await db.UserAccounts.CountAsync());
        Assert.Equal(PermissionCatalog.All.Count, await db.RolePermissions.CountAsync());
        Assert.Equal(1, (await db.AuthorizationStates.SingleAsync()).RbacRevision);

        await using var audit = database.CreateAuditContext();
        var successAudit = await audit.SecurityAuditEvents.SingleAsync(
            entry => entry.EventType == "Owner.Bootstrapped" && entry.Outcome == "Succeeded");
        Assert.DoesNotContain("owner@example.test", successAudit.SafeMetadata, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rigx-owner", successAudit.SafeMetadata, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ValidRequest.Password, successAudit.SafeMetadata, StringComparison.Ordinal);

        var repeat = await CaptureAsync(service);
        Assert.Equal(FirstOwnerBootstrapFailure.AlreadyInitialized, repeat?.Failure);
        Assert.Equal(1, await db.UserAccounts.CountAsync());
    }

    [Fact]
    public async Task Audit_failure_rolls_back_the_owner_account_and_role()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using (var db = database.CreateIdentityContext())
        await using (var audit = database.CreateAuditContext())
        {
            await db.Database.MigrateAsync();
            await audit.Database.MigrateAsync();
            await audit.Database.ExecuteSqlRawAsync("DROP TABLE security_audit_events");
        }

        var service = database.CreateService();
        var exception = await Assert.ThrowsAsync<FirstOwnerBootstrapException>(() =>
            service.BootstrapAsync(ValidRequest));
        Assert.Equal(FirstOwnerBootstrapFailure.Failed, exception.Failure);

        await using var verify = database.CreateIdentityContext();
        Assert.Equal(0, await verify.Roles.CountAsync(role => role.Code == "Owner"));
        Assert.Equal(0, await verify.UserAccounts.CountAsync());
        Assert.Equal(0, await verify.RolePermissions.CountAsync());
    }

    [Fact]
    public void Invalid_input_and_weak_password_are_rejected_before_any_database_work()
    {
        var normalized = new FirstOwnerBootstrapRequest(
            " ",
            "not-an-email",
            "RIGX Owner",
            "a sufficiently long owner password");
        var inputFailure = Assert.Throws<FirstOwnerBootstrapException>(() =>
            FirstOwnerBootstrapService.ValidateRequest(normalized));
        Assert.Equal(FirstOwnerBootstrapFailure.InvalidInput, inputFailure.Failure);

        var weak = ValidRequest with { Password = "short" };
        var passwordFailure = Assert.Throws<FirstOwnerBootstrapException>(() =>
            FirstOwnerBootstrapService.ValidateRequest(weak));
        Assert.Equal(FirstOwnerBootstrapFailure.PasswordPolicy, passwordFailure.Failure);
    }

    private static async Task<FirstOwnerBootstrapException?> CaptureAsync(FirstOwnerBootstrapService service)
    {
        try
        {
            await service.BootstrapAsync(ValidRequest);
            return null;
        }
        catch (FirstOwnerBootstrapException exception)
        {
            return exception;
        }
    }
}

public sealed class OwnerBootstrapPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("owner_bootstrap_test")
        .WithUsername("owner_bootstrap_test")
        .WithPassword("test-only-password")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();
    }

    public async Task<OwnerBootstrapDatabase> CreateDatabaseAsync()
    {
        var databaseName = $"owner_bootstrap_{Guid.NewGuid():N}";
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
        var database = new OwnerBootstrapDatabase(builder.ConnectionString);
        await using (var roles = new NpgsqlConnection(builder.ConnectionString))
        {
            await roles.OpenAsync();
            foreach (var role in new[]
                     {
                         "hardware_store_audit_owner",
                         "hardware_store_app",
                         "hardware_store_audit_writer"
                     })
            {
                await using var exists = roles.CreateCommand();
                exists.CommandText = "SELECT 1 FROM pg_roles WHERE rolname = @name";
                exists.Parameters.AddWithValue("name", role);
                if (await exists.ExecuteScalarAsync() is null)
                {
                    await using var create = roles.CreateCommand();
                    create.CommandText = $"CREATE ROLE \"{role}\"";
                    await create.ExecuteNonQueryAsync();
                }
            }
        }
        await using var identity = database.CreateIdentityContext();
        await using var audit = database.CreateAuditContext();
        await using var catalog = database.CreateCatalogContext();
        await using var storefront = database.CreateStorefrontContext();
        await using var purchasing = database.CreatePurchasingContext();
        await using var retail = database.CreateRetailContext();
        await identity.Database.MigrateAsync();
        await audit.Database.MigrateAsync();
        await catalog.Database.MigrateAsync();
        await storefront.Database.MigrateAsync();
        await purchasing.Database.MigrateAsync();
        await retail.Database.MigrateAsync();
        return database;
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
}

public sealed class OwnerBootstrapDatabase(string connectionString)
{
    public string ConnectionString => connectionString;

    public IdentityDbContext CreateIdentityContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);

    public SecurityAuditDbContext CreateAuditContext() =>
        new(new DbContextOptionsBuilder<SecurityAuditDbContext>().UseNpgsql(connectionString).Options);

    public CatalogDbContext CreateCatalogContext() =>
        new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString).Options);

    public StorefrontDbContext CreateStorefrontContext() =>
        new(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);

    public PurchasingDbContext CreatePurchasingContext() =>
        new(new DbContextOptionsBuilder<PurchasingDbContext>().UseNpgsql(connectionString).Options);

    public RetailDbContext CreateRetailContext() =>
        new(new DbContextOptionsBuilder<RetailDbContext>().UseNpgsql(connectionString).Options);

    public RetailTestServices CreateRetailServices() =>
        RetailTestServices.Create(new TestRetailContextFactory(connectionString), TimeProvider.System);

    public WishlistService CreateWishlistService() =>
        new(new TestStorefrontContextFactory(connectionString), TimeProvider.System);

    public FirstOwnerBootstrapService CreateService()
    {
        var identityFactory = new TestIdentityContextFactory(connectionString);
        var auditFactory = new TestAuditContextFactory(connectionString);
        return new FirstOwnerBootstrapService(
            identityFactory,
            auditFactory,
            new SecurityAuditWriter(auditFactory),
            new PasswordHashingService(new PasswordHasher<UserAccount>()),
            TimeProvider.System,
            TimeZoneInfo.Utc,
            NullLogger<FirstOwnerBootstrapService>.Instance);
    }

    public IdentityAdministrationService CreateAdministrationService(
        IIdentityEmailDispatcher? emailDispatcher = null,
        IConfiguration? configuration = null)
    {
        var identityFactory = new TestIdentityContextFactory(connectionString);
        var auditFactory = new TestAuditContextFactory(connectionString);
        return new IdentityAdministrationService(
            identityFactory,
            new SecurityAuditWriter(auditFactory),
            TimeProvider.System,
            emailDispatcher: emailDispatcher,
            configuration: configuration,
            businessTimeZone: TimeZoneInfo.Utc);
    }

    public CustomerAddressService CreateAddressService()
    {
        var identityFactory = new TestIdentityContextFactory(connectionString);
        var auditFactory = new TestAuditContextFactory(connectionString);
        return new CustomerAddressService(
            identityFactory,
            new SecurityAuditWriter(auditFactory),
            TimeProvider.System);
    }

    private sealed class TestIdentityContextFactory(string connectionString)
        : IDbContextFactory<IdentityDbContext>
    {
        public IdentityDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class TestAuditContextFactory(string connectionString)
        : IDbContextFactory<SecurityAuditDbContext>
    {
        public SecurityAuditDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<SecurityAuditDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class TestStorefrontContextFactory(string connectionString)
        : IDbContextFactory<StorefrontDbContext>
    {
        public StorefrontDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<StorefrontDbContext>().UseNpgsql(connectionString).Options);
    }

    private sealed class TestRetailContextFactory(string connectionString)
        : IDbContextFactory<RetailDbContext>
    {
        public RetailDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<RetailDbContext>().UseNpgsql(connectionString).Options);
    }
}
