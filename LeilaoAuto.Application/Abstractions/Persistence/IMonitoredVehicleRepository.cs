using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Abstractions.Persistence;

public interface IMonitoredVehicleRepository : IBaseRepository<MonitoredVehicle>
{
    Task<IReadOnlyList<MonitoredVehicle>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
