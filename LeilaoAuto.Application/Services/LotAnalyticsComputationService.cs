using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Analytics;
using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Services;

public class LotAnalyticsComputationService : ILotAnalyticsComputationService
{
    private const double GroupSimilarityThreshold = 0.6;
    private readonly IModelNormalizationService _modelNormalizationService;

    public LotAnalyticsComputationService(IModelNormalizationService modelNormalizationService)
    {
        _modelNormalizationService = modelNormalizationService;
    }

    public IReadOnlyList<ModelAveragePriceDto> GroupAndCalculateModelPrices(IEnumerable<AuctionLot> closedLots)
    {
        var buckets = new List<ModelBucket>();

        foreach (var lot in closedLots)
        {
            if (!lot.FinalPrice.HasValue || !lot.HasValidLotUrl())
            {
                continue;
            }

            var comparableModel = _modelNormalizationService.NormalizeComparable(lot.Model, lot.Make);
            if (string.IsNullOrWhiteSpace(comparableModel))
            {
                continue;
            }

            var bucket = FindBestBucket(buckets, comparableModel);
            if (bucket is null)
            {
                bucket = new ModelBucket(comparableModel);
                buckets.Add(bucket);
            }

            bucket.Prices.Add(lot.FinalPrice.Value);
        }

        return buckets
            .Where(bucket => bucket.Prices.Count > 0)
            .Select(bucket => new ModelAveragePriceDto(
                bucket.ComparableModel,
                decimal.Round(bucket.Prices.Average(), 2),
                bucket.Prices.Min(),
                bucket.Prices.Max(),
                bucket.Prices.Count))
            .OrderByDescending(item => item.Quantity)
            .ThenBy(item => item.ComparableModel)
            .ToList();
    }

    private ModelBucket? FindBestBucket(IReadOnlyCollection<ModelBucket> buckets, string comparableModel)
    {
        ModelBucket? bestBucket = null;
        var bestSimilarity = 0d;

        foreach (var bucket in buckets)
        {
            var similarity = _modelNormalizationService.Similarity(bucket.ComparableModel, comparableModel);
            if (similarity < GroupSimilarityThreshold || similarity <= bestSimilarity)
            {
                continue;
            }

            bestSimilarity = similarity;
            bestBucket = bucket;
        }

        return bestBucket;
    }

    private sealed class ModelBucket
    {
        public ModelBucket(string comparableModel)
        {
            ComparableModel = comparableModel;
        }

        public string ComparableModel { get; }
        public List<decimal> Prices { get; } = [];
    }
}
