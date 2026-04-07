namespace LeilaoAuto.Application.Contracts.Analytics;

public sealed record OpportunityDto(
    Guid LotId,
    string Auctioneer,
    string LotNumber,
    string Model,
    string ComparableModel,
    decimal CurrentPrice,
    decimal HistoricalAveragePrice,
    decimal PriceGap,
    decimal PriceGapPercent,
    decimal RiskScore,
    string LotUrl);
