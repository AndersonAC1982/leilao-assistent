namespace LeilaoAuto.Application.Contracts.Scoring;

public sealed record RiskScoreResult(
    decimal RiskScore,
    string DamageLevel,
    string Decision,
    IReadOnlyList<string> CriticalKeywords);
