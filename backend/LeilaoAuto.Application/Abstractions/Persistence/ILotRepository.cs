using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Abstractions.Persistence;

public interface ILotRepository : IBaseRepository<Lot>
{
    Task<IReadOnlyList<Lot>> GetActiveLotsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Lot>> GetClosedLotsAsync(CancellationToken cancellationToken);
}
