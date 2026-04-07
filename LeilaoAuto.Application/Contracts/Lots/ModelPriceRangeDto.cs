namespace LeilaoAuto.Application.Contracts.Lots;

public sealed record ModelPriceRangeDto(
    string ComparableModel,
    decimal AveragePrice,
    decimal MinPrice,
    decimal MaxPrice,
    int Quantity);
