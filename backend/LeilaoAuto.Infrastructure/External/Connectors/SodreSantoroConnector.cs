using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector estruturado por dominio com mock inicial.
/// TODO: implementar scraping/API real especifica do dominio.
/// </summary>
public class SodreSantoroConnector : BaseLotConnector
{
    public SodreSantoroConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger<SodreSantoroConnector> logger)
        : base(httpClientFactory, options, logger)
    {
    }

    public override string Name => "SodreSantoro";

    public override IReadOnlyCollection<string> SupportedDomains =>
    [
        "sodresantoro.com.br",
        "www.sodresantoro.com.br"
    ];

    public override Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken)
    {
        // TODO(domain): substituir mock por consulta HTTP real com parser dedicado.
        var raw = BuildMockRawLots("sodresantoro", "Sodre Santoro");
        return Task.FromResult(raw);
    }

    public override Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        // TODO(domain): ajustar mapeamento com campos reais da origem.
        var map = EnsureDictionary(raw);
        var parsed = BuildProviderLot(map, "sodresantoro", "Sodre Santoro");
        return Task.FromResult(parsed);
    }

    public override bool ValidateLotUrl(string? url)
    {
        if (!base.ValidateLotUrl(url) || string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        if (!host.Equals("sodresantoro.com.br", StringComparison.Ordinal)
            && !host.EndsWith(".sodresantoro.com.br", StringComparison.Ordinal))
        {
            return false;
        }

        var path = uri.AbsolutePath.TrimEnd('/').ToLowerInvariant();
        if (path is "/" or "/veiculos" or "/veiculos/lotes" or "/lotes")
        {
            return false;
        }

        return path.Contains("/lote", StringComparison.Ordinal)
               || path.Contains("/oferta", StringComparison.Ordinal)
               || path.Contains("/detalhe", StringComparison.Ordinal);
    }
}
