using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Domain.Services;

public static class LotScoring
{
    public static decimal CalculateOpportunityScore(AuctionLot lot, decimal? modelAveragePrice, decimal? fipeReferencePrice = null)
    {
        var referencePrice = modelAveragePrice ?? fipeReferencePrice;
        if (!referencePrice.HasValue || referencePrice <= 0 || !lot.CurrentBid.HasValue || lot.CurrentBid <= 0)
        {
            return 0;
        }

        var discount = (referencePrice.Value - lot.CurrentBid.Value) / referencePrice.Value;
        var bidGapScore = decimal.Clamp(discount * 120, 0, 100);

        if (lot.VehicleCondition is VehicleCondition.Running or VehicleCondition.Unknown)
        {
            bidGapScore += 5;
        }

        return decimal.Round(decimal.Clamp(bidGapScore, 0, 100), 2);
    }

    public static decimal CalculateRiskScore(AuctionLot lot)
    {
        decimal risk = 0;

        if (lot.VehicleCondition is VehicleCondition.Damaged or VehicleCondition.Flooded or VehicleCondition.Scrap)
        {
            risk += 35;
        }
        else if (lot.VehicleCondition == VehicleCondition.TheftRecovery)
        {
            risk += 25;
        }

        if (lot.Year <= DateTime.UtcNow.Year - 12)
        {
            risk += 20;
        }

        if (!LotUrlGuard.IsValidLotUrl(lot.LotUrl))
        {
            risk += 40;
        }

        if (lot.Status == LotStatus.Closed)
        {
            risk += 5;
        }

        return decimal.Round(decimal.Clamp(risk, 0, 100), 2);
    }
}
