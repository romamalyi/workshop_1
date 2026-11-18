namespace LeoHR.Server.Features.Promotions;

public class PromotionReadyEmailJob(IServiceScopeFactory scopeFactory, ILogger<PromotionReadyEmailJob> logger)
    : BackgroundService
{
    // Polled rather than sent where the status changes: Ready is set in many places, several inside transactions.
    private static readonly TimeSpan interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(interval);
        do
        {
            await NotifyReadyPromotions();
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task NotifyReadyPromotions()
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            int sent = await scope.ServiceProvider.GetRequiredService<IPromotionsService>().NotifyReadyPromotions();
            if (sent > 0) logger.LogInformation("Emailed HR about {Count} promotions ready to send", sent);
        }
        catch (Exception ex)
        {
            // Retried on the next tick; a failed run must not stop the host.
            logger.LogError(ex, "Emailing HR about promotions ready to send failed");
        }
    }
}
