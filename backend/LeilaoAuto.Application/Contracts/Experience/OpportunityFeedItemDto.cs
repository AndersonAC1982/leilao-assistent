namespace LeilaoAuto.Application.Contracts.Experience;

public sealed record OpportunityFeedItemDto(
    Guid LotId,
    string Source,
    decimal Score,
    string ScoreLabel,
    string Title,
    string Location,
    decimal Value,
    DateTimeOffset DateUtc,
    string Summary,
    decimal RiskScore,
    string RiskDecision,
    string LotUrl,
    int Status);
