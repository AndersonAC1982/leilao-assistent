using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector funcional fim-a-fim: busca raw (HTTP com fallback mock), parse e validacao de lotUrl.
/// </summary>
public class SuperbidConnector : BaseLotConnector
{
    private readonly AuctionProviderOptions _options;

    public SuperbidConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger<SuperbidConnector> logger)
        : base(httpClientFactory, options, logger)
    {
        _options = options.Value;
    }

    public override string Name => "Superbid";

    public override IReadOnlyCollection<string> SupportedDomains =>
    [
        "superbid.com.br",
        "www.superbid.com.br"
    ];

    public override async Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken)
    {
        if (_options.MockMode)
        {
            return BuildMockRawLots("superbid", "Superbid");
        }

        try
        {
            var endpoint = string.IsNullOrWhiteSpace(_options.LotsEndpoint) ? "/api/lots" : _options.LotsEndpoint;
            var raw = await FetchRawArrayAsync(endpoint, cancellationToken);
            if (raw.Count > 0)
            {
                return raw;
            }
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Superbid connector failed in remote search. Falling back to structured mock.");
        }

        return BuildMockRawLots("superbid", "Superbid");
    }

    public override Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        // Domain-specific parse is intentionally isolated here.
        var map = EnsureDictionary(raw);
        var parsed = BuildProviderLot(map, "superbid", "Superbid");
        return Task.FromResult(parsed);
    }

    public override bool ValidateLotUrl(string? url)
    {
        if (!base.ValidateLotUrl(url) || string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        if (!host.Equals("superbid.net", StringComparison.Ordinal)
            && !host.EndsWith(".superbid.net", StringComparison.Ordinal)
            && !host.Equals("superbid.com.br", StringComparison.Ordinal)
            && !host.EndsWith(".superbid.com.br", StringComparison.Ordinal))
        {
            return false;
        }

        var path = uri.AbsolutePath.TrimEnd('/').ToLowerInvariant();
        if (!path.StartsWith("/oferta/", StringComparison.Ordinal))
        {
            return false;
        }

        if (path is "/oferta" or "/oferta/lista" or "/busca" or "/lotes")
        {
            return false;
        }

        var lastSegment = path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        return Regex.IsMatch(lastSegment, @"-\d{4,}$", RegexOptions.CultureInvariant);
    }
}
