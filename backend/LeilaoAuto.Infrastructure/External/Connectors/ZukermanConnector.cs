using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector estruturado por dominio com mock inicial.
/// TODO: implementar scraping/API real especifica do dominio.
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
        // TODO(domain): substituir mock por consulta HTTP real com parser dedicado.
        var raw = BuildMockRawLots("zukerman", "Zukerman Leiloes");
        return Task.FromResult(raw);
    }

    public override Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        // TODO(domain): ajustar mapeamento com campos reais da origem.
        var map = EnsureDictionary(raw);
        var parsed = BuildProviderLot(map, "zukerman", "Zukerman Leiloes");
        return Task.FromResult(parsed);
    }
}
