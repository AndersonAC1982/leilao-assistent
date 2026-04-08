using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Analytics;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Common;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Services;

public class LotService : ILotService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuctionLotRepository _auctionLotRepository;
    private readonly IAuctionProviderClient _auctionProviderClient;
    private readonly IFipePriceProvider _fipePriceProvider;
    private readonly IBillingGateway _billingGateway;
    private readonly IAlertPublisher _alertPublisher;
    private readonly IOpportunityScoringService _opportunityScoringService;
    private readonly IRiskScoringService _riskScoringService;
    private readonly ILotAnalyticsComputationService _lotAnalyticsComputationService;

    public LotService(
        IUserRepository userRepository,
        IAuctionLotRepository auctionLotRepository,
        IAuctionProviderClient auctionProviderClient,
        IFipePriceProvider fipePriceProvider,
        IBillingGateway billingGateway,
        IAlertPublisher alertPublisher,
        IOpportunityScoringService opportunityScoringService,
        IRiskScoringService riskScoringService,
        ILotAnalyticsComputationService lotAnalyticsComputationService)
    {
        _userRepository = userRepository;
        _auctionLotRepository = auctionLotRepository;
        _auctionProviderClient = auctionProviderClient;
        _fipePriceProvider = fipePriceProvider;
        _billingGateway = billingGateway;
        _alertPublisher = alertPublisher;
        _opportunityScoringService = opportunityScoringService;
        _riskScoringService = riskScoringService;
        _lotAnalyticsComputationService = lotAnalyticsComputationService;
    }

    public async Task<LotSearchResultDto> SearchAsync(Guid userId, LotSearchFilterRequest filter, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: false, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found for current token.");

        await _billingGateway.RegisterSearchAsync(userId, cancellationToken);

        var activeLots = (await GetActiveAsync(filter, cancellationToken)).ToList();
        var closedLots = (await GetClosedAsync(filter, cancellationToken)).ToList();

        var closedDomainLots = await _auctionLotRepository.SearchClosedAsync(filter, cancellationToken);
        var averages = _lotAnalyticsComputationService
            .GroupAndCalculateModelPrices(closedDomainLots)
            .Select(item => new ModelPriceRangeDto(
                item.ComparableModel,
                item.AveragePrice,
                item.MinPrice,
                item.MaxPrice,
                item.Quantity))
            .OrderByDescending(item => item.Quantity)
            .ToList();

        if (user.Plan == PlanType.Free)
        {
            activeLots = activeLots.Take(10).ToList();
            closedLots = closedLots.Take(10).ToList();
            averages = averages.Take(3).ToList();
        }

        return new LotSearchResultDto(activeLots, closedLots, averages);
    }

    public async Task<IReadOnlyList<LotDto>> GetActiveAsync(LotSearchFilterRequest filter, CancellationToken cancellationToken)
    {
        var lots = await _auctionLotRepository.SearchActiveAsync(filter, cancellationToken);
        if (lots.Count == 0)
        {
            return [];
        }

        var averages = await _auctionLotRepository.GetAverageFinalPriceByNormalizedModelsAsync(
            lots.Select(lot => lot.NormalizedModel).Distinct().ToArray(),
            cancellationToken);

        var mapped = await BuildLotsAsync(lots, averages, cancellationToken);
        return mapped
            .Where(lot => lot.Status == LotStatus.Active || lot.Status == LotStatus.Confirmed)
            .OrderByDescending(lot => lot.OpportunityScore)
            .ThenBy(lot => lot.RiskScore)
            .ToList();
    }

    public async Task<IReadOnlyList<LotDto>> GetClosedAsync(LotSearchFilterRequest filter, CancellationToken cancellationToken)
    {
        var lots = await _auctionLotRepository.SearchClosedAsync(filter, cancellationToken);
        if (lots.Count == 0)
        {
            return [];
        }

        var averages = await _auctionLotRepository.GetAverageFinalPriceByNormalizedModelsAsync(
            lots.Select(lot => lot.NormalizedModel).Distinct().ToArray(),
            cancellationToken);

        var mapped = await BuildLotsAsync(lots, averages, cancellationToken);
        return mapped
            .Where(lot => lot.Status == LotStatus.Closed)
            .OrderByDescending(lot => lot.UpdatedAtUtc)
            .ToList();
    }

    public async Task<LotDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var lot = await _auctionLotRepository.GetByIdAsync(id, cancellationToken);
        if (lot is null)
        {
            return null;
        }

        var averages = await _auctionLotRepository.GetAverageFinalPriceByNormalizedModelsAsync([lot.NormalizedModel], cancellationToken);
        var mapped = await BuildLotsAsync([lot], averages, cancellationToken);
        return mapped.SingleOrDefault();
    }

    public async Task<int> RefreshAsync(CancellationToken cancellationToken)
    {
        var providerLots = await _auctionProviderClient.FetchLatestLotsAsync(cancellationToken);
        if (providerLots.Count == 0)
        {
            return 0;
        }

        var domainLots = new List<AuctionLot>(providerLots.Count);
        foreach (var lot in providerLots)
        {
            try
            {
                var mapped = new AuctionLot(
                    lot.ExternalId,
                    lot.Auctioneer,
                    lot.LotNumber,
                    lot.Make,
                    lot.Model,
                    lot.Year,
                    lot.VehicleType,
                    lot.Uf,
                    lot.VehicleCondition,
                    lot.Status,
                    lot.LotUrl,
                    lot.CurrentBid,
                    lot.FinalPrice,
                    lot.AppraisedValue,
                    lot.StartsAt,
                    lot.EndsAt);

                domainLots.Add(mapped);
            }
            catch (DomainRuleException)
            {
                // Ignora lotes sem URL exata valida.
            }
        }

        if (domainLots.Count == 0)
        {
            return 0;
        }

        await _auctionLotRepository.UpsertRangeAsync(domainLots, cancellationToken);
        await _auctionLotRepository.SaveChangesAsync(cancellationToken);

        return domainLots.Count;
    }

    private async Task<IReadOnlyList<LotDto>> BuildLotsAsync(
        IReadOnlyCollection<AuctionLot> lots,
        IReadOnlyDictionary<string, decimal> averages,
        CancellationToken cancellationToken)
    {
        var results = new List<LotDto>(lots.Count);
        foreach (var lot in lots)
        {
            averages.TryGetValue(lot.NormalizedModel, out var averageValue);
            var average = averageValue > 0 ? averageValue : (decimal?)null;
            if (!average.HasValue)
            {
                average = await _fipePriceProvider.GetPriceByNormalizedModelAsync(lot.NormalizedModel, lot.Year, cancellationToken);
            }

            var opportunity = _opportunityScoringService.Score(lot.CurrentBid, lot.FinalPrice, average);
            var title = $"{lot.Make} {lot.Model} {lot.Year}".Trim();
            var description = BuildDescriptionForRisk(lot);
            var risk = _riskScoringService.Score(title, description, lot.VehicleCondition, lot.Year, lot.HasValidLotUrl());

            var dto = new LotDto(
                lot.Id,
                title,
                description,
                lot.Auctioneer,
                lot.Auctioneer,
                lot.LotNumber,
                lot.Make,
                lot.Model,
                lot.Year,
                lot.VehicleType,
                lot.Uf,
                lot.VehicleCondition,
                lot.Status,
                lot.CurrentBid,
                lot.FinalPrice,
                opportunity.HistoricalAveragePrice,
                lot.LotUrl,
                opportunity.Score,
                opportunity.Label,
                risk.RiskScore,
                risk.DamageLevel,
                risk.Decision,
                lot.UpdatedAtUtc);

            results.Add(dto);
        }

        return results;
    }

    private static string BuildDescriptionForRisk(AuctionLot lot)
    {
        var conditionText = lot.VehicleCondition switch
        {
            VehicleCondition.Damaged => "sinistro media monta",
            VehicleCondition.Flooded => "enchente",
            VehicleCondition.Scrap => "sucata",
            VehicleCondition.TheftRecovery => "recuperavel",
            _ => "sem indicio relevante"
        };

        return $"{conditionText}. Lote {lot.LotNumber} em {lot.Uf}.";
    }
}
