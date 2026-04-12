using FluentAssertions;
using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Experience;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Application.Services;
using LeilaoAuto.Domain.Common;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Infrastructure.External;
using LeilaoAuto.Infrastructure.External.Connectors;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Tests;

public class ExtensionExperienceTests
{
    [Fact]
    public async Task Opportunities_Endpoint_Should_Filter_InvalidLotUrl_And_Apply_FreeQuota()
    {
        var user = new User("free-extension@leilaoauto.local", "hash", UserRole.User, PlanType.Free);
        var userId = user.Id;
        var lots = new List<LotDto>();

        for (var index = 0; index < 14; index++)
        {
            lots.Add(CreateLotDto(
                id: Guid.NewGuid(),
                source: "Superbid",
                auctioneer: "Superbid",
                score: 70 + index,
                lotUrl: $"https://www.superbid.net/oferta/veiculo-automotor-gm-omega-gls-{4583000 + index}"));
        }

        lots.Add(CreateLotDto(
            id: Guid.NewGuid(),
            source: "Sodre Santoro",
            auctioneer: "Sodre Santoro",
            score: 99,
            lotUrl: "https://www.sodresantoro.com.br/veiculos/lotes?lot_brand=jeep&page=1"));

        lots.Add(CreateLotDto(
            id: Guid.NewGuid(),
            source: "Superbid",
            auctioneer: "Superbid",
            score: 40,
            lotUrl: "https://www.superbid.net/"));

        var service = BuildService(
            users: [user],
            lotService: new FakeLotService(new LotSearchResultDto(lots, [], []), refreshResult: 0),
            connectorExecutionLogRepository: new InMemoryConnectorExecutionLogRepository(),
            userSettingsRepository: new InMemoryUserSettingsRepository(),
            connectors:
            [
                CreateSuperbidConnector(),
                CreateSodreConnector()
            ]);

        var result = await service.GetOpportunitiesAsync(
            userId,
            new OpportunityFeedQueryRequest(),
            CancellationToken.None);

        result.Should().HaveCount(12, "plano Free precisa de limite de resultados no endpoint /api/opportunities");
        result.Should().OnlyContain(item =>
            item.LotUrl.Contains("/oferta/", StringComparison.OrdinalIgnoreCase)
            && item.LotUrl.Contains("superbid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScannerRun_Endpoint_Should_Enforce_DailyQuota_ByPlan()
    {
        var user = new User("free-scanner@leilaoauto.local", "hash", UserRole.User, PlanType.Free);
        var userId = user.Id;

        var logs = new List<ConnectorExecutionLog>();
        for (var index = 0; index < 5; index++)
        {
            logs.Add(new ConnectorExecutionLog(
                connectorName: "ScannerManual",
                executedAt: DateTime.UtcNow.AddMinutes(-30 - index),
                success: true,
                recordsRead: 10,
                recordsSaved: 10,
                message: "seed",
                payloadJson: "{}",
                userId: userId));
        }

        var fakeLotService = new FakeLotService(new LotSearchResultDto([], [], []), refreshResult: 11);
        var service = BuildService(
            users: [user],
            lotService: fakeLotService,
            connectorExecutionLogRepository: new InMemoryConnectorExecutionLogRepository(logs),
            userSettingsRepository: new InMemoryUserSettingsRepository(),
            connectors: [CreateSuperbidConnector()]);

        var action = async () => await service.RunScannerAsync(userId, request: null, CancellationToken.None);

        await action.Should().ThrowAsync<DomainRuleException>();
        fakeLotService.RefreshCallCount.Should().Be(0, "varredura não deve ser executada quando a cota diária já foi atingida");
    }

    [Fact]
    public async Task ScannerRun_Should_Forward_SelectedFilters_And_Persist_Settings()
    {
        var user = new User("pro-run@leilaoauto.local", "hash", UserRole.User, PlanType.Pro);
        var userId = user.Id;
        var fakeLotService = new FakeLotService(new LotSearchResultDto([], [], []), refreshResult: 7);
        var userSettingsRepository = new InMemoryUserSettingsRepository();

        var service = BuildService(
            users: [user],
            lotService: fakeLotService,
            connectorExecutionLogRepository: new InMemoryConnectorExecutionLogRepository(),
            userSettingsRepository: userSettingsRepository,
            connectors:
            [
                CreateSuperbidConnector(),
                CreateSodreConnector()
            ]);

        var request = new ScannerRunRequest
        {
            Category = "Imóveis",
            ActiveSources = ["Superbid", "VIP Leilões"],
            Search = "apartamento",
            MinScore = 75,
            Region = "sp",
            MaxPrice = 250000
        };

        var response = await service.RunScannerAsync(userId, request, CancellationToken.None);

        response.Success.Should().BeTrue();
        fakeLotService.RefreshCallCount.Should().Be(1);
        fakeLotService.LastRefreshSearch.Should().Be("apartamento");
        fakeLotService.LastRefreshMaxPrice.Should().Be(250000m);
        fakeLotService.LastRefreshActiveSources.Should().BeEquivalentTo(["Superbid", "VIP Leilões"]);
        fakeLotService.LastRefreshFilter.Should().NotBeNull();
        fakeLotService.LastRefreshFilter!.Uf.Should().Be("SP");
        fakeLotService.LastRefreshFilter.Model.Should().Be("apartamento");

        var persistedSettings = await userSettingsRepository.GetByUserIdAsync(userId, CancellationToken.None);
        persistedSettings.Should().NotBeNull();
        persistedSettings!.Category.Should().Be("Imóveis");
        persistedSettings.Region.Should().Be("SP");
        persistedSettings.ActiveSources.Should().Be("Superbid|VIP Leilões");
        persistedSettings.MaxPrice.Should().Be(250000m);
    }

    [Fact]
    public async Task History_Endpoint_Should_Clamp_Take_ByPlan()
    {
        var user = new User("free-history@leilaoauto.local", "hash", UserRole.User, PlanType.Free);
        var userId = user.Id;

        var logs = Enumerable.Range(1, 40)
            .Select(index => new ConnectorExecutionLog(
                connectorName: $"Connector-{index}",
                executedAt: DateTime.UtcNow.AddMinutes(-index),
                success: true,
                recordsRead: index,
                recordsSaved: index - 1,
                message: "ok",
                payloadJson: "{}",
                userId: userId))
            .ToList();

        var service = BuildService(
            users: [user],
            lotService: new FakeLotService(new LotSearchResultDto([], [], []), refreshResult: 0),
            connectorExecutionLogRepository: new InMemoryConnectorExecutionLogRepository(logs),
            userSettingsRepository: new InMemoryUserSettingsRepository(),
            connectors: [CreateSuperbidConnector()]);

        var history = await service.GetHistoryAsync(userId, take: 99, CancellationToken.None);

        history.Should().HaveCount(12, "plano Free precisa de histórico reduzido no endpoint /api/history");
    }

    [Fact]
    public async Task Settings_Endpoint_Should_Downgrade_AdvancedFilters_For_ProPlan()
    {
        var user = new User("pro-settings@leilaoauto.local", "hash", UserRole.User, PlanType.Pro);
        var userId = user.Id;

        var service = BuildService(
            users: [user],
            lotService: new FakeLotService(new LotSearchResultDto([], [], []), refreshResult: 0),
            connectorExecutionLogRepository: new InMemoryConnectorExecutionLogRepository(),
            userSettingsRepository: new InMemoryUserSettingsRepository(),
            connectors: [CreateSuperbidConnector()]);

        var updated = await service.UpdateSettingsAsync(
            userId,
            new UpdateUserSettingsRequest
            {
                Search = "gol",
                Source = "Superbid",
                MinScore = 60,
                VehicleType = (int)VehicleType.Car,
                Region = "SP",
                AdvancedFiltersEnabled = true
            },
            CancellationToken.None);

        updated.AdvancedFiltersEnabled.Should().BeFalse("plano Pro não pode habilitar filtros avançados em /api/settings");
    }

    [Fact]
    public void SuperbidConnector_Should_Allow_Only_Exact_Lot_Url()
    {
        var connector = CreateSuperbidConnector();

        connector.ValidateLotUrl("https://www.superbid.net/oferta/veiculo-automotor-gm-omega-gls-4583144")
            .Should()
            .BeTrue();

        connector.ValidateLotUrl("https://www.superbid.net/oferta")
            .Should()
            .BeFalse();

        connector.ValidateLotUrl("https://www.superbid.net/busca?marca=gol")
            .Should()
            .BeFalse();
    }

    [Fact]
    public void MegaLeiloesConnector_Should_Allow_Only_Exact_Lot_Url()
    {
        var connector = CreateMegaConnector();

        connector.ValidateLotUrl("https://www.megaleiloes.com.br/veiculos/carros/sp/santos/carro-honda-civic-lx-2004-j121973")
            .Should()
            .BeTrue();

        connector.ValidateLotUrl("https://www.megaleiloes.com.br/veiculos/carros")
            .Should()
            .BeFalse();

        connector.ValidateLotUrl("https://www.megaleiloes.com.br/imoveis/apartamentos/sp/sao-paulo/apartamento-218-m2-03-vagas-brooklin-paulista-sao-paulo-sp-x121107")
            .Should()
            .BeFalse();
    }

    private static ExperienceService BuildService(
        IReadOnlyCollection<User> users,
        FakeLotService lotService,
        InMemoryConnectorExecutionLogRepository connectorExecutionLogRepository,
        InMemoryUserSettingsRepository userSettingsRepository,
        IReadOnlyCollection<ILotConnector> connectors)
    {
        var userRepository = new InMemoryUserRepository(users);
        var connectorFactory = new InMemoryConnectorFactory(connectors);

        return new ExperienceService(
            lotService,
            connectorFactory,
            userRepository,
            connectorExecutionLogRepository,
            userSettingsRepository,
            NullLogger<ExperienceService>.Instance);
    }

    private static LotDto CreateLotDto(Guid id, string source, string auctioneer, decimal score, string lotUrl)
    {
        return new LotDto(
            Id: id,
            Title: "Volkswagen Gol 1.6 MSI 2021",
            Description: "Lote de teste",
            Source: source,
            Auctioneer: auctioneer,
            LotNumber: "1001",
            Make: "Volkswagen",
            Model: "Gol 1.6 MSI",
            Year: 2021,
            VehicleType: VehicleType.Car,
            Uf: "SP",
            VehicleCondition: VehicleCondition.Running,
            Status: LotStatus.Active,
            CurrentBid: 30000m,
            FinalPrice: null,
            ReferenceAveragePrice: 40000m,
            LotUrl: lotUrl,
            OpportunityScore: score,
            OpportunityLabel: "BOM_PRECO",
            RiskScore: 20m,
            DamageLevel: "BAIXO",
            RiskDecision: "COMPRA_SEGURA",
            UpdatedAtUtc: DateTimeOffset.UtcNow);
    }

    private static SuperbidConnector CreateSuperbidConnector()
    {
        return new SuperbidConnector(
            new FakeHttpClientFactory(),
            Options.Create(new AuctionProviderOptions()),
            NullLogger<SuperbidConnector>.Instance);
    }

    private static SodreSantoroConnector CreateSodreConnector()
    {
        return new SodreSantoroConnector(
            new FakeHttpClientFactory(),
            Options.Create(new AuctionProviderOptions()),
            NullLogger<SodreSantoroConnector>.Instance);
    }

    private static MegaLeiloesConnector CreateMegaConnector()
    {
        return new MegaLeiloesConnector(
            new FakeHttpClientFactory(),
            Options.Create(new AuctionProviderOptions()),
            NullLogger<MegaLeiloesConnector>.Instance);
    }

    private sealed class FakeLotService : ILotService
    {
        private readonly LotSearchResultDto _searchResult;
        private readonly int _refreshResult;

        public FakeLotService(LotSearchResultDto searchResult, int refreshResult)
        {
            _searchResult = searchResult;
            _refreshResult = refreshResult;
        }

        public int RefreshCallCount { get; private set; }
        public LotSearchFilterRequest? LastRefreshFilter { get; private set; }
        public IReadOnlyCollection<string>? LastRefreshActiveSources { get; private set; }
        public string? LastRefreshSearch { get; private set; }
        public decimal? LastRefreshMaxPrice { get; private set; }

        public Task<LotSearchResultDto> SearchAsync(Guid userId, LotSearchFilterRequest filter, CancellationToken cancellationToken)
        {
            return Task.FromResult(_searchResult);
        }

        public Task<IReadOnlyList<LotDto>> GetActiveAsync(LotSearchFilterRequest filter, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<LotDto>>(_searchResult.ActiveLots);
        }

        public Task<IReadOnlyList<LotDto>> GetClosedAsync(LotSearchFilterRequest filter, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<LotDto>>(_searchResult.ClosedLots);
        }

        public Task<LotDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<LotDto?>(_searchResult.ActiveLots.Concat(_searchResult.ClosedLots).FirstOrDefault(lot => lot.Id == id));
        }

        public Task<int> RefreshAsync(CancellationToken cancellationToken)
        {
            return RefreshAsync(new LotSearchFilterRequest(), activeSources: null, search: null, maxPrice: null, cancellationToken);
        }

        public Task<int> RefreshAsync(
            LotSearchFilterRequest filter,
            IReadOnlyCollection<string>? activeSources,
            string? search,
            decimal? maxPrice,
            CancellationToken cancellationToken)
        {
            LastRefreshFilter = filter;
            LastRefreshActiveSources = activeSources;
            LastRefreshSearch = search;
            LastRefreshMaxPrice = maxPrice;
            RefreshCallCount++;
            return Task.FromResult(_refreshResult);
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users;

        public InMemoryUserRepository(IEnumerable<User> users)
        {
            _users = users.ToList();
        }

        public Task<User?> GetByIdAsync(Guid userId, bool includeVehicles, CancellationToken cancellationToken)
        {
            return Task.FromResult(_users.FirstOrDefault(user => user.Id == userId));
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var normalized = email.Trim().ToLowerInvariant();
            return Task.FromResult(_users.FirstOrDefault(user => user.Email == normalized));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryConnectorExecutionLogRepository : IConnectorExecutionLogRepository
    {
        private readonly List<ConnectorExecutionLog> _logs;

        public InMemoryConnectorExecutionLogRepository(IEnumerable<ConnectorExecutionLog>? seed = null)
        {
            _logs = seed?.ToList() ?? [];
        }

        public Task<ConnectorExecutionLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_logs.FirstOrDefault(log => log.Id == id));
        }

        public Task AddAsync(ConnectorExecutionLog entity, CancellationToken cancellationToken)
        {
            _logs.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(ConnectorExecutionLog entity)
        {
            // No-op for in-memory tests.
        }

        public void Remove(ConnectorExecutionLog entity)
        {
            _logs.Remove(entity);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ConnectorExecutionLog>> GetRecentAsync(int take, CancellationToken cancellationToken)
        {
            var safeTake = take <= 0 ? 20 : take;
            return Task.FromResult<IReadOnlyList<ConnectorExecutionLog>>(
                _logs.OrderByDescending(log => log.ExecutedAt).Take(safeTake).ToList());
        }

        public Task<IReadOnlyList<ConnectorExecutionLog>> GetRecentByUserIdAsync(Guid userId, int take, CancellationToken cancellationToken)
        {
            var safeTake = take <= 0 ? 20 : take;
            return Task.FromResult<IReadOnlyList<ConnectorExecutionLog>>(
                _logs
                    .Where(log => log.UserId == null || log.UserId == userId)
                    .OrderByDescending(log => log.ExecutedAt)
                    .Take(safeTake)
                    .ToList());
        }

        public Task<int> CountByUserAndConnectorAsync(
            Guid userId,
            string connectorName,
            DateTime fromInclusiveUtc,
            DateTime toExclusiveUtc,
            CancellationToken cancellationToken)
        {
            var count = _logs.Count(log =>
                log.UserId == userId
                && log.ConnectorName == connectorName
                && log.ExecutedAt >= fromInclusiveUtc
                && log.ExecutedAt < toExclusiveUtc);

            return Task.FromResult(count);
        }
    }

    private sealed class InMemoryUserSettingsRepository : IUserSettingsRepository
    {
        private readonly List<UserSettings> _settings = [];

        public Task<UserSettings?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_settings.FirstOrDefault(setting => setting.Id == id));
        }

        public Task AddAsync(UserSettings entity, CancellationToken cancellationToken)
        {
            _settings.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(UserSettings entity)
        {
            // No-op: entity reference is already updated.
        }

        public void Remove(UserSettings entity)
        {
            _settings.Remove(entity);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<UserSettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_settings.FirstOrDefault(setting => setting.UserId == userId));
        }
    }

    private sealed class InMemoryConnectorFactory : IConnectorFactory
    {
        private readonly IReadOnlyList<ILotConnector> _connectors;

        public InMemoryConnectorFactory(IReadOnlyCollection<ILotConnector> connectors)
        {
            _connectors = connectors.ToList();
        }

        public ILotConnector CreateByName(string name)
        {
            return _connectors.First(connector => connector.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<ILotConnector> CreateByDomain(string domain)
        {
            var normalized = domain.Trim().ToLowerInvariant();

            return _connectors
                .Where(connector => connector.SupportedDomains.Any(supported =>
                    supported.Equals(normalized, StringComparison.OrdinalIgnoreCase)
                    || normalized.EndsWith($".{supported}", StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new FakeHttpMessageHandler())
            {
                BaseAddress = new Uri("https://www.superbid.net")
            };
        }
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("<html><body></body></html>")
            };

            return Task.FromResult(response);
        }
    }
}
