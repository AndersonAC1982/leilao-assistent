using LeilaoAuto.Application.Contracts.Analytics;
using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Abstractions.Services;

public interface ILotAnalyticsComputationService
{
    IReadOnlyList<ModelAveragePriceDto> GroupAndCalculateModelPrices(IEnumerable<AuctionLot> closedLots);
}
