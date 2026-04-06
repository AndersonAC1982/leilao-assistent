using LeilaoAuto.Application.Contracts.Lots;

namespace LeilaoAuto.Application.Abstractions.Services;

public interface ILotService
{
    Task<IReadOnlyList<LotDto>> SearchActiveAsync(Guid userId, LotSearchFilterRequest filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<LotDto>> GetClosedHistoryBySimilarityAsync(Guid userId, LotSearchFilterRequest? filter, CancellationToken cancellationToken);
    Task<LotDto?> FindExactActiveAsync(ExactLotRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ModelAverageDto>> GetModelAveragesByUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<int> SyncLatestLotsAsync(CancellationToken cancellationToken);
}
