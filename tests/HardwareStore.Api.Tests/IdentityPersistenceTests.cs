using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Authentication;
using HardwareStore.Modules.Identity.Authorization;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class IdentityPersistenceTests
{
    [Fact]
    public void PersonCode_uses_business_month_when_it_differs_from_utc_month()
    {
        var cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        var utc = new DateTimeOffset(2026, 1, 31, 22, 30, 0, TimeSpan.Zero);

        Assert.Equal("PER-202602-00007", PersonCodeFormatter.Format(utc, cairo, 7));
    }

    [Fact]
    public void PersonCode_changes_at_the_business_month_boundary()
    {
        var cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");

        Assert.Equal("PER-202602-99999", PersonCodeFormatter.Format(new DateTimeOffset(2026, 2, 28, 21, 59, 59, TimeSpan.Zero), cairo, 99999));
        Assert.Equal("PER-202603-00001", PersonCodeFormatter.Format(new DateTimeOffset(2026, 2, 28, 22, 0, 0, TimeSpan.Zero), cairo, 1));
    }

    [Fact]
    public void PersonCode_uses_the_IANA_time_zone_daylight_saving_rules()
    {
        var cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        var utcDuringDaylightSaving = new DateTimeOffset(2026, 4, 30, 21, 30, 0, TimeSpan.Zero);

        Assert.Equal("PER-202605-00001", PersonCodeFormatter.Format(utcDuringDaylightSaving, cairo, 1));
    }

    [Fact]
    public void PersonCode_rejects_sequence_outside_the_approved_range()
    {
        var cairo = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        Assert.Throws<ArgumentOutOfRangeException>(() => PersonCodeFormatter.Format(DateTimeOffset.UtcNow, cairo, 100000));
    }

    [Fact]
    public void Identity_model_contains_the_approved_persistence_tables()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=none;Password=none")
            .Options;
        using var context = new IdentityDbContext(options);

        var tables = context.Model.GetEntityTypes().Select(entity => entity.GetTableName()).Order().ToArray();
        Assert.Equal(new[]
        {
            "account_restrictions", "auth_sessions", "authorization_state", "customer_addresses", "email_codes",
            "identity_email_change_requests", "identity_email_outbox", "permissions",
            "person_code_counters", "persons", "role_permissions", "roles",
            "user_accounts", "user_notifications", "user_roles"
        }, tables);

        var designModel = context.GetService<IDesignTimeModel>().Model;
        var seededPermissions = designModel.FindEntityType(typeof(Permission))!.GetSeedData()
            .Select(row => (string)row[nameof(Permission.Code)]!).ToHashSet(StringComparer.Ordinal);
        Assert.True(PermissionCatalog.All.SetEquals(seededPermissions));

        var customer = designModel.FindEntityType(typeof(Role))!.GetSeedData()
            .Single(row => (string)row[nameof(Role.Code)]! == "Customer");
        Assert.Equal("Customer", customer[nameof(Role.Name)]);
        Assert.Empty(designModel.FindEntityType(typeof(RolePermission))!.GetSeedData());

        var accountType = designModel.FindEntityType(typeof(UserAccount))!;
        Assert.Contains(accountType.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(UserAccount.PersonId)]));
        var emailCodeType = designModel.FindEntityType(typeof(EmailCode))!;
        Assert.Contains(emailCodeType.GetIndexes(), index => index.IsUnique &&
            index.GetFilter() == "used_at_utc IS NULL AND revoked_at_utc IS NULL");
        var allowedCascades = new HashSet<string>(["AccountRestriction", "IdentityEmailChangeRequest", "UserNotification"], StringComparer.Ordinal);
        Assert.All(designModel.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()), foreignKey =>
            Assert.Equal(allowedCascades.Contains(foreignKey.DeclaringEntityType.ClrType.Name)
                ? DeleteBehavior.Cascade
                : DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
    }

    [Fact]
    public void Invalid_business_timezone_fails_during_service_registration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=localhost;Database=unused;Username=unused",
            ["Business:TimeZone"] = "Not/A_Real_Zone"
        }).Build();

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddIdentityPersistence(configuration));
    }

}

