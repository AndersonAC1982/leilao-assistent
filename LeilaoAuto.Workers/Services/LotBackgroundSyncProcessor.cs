using System.Text.Json;
using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Common;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Workers.Configuration;
using Microsoft.Extensions.Options;
using Polly;

namespace LeilaoAuto.Workers.Services;

public class LotBackgroundSyncProcessor : ILotBackgroundSyncProcessor
{
    private readonly IConnectorRegistry _connectorRegistry;
    private readonly IAuctionLotRepository _auctionLotRepository;
    private readonly IConnectorExecutionLogRepository _connectorExecutionLogRepository;
    private readonly ILotAnalyticsRepository _lotAnalyticsRepository;
    private readonly ILotAnalyticsComputationService _lotAnalyticsComputationService;
    private readonly WorkerOptions _options;
    private readonly ILogger<LotBackgroundSyncProcessor> _logger;

    public LotBackgroundSyncProcessor(
        IConnectorRegistry connectorRegistry,
        IAuctionLotRepository auctionLotRepository,
        IConnectorExecutionLogRepository connectorExecutionLogRepository,
        ILotAnalyticsRepository lotAnalyticsRepository,
        ILotAnalyticsComputationService lotAnalyticsComputationService,
        IOptions<WorkerOptions> options,
        ILogger<LotBackgroundSyncProcessor> logger)
    {
        _connectorRegistry = connectorRegistry;
        _auctionLotRepository = auctionLotRepository;
        _connectorExecutionLogRepository = connectorExecutionLogRepository;
        _lotAnalyticsRepository = lotAnalyticsRepository;
        _lotAnalyticsComputationService = lotAnalyticsComputationService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LotSyncCycleResult> RunCycleAsync(CancellationToken cancellationToken)
    {
        var connectors = _connectorRegistry.GetAll();
        if (connectors.Count == 0)
        {
            _logger.LogWarning("No connectors registered for background processing.");
            return new LotSyncCycleResult(0, 0, 0, 0, 0, []);
        }

        var results = new List<ConnectorSyncResult>(connectors.Count);
        foreach (var connector in connectors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await ProcessConnectorAsync(connector, cancellationToken);
            results.Add(result);
        }

        var analyticsUpdated = await RecalculateAnalyticsAsync(cancellationToken);

        return new LotSyncCycleResult(
            ConnectorsProcessed: connectors.Count,
            RecordsRead: results.Sum(item => item.RecordsRead),
            RecordsSaved: results.Sum(item => item.RecordsSaved),
            AnalyticsUpdated: analyticsUpdated,
            Failures: results.Count(item => !item.Success),
            ConnectorResults: results);
    }

    private async Task<ConnectorSyncResult> ProcessConnectorAsync(ILotConnector connector, CancellationToken cancellationToken)
    {
        var executedAt = DateTime.UtcNow;
        var filters = new LotSearchFilterRequest();
        var safeRetryCount = Math.Max(1, _options.ConnectorRetryCount);
        var retryDelaySeconds = Math.Max(1, _options.ConnectorRetryDelaySeconds);

        var retryPolicy = Policy<IReadOnlyList<object>>
            .Handle<Exception>()
            .WaitAndRetryAsync(
                safeRetryCount,
                retryAttempt => TimeSpan.FromSeconds(retryDelaySeconds * retryAttempt),
                (outcome, delay, retryCount, _) =>
                {
                    _logger.LogWarning(
                        outcome.Exception,
                        "Connector {Connector} failed on attempt {Attempt}. Retrying in {DelaySeconds}s.",
                        connector.Name,
                        retryCount,
                        delay.TotalSeconds);
                });

        try
        {
            var rawLots = (await retryPolicy.ExecuteAsync(
                    (token) => connector.SearchAsync(filters, token),
                    cancellationToken))
                .ToList();

            var parsedLots = new List<AuctionLot>(rawLots.Count);
            var discarded = 0;

            foreach (var raw in rawLots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ProviderLotDto? parsed;
                try
                {
                    parsed = await connector.ParseAsync(raw, cancellationToken);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Connector {Connector} failed to parse one lot payload.", connector.Name);
                    discarded++;
                    continue;
                }

                if (parsed is null || !connector.ValidateLotUrl(parsed.LotUrl))
                {
                    discarded++;
                    continue;
                }

                try
                {
                    parsedLots.Add(new AuctionLot(
                        parsed.ExternalId,
                        parsed.Auctioneer,
                        parsed.LotNumber,
                        parsed.Make,
                        parsed.Model,
                        parsed.Year,
                        parsed.VehicleType,
                        parsed.Uf,
                        parsed.VehicleCondition,
                        parsed.Status,
                        parsed.LotUrl,
                        parsed.CurrentBid,
                        parsed.FinalPrice,
                        parsed.AppraisedValue,
                        parsed.StartsAt,
                        parsed.EndsAt));
                }
                catch (DomainRuleException exception)
                {
                    _logger.LogWarning(exception, "Connector {Connector} returned an invalid lot URL and the lot was discarded.", connector.Name);
                    discarded++;
                }
            }

            if (parsedLots.Count > 0)
            {
                await _auctionLotRepository.UpsertRangeAsync(parsedLots, cancellationToken);
                await _auctionLotRepository.SaveChangesAsync(cancellationToken);
            }

            var message = $"Processed {parsedLots.Count} lot(s) from {rawLots.Count} raw payload(s). Discarded {discarded}.";
            await WriteConnectorLogAsync(
                connector.Name,
                executedAt,
                success: true,
                recordsRead: rawLots.Count,
                recordsSaved: parsedLots.Count,
                message,
                new
                {
                    discarded,
                    retryCount = safeRetryCount,
                    supportedDomains = connector.SupportedDomains
                },
                cancellationToken);

            return new ConnectorSyncResult(
                connector.Name,
                Success: true,
                RecordsRead: rawLots.Count,
                RecordsSaved: parsedLots.Count,
                Discarded: discarded,
                Message: message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var message = $"Connector failed after retries: {exception.Message}";
            _logger.LogError(exception, "Connector {Connector} failed after retries.", connector.Name);

            await WriteConnectorLogAsync(
                connector.Name,
                executedAt,
                success: false,
                recordsRead: 0,
                recordsSaved: 0,
                message,
                new { error = exception.Message },
                cancellationToken);

            return new ConnectorSyncResult(
                connector.Name,
                Success: false,
                RecordsRead: 0,
                RecordsSaved: 0,
                Discarded: 0,
                Message: message);
        }
    }

    private async Task<int> RecalculateAnalyticsAsync(CancellationToken cancellationToken)
    {
        var closedLots = await _auctionLotRepository.GetClosedLotsAsync(cancellationToken);
        var modelAverages = _lotAnalyticsComputationService.GroupAndCalculateModelPrices(closedLots);

        if (modelAverages.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var average in modelAverages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existing = await _lotAnalyticsRepository.GetByNormalizedModelAsync(average.ComparableModel, cancellationToken);
            if (existing is null)
            {
                await _lotAnalyticsRepository.AddAsync(
                    new LotAnalytics(
                        average.ComparableModel,
                        average.AveragePrice,
                        average.MinPrice,
                        average.MaxPrice,
                        average.Quantity,
                        now),
                    cancellationToken);
                continue;
            }

            existing.Refresh(
                average.AveragePrice,
                average.MinPrice,
                average.MaxPrice,
                average.Quantity,
                now);
            _lotAnalyticsRepository.Update(existing);
        }

        await _lotAnalyticsRepository.SaveChangesAsync(cancellationToken);
        return modelAverages.Count;
    }

    private async Task WriteConnectorLogAsync(
        string connectorName,
        DateTime executedAt,
        bool success,
        int recordsRead,
        int recordsSaved,
        string message,
        object payload,
        CancellationToken cancellationToken)
    {
        try
        {
            var payloadJson = JsonSerializer.Serialize(payload);
            var log = new ConnectorExecutionLog(
                connectorName,
                executedAt,
                success,
                recordsRead,
                recordsSaved,
                message,
                payloadJson);

            await _connectorExecutionLogRepository.AddAsync(log, cancellationToken);
            await _connectorExecutionLogRepository.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to persist ConnectorExecutionLog for {Connector}.", connectorName);
        }
    }
}
