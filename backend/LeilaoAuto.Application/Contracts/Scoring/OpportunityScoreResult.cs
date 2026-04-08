namespace LeilaoAuto.Application.Contracts.Scoring;

public sealed record OpportunityScoreResult(
    decimal Score,
    string Label,
    decimal? EvaluatedPrice,
    decimal? HistoricalAveragePrice);
