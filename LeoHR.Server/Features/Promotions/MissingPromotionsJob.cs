namespace LeoHR.Server.Features.Promotions;

public class MissingPromotionsJob(IServiceScopeFactory scopeFactory, ILogger<MissingPromotionsJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan interval = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(interval);
        do
        {
            await SyncPromotions();
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SyncPromotions()
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IPromotionsService promotionsService = scope.ServiceProvider.GetRequiredService<IPromotionsService>();
            int deleted = await promotionsService.DeleteLeaversPromotions();
            logger.LogInformation("Deleted {Count} open promotions of employees who left", deleted);
            int created = await promotionsService.CreateMissingPromotions();
            logger.LogInformation("Created {Count} missing promotions", created);
        }
        catch (Exception ex)
        {
            // Retried on the next tick; a failed run must not stop the host.
            logger.LogError(ex, "Syncing promotions with active employees failed");
        }
    }
}
