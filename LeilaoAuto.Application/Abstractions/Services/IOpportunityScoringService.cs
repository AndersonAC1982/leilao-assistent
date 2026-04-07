using LeilaoAuto.Application.Contracts.Scoring;

namespace LeilaoAuto.Application.Abstractions.Services;

public interface IOpportunityScoringService
{
    OpportunityScoreResult Score(decimal? currentPrice, decimal? finalPrice, decimal? historicalAveragePrice);
}
