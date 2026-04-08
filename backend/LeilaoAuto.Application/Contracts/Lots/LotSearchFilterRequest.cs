using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Lots;

public sealed class LotSearchFilterRequest
{
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public VehicleType? VehicleType { get; init; }
    public string? Uf { get; init; }
    public VehicleCondition? VehicleCondition { get; init; }
}
