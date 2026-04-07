using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Abstractions.Persistence;

public interface IAuctionLotRepository
{
    Task<IReadOnlyList<AuctionLot>> GetActiveLotsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AuctionLot>> GetClosedLotsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AuctionLot>> SearchActiveAsync(LotSearchFilterRequest filter, CancellationToken cancellationToken);
    Task<AuctionLot?> FindExactActiveAsync(string auctioneer, string lotNumber, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuctionLot>> GetClosedByNormalizedModelsAsync(
        IReadOnlyCollection<string> normalizedModels,
        LotSearchFilterRequest? filter,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, decimal>> GetAverageFinalPriceByNormalizedModelsAsync(
        IReadOnlyCollection<string> normalizedModels,
        CancellationToken cancellationToken);
    Task UpsertRangeAsync(IEnumerable<AuctionLot> lots, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
