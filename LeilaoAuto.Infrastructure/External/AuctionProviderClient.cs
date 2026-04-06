using System.Net.Http.Json;
using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External;

public class AuctionProviderClient : IAuctionProviderClient
{
    private readonly HttpClient _httpClient;
    private readonly AuctionProviderOptions _options;
    private readonly ILogger<AuctionProviderClient> _logger;

    public AuctionProviderClient(
        HttpClient httpClient,
        IOptions<AuctionProviderOptions> options,
        ILogger<AuctionProviderClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProviderLotDto>> FetchLatestLotsAsync(CancellationToken cancellationToken)
    {
        if (_options.MockMode)
        {
            return BuildMockLots();
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _options.LotsEndpoint);
            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Add("X-Api-Key", _options.ApiKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var lots = await response.Content.ReadFromJsonAsync<List<ProviderLotDto>>(cancellationToken: cancellationToken);
            return lots ?? [];
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Falha ao obter lotes de provedor externo. Mantendo fallback mock.");
            return BuildMockLots();
        }
    }

    private static IReadOnlyList<ProviderLotDto> BuildMockLots()
    {
        return
        [
            new ProviderLotDto(
                ExternalId: "mock-active-001",
                Auctioneer: "Leiloeiro Sul",
                LotNumber: "1042",
                Make: "Volkswagen",
                Model: "Gol 1.6 MSI",
                Year: 2020,
                VehicleType: VehicleType.Car,
                Uf: "SP",
                VehicleCondition: VehicleCondition.Running,
                Status: LotStatus.Active,
                LotUrl: "https://leiloeiro-sul.example/lote/1042",
                CurrentBid: 28700m,
                FinalPrice: null,
                AppraisedValue: 35500m,
                StartsAt: DateTimeOffset.UtcNow.AddHours(-2),
                EndsAt: DateTimeOffset.UtcNow.AddHours(5)),

            new ProviderLotDto(
                ExternalId: "mock-active-002",
                Auctioneer: "Leiloeiro Sul",
                LotNumber: "2089",
                Make: "Honda",
                Model: "CG 160 FAN",
                Year: 2022,
                VehicleType: VehicleType.Motorcycle,
                Uf: "MG",
                VehicleCondition: VehicleCondition.Running,
                Status: LotStatus.Active,
                LotUrl: "https://leiloeiro-sul.example/lote/2089",
                CurrentBid: 8700m,
                FinalPrice: null,
                AppraisedValue: 11400m,
                StartsAt: DateTimeOffset.UtcNow.AddHours(-1),
                EndsAt: DateTimeOffset.UtcNow.AddHours(4)),

            new ProviderLotDto(
                ExternalId: "mock-closed-001",
                Auctioneer: "Leiloeiro Centro",
                LotNumber: "778",
                Make: "Volkswagen",
                Model: "Gol 1.6 MSI",
                Year: 2019,
                VehicleType: VehicleType.Car,
                Uf: "SP",
                VehicleCondition: VehicleCondition.Damaged,
                Status: LotStatus.Closed,
                LotUrl: "https://leiloeiro-centro.example/lote/778",
                CurrentBid: null,
                FinalPrice: 33100m,
                AppraisedValue: 34800m,
                StartsAt: DateTimeOffset.UtcNow.AddDays(-8),
                EndsAt: DateTimeOffset.UtcNow.AddDays(-7))
        ];
    }
}
