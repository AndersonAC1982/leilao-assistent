using LeilaoAuto.Application.Contracts.Lots;

namespace LeilaoAuto.Application.Abstractions.External;

public interface IAuctionProviderClient
{
    Task<IReadOnlyList<ProviderLotDto>> FetchLatestLotsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProviderLotDto>> FetchLatestLotsAsync(
        LotSearchFilterRequest filters,
        IReadOnlyCollection<string>? activeSources,
        CancellationToken cancellationToken);
}
