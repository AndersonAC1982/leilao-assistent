using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace LeilaoAuto.Infrastructure.External;

/// <summary>
/// Orquestra todos os conectores registrados e consolida lotes validos.
/// </summary>
public class AuctionProviderClient : IAuctionProviderClient
{
    private static readonly IReadOnlyDictionary<string, string> SourceAliasToConnectorName =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["superbid"] = "Superbid",
            ["sodresantoro"] = "SodreSantoro",
            ["sodre"] = "SodreSantoro",
            ["vipleiloes"] = "VipLeiloes",
            ["vip"] = "VipLeiloes",
            ["freitas"] = "Freitas",
            ["zukerman"] = "Zukerman",
            ["zuk"] = "Zukerman",
            ["megaleiloes"] = "MegaLeiloes",
            ["mega"] = "MegaLeiloes",
            ["pactoleiloes"] = "PactoLeiloes",
            ["pacto"] = "PactoLeiloes",
            ["milanleiloes"] = "MilanLeiloes",
            ["milan"] = "MilanLeiloes"
        };

    private readonly IConnectorRegistry _connectorRegistry;
    private readonly IConnectorExecutionLogRepository _connectorExecutionLogRepository;
    private readonly ILogger<AuctionProviderClient> _logger;

    public AuctionProviderClient(
        IConnectorRegistry connectorRegistry,
        IConnectorExecutionLogRepository connectorExecutionLogRepository,
        ILogger<AuctionProviderClient> logger)
    {
        _connectorRegistry = connectorRegistry;
        _connectorExecutionLogRepository = connectorExecutionLogRepository;
        _logger = logger;
    }

    public Task<IReadOnlyList<ProviderLotDto>> FetchLatestLotsAsync(CancellationToken cancellationToken)
    {
        return FetchLatestLotsAsync(new LotSearchFilterRequest(), activeSources: null, cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderLotDto>> FetchLatestLotsAsync(
        LotSearchFilterRequest filters,
        IReadOnlyCollection<string>? activeSources,
        CancellationToken cancellationToken)
    {
        var connectors = ResolveConnectors(activeSources);
        if (connectors.Count == 0)
        {
            _logger.LogWarning("No lot connectors selected for current scanner execution.");
            return [];
        }

        var effectiveFilter = filters ?? new LotSearchFilterRequest();
        var tasks = connectors.Select(connector => FetchFromConnectorAsync(connector, effectiveFilter, cancellationToken));
        var reports = await Task.WhenAll(tasks);

        await PersistConnectorObservabilityAsync(reports, cancellationToken);

        var merged = reports
            .SelectMany(report => report.Lots)
            .GroupBy(item => item.ExternalId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        _logger.LogInformation(
            "Connector aggregation completed with {Count} unique lots from {ConnectorCount} connectors. Sources={Sources}.",
            merged.Count,
            connectors.Count,
            string.Join(", ", connectors.Select(connector => connector.Name)));

        return merged;
    }

    private IReadOnlyList<ILotConnector> ResolveConnectors(IReadOnlyCollection<string>? activeSources)
    {
        var allConnectors = _connectorRegistry.GetAll();
        if (activeSources is null || activeSources.Count == 0)
        {
            return allConnectors;
        }

        var selectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in activeSources)
        {
            var normalizedSource = NormalizeToken(source);
            if (string.IsNullOrWhiteSpace(normalizedSource))
            {
                continue;
            }

            if (SourceAliasToConnectorName.TryGetValue(normalizedSource, out var mappedConnectorName))
            {
                selectedNames.Add(mappedConnectorName);
                continue;
            }

            foreach (var connector in allConnectors)
            {
                if (MatchesSource(connector, normalizedSource))
                {
                    selectedNames.Add(connector.Name);
                }
            }
        }

        if (selectedNames.Count == 0)
        {
            return [];
        }

        return allConnectors
            .Where(connector => selectedNames.Contains(connector.Name))
            .ToList();
    }

    private static bool MatchesSource(ILotConnector connector, string normalizedSource)
    {
        var connectorToken = NormalizeToken(connector.Name);
        if (connectorToken.Contains(normalizedSource, StringComparison.OrdinalIgnoreCase)
            || normalizedSource.Contains(connectorToken, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return connector.SupportedDomains.Any(domain =>
        {
            var normalizedDomain = NormalizeToken(domain);
            return normalizedDomain.Contains(normalizedSource, StringComparison.OrdinalIgnoreCase)
                   || normalizedSource.Contains(normalizedDomain, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static string NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Trim()
            .Normalize(NormalizationForm.FormD);

        var buffer = new char[normalized.Length];
        var index = 0;

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                buffer[index++] = char.ToLowerInvariant(ch);
            }
        }

        return new string(buffer, 0, index);
    }

    private async Task<ConnectorFetchReport> FetchFromConnectorAsync(
        ILotConnector connector,
        LotSearchFilterRequest filters,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var rawItems = await connector.SearchAsync(filters, cancellationToken);
            var parsedLots = new List<ProviderLotDto>(rawItems.Count);
            var parseDiscarded = 0;
            var invalidUrlDiscarded = 0;

            foreach (var raw in rawItems)
            {
                var parsed = await connector.ParseAsync(raw, cancellationToken);
                if (parsed is null)
                {
                    parseDiscarded++;
                    continue;
                }

                if (parsed.Status == LotStatus.Confirmed && !connector.ValidateLotUrl(parsed.LotUrl))
                {
                    invalidUrlDiscarded++;
                    continue;
                }

                if (!connector.ValidateLotUrl(parsed.LotUrl))
                {
                    invalidUrlDiscarded++;
                    continue;
                }

                parsedLots.Add(parsed);
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Connector {Connector} completed. RawItems={RawItems}, ParsedLots={ParsedLots}, ParseDiscarded={ParseDiscarded}, InvalidUrlDiscarded={InvalidUrlDiscarded}, ElapsedMs={ElapsedMs}.",
                connector.Name,
                rawItems.Count,
                parsedLots.Count,
                parseDiscarded,
                invalidUrlDiscarded,
                stopwatch.ElapsedMilliseconds);

            return new ConnectorFetchReport(
                connector.Name,
                rawItems.Count,
                parsedLots.Count,
                parseDiscarded,
                invalidUrlDiscarded,
                true,
                null,
                stopwatch.ElapsedMilliseconds,
                parsedLots);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                exception,
                "Connector {Connector} failed during fetch. ElapsedMs={ElapsedMs}.",
                connector.Name,
                stopwatch.ElapsedMilliseconds);

            return new ConnectorFetchReport(
                connector.Name,
                RawItems: 0,
                ParsedLots: 0,
                ParseDiscarded: 0,
                InvalidUrlDiscarded: 0,
                Success: false,
                Error: exception.Message,
                ElapsedMs: stopwatch.ElapsedMilliseconds,
                Lots: []);
        }
    }

    private async Task PersistConnectorObservabilityAsync(
        IReadOnlyCollection<ConnectorFetchReport> reports,
        CancellationToken cancellationToken)
    {
        if (reports.Count == 0)
        {
            return;
        }

        var executedAt = DateTime.UtcNow;

        foreach (var report in reports)
        {
            var payload = JsonSerializer.Serialize(new
            {
                connector = report.ConnectorName,
                report.RawItems,
                report.ParsedLots,
                report.ParseDiscarded,
                report.InvalidUrlDiscarded,
                report.Success,
                report.ElapsedMs,
                report.Error
            });

            await _connectorExecutionLogRepository.AddAsync(
                new ConnectorExecutionLog(
                    connectorName: report.ConnectorName,
                    executedAt: executedAt,
                    success: report.Success,
                    recordsRead: report.RawItems,
                    recordsSaved: report.ParsedLots,
                    message: report.Success
                        ? $"Connector run completed. InvalidUrlDiscarded={report.InvalidUrlDiscarded}; ParseDiscarded={report.ParseDiscarded}."
                        : report.Error,
                    payloadJson: payload),
                cancellationToken);
        }

        try
        {
            await _connectorExecutionLogRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to persist connector observability logs.");
        }
    }

    private sealed record ConnectorFetchReport(
        string ConnectorName,
        int RawItems,
        int ParsedLots,
        int ParseDiscarded,
        int InvalidUrlDiscarded,
        bool Success,
        string? Error,
        long ElapsedMs,
        IReadOnlyList<ProviderLotDto> Lots);
}
