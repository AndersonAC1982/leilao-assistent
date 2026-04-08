using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector estruturado por dominio com mock inicial.
/// TODO: implementar scraping/API real especifica do dominio.
/// </summary>
public class MegaLeiloesConnector : BaseLotConnector
{
    public MegaLeiloesConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger<MegaLeiloesConnector> logger)
        : base(httpClientFactory, options, logger)
    {
    }

    public override string Name => "MegaLeiloes";

    public override IReadOnlyCollection<string> SupportedDomains =>
    [
        "megaleiloes.com.br",
        "www.megaleiloes.com.br"
    ];

    public override Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken)
    {
        // TODO(domain): substituir mock por consulta HTTP real com parser dedicado.
        var raw = BuildMockRawLots("megaleiloes", "Mega Leiloes");
        return Task.FromResult(raw);
    }

    public override Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        // TODO(domain): ajustar mapeamento com campos reais da origem.
        var map = EnsureDictionary(raw);
        var parsed = BuildProviderLot(map, "megaleiloes", "Mega Leiloes");
        return Task.FromResult(parsed);
    }
}
