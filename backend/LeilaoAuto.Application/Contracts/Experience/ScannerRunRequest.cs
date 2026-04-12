namespace LeilaoAuto.Application.Contracts.Experience;

public sealed class ScannerRunRequest
{
    public string Search { get; init; } = string.Empty;
    public string Category { get; init; } = "Todas";
    public IReadOnlyList<string>? ActiveSources { get; init; }
    public decimal MinScore { get; init; } = 60;
    public string? Region { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? VehicleType { get; init; }
}
