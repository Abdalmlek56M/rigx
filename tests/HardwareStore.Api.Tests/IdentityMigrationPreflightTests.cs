using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Identity.Persistence;
using HardwareStore.Modules.Identity.Authentication;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text;
using Testcontainers.PostgreSql;
using Xunit;

namespace HardwareStore.Api.Tests;

[Trait("Category", "RequiresDocker")]
public sealed class PostgreSqlIdentityMigrationPreflightTests(
    PostgreSqlIdentityMigrationPreflightFixture fixture)
    : IClassFixture<PostgreSqlIdentityMigrationPreflightFixture>
{
    private const string M32cMigration = "20261105053622_M3_2c_CustomerRegistration";

    [Fact]
    public async Task Cross_account_username_email_collision_aborts_migration_without_changing_accounts()
    {
        var connectionString = await fixture.CreateDatabaseAtM32bAsync();
        await using var db = CreateContext(connectionString);

        await AddLegacyAccountAsync(db, "shared@example.test", "first@example.test");
        await AddLegacyAccountAsync(db, "second-user", "shared@example.test");
        var before = await ReadAccountsAsync(connectionString);
        var migrationHistoryBefore = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        var schemaBefore = await ReadM32cSchemaAsync(connectionString);

        var exception = await Record.ExceptionAsync(() => db.Database.MigrateAsync());

        AssertMigrationPreflightFailure(exception, "existing login identifiers collide across accounts");
        Assert.Equal(before, await ReadAccountsAsync(connectionString));
        await AssertMigrationFailureLeftHistoryAndSchemaUnchangedAsync(
            db, connectionString, migrationHistoryBefore, schemaBefore);
        Assert.DoesNotContain(M32cMigration, await db.Database.GetAppliedMigrationsAsync());
        Assert.False(await TableExistsAsync(connectionString, "user_account_login_identifiers"));
        Assert.False(await TableExistsAsync(connectionString, "identity_tokens"));
    }

    [Fact]
    public async Task Verified_account_without_password_hash_aborts_migration_without_changing_accounts()
    {
        var connectionString = await fixture.CreateDatabaseAtM32bAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            // Simulate a legacy database state that the old NOT NULL column normally prevents.
            command.CommandText = "ALTER TABLE user_accounts ALTER COLUMN password_hash DROP NOT NULL";
            await command.ExecuteNonQueryAsync();
        }

        await using var db = CreateContext(connectionString);
        await AddLegacyAccountAsync(db, "verified-user", "verified@example.test", passwordHash: null,
            isEmailVerified: true);
        var before = await ReadAccountsAsync(connectionString);
        var migrationHistoryBefore = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        // Capture the test schema after dropping NOT NULL to construct this legacy state.
        var schemaBefore = await ReadM32cSchemaAsync(connectionString);

        var exception = await Record.ExceptionAsync(() => db.Database.MigrateAsync());

        AssertMigrationPreflightFailure(exception, "a verified account has no password hash");
        Assert.Equal(before, await ReadAccountsAsync(connectionString));
        await AssertMigrationFailureLeftHistoryAndSchemaUnchangedAsync(
            db, connectionString, migrationHistoryBefore, schemaBefore);
        Assert.DoesNotContain(M32cMigration, await db.Database.GetAppliedMigrationsAsync());
        Assert.False(await TableExistsAsync(connectionString, "user_account_login_identifiers"));
        Assert.False(await TableExistsAsync(connectionString, "identity_tokens"));
    }

    [Fact]
    public async Task Same_account_username_and_email_identifier_is_deduplicated_by_migration()
    {
        var connectionString = await fixture.CreateDatabaseAtM32bAsync();
        await using var db = CreateContext(connectionString);
        var account = await AddLegacyAccountAsync(db, "same@example.test", "same@example.test");

        await db.Database.MigrateAsync();

        Assert.Contains(M32cMigration, await db.Database.GetAppliedMigrationsAsync());
        var identifiers = await db.UserAccountLoginIdentifiers
            .Where(identifier => identifier.UserAccountId == account.Id)
            .Select(identifier => identifier.NormalizedIdentifier)
            .ToListAsync();
        Assert.Equal(new[] { "SAME@EXAMPLE.TEST" }, identifiers);
    }

    private static IdentityDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);

    private static async Task<UserAccount> AddLegacyAccountAsync(
        IdentityDbContext db,
        string username,
        string email,
        string? passwordHash = "legacy-hash",
        bool isEmailVerified = true)
    {
        var personId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var personCode = $"PER-{DateTime.UtcNow:yyyyMM}-{Random.Shared.Next(10000, 99999)}";
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO persons (id, person_code, full_name, is_customer, created_at_utc) VALUES ({0}, {1}, {2}, {3}, {4})",
            personId, personCode, $"Migration preflight {Guid.NewGuid():N}", false, DateTimeOffset.UtcNow);
        var normalizedUsername = IdentityLoginService.NormalizeIdentifier(username);
        var normalizedEmail = IdentityLoginService.NormalizeIdentifier(email);
        await db.Database.ExecuteSqlAsync(
            $"INSERT INTO user_accounts (id, person_id, username, normalized_username, email, normalized_email, password_hash, is_email_verified, created_at_utc) VALUES ({accountId}, {personId}, {username}, {normalizedUsername}, {email}, {normalizedEmail}, {passwordHash}, {isEmailVerified}, {DateTimeOffset.UtcNow})");
        return new UserAccount
        {
            Id = accountId,
            PersonId = personId,
            Username = username,
            NormalizedUsername = IdentityLoginService.NormalizeIdentifier(username),
            Email = email,
            NormalizedEmail = IdentityLoginService.NormalizeIdentifier(email),
            PasswordHash = passwordHash,
            IsEmailVerified = isEmailVerified,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static async Task<AccountSnapshot[]> ReadAccountsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, person_id, username, normalized_username, email, normalized_email,
                   password_hash, is_email_verified, is_active, access_failed_count,
                   access_failed_window_started_utc, lockout_end_utc, authorization_version, created_at_utc
            FROM user_accounts
            ORDER BY id
            """;

        var rows = new List<AccountSnapshot>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new AccountSnapshot(
                reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
                reader.GetString(4), reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetBoolean(7), reader.GetBoolean(8), reader.GetInt32(9),
                reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
                reader.IsDBNull(11) ? null : reader.GetFieldValue<DateTimeOffset>(11),
                reader.GetInt32(12), reader.GetFieldValue<DateTimeOffset>(13)));
        }

        return rows.ToArray();
    }

    private static async Task<bool> TableExistsAsync(string connectionString, string tableName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = @table_name
            )
            """;
        command.Parameters.AddWithValue("table_name", tableName);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task AssertMigrationFailureLeftHistoryAndSchemaUnchangedAsync(
        IdentityDbContext db,
        string connectionString,
        string[] migrationHistoryBefore,
        MigrationSchemaSnapshot schemaBefore)
    {
        var migrationHistoryAfter = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Equal(migrationHistoryBefore, migrationHistoryAfter);

        var schemaAfter = await ReadM32cSchemaAsync(connectionString);
        Assert.Equal(schemaBefore.Tables, schemaAfter.Tables);
        Assert.Equal(schemaBefore.Columns, schemaAfter.Columns);
        Assert.Equal(schemaBefore.Constraints, schemaAfter.Constraints);
        Assert.Equal(schemaBefore.Indexes, schemaAfter.Indexes);
    }

    private static async Task<MigrationSchemaSnapshot> ReadM32cSchemaAsync(string connectionString)
    {
        const string tablesSql = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE'
              AND table_name IN ('user_accounts', 'identity_tokens', 'user_account_login_identifiers')
            ORDER BY table_name
            """;
        const string columnsSql = """
            SELECT table_name || '|' || column_name || '|' || data_type || '|' || is_nullable || '|' ||
                   COALESCE(character_maximum_length::text, '')
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name IN ('user_accounts', 'identity_tokens', 'user_account_login_identifiers')
            ORDER BY table_name, ordinal_position
            """;
        const string constraintsSql = """
            SELECT relation.relname || '|' || constraint_row.conname || '|' || constraint_row.contype::text || '|' ||
                   pg_get_constraintdef(constraint_row.oid) || '|' || constraint_row.convalidated::text
            FROM pg_constraint AS constraint_row
            INNER JOIN pg_class AS relation ON relation.oid = constraint_row.conrelid
            INNER JOIN pg_namespace AS schema_row ON schema_row.oid = relation.relnamespace
            WHERE schema_row.nspname = 'public'
              AND relation.relname IN ('user_accounts', 'identity_tokens', 'user_account_login_identifiers')
            ORDER BY relation.relname, constraint_row.conname
            """;
        const string indexesSql = """
            SELECT tablename || '|' || indexname || '|' || indexdef
            FROM pg_indexes
            WHERE schemaname = 'public'
              AND tablename IN ('user_accounts', 'identity_tokens', 'user_account_login_identifiers')
            ORDER BY tablename, indexname
            """;

        return new MigrationSchemaSnapshot(
            await ReadSchemaRowsAsync(connectionString, tablesSql),
            await ReadSchemaRowsAsync(connectionString, columnsSql),
            await ReadSchemaRowsAsync(connectionString, constraintsSql),
            await ReadSchemaRowsAsync(connectionString, indexesSql));
    }

    private static async Task<string[]> ReadSchemaRowsAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var rows = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            rows.Add(reader.GetString(0));

        return rows.ToArray();
    }

    private static void AssertMigrationPreflightFailure(Exception? exception, string expectedMessage)
    {
        Assert.NotNull(exception);
        var postgresException = FindPostgresException(exception);
        Assert.NotNull(postgresException);
        Assert.Equal("P0001", postgresException.SqlState);
        Assert.Contains(expectedMessage, postgresException.MessageText, StringComparison.Ordinal);
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException)
                return postgresException;
        }

        return null;
    }

    private sealed record AccountSnapshot(
        Guid Id,
        Guid PersonId,
        string Username,
        string NormalizedUsername,
        string Email,
        string NormalizedEmail,
        string? PasswordHash,
        bool IsEmailVerified,
        bool IsActive,
        int AccessFailedCount,
        DateTimeOffset? AccessFailedWindowStartedUtc,
        DateTimeOffset? LockoutEndUtc,
        int AuthorizationVersion,
        DateTimeOffset CreatedAtUtc);

    private sealed record MigrationSchemaSnapshot(
        string[] Tables,
        string[] Columns,
        string[] Constraints,
        string[] Indexes);
}

public sealed class PostgreSqlIdentityMigrationPreflightFixture : IAsyncLifetime
{
    private const string M32bMigration = "20261105033338_M3_2b_LoginSecurity";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("identity_m3_2c_preflight_test")
        .WithUsername("preflight_test")
        .WithPassword("test-only-password")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public async Task<string> CreateDatabaseAtM32bAsync()
    {
        var databaseName = $"m32c_{Guid.NewGuid():N}";
        await using (var adminConnection = new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await adminConnection.OpenAsync();
            await using var createDatabase = adminConnection.CreateCommand();
            createDatabase.CommandText = $"CREATE DATABASE \"{databaseName}\"";
            await createDatabase.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = databaseName
        };
        var connectionString = builder.ConnectionString;

        await using var context = new IdentityDbContext(
            new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options);
        await context.Database.MigrateAsync(M32bMigration);
        return connectionString;
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
}
