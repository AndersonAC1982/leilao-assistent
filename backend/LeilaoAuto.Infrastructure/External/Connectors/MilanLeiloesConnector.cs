using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector estruturado por domínio.
/// TODO: implementar scraping/API real específica do domínio.
/// Enquanto isso, retorna vazio (sem mock em runtime).
/// </summary>
public class MilanLeiloesConnector : BaseLotConnector
{
    public MilanLeiloesConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger<MilanLeiloesConnector> logger)
        : base(httpClientFactory, options, logger)
    {
    }

    public override string Name => "MilanLeiloes";

    public override IReadOnlyCollection<string> SupportedDomains =>
    [
        "milanleiloes.com.br",
        "www.milanleiloes.com.br"
    ];

    public override Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken)
    {
        Logger.LogInformation("MilanLeiloes connector is not implemented yet. Returning empty result.");
        return Task.FromResult<IReadOnlyList<object>>([]);
    }

    public override Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        // TODO(domain): ajustar mapeamento com campos reais da origem.
        var map = EnsureDictionary(raw);
        var parsed = BuildProviderLot(map, "milanleiloes", "Milan Leiloes");
        return Task.FromResult(parsed);
    }
}
