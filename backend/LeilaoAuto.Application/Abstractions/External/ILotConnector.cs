using LeilaoAuto.Application.Contracts.Lots;

namespace LeilaoAuto.Application.Abstractions.External;

public interface ILotConnector
{
    string Name { get; }
    IReadOnlyCollection<string> SupportedDomains { get; }
    Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken);
    Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken);
    bool ValidateLotUrl(string? url);
}
