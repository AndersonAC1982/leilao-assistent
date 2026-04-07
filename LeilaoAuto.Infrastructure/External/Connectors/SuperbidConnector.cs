using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
}
