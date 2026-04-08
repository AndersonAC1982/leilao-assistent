using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector estruturado por dominio com mock inicial.
/// TODO: implementar scraping/API real especifica do dominio.
/// </summary>
public class FreitasConnector : BaseLotConnector
{
    public FreitasConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger<FreitasConnector> logger)
        : base(httpClientFactory, options, logger)
    {
    }

    public override string Name => "Freitas";

    public override IReadOnlyCollection<string> SupportedDomains =>
    [
        "freitasleiloes.com.br",
        "www.freitasleiloes.com.br"
    ];

    public override Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken)
    {
        // TODO(domain): substituir mock por consulta HTTP real com parser dedicado.
        var raw = BuildMockRawLots("freitas", "Freitas Leiloes");
        return Task.FromResult(raw);
    }

    public override Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        // TODO(domain): ajustar mapeamento com campos reais da origem.
        var map = EnsureDictionary(raw);
        var parsed = BuildProviderLot(map, "freitas", "Freitas Leiloes");
        return Task.FromResult(parsed);
    }
}
