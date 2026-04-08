namespace LeilaoAuto.Application.Contracts.Lots;

public sealed record LotSearchResultDto(
    IReadOnlyList<LotDto> ActiveLots,
    IReadOnlyList<LotDto> ClosedLots,
    IReadOnlyList<ModelPriceRangeDto> Averages);
