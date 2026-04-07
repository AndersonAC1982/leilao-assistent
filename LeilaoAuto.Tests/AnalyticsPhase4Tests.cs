using FluentAssertions;
using LeilaoAuto.Application.Services;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;

namespace LeilaoAuto.Tests;

public class AnalyticsPhase4Tests
{
    [Fact]
    public void ModelNormalizer_Should_Normalize_And_Match_Equivalent_Models()
    {
        var normalizedA = ModelNormalizer.NormalizeComparable("Honda CG 160 Start", "Honda");
        var normalizedB = ModelNormalizer.NormalizeComparable("CG160 START");
        var normalizedC = ModelNormalizer.NormalizeComparable("Honda CG 160 2022", "Honda");

        normalizedA.Should().Be("CG 160 START");
        normalizedB.Should().Be("CG 160 START");
        normalizedC.Should().Be("CG 160");

        ModelMatcher.IsMatch("Honda CG 160 Start", "CG160 START", 0.6).Should().BeTrue();
        ModelMatcher.IsMatch("Honda CG 160 Start", "Honda CG 160 2022", 0.6).Should().BeTrue();
    }

    [Fact]
    public void LotAnalyticsComputation_Should_Group_Similar_Models()
    {
        var service = new LotAnalyticsComputationService(new ModelNormalizationService());

        var lots = new[]
        {
            CreateClosedLot("seed-1", "Honda", "Honda CG 160 Start", 10000m),
            CreateClosedLot("seed-2", "Honda", "CG160 START", 12000m),
            CreateClosedLot("seed-3", "Honda", "Honda CG 160 2022", 11000m)
        };

        var grouped = service.GroupAndCalculateModelPrices(lots);

        grouped.Should().ContainSingle();
        grouped.Single().ComparableModel.Should().Be("CG 160 START");
    }

    [Fact]
    public void LotAnalyticsComputation_Should_Calculate_Average_Min_Max_And_Quantity()
    {
        var service = new LotAnalyticsComputationService(new ModelNormalizationService());

        var lots = new[]
        {
            CreateClosedLot("seed-11", "Honda", "CG 160 START", 10000m),
            CreateClosedLot("seed-12", "Honda", "Honda CG160 Start", 12000m),
            CreateClosedLot("seed-13", "Honda", "CG 160 2022", 11000m)
        };

        var analytics = service.GroupAndCalculateModelPrices(lots).Single();

        analytics.Quantity.Should().Be(3);
        analytics.AveragePrice.Should().Be(11000m);
        analytics.MinPrice.Should().Be(10000m);
        analytics.MaxPrice.Should().Be(12000m);
    }

    private static AuctionLot CreateClosedLot(string externalId, string make, string model, decimal finalPrice)
    {
        return new AuctionLot(
            externalId: externalId,
            auctioneer: "Leiloeiro Teste",
            lotNumber: $"LT-{externalId}",
            make: make,
            model: model,
            year: 2022,
            vehicleType: VehicleType.Motorcycle,
            uf: "SP",
            vehicleCondition: VehicleCondition.Running,
            status: LotStatus.Closed,
            lotUrl: $"https://teste.com/lote/{externalId}",
            currentBid: null,
            finalPrice: finalPrice,
            appraisedValue: finalPrice + 1000m,
            startsAt: DateTimeOffset.UtcNow.AddDays(-10),
            endsAt: DateTimeOffset.UtcNow.AddDays(-8));
    }
}
