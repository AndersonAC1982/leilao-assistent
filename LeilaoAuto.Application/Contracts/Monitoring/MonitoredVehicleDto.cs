using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Monitoring;

public sealed record MonitoredVehicleDto(
    Guid Id,
    string Brand,
    string Model,
    int Year,
    VehicleType Type,
    string Uf,
    VehicleCondition VehicleState,
    DateTime CreatedAt);
