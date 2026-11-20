using HardwareStore.Modules.Identity;
using HardwareStore.Modules.Storefront;

/// <summary>Every 10 minutes, tells waiting notify-me customers their model is back (see <see cref="BackInStockNotifier"/>).</summary>
public sealed class BackInStockJob(IServiceScopeFactory scopeFactory, ILogger<BackInStockJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<BackInStockNotifier>().RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Back-in-stock matching failed."); }
        }
    }
}

/// <summary>One run of the back-in-stock job: matches waiting requests to stock and emails the ones that gave an email and notifies
/// the ones sent from an account, once.</summary>
public sealed class BackInStockNotifier(NotifyMeService notifyMe, IdentityEmailOutboxWriter outbox, CustomerNotificationService notifications, OrderLinks links)
{
    /// <summary>Returns how many requests found their item in this run.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var matches = await notifyMe.MatchBackInStockAsync(cancellationToken);
        foreach (var match in matches.Where(match => match.Email is not null))
            await outbox.EnqueueAsync(IdentityEmailOutboxWriter.BackInStockKind, match.UserAccountId,
                new BackInStockEmailMessage(match.Email!, match.Name, match.ItemName, links.ItemUrl(match.ItemCode)), cancellationToken);
        foreach (var match in matches.Where(match => match.UserAccountId is not null))
            await notifications.AddBackInStockAsync(match.UserAccountId!.Value, match.ItemCode, match.ItemName, cancellationToken);
        return matches.Count;
    }
}
