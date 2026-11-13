using System.Net;
using HardwareStore.Common.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace HardwareStore.Api.Tests;

public sealed class PostgreSqlReadinessTests
{
    [Fact]
    [Trait("Category", "RequiresDocker")]
    public async Task Ready_health_endpoint_connects_to_postgresql_without_creating_schema()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("hardware_store_test")
            .WithUsername("hardware_store_test")
            .WithPassword("test-only-password")
            .Build();

        await postgres.StartAsync();

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
            host.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<HardwareStoreDbContext>>();
                services.AddDbContext<HardwareStoreDbContext>(options =>
                    options.UseNpgsql(postgres.GetConnectionString()));
            }));

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/ready");

        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, responseBody);
    }
}
