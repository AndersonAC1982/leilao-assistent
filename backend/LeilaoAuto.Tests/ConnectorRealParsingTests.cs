using FluentAssertions;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Infrastructure.External;
using LeilaoAuto.Infrastructure.External.Connectors;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Tests;

public class ConnectorRealParsingTests
{
    [Fact]
    public async Task Vip_Search_Should_Parse_Catalog_And_Return_Exact_Lot_Url()
    {
        const string catalogHtml = """
            <html>
              <body>
                <a class="anc-body" href="/evento/anuncio/ford-ka-sel-10-sd-136318">
                  <h3>FORD - KA SEL 1.0 SD</h3>
                  <span class="valor-atual">R$ 19.000,00</span>
                  <strong>Local:</strong> GO
                </a>
                <a class="anc-body" href="/evento/anuncio">invalido</a>
              </body>
            </html>
            """;

        var connector = new VipLeiloesConnector(
            new MapHttpClientFactory(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["https://www.vipleiloes.com.br"] = catalogHtml
            }),
            Options.Create(new AuctionProviderOptions()),
            NullLogger<VipLeiloesConnector>.Instance);

        var result = await connector.SearchAsync(
            new LotSearchFilterRequest { Make = "Ford" },
            CancellationToken.None);

        result.Should().HaveCount(1);
        var map = result[0].Should().BeAssignableTo<Dictionary<string, object?>>().Subject;
        var lotUrl = map["lotUrl"]?.ToString();
        lotUrl.Should().Be("https://www.vipleiloes.com.br/evento/anuncio/ford-ka-sel-10-sd-136318");
        connector.ValidateLotUrl(lotUrl).Should().BeTrue();
    }

    [Fact]
    public async Task Vip_Parse_Should_Read_Price_Status_And_Location()
    {
        const string lotUrl = "https://www.vipleiloes.com.br/evento/anuncio/ford-ka-sel-10-sd-136318";
        const string detailHtml = """
            <html>
              <body>
                <input id="anuncioId" value="75ac56f8-789e-4a18-b01a-b42400ce07b1" />
                <h1 class="detan-name detan-mob">FORD - KA SEL 1.0 SD</h1>
                <table>
                  <tr><th>Veículo</th><td>FORD - KA SEL 1.0 SD</td></tr>
                  <tr><th>Ano</th><td>2015 / 2016</td></tr>
                  <tr><th>Localização</th><td>AV. PERIMETRAL NORTE, 3442, GOIÂNIA, GO - CEP: 74445190</td></tr>
                  <tr><th>Final da placa</th><td>6 - GO</td></tr>
                  <tr><th>Procedência</th><td>Recuperado Financiamento (Conservado)</td></tr>
                </table>
                <span data-bind-valorInicial>10.000,00</span>
                <h2 data-bind-valorAtual>R$ 19.000,00</h2>
                <div data-bind-eventoEncerrado class="detan-timemsg d-none"><span>Leilão encerrado</span></div>
                <div class="offer-text"><p>Veículo em bom estado.</p></div>
              </body>
            </html>
            """;

        var connector = new VipLeiloesConnector(
            new MapHttpClientFactory(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [lotUrl] = detailHtml
            }),
            Options.Create(new AuctionProviderOptions()),
            NullLogger<VipLeiloesConnector>.Instance);

        var parsed = await connector.ParseAsync(
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["lotUrl"] = lotUrl
            },
            CancellationToken.None);

        parsed.Should().NotBeNull();
        parsed!.Make.Should().Be("FORD");
        parsed.Model.Should().Contain("KA SEL");
        parsed.Uf.Should().Be("GO");
        parsed.CurrentBid.Should().Be(19000m);
        parsed.Status.Should().Be(LotStatus.Active);
    }

    [Fact]
    public async Task Sodre_Parse_Should_Read_Vehicle_Details_With_Exact_Lot_Url()
    {
        const string lotUrl = "https://hml-web.sodresantoro.com.br/leilao/28060/lote/2714922/";
        const string detailHtml = """
            <html>
              <body>
                <div id="titleLot">CHEVROLET Corsa Sed Classic Super 1.6 MPFI VHC 8V 04/04</div>
                <span id="currentBid" class="valor">R$ 2.673</span>
                <div id="aditionalInfoLot_lot_address"><strong>Local do lote:</strong> AVENIDA RUI BARBOSA, 797 - PENÁPOLIS/SP</div>
                <div id="aditionalInfoLot_lot_number"><strong>Lote:</strong> AR0046</div>
                <div
                  id="lotInfoDetail"
                  data-params-lote-id="2714922"
                  data-params-segmento="veiculos"
                  data-params-categoria="Carros"
                  data-params-marca="Chevrolet"
                  data-params-modelo="Corsa Sed Classic Super 1.6 MPFI VHC 8V"
                  data-params-monta="Sem sinistro"
                  data-params-ano-fab="2004"
                  data-params-ano-modelo="2004"
                  data-params-descricao="GM CORSA CLASSIC, DEPÓSITO: AVENIDA RUI BARBOSA, 797 - PENÁPOLIS/SP">
                </div>
                <div id="detail_info_lot_description">GM CORSA CLASSIC, DEPÓSITO: AVENIDA RUI BARBOSA, 797 - PENÁPOLIS/SP</div>
              </body>
            </html>
            """;

        var connector = new SodreSantoroConnector(
            new MapHttpClientFactory(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [lotUrl] = detailHtml
            }),
            Options.Create(new AuctionProviderOptions()),
            NullLogger<SodreSantoroConnector>.Instance);

        var parsed = await connector.ParseAsync(
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["lotUrl"] = lotUrl
            },
            CancellationToken.None);

        parsed.Should().NotBeNull();
        parsed!.Make.Should().Be("CHEVROLET");
        parsed.Model.ToUpperInvariant().Should().Contain("CORSA");
        parsed.Uf.Should().Be("SP");
        parsed.CurrentBid.Should().Be(2673m);
        parsed.Status.Should().Be(LotStatus.Active);
        connector.ValidateLotUrl(parsed.LotUrl).Should().BeTrue();
    }

    [Fact]
    public async Task Sodre_Parse_Should_Discard_NonVehicle_Lots()
    {
        const string lotUrl = "https://hml-web.sodresantoro.com.br/leilao/28060/lote/2713353/";
        const string detailHtml = """
            <html>
              <body>
                <div id="titleLot">ROLO COMPACTADOR DYNAPAC CP221</div>
                <span id="currentBid" class="valor">R$ 30.000</span>
                <div id="lotInfoDetail"
                  data-params-lote-id="2713353"
                  data-params-segmento="materiais"
                  data-params-categoria="Terraplenagem"
                  data-params-marca="DYNAPAC"
                  data-params-modelo="CP221"
                  data-params-ano-fab="2020"
                  data-params-ano-modelo="2020">
                </div>
              </body>
            </html>
            """;

        var connector = new SodreSantoroConnector(
            new MapHttpClientFactory(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [lotUrl] = detailHtml
            }),
            Options.Create(new AuctionProviderOptions()),
            NullLogger<SodreSantoroConnector>.Instance);

        var parsed = await connector.ParseAsync(
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["lotUrl"] = lotUrl
            },
            CancellationToken.None);

        parsed.Should().BeNull();
    }

    [Fact]
    public async Task Sodre_Search_Should_Only_Return_Exact_Lot_Urls()
    {
        const string catalogUrl = "https://hml-web.sodresantoro.com.br/";
        const string seedLotUrl = "https://hml-web.sodresantoro.com.br/leilao/28060/lote/2714922/";
        const string catalogHtml = """
            <html>
              <body>
                <div class="lote" data-link="/leilao/28060/lote/2714922/">
                  <a class="lote-link" title="CHEVROLET Corsa Sed Classic Super 1.6 MPFI VHC 8V 04/04"></a>
                  <span class="valor">R$ 2.673</span>
                </div>
                <a href="/veiculos/lotes?lot_brand=gm">nao exato</a>
              </body>
            </html>
            """;

        const string lotHtml = """
            <html>
              <body>
                <select>
                  <option value="/leilao/28060/lote/2714922/">Leilão: 28060 - Lote: AR0046</option>
                  <option value="/veiculos/lotes?lot_brand=gm">inválido</option>
                </select>
              </body>
            </html>
            """;

        var connector = new SodreSantoroConnector(
            new MapHttpClientFactory(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [catalogUrl] = catalogHtml,
                [seedLotUrl] = lotHtml
            }),
            Options.Create(new AuctionProviderOptions()),
            NullLogger<SodreSantoroConnector>.Instance);

        var result = await connector.SearchAsync(new LotSearchFilterRequest(), CancellationToken.None);

        result.Should().NotBeEmpty();
        foreach (var item in result)
        {
            var map = item as Dictionary<string, object?>;
            var lotUrl = map is not null && map.TryGetValue("lotUrl", out var urlValue) ? urlValue?.ToString() : null;
            connector.ValidateLotUrl(lotUrl).Should().BeTrue();
        }
    }

    private sealed class MapHttpClientFactory : IHttpClientFactory
    {
        private readonly IReadOnlyDictionary<string, string> _responsesByUrl;

        public MapHttpClientFactory(IReadOnlyDictionary<string, string> responsesByUrl)
        {
            _responsesByUrl = responsesByUrl
                .ToDictionary(pair => NormalizeRequestUrl(pair.Key), pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new MapHttpMessageHandler(_responsesByUrl))
            {
                BaseAddress = new Uri("https://www.vipleiloes.com.br")
            };
        }

        private static string NormalizeRequestUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return url.TrimEnd('/');
            }

            var builder = new UriBuilder(uri)
            {
                Query = string.Empty,
                Fragment = string.Empty
            };

            return builder.Uri.ToString().TrimEnd('/');
        }
    }

    private sealed class MapHttpMessageHandler : HttpMessageHandler
    {
        private readonly IReadOnlyDictionary<string, string> _responsesByUrl;

        public MapHttpMessageHandler(IReadOnlyDictionary<string, string> responsesByUrl)
        {
            _responsesByUrl = responsesByUrl;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = NormalizeRequestUrl(request.RequestUri);
            if (_responsesByUrl.TryGetValue(key, out var html))
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(html)
                });
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
            {
                Content = new StringContent("<html><body>not-found</body></html>")
            });
        }

        private static string NormalizeRequestUrl(Uri? uri)
        {
            if (uri is null)
            {
                return string.Empty;
            }

            var builder = new UriBuilder(uri)
            {
                Query = string.Empty,
                Fragment = string.Empty
            };

            return builder.Uri.ToString().TrimEnd('/');
        }
    }
}
