using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Monitoring;
using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Services;

public class MonitoringService : IMonitoringService
{
    private readonly IUserRepository _userRepository;

    public MonitoringService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<MonitoredVehicleDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: true, cancellationToken);
        if (user is null)
        {
            return [];
        }

        return user.MonitoredVehicles
            .OrderByDescending(vehicle => vehicle.CreatedAtUtc)
            .Select(ToDto)
            .ToList();
    }

    public async Task<MonitoredVehicleDto> AddAsync(Guid userId, CreateMonitoredVehicleRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: true, cancellationToken)
                   ?? throw new KeyNotFoundException("Usuário não encontrado.");

        var vehicle = new MonitoredVehicle(
            userId,
            request.Make,
            request.Model,
            request.YearFrom,
            request.YearTo,
            request.VehicleType,
            request.Uf,
            request.VehicleCondition);

        user.AddMonitoredVehicle(vehicle);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return ToDto(vehicle);
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid monitoredVehicleId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: true, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var existing = user.MonitoredVehicles.FirstOrDefault(vehicle => vehicle.Id == monitoredVehicleId);
        if (existing is null)
        {
            return false;
        }

        user.RemoveMonitoredVehicle(monitoredVehicleId);
        await _userRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static MonitoredVehicleDto ToDto(MonitoredVehicle vehicle)
    {
        return new MonitoredVehicleDto(
            vehicle.Id,
            vehicle.Make,
            vehicle.Model,
            vehicle.YearFrom,
            vehicle.YearTo,
            vehicle.VehicleType,
            vehicle.Uf,
            vehicle.VehicleCondition,
            vehicle.CreatedAtUtc);
    }
}
