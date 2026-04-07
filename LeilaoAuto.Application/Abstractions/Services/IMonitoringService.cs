using LeilaoAuto.Application.Contracts.Monitoring;

namespace LeilaoAuto.Application.Abstractions.Services;

public interface IMonitoringService
{
    Task<IReadOnlyList<MonitoredVehicleDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<MonitoredVehicleDto> AddAsync(Guid userId, CreateMonitoredVehicleRequest request, CancellationToken cancellationToken);
    Task<MonitoredVehicleDto> UpdateAsync(Guid userId, Guid monitoredVehicleId, UpdateMonitoredVehicleRequest request, CancellationToken cancellationToken);
    Task RemoveAsync(Guid userId, Guid monitoredVehicleId, CancellationToken cancellationToken);
}
