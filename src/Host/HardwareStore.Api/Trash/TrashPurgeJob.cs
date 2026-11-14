/// <summary>Every 6 hours, purges trashed items that are past the 30-day retention (see <see cref="TrashService.PurgeExpiredAsync"/>).</summary>
public sealed class TrashPurgeJob(IServiceScopeFactory scopeFactory, ILogger<TrashPurgeJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<TrashService>().PurgeExpiredAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Trash purge failed."); }
        }
    }
}
