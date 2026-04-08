namespace LeilaoAuto.Application.Contracts.Analytics;

public sealed record ModelAveragePriceDto(
    string ComparableModel,
    decimal AveragePrice,
    decimal MinPrice,
    decimal MaxPrice,
    int Quantity);
