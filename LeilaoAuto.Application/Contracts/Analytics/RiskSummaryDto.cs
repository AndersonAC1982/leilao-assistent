namespace LeilaoAuto.Application.Contracts.Analytics;

public sealed record RiskSummaryDto(
    int TotalActiveLots,
    decimal AverageRiskScore,
    int LowRiskCount,
    int MediumRiskCount,
    int HighRiskCount,
    IReadOnlyList<RiskModelSummaryDto> TopRiskModels);

public sealed record RiskModelSummaryDto(
    string ComparableModel,
    int Quantity,
    decimal AverageRiskScore);
