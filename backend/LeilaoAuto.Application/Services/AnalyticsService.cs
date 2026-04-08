using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Analytics;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    private const double MatchThreshold = 0.58;

    private readonly IUserRepository _userRepository;
    private readonly IAuctionLotRepository _auctionLotRepository;
    private readonly IModelNormalizationService _modelNormalizationService;
    private readonly ILotAnalyticsComputationService _lotAnalyticsComputationService;
    private readonly IOpportunityScoringService _opportunityScoringService;
    private readonly IRiskScoringService _riskScoringService;

    public AnalyticsService(
        IUserRepository userRepository,
        IAuctionLotRepository auctionLotRepository,
        IModelNormalizationService modelNormalizationService,
        ILotAnalyticsComputationService lotAnalyticsComputationService,
        IOpportunityScoringService opportunityScoringService,
        IRiskScoringService riskScoringService)
    {
        _userRepository = userRepository;
        _auctionLotRepository = auctionLotRepository;
        _modelNormalizationService = modelNormalizationService;
        _lotAnalyticsComputationService = lotAnalyticsComputationService;
        _opportunityScoringService = opportunityScoringService;
        _riskScoringService = riskScoringService;
    }

    public async Task<IReadOnlyList<ModelAveragePriceDto>> GetAveragePriceAsync(
        Guid userId,
        string? modelFilter,
        CancellationToken cancellationToken)
    {
        var context = await BuildAnalyticsContextAsync(userId, cancellationToken);
        if (context.ClosedLots.Count == 0)
        {
            return [];
        }

        var averages = _lotAnalyticsComputationService.GroupAndCalculateModelPrices(context.ClosedLots);
        var normalizedFilter = _modelNormalizationService.NormalizeComparable(modelFilter);

        return averages
            .Where(item => MatchesModelFilter(item.ComparableModel, normalizedFilter))
            .OrderByDescending(item => item.AveragePrice)
            .ToList();
    }

    public async Task<IReadOnlyList<OpportunityDto>> GetOpportunitiesAsync(
        Guid userId,
        string? modelFilter,
        CancellationToken cancellationToken)
    {
        var context = await BuildAnalyticsContextAsync(userId, cancellationToken);
        if (context.ActiveLots.Count == 0 || context.ClosedLots.Count == 0)
        {
            return [];
        }

        var averages = _lotAnalyticsComputationService.GroupAndCalculateModelPrices(context.ClosedLots);
        if (averages.Count == 0)
        {
            return [];
        }

        var normalizedFilter = _modelNormalizationService.NormalizeComparable(modelFilter);
        var opportunities = new List<OpportunityDto>();

        foreach (var lot in context.ActiveLots)
        {
            if (!lot.CurrentBid.HasValue || lot.CurrentBid <= 0)
            {
                continue;
            }

            var comparableModel = _modelNormalizationService.NormalizeComparable(lot.Model, lot.Make);
            if (!MatchesModelFilter(comparableModel, normalizedFilter))
            {
                continue;
            }

            var historicalAverage = FindBestAverage(averages, comparableModel);
            if (historicalAverage is null)
            {
                continue;
            }

            var opportunity = _opportunityScoringService.Score(lot.CurrentBid, lot.FinalPrice, historicalAverage.AveragePrice);
            if (opportunity.Label == "ACIMA_DA_MEDIA")
            {
                continue;
            }

            var title = $"{lot.Make} {lot.Model} {lot.Year}".Trim();
            var description = BuildDescriptionForRisk(lot);
            var risk = _riskScoringService.Score(title, description, lot.VehicleCondition, lot.Year, lot.HasValidLotUrl());

            var currentPrice = lot.CurrentBid.Value;
            var priceGap = decimal.Round(historicalAverage.AveragePrice - currentPrice, 2);
            var priceGapPercent = historicalAverage.AveragePrice <= 0
                ? 0
                : decimal.Round((priceGap / historicalAverage.AveragePrice) * 100m, 2);

            opportunities.Add(new OpportunityDto(
                lot.Id,
                title,
                lot.Auctioneer,
                lot.LotNumber,
                $"{lot.Make} {lot.Model}".Trim(),
                comparableModel,
                currentPrice,
                historicalAverage.AveragePrice,
                priceGap,
                priceGapPercent,
                opportunity.Score,
                opportunity.Label,
                risk.RiskScore,
                risk.DamageLevel,
                risk.Decision,
                lot.LotUrl));
        }

        return opportunities
            .OrderByDescending(item => item.PriceGapPercent)
            .ThenBy(item => item.RiskScore)
            .ToList();
    }

    public async Task<RiskSummaryDto> GetRiskSummaryAsync(
        Guid userId,
        string? modelFilter,
        CancellationToken cancellationToken)
    {
        var context = await BuildAnalyticsContextAsync(userId, cancellationToken);
        if (context.ActiveLots.Count == 0)
        {
            return new RiskSummaryDto(0, 0, 0, 0, 0, []);
        }

        var normalizedFilter = _modelNormalizationService.NormalizeComparable(modelFilter);
        var lotRisks = context.ActiveLots
            .Select(lot =>
            {
                var title = $"{lot.Make} {lot.Model} {lot.Year}".Trim();
                var description = BuildDescriptionForRisk(lot);
                var risk = _riskScoringService.Score(title, description, lot.VehicleCondition, lot.Year, lot.HasValidLotUrl());

                return new
                {
                    ComparableModel = _modelNormalizationService.NormalizeComparable(lot.Model, lot.Make),
                    Score = risk.RiskScore
                };
            })
            .Where(item => MatchesModelFilter(item.ComparableModel, normalizedFilter))
            .ToList();

        if (lotRisks.Count == 0)
        {
            return new RiskSummaryDto(0, 0, 0, 0, 0, []);
        }

        var lowRiskCount = lotRisks.Count(item => item.Score < 35m);
        var mediumRiskCount = lotRisks.Count(item => item.Score >= 35m && item.Score < 70m);
        var highRiskCount = lotRisks.Count(item => item.Score >= 70m);
        var averageRiskScore = decimal.Round(lotRisks.Average(item => item.Score), 2);

        var topRiskModels = lotRisks
            .GroupBy(item => item.ComparableModel)
            .Select(group => new RiskModelSummaryDto(
                group.Key,
                group.Count(),
                decimal.Round(group.Average(item => item.Score), 2)))
            .OrderByDescending(item => item.AverageRiskScore)
            .ThenByDescending(item => item.Quantity)
            .Take(5)
            .ToList();

        return new RiskSummaryDto(
            lotRisks.Count,
            averageRiskScore,
            lowRiskCount,
            mediumRiskCount,
            highRiskCount,
            topRiskModels);
    }

    private async Task<AnalyticsContext> BuildAnalyticsContextAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: true, cancellationToken);
        if (user is null || user.MonitoredVehicles.Count == 0)
        {
            return AnalyticsContext.Empty;
        }

        var monitoredModels = user.MonitoredVehicles
            .Select(vehicle => _modelNormalizationService.NormalizeComparable(vehicle.Model, vehicle.Brand))
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct()
            .ToArray();

        if (monitoredModels.Length == 0)
        {
            return AnalyticsContext.Empty;
        }

        var activeLots = await _auctionLotRepository.GetActiveLotsAsync(cancellationToken);
        var closedLots = await _auctionLotRepository.GetClosedLotsAsync(cancellationToken);

        return new AnalyticsContext(
            FilterByMonitoredModels(activeLots, monitoredModels),
            FilterByMonitoredModels(closedLots, monitoredModels)
                .Where(lot => lot.FinalPrice.HasValue)
                .ToList());
    }

    private List<AuctionLot> FilterByMonitoredModels(
        IEnumerable<AuctionLot> source,
        IReadOnlyCollection<string> monitoredModels)
    {
        var result = new List<AuctionLot>();

        foreach (var lot in source)
        {
            var lotComparableModel = _modelNormalizationService.NormalizeComparable(lot.Model, lot.Make);
            if (string.IsNullOrWhiteSpace(lotComparableModel))
            {
                continue;
            }

            var isMatch = monitoredModels.Any(model =>
                _modelNormalizationService.IsSimilar(model, lotComparableModel, MatchThreshold));

            if (isMatch)
            {
                result.Add(lot);
            }
        }

        return result;
    }

    private ModelAveragePriceDto? FindBestAverage(
        IReadOnlyList<ModelAveragePriceDto> averages,
        string comparableModel)
    {
        var exact = averages.FirstOrDefault(item =>
            item.ComparableModel.Equals(comparableModel, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact;
        }

        ModelAveragePriceDto? bestAverage = null;
        var bestSimilarity = 0d;
        foreach (var average in averages)
        {
            var similarity = _modelNormalizationService.Similarity(average.ComparableModel, comparableModel);
            if (similarity < MatchThreshold || similarity <= bestSimilarity)
            {
                continue;
            }

            bestSimilarity = similarity;
            bestAverage = average;
        }

        return bestAverage;
    }

    private bool MatchesModelFilter(string comparableModel, string normalizedFilter)
    {
        if (string.IsNullOrWhiteSpace(normalizedFilter))
        {
            return true;
        }

        return _modelNormalizationService.IsSimilar(comparableModel, normalizedFilter, MatchThreshold);
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

    private sealed record AnalyticsContext(IReadOnlyList<AuctionLot> ActiveLots, IReadOnlyList<AuctionLot> ClosedLots)
    {
        public static AnalyticsContext Empty { get; } = new([], []);
    }
}
