using HardwareStore.Modules.Retail;

/// <summary>Every hour, posts the recurring monthly expenses whose day has come (business time) — see <see cref="RecurringExpenseService"/>.</summary>
public sealed class RecurringExpenseJob(IServiceScopeFactory scopeFactory, ILogger<RecurringExpenseJob> logger) : BackgroundService
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
                var result = await scope.ServiceProvider.GetRequiredService<RecurringExpenseService>()
                    .PostDueAsync(scope.ServiceProvider.GetRequiredService<TimeZoneInfo>(), stoppingToken);
                if (result.Blocked > 0) logger.LogWarning("{Blocked} recurring expense(s) could not be posted (check the account balance).", result.Blocked);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Recurring expense posting failed."); }
        }
    }
}
