using FluentAssertions;
using LeilaoAuto.Domain.Common;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;

namespace LeilaoAuto.Tests;

public class DomainRulesTests
{
    [Fact]
    public void User_Should_NotAllow_MoreThanFourMonitoredVehicles()
    {
        var user = new User("teste@leilaoauto.local", "hash");

        for (var index = 0; index < 4; index++)
        {
            user.AddMonitoredVehicle(new MonitoredVehicle(
                user.Id,
                "Marca",
                $"Modelo {index}",
                2018,
                2022,
                VehicleType.Car,
                "SP",
                VehicleCondition.Unknown));
        }

        var action = () => user.AddMonitoredVehicle(new MonitoredVehicle(
            user.Id,
            "Marca",
            "Modelo extra",
            2018,
            2022,
            VehicleType.Car,
            "SP",
            VehicleCondition.Unknown));

        action.Should().Throw<DomainRuleException>();
    }

    [Fact]
    public void LotUrlGuard_Should_Reject_HomePageUrls()
    {
        LotUrlGuard.IsValidLotUrl("https://meu-leiloeiro.com/").Should().BeFalse();
        LotUrlGuard.IsValidLotUrl("https://meu-leiloeiro.com/home").Should().BeFalse();
        LotUrlGuard.IsValidLotUrl("https://meu-leiloeiro.com/lote/12345").Should().BeTrue();
    }

    [Fact]
    public void ModelNormalizer_Should_Remove_Accents_And_Noise()
    {
        var normalized = ModelNormalizer.Normalize(" Gol   1.6   MSI \u00C1lcool ");
        normalized.Should().Be("GOL 1 6 MSI ALCOOL");
    }

    [Fact]
    public void LotScoring_Should_Produce_OpportunityAndRiskValues()
    {
        var lot = new AuctionLot(
            externalId: "test-001",
            auctioneer: "Leiloeiro Teste",
            lotNumber: "5001",
            make: "Volkswagen",
            model: "Gol 1.6 MSI",
            year: 2018,
            vehicleType: VehicleType.Car,
            uf: "SP",
            vehicleCondition: VehicleCondition.Damaged,
            status: LotStatus.Active,
            lotUrl: "https://teste.com/lote/5001",
            currentBid: 21000m,
            finalPrice: null,
            appraisedValue: 30000m,
            startsAt: DateTimeOffset.UtcNow,
            endsAt: DateTimeOffset.UtcNow.AddHours(5));

        var opportunity = LotScoring.CalculateOpportunityScore(lot, 32000m);
        var risk = LotScoring.CalculateRiskScore(lot);

        opportunity.Should().BeGreaterThan(0);
        risk.Should().BeGreaterThan(0);
    }
}

