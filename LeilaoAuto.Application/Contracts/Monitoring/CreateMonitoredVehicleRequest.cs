using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Monitoring;

public sealed record CreateMonitoredVehicleRequest(
    string Make,
    string Model,
    int? YearFrom,
    int? YearTo,
    VehicleType? VehicleType,
    string? Uf,
    VehicleCondition? VehicleCondition);
