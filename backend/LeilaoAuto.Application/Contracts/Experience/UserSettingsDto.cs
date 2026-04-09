namespace LeilaoAuto.Application.Contracts.Experience;

public sealed record UserSettingsDto(
    string Search,
    string Source,
    decimal MinScore,
    int? VehicleType,
    string? Region,
    bool AdvancedFiltersEnabled,
    string Category,
    IReadOnlyList<string> ActiveSources,
    decimal? MaxPrice,
    DateTimeOffset UpdatedAtUtc);