[Trait("Category", "RequiresDocker")]
public sealed class PostgreSqlIdentityPersistenceTests(PostgreSqlIdentityFixture fixture)
    : IClassFixture<PostgreSqlIdentityFixture>
{
    internal static readonly Guid CustomerRoleId = Guid.Parse("11111111-1111-4111-8111-111111111111");

    [Fact]
    public async Task Migration_creates_only_identity_tables_and_exact_permission_catalog()
    {
        await using var context = fixture.CreateContext();
        var applied = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20261104134655_M3_1_IdentityPersistence", applied);
        Assert.Contains("20261104171407_M3_2a_AuthenticationSessions", applied);
        Assert.Contains("20261104171605_M3_2a_AuthorizationStateSingleton", applied);
        Assert.Contains("20261105033338_M3_2b_LoginSecurity", applied);
        Assert.Contains("20261105053622_M3_2c_CustomerRegistration", applied);
        Assert.Contains("20261105134758_M4_CatalogPermissions", applied);

        await using var auditContext = fixture.CreateAuditContext();
        var appliedAuditMigrations = await auditContext.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20261104171417_M3_2a_SecurityAuditEvents", appliedAuditMigrations);

        var tables = await context.Database.SqlQuery<string>($"""
            SELECT table_name AS "Value"
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE'
              AND table_name <> '__EFMigrationsHistory'
            """).ToListAsync();
        var requiredTables = new[]
        {
            "persons", "user_accounts", "roles", "permissions", "user_roles",
            "role_permissions", "person_code_counters", "auth_sessions", "authorization_state",
            "email_codes", "security_audit_events",
            "customer_addresses", "account_restrictions", "user_notifications",
            "identity_email_outbox", "identity_email_change_requests"
        };
        Assert.All(requiredTables, table => Assert.Contains(table, tables));

        var actualPermissions = await context.Permissions.Select(permission => permission.Code).ToHashSetAsync();
        Assert.True(PermissionCatalog.All.SetEquals(actualPermissions));
        Assert.Equal(0, await context.RolePermissions.CountAsync(grant => grant.RoleId == PostgreSqlIdentityPersistenceTests.CustomerRoleId));
    }

    [Fact]
    public async Task Duplicate_accounts_and_role_assignments_are_rejected()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero));
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        Person person;
        await using (var context = fixture.CreateContext())
            person = await new PersonCodeGenerator(context, clock, zone)
                .CreatePersonAsync(new Person { FullName = "Duplicate constraint test" });

        var userAccountId = Guid.NewGuid();
        var username = $"user-{Guid.NewGuid():N}";
        var email = $"{Guid.NewGuid():N}@example.test";
        await using (var context = fixture.CreateContext())
        {
            context.UserAccounts.Add(new UserAccount
            {
                Id = userAccountId, PersonId = person.Id, Username = username,
                NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
                Email = email, NormalizedEmail = IdentityLoginService.NormalizeIdentifier(email),
                CreatedAtUtc = clock.GetUtcNow()
            });
            await context.SaveChangesAsync();
        }

        await using (var context = fixture.CreateContext())
        {
            var duplicateUsername = $"duplicate-{Guid.NewGuid():N}";
            var duplicateEmail = $"{Guid.NewGuid():N}@example.test";
            context.UserAccounts.Add(new UserAccount
            {
                Id = Guid.NewGuid(), PersonId = person.Id, Username = duplicateUsername,
                NormalizedUsername = IdentityLoginService.NormalizeIdentifier(duplicateUsername),
                Email = duplicateEmail, NormalizedEmail = IdentityLoginService.NormalizeIdentifier(duplicateEmail),
                CreatedAtUtc = clock.GetUtcNow()
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        var roleAssignment = new UserRole { UserAccountId = userAccountId, RoleId = CustomerRoleId };
        await using (var context = fixture.CreateContext())
        {
            context.UserRoles.Add(roleAssignment);
            await context.SaveChangesAsync();
        }
        await using (var context = fixture.CreateContext())
        {
            context.UserRoles.Add(new UserRole { UserAccountId = userAccountId, RoleId = CustomerRoleId });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        var testRoleId = Guid.NewGuid();
        var permissionCode = PermissionCatalog.All.OrderBy(code => code, StringComparer.Ordinal).First();
        await using (var context = fixture.CreateContext())
        {
            context.Roles.Add(new Role { Id = testRoleId, Code = $"Test-{Guid.NewGuid():N}", Name = "Test role" });
            context.RolePermissions.Add(new RolePermission { RoleId = testRoleId, PermissionCode = permissionCode });
            await context.SaveChangesAsync();
        }
        await using (var context = fixture.CreateContext())
        {
            context.RolePermissions.Add(new RolePermission { RoleId = testRoleId, PermissionCode = permissionCode });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
        await using (var context = fixture.CreateContext())
        {
            context.RolePermissions.Add(new RolePermission { RoleId = testRoleId, PermissionCode = "Unapproved.Permission" });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task Person_deletion_is_restricted_while_an_account_exists()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero));
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        Person person;
        await using (var context = fixture.CreateContext())
            person = await new PersonCodeGenerator(context, clock, zone)
                .CreatePersonAsync(new Person { FullName = "Restrict delete test" });

        await using (var context = fixture.CreateContext())
        {
            var restrictedUsername = $"restrict-{Guid.NewGuid():N}";
            var restrictedEmail = $"{Guid.NewGuid():N}@example.test";
            context.UserAccounts.Add(new UserAccount
            {
                Id = Guid.NewGuid(), PersonId = person.Id, Username = restrictedUsername,
                NormalizedUsername = IdentityLoginService.NormalizeIdentifier(restrictedUsername),
                Email = restrictedEmail, NormalizedEmail = IdentityLoginService.NormalizeIdentifier(restrictedEmail),
                CreatedAtUtc = clock.GetUtcNow()
            });
            await context.SaveChangesAsync();
        }
        await using (var context = fixture.CreateContext())
        {
            context.Persons.Remove(person);
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task Concurrent_person_code_allocation_has_no_collisions()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 8, 15, 10, 0, 0, TimeSpan.Zero));
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        var generated = await Task.WhenAll(Enumerable.Range(0, 32).Select(async index =>
        {
            await using var context = fixture.CreateContext();
            return await new PersonCodeGenerator(context, clock, zone)
                .CreatePersonAsync(new Person { FullName = $"Concurrent {index}" });
        }));

        Assert.Equal(32, generated.Select(person => person.PersonCode).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Person_code_is_not_reused_after_person_deletion()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero));
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        Person first;
        await using (var context = fixture.CreateContext())
            first = await new PersonCodeGenerator(context, clock, zone)
                .CreatePersonAsync(new Person { FullName = "First identity" });

        await using (var context = fixture.CreateContext())
        {
            context.Persons.Remove(first);
            await context.SaveChangesAsync();
        }

        await using (var context = fixture.CreateContext())
        {
            var second = await new PersonCodeGenerator(context, clock, zone)
                .CreatePersonAsync(new Person { FullName = "Second identity" });
            Assert.Equal("PER-202609-00002", second.PersonCode);
        }
    }

    [Fact]
    public async Task Person_insert_failure_rolls_back_the_monthly_counter()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero));
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        await using (var context = fixture.CreateContext())
        {
            var tooLongName = new string('x', 256);
            await Assert.ThrowsAsync<DbUpdateException>(() => new PersonCodeGenerator(context, clock, zone)
                .CreatePersonAsync(new Person { FullName = tooLongName }));
        }

        await using (var context = fixture.CreateContext())
        {
            Assert.Equal(0, await context.PersonCodeCounters.CountAsync(counter => counter.YearMonth == "202610"));
            var person = await new PersonCodeGenerator(context, clock, zone)
                .CreatePersonAsync(new Person { FullName = "After rollback" });
            Assert.Equal("PER-202610-00001", person.PersonCode);
        }
    }

    [Fact]
    public async Task Monthly_person_code_limit_stops_at_99999()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 11, 15, 10, 0, 0, TimeSpan.Zero));
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        await using (var context = fixture.CreateContext())
        {
            context.PersonCodeCounters.Add(new PersonCodeCounter { YearMonth = "202611", LastValue = 99999 });
            await context.SaveChangesAsync();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new PersonCodeGenerator(context, clock, zone).CreatePersonAsync(new Person { FullName = "At limit" }));
            Assert.Contains("99,999", exception.Message, StringComparison.Ordinal);
        }
    }
}

