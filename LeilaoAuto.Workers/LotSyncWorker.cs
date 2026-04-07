using LeilaoAuto.Application.Abstractions.Services;

namespace LeilaoAuto.Workers;

public class LotSyncWorker : BackgroundService
{
    private readonly ILogger<LotSyncWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public LotSyncWorker(
        ILogger<LotSyncWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue<int?>("Worker:SyncIntervalSeconds") ?? 180;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        _logger.LogInformation("LotSyncWorker iniciado com intervalo de {IntervalSeconds} segundos.", intervalSeconds);

        do
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var lotService = scope.ServiceProvider.GetRequiredService<ILotService>();
                var synced = await lotService.RefreshAsync(stoppingToken);
                _logger.LogInformation("Sincronização concluída com {Synced} lote(s) processado(s).", synced);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Falha ao sincronizar lotes no worker.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

