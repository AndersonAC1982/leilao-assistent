using LeilaoAuto.Application.Contracts.Lots;

namespace LeilaoAuto.Application.Contracts.Connectors;

public sealed record ConnectorResult(
    string Name,
    IReadOnlyList<string> SupportedDomains,
    int RawItems,
    int ParsedLots,
    int DiscardedItems,
    IReadOnlyList<ProviderLotDto> Lots,
    IReadOnlyList<string> Notes);
