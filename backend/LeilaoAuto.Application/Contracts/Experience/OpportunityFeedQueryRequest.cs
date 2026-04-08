namespace LeilaoAuto.Application.Contracts.Experience;

public sealed class OpportunityFeedQueryRequest
{
    public string? Search { get; init; }
    public string? Source { get; init; }
    public decimal? MinScore { get; init; }
    public int? VehicleType { get; init; }
    public string? Region { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public string? Uf { get; init; }
    public int? VehicleCondition { get; init; }
}
