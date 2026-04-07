namespace LeilaoAuto.Application.Contracts.Analytics;

public sealed record OpportunityDto(
    Guid LotId,
    string Title,
    string Auctioneer,
    string LotNumber,
    string Model,
    string ComparableModel,
    decimal CurrentPrice,
    decimal HistoricalAveragePrice,
    decimal PriceGap,
    decimal PriceGapPercent,
    decimal OpportunityScore,
    string OpportunityLabel,
    decimal RiskScore,
    string DamageLevel,
    string RiskDecision,
    string LotUrl);
