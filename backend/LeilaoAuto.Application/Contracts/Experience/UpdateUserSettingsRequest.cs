namespace LeilaoAuto.Application.Contracts.Experience;

public sealed class UpdateUserSettingsRequest
{
    public string Search { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public decimal MinScore { get; init; }
    public int? VehicleType { get; init; }
    public string? Region { get; init; }
    public bool AdvancedFiltersEnabled { get; init; }
}
