using HardwareStore.Common.Infrastructure.Audit;
using HardwareStore.Common.Infrastructure.Persistence;
using HardwareStore.Modules.Auditing;
using HardwareStore.Modules.Identity.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace HardwareStore.Api.Tests;

/// <summary>The real API host pointed at one test database, with an ephemeral key ring.</summary>
internal sealed class IdentityLoginApiFactory(
    string connectionString,
    bool failSuccessAudit = false,
    TimeProvider? timeProvider = null,
    Action<IServiceCollection>? configureTestServices = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["DataProtection:KeyRingPath"] = Path.Combine(Directory.GetCurrentDirectory(), ".backups", "m3_2b-test-keyring")
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IDbContextFactory<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();
            services.AddAuditedDbContextFactory<IdentityDbContext>(connectionString);

            services.RemoveAll<DbContextOptions<SecurityAuditDbContext>>();
            services.RemoveAll<IDbContextFactory<SecurityAuditDbContext>>();
            services.RemoveAll<SecurityAuditDbContext>();
            services.AddDbContextFactory<SecurityAuditDbContext>(options => options.UseNpgsql(connectionString));

            services.RemoveAll<DbContextOptions<HardwareStoreDbContext>>();
            services.RemoveAll<HardwareStoreDbContext>();
            services.AddDbContext<HardwareStoreDbContext>(options => options.UseNpgsql(connectionString));

            services.RemoveAll<IDataProtectionProvider>();
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            if (failSuccessAudit)
            {
                services.RemoveAll<ISecurityAuditWriter>();
                services.AddScoped<ISecurityAuditWriter, FailSuccessfulLoginAuditWriter>();
            }

            if (timeProvider is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(timeProvider);
            }

            configureTestServices?.Invoke(services);
        });
    }

    public HttpClient CreatePreflightedClient(WebApplicationFactoryClientOptions options)
    {
        var expected = new NpgsqlConnectionStringBuilder(connectionString);
        using var identityDb = Services.GetRequiredService<IDbContextFactory<IdentityDbContext>>().CreateDbContext();
        AssertTarget(identityDb.Database.GetDbConnection(), expected);

        using var auditDb = Services.GetRequiredService<IDbContextFactory<SecurityAuditDbContext>>().CreateDbContext();
        AssertTarget(auditDb.Database.GetDbConnection(), expected);

        using var scope = Services.CreateScope();
        using var applicationDb = scope.ServiceProvider.GetRequiredService<HardwareStoreDbContext>();
        AssertTarget(applicationDb.Database.GetDbConnection(), expected);

        return CreateClient(options);
    }

    private static void AssertTarget(System.Data.Common.DbConnection connection, NpgsqlConnectionStringBuilder expected)
    {
        var actual = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
        Assert.Equal(expected.Host, actual.Host);
        Assert.Equal(expected.Port, actual.Port);
        Assert.Equal(expected.Database, actual.Database);
    }
}

internal sealed class FailSuccessfulLoginAuditWriter(
    Microsoft.EntityFrameworkCore.IDbContextFactory<HardwareStore.Modules.Auditing.SecurityAuditDbContext> contextFactory)
    : ISecurityAuditWriter
{
    public Task WriteAsync(SecurityAuditEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.EventType == "Identity.Login.Succeeded")
            throw new InvalidOperationException("Injected successful-login audit failure.");
        return new HardwareStore.Modules.Auditing.SecurityAuditWriter(contextFactory).WriteAsync(entry, cancellationToken);
    }
}
