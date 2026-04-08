using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector estruturado por dominio com mock inicial.
/// TODO: implementar scraping/API real especifica do dominio.
/// </summary>
public class VipLeiloesConnector : BaseLotConnector
{
    public VipLeiloesConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger<VipLeiloesConnector> logger)
        : base(httpClientFactory, options, logger)
    {
    }

    public override string Name => "VipLeiloes";

    public override IReadOnlyCollection<string> SupportedDomains =>
    [
        "vipleiloes.com.br",
        "www.vipleiloes.com.br"
    ];

    public override Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken)
    {
        // TODO(domain): substituir mock por consulta HTTP real com parser dedicado.
        var raw = BuildMockRawLots("vipleiloes", "Vip Leiloes");
        return Task.FromResult(raw);
    }

    public override Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        // TODO(domain): ajustar mapeamento com campos reais da origem.
        var map = EnsureDictionary(raw);
        var parsed = BuildProviderLot(map, "vipleiloes", "Vip Leiloes");
        return Task.FromResult(parsed);
    }
}
