using LeilaoAuto.Application.Contracts.Lots;

namespace LeilaoAuto.Application.Abstractions.Services;

public interface ILotService
{
    Task<LotSearchResultDto> SearchAsync(Guid userId, LotSearchFilterRequest filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<LotDto>> GetActiveAsync(LotSearchFilterRequest filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<LotDto>> GetClosedAsync(LotSearchFilterRequest filter, CancellationToken cancellationToken);
    Task<LotDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<int> RefreshAsync(CancellationToken cancellationToken);
}
