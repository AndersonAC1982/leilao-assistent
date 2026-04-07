using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Common;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Services;

namespace LeilaoAuto.Application.Services;

public class LotService : ILotService
{
    private readonly IAuctionLotRepository _auctionLotRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuctionProviderClient _auctionProviderClient;
    private readonly IFipePriceProvider _fipePriceProvider;
    private readonly IBillingGateway _billingGateway;
    private readonly IAlertPublisher _alertPublisher;
    private readonly IOpportunityScoringService _opportunityScoringService;
    private readonly IRiskScoringService _riskScoringService;

    public LotService(
        IAuctionLotRepository auctionLotRepository,
        IUserRepository userRepository,
        IAuctionProviderClient auctionProviderClient,
        IFipePriceProvider fipePriceProvider,
        IBillingGateway billingGateway,
        IAlertPublisher alertPublisher,
        IOpportunityScoringService opportunityScoringService,
        IRiskScoringService riskScoringService)
    {
        _auctionLotRepository = auctionLotRepository;
        _userRepository = userRepository;
        _auctionProviderClient = auctionProviderClient;
        _fipePriceProvider = fipePriceProvider;
        _billingGateway = billingGateway;
        _alertPublisher = alertPublisher;
        _opportunityScoringService = opportunityScoringService;
        _riskScoringService = riskScoringService;
    }

    public async Task<IReadOnlyList<LotDto>> SearchActiveAsync(Guid userId, LotSearchFilterRequest filter, CancellationToken cancellationToken)
    {
        await _billingGateway.RegisterSearchAsync(userId, cancellationToken);

        var lots = await _auctionLotRepository.SearchActiveAsync(filter, cancellationToken);
        var validLots = lots.Where(lot => lot.HasValidLotUrl()).ToList();
        if (validLots.Count == 0)
        {
            return [];
        }

        var averages = await _auctionLotRepository.GetAverageFinalPriceByNormalizedModelsAsync(
            validLots.Select(lot => lot.NormalizedModel).Distinct().ToArray(),
            cancellationToken);

        var mapped = await BuildLotsAsync(validLots, averages, cancellationToken);
        foreach (var lot in mapped.Where(item => item.OpportunityScore >= 70 && item.RiskScore <= 30))
        {
            await _alertPublisher.PublishOpportunityAsync(userId, lot, cancellationToken);
        }

        return mapped
            .OrderByDescending(lot => lot.OpportunityScore)
            .ThenBy(lot => lot.RiskScore)
            .ToList();
    }

    public async Task<IReadOnlyList<LotDto>> GetClosedHistoryBySimilarityAsync(
        Guid userId,
        LotSearchFilterRequest? filter,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: true, cancellationToken);
        if (user is null || user.MonitoredVehicles.Count == 0)
        {
            return [];
        }

        var normalizedModels = user.MonitoredVehicles
            .Select(vehicle => vehicle.NormalizedModel)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToArray();

        var lots = await _auctionLotRepository.GetClosedByNormalizedModelsAsync(normalizedModels, filter, cancellationToken);
        var similarLots = lots
            .Where(lot => lot.HasValidLotUrl())
            .Where(lot => normalizedModels.Any(monitored => ModelMatcher.IsMatch(monitored, lot.NormalizedModel)))
            .ToList();

        if (similarLots.Count == 0)
        {
            return [];
        }

        var averages = await _auctionLotRepository.GetAverageFinalPriceByNormalizedModelsAsync(normalizedModels, cancellationToken);
        return await BuildLotsAsync(similarLots, averages, cancellationToken);
    }

    public async Task<LotDto?> FindExactActiveAsync(ExactLotRequest request, CancellationToken cancellationToken)
    {
        var lot = await _auctionLotRepository.FindExactActiveAsync(request.Auctioneer, request.LotNumber, cancellationToken);
        if (lot is null || !lot.HasValidLotUrl())
        {
            return null;
        }

        var averages = await _auctionLotRepository.GetAverageFinalPriceByNormalizedModelsAsync([lot.NormalizedModel], cancellationToken);
        var list = await BuildLotsAsync([lot], averages, cancellationToken);
        return list.SingleOrDefault();
    }

    public async Task<IReadOnlyList<ModelAverageDto>> GetModelAveragesByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: true, cancellationToken);
        if (user is null || user.MonitoredVehicles.Count == 0)
        {
            return [];
        }

        var normalizedModels = user.MonitoredVehicles
            .Select(vehicle => vehicle.NormalizedModel)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToArray();

        var averages = await _auctionLotRepository.GetAverageFinalPriceByNormalizedModelsAsync(normalizedModels, cancellationToken);
        return averages
            .OrderBy(item => item.Key)
            .Select(item => new ModelAverageDto(item.Key, item.Value))
            .ToList();
    }

    public async Task<int> SyncLatestLotsAsync(CancellationToken cancellationToken)
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
                // Ignora lotes sem URL exata válida.
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
            Domain.Enums.VehicleCondition.Damaged => "sinistro media monta",
            Domain.Enums.VehicleCondition.Flooded => "enchente",
            Domain.Enums.VehicleCondition.Scrap => "sucata",
            Domain.Enums.VehicleCondition.TheftRecovery => "recuperavel",
            _ => "sem indicio relevante"
        };

        return $"{conditionText}. Lote {lot.LotNumber} em {lot.Uf}.";
    }
}
