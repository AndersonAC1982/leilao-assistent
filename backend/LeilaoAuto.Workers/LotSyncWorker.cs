using LeilaoAuto.Workers.Configuration;
using LeilaoAuto.Workers.Services;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Workers;

public class LotSyncWorker : BackgroundService
{
    private readonly ILogger<LotSyncWorker> _logger;
    private readonly ILotBackgroundSyncProcessor _syncProcessor;
    private readonly WorkerOptions _options;

    public LotSyncWorker(
        ILogger<LotSyncWorker> logger,
        ILotBackgroundSyncProcessor syncProcessor,
        IOptions<WorkerOptions> options)
    {
        _logger = logger;
        _syncProcessor = syncProcessor;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("LotSyncWorker is disabled by configuration.");
            return;
        }

        var intervalSeconds = Math.Max(15, _options.SyncIntervalSeconds);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        _logger.LogInformation(
            "LotSyncWorker started. Interval={IntervalSeconds}s, RetryCount={RetryCount}, RetryDelay={RetryDelaySeconds}s.",
            intervalSeconds,
            Math.Max(1, _options.ConnectorRetryCount),
            Math.Max(1, _options.ConnectorRetryDelaySeconds));

        if (_options.RunOnStartup)
        {
            await RunCycleSafelyAsync(stoppingToken);
        }

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleSafelyAsync(stoppingToken);
        }
    }

    private async Task RunCycleSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _syncProcessor.RunCycleAsync(cancellationToken);
            _logger.LogInformation(
                "Sync cycle finished. Connectors={Connectors}, Read={Read}, Saved={Saved}, AnalyticsUpdated={AnalyticsUpdated}, Failures={Failures}.",
                result.ConnectorsProcessed,
                result.RecordsRead,
                result.RecordsSaved,
                result.AnalyticsUpdated,
                result.Failures);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled error in sync cycle. Worker remains alive for next execution.");
        }
    }
}
