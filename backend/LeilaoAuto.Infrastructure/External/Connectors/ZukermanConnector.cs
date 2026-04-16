using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector estruturado por domínio.
/// TODO: implementar scraping/API real específica do domínio.
/// Enquanto isso, retorna vazio (sem mock em runtime).
/// </summary>
public class ZukermanConnector : BaseLotConnector
{
    public ZukermanConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger<ZukermanConnector> logger)
        : base(httpClientFactory, options, logger)
    {
    }

    public override string Name => "Zukerman";

    public override IReadOnlyCollection<string> SupportedDomains =>
    [
        "zukerman.com.br",
        "www.zukerman.com.br"
    ];

    public override Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Zukerman connector is not implemented yet. Returning empty result.");
        return Task.FromResult<IReadOnlyList<object>>([]);
    }

    public override Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        // TODO(domain): ajustar mapeamento com campos reais da origem.
        var map = EnsureDictionary(raw);
        var parsed = BuildProviderLot(map, "zukerman", "Zukerman Leiloes");
        return Task.FromResult(parsed);
    }
}
