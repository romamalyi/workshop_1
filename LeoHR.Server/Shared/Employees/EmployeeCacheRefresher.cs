using LeoHR.Server.Shared.FeatureFlags;

using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;

namespace LeoHR.Server.Shared.Employees;

public sealed class EmployeeCacheRefresher(
    IServiceScopeFactory scopeFactory,
    IFeatureManager features,
    IOptions<EmployeeApiOptions> options,
    ILogger<EmployeeCacheRefresher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await features.IsEnabledAsync(FeatureFlagNames.EmployeeDataCache))
            return;

        // Half the cache lifetime: the list is replaced before it expires, so no request waits on a cold fetch.
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.CacheExpirationMinutes) / 2.0);
        using PeriodicTimer timer = new(interval);
        do
        {
            await RefreshAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IEmployeeApiClient>().RefreshAllAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Retried on the next tick; meanwhile requests keep using the current or stale list.
            logger.LogWarning(ex, "Refreshing the employee cache failed");
        }
    }
}
