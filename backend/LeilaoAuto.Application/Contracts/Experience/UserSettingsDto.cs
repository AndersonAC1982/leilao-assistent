namespace LeilaoAuto.Application.Contracts.Experience;

public sealed record UserSettingsDto(
    string Search,
    string Source,
    decimal MinScore,
    int? VehicleType,
    string? Region,
    bool AdvancedFiltersEnabled,
    DateTimeOffset UpdatedAtUtc);
