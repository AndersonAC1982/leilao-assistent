using FluentAssertions;
using LeilaoAuto.Application.Services;
using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Tests;

public class ScoringPhase5Tests
{
    [Fact]
    public void OpportunityScoring_Should_Return_Oportunidade_When_Price_Is_Well_Below_Average()
    {
        var service = new OpportunityScoringService();

        var result = service.Score(currentPrice: 20000m, finalPrice: null, historicalAveragePrice: 30000m);

        result.Score.Should().BeGreaterThan(70m);
        result.Label.Should().Be("OPORTUNIDADE");
    }

    [Fact]
    public void OpportunityScoring_Should_Return_AcimaDaMedia_When_Price_Is_Above_Average()
    {
        var service = new OpportunityScoringService();

        var result = service.Score(currentPrice: 34000m, finalPrice: null, historicalAveragePrice: 30000m);

        result.Label.Should().Be("ACIMA_DA_MEDIA");
    }

    [Fact]
    public void RiskScoring_Should_Detect_Critical_Keywords_With_Accent_Insensitive_Matching()
    {
        var service = new RiskScoringService();

        var result = service.Score(
            title: "Honda CG 160 sinistro",
            description: "veiculo recuperável de enchente e sem motor",
            condition: VehicleCondition.Unknown,
            year: 2022,
            hasValidLotUrl: true);

        result.CriticalKeywords.Should().Contain("SINISTRO");
        result.CriticalKeywords.Should().Contain("RECUPERAVEL");
        result.CriticalKeywords.Should().Contain("ENCHENTE");
        result.CriticalKeywords.Should().Contain("SEM MOTOR");
    }

    [Fact]
    public void RiskScoring_Should_Return_AltoRisco_For_Severe_Damage_Text()
    {
        var service = new RiskScoringService();

        var result = service.Score(
            title: "Lote com grande monta",
            description: "veiculo sucata com sinistro",
            condition: VehicleCondition.Scrap,
            year: 2010,
            hasValidLotUrl: true);

        result.RiskScore.Should().BeGreaterThanOrEqualTo(70m);
        result.Decision.Should().Be("ALTO_RISCO");
    }
}
