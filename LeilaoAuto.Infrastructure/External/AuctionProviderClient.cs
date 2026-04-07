using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LeilaoAuto.Infrastructure.External;

/// <summary>
/// Orquestra todos os conectores registrados e consolida lotes validos.
/// </summary>
public class AuctionProviderClient : IAuctionProviderClient
{
    private readonly IConnectorRegistry _connectorRegistry;
    private readonly ILogger<AuctionProviderClient> _logger;

    public AuctionProviderClient(
        IConnectorRegistry connectorRegistry,
        ILogger<AuctionProviderClient> logger)
    {
        _connectorRegistry = connectorRegistry;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProviderLotDto>> FetchLatestLotsAsync(CancellationToken cancellationToken)
    {
        var connectors = _connectorRegistry.GetAll();
        if (connectors.Count == 0)
        {
            _logger.LogWarning("No lot connectors registered.");
            return [];
        }

        var filter = new LotSearchFilterRequest();
        var tasks = connectors.Select(connector => FetchFromConnectorAsync(connector, filter, cancellationToken));
        var allResults = await Task.WhenAll(tasks);

        var merged = allResults
            .SelectMany(result => result)
            .GroupBy(item => item.ExternalId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        _logger.LogInformation("Connector aggregation completed with {Count} unique lots from {ConnectorCount} connectors.", merged.Count, connectors.Count);
        return merged;
    }

    private async Task<IReadOnlyList<ProviderLotDto>> FetchFromConnectorAsync(
        ILotConnector connector,
        LotSearchFilterRequest filters,
        CancellationToken cancellationToken)
    {
        try
        {
            var rawItems = await connector.SearchAsync(filters, cancellationToken);
            var parsedLots = new List<ProviderLotDto>(rawItems.Count);

            foreach (var raw in rawItems)
            {
                var parsed = await connector.ParseAsync(raw, cancellationToken);
                if (parsed is null)
                {
                    continue;
                }

                if (parsed.Status == LotStatus.Confirmed && !connector.ValidateLotUrl(parsed.LotUrl))
                {
                    continue;
                }

                if (!connector.ValidateLotUrl(parsed.LotUrl))
                {
                    continue;
                }

                parsedLots.Add(parsed);
            }

            _logger.LogInformation("Connector {Connector} returned {Parsed}/{Raw} parsed lots.", connector.Name, parsedLots.Count, rawItems.Count);
            return parsedLots;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Connector {Connector} failed during fetch.", connector.Name);
            return [];
        }
    }
}
