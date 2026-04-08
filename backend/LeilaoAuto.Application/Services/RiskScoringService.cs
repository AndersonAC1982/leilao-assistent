using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Scoring;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;

namespace LeilaoAuto.Application.Services;

public class RiskScoringService : IRiskScoringService
{
    private static readonly (string Keyword, decimal Weight)[] KeywordWeights =
    [
        ("SINISTRO", 30m),
        ("RECUPERAVEL", 28m),
        ("SUCATA", 55m),
        ("ENCHENTE", 45m),
        ("SEM MOTOR", 38m),
        ("GRANDE MONTA", 60m),
        ("MEDIA MONTA", 42m),
        ("PEQUENA MONTA", 25m)
    ];

    public RiskScoreResult Score(string title, string? description, VehicleCondition condition, int year, bool hasValidLotUrl)
    {
        var normalizedText = BuildNormalizedText(title, description);
        var detectedKeywords = DetectKeywords(normalizedText);

        var risk = detectedKeywords.Sum(item => item.Weight);
        risk += ConditionWeight(condition);

        if (year <= DateTime.UtcNow.Year - 12)
        {
            risk += 12m;
        }

        if (!hasValidLotUrl)
        {
            risk += 25m;
        }

        var riskScore = decimal.Round(decimal.Clamp(risk, 0m, 100m), 2);
        var damageLevel = ResolveDamageLevel(detectedKeywords.Select(item => item.Keyword).ToArray(), condition);
        var decision = ResolveDecision(riskScore);

        return new RiskScoreResult(
            riskScore,
            damageLevel,
            decision,
            detectedKeywords.Select(item => item.Keyword).ToArray());
    }

    private static string BuildNormalizedText(string title, string? description)
    {
        var combined = $"{title} {description}";
        return ModelNormalizer.Normalize(combined);
    }

    private static IReadOnlyList<(string Keyword, decimal Weight)> DetectKeywords(string normalizedText)
    {
        var detected = new List<(string Keyword, decimal Weight)>();

        foreach (var (keyword, weight) in KeywordWeights)
        {
            if (normalizedText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                detected.Add((keyword, weight));
            }
        }

        return detected;
    }

    private static decimal ConditionWeight(VehicleCondition condition)
    {
        return condition switch
        {
            VehicleCondition.Flooded => 30m,
            VehicleCondition.Scrap => 40m,
            VehicleCondition.Damaged => 22m,
            VehicleCondition.TheftRecovery => 18m,
            _ => 0m
        };
    }

    private static string ResolveDamageLevel(IReadOnlyCollection<string> keywords, VehicleCondition condition)
    {
        if (keywords.Contains("SUCATA"))
        {
            return "SUCATA";
        }

        if (keywords.Contains("GRANDE MONTA"))
        {
            return "GRANDE_MONTA";
        }

        if (keywords.Contains("MEDIA MONTA"))
        {
            return "MEDIA_MONTA";
        }

        if (keywords.Contains("PEQUENA MONTA"))
        {
            return "PEQUENA_MONTA";
        }

        return condition switch
        {
            VehicleCondition.Scrap => "SUCATA",
            VehicleCondition.Flooded => "DANO_RELEVANTE",
            VehicleCondition.Damaged => "DANO_RELEVANTE",
            _ => "SEM_INDICIO"
        };
    }

    private static string ResolveDecision(decimal riskScore)
    {
        if (riskScore >= 70m)
        {
            return "ALTO_RISCO";
        }

        if (riskScore >= 35m)
        {
            return "OPORTUNIDADE_COM_RISCO";
        }

        return "COMPRA_SEGURA";
    }
}
