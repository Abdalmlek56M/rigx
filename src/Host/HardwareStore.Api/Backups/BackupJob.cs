/// <summary>Every hour, runs the daily backup when it is due (see <see cref="BackupService.RunScheduledIfDueAsync"/>).</summary>
public sealed class BackupJob(IServiceScopeFactory scopeFactory, ILogger<BackupJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<BackupService>().RunScheduledIfDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Scheduled backup failed."); }
        }
    }
}