public sealed class PostgreSqlIdentityFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("identity_m3_1_test")
        .WithUsername("identity_test")
        .WithPassword("test-only-password")
        .Build();

    private DbContextOptions<IdentityDbContext> _options = null!;
    private DbContextOptions<HardwareStore.Modules.Auditing.SecurityAuditDbContext> _auditOptions = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _options = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
        await ProvisionAuditRolesAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await using (var connection = new Npgsql.NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO hardware_store_app; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO hardware_store_app;";
            await command.ExecuteNonQueryAsync();
        }
        _auditOptions = new DbContextOptionsBuilder<HardwareStore.Modules.Auditing.SecurityAuditDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).Options;
        await using var auditContext = CreateAuditContext();
        await auditContext.Database.MigrateAsync();
    }

    public IdentityDbContext CreateContext() => new(_options);
    public string GetConnectionString() => _postgres.GetConnectionString();

    public HardwareStore.Modules.Auditing.SecurityAuditDbContext CreateAuditContext() => new(_auditOptions);

    public string RuntimeConnectionString => new Npgsql.NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
    {
        Username = "hardware_store_app",
        Password = "runtime-test-password"
    }.ConnectionString;

    private async Task ProvisionAuditRolesAsync()
    {
        await using var connection = new Npgsql.NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE ROLE hardware_store_app LOGIN INHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD 'runtime-test-password';
            CREATE ROLE hardware_store_audit_owner NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE;
            CREATE ROLE hardware_store_audit_writer NOLOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE;
            GRANT hardware_store_audit_owner TO identity_test;
            GRANT hardware_store_audit_writer TO hardware_store_app;
            GRANT USAGE ON SCHEMA public TO hardware_store_app;
            """;
        await command.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
