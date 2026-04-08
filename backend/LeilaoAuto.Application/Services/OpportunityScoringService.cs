using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Scoring;

namespace LeilaoAuto.Application.Services;

public class OpportunityScoringService : IOpportunityScoringService
{
    public OpportunityScoreResult Score(decimal? currentPrice, decimal? finalPrice, decimal? historicalAveragePrice)
    {
        var evaluatedPrice = currentPrice ?? finalPrice;
        if (!evaluatedPrice.HasValue || evaluatedPrice <= 0 || !historicalAveragePrice.HasValue || historicalAveragePrice <= 0)
        {
            return new OpportunityScoreResult(0m, "ACIMA_DA_MEDIA", evaluatedPrice, historicalAveragePrice);
        }

        var average = historicalAveragePrice.Value;
        var price = evaluatedPrice.Value;
        var deltaPercent = (average - price) / average;
        var rawScore = 50m + (deltaPercent * 250m);
        var score = decimal.Round(decimal.Clamp(rawScore, 0m, 100m), 2);

        var label = ResolveLabel(price, average, score);
        return new OpportunityScoreResult(score, label, price, average);
    }

    private static string ResolveLabel(decimal price, decimal average, decimal score)
    {
        if (price > average)
        {
            return "ACIMA_DA_MEDIA";
        }

        if (price <= average * 0.85m || score >= 75m)
        {
            return "OPORTUNIDADE";
        }

        return "BOM_PRECO";
    }
}
