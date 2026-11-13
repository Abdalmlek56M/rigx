using HardwareStore.Common.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class DatabaseHealthCheck(
    HardwareStoreDbContext dbContext,
    ILogger<DatabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            await dbContext.Database.CloseConnectionAsync();
            return HealthCheckResult.Healthy("PostgreSQL accepts connections.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "PostgreSQL readiness check failed.");
            return HealthCheckResult.Unhealthy("PostgreSQL is unavailable.", exception);
        }
    }
}
