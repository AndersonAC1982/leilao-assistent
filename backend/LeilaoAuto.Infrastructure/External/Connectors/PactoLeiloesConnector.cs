using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector estruturado por domínio.
/// TODO: implementar scraping/API real específica do domínio.
/// Enquanto isso, retorna vazio (sem mock em runtime).
/// </summary>
public class PactoLeiloesConnector : BaseLotConnector
{
    public PactoLeiloesConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger<PactoLeiloesConnector> logger)
        : base(httpClientFactory, options, logger)
    {
    }

    public override string Name => "PactoLeiloes";

    public override IReadOnlyCollection<string> SupportedDomains =>
    [
        "pactoleiloes.com.br",
        "www.pactoleiloes.com.br"
    ];

    public override Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken)
    {
        Logger.LogInformation("PactoLeiloes connector is not implemented yet. Returning empty result.");
        return Task.FromResult<IReadOnlyList<object>>([]);
    }

    public override Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        // TODO(domain): ajustar mapeamento com campos reais da origem.
        var map = EnsureDictionary(raw);
        var parsed = BuildProviderLot(map, "pactoleiloes", "Pacto Leiloes");
        return Task.FromResult(parsed);
    }
}
