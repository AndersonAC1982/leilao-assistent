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
            .OrderByDescending(vehicle => vehicle.CreatedAt)
            .Select(ToDto)
            .ToList();
    }

    public async Task<MonitoredVehicleDto> AddAsync(Guid userId, CreateMonitoredVehicleRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: true, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var vehicle = new MonitoredVehicle(
            userId,
            request.Brand,
            request.Model,
            request.Year,
            request.Type,
            request.Uf,
            request.VehicleState);

        user.AddMonitoredVehicle(vehicle);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return ToDto(vehicle);
    }

    public async Task<MonitoredVehicleDto> UpdateAsync(Guid userId, Guid monitoredVehicleId, UpdateMonitoredVehicleRequest request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: true, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var existing = user.MonitoredVehicles.FirstOrDefault(vehicle => vehicle.Id == monitoredVehicleId)
            ?? throw new KeyNotFoundException("Monitored vehicle not found.");

        existing.Update(
            request.Brand,
            request.Model,
            request.Year,
            request.Type,
            request.Uf,
            request.VehicleState);

        await _userRepository.SaveChangesAsync(cancellationToken);

        return ToDto(existing);
    }

    public async Task RemoveAsync(Guid userId, Guid monitoredVehicleId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, includeVehicles: true, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        var existing = user.MonitoredVehicles.FirstOrDefault(vehicle => vehicle.Id == monitoredVehicleId)
            ?? throw new KeyNotFoundException("Monitored vehicle not found.");

        user.RemoveMonitoredVehicle(existing.Id);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }

    private static MonitoredVehicleDto ToDto(MonitoredVehicle vehicle)
    {
        return new MonitoredVehicleDto(
            vehicle.Id,
            vehicle.Brand,
            vehicle.Model,
            vehicle.Year,
            vehicle.Type,
            vehicle.Uf,
            vehicle.VehicleState,
            vehicle.CreatedAt);
    }
}
