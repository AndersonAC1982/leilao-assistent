namespace LeilaoAuto.Infrastructure.External;

public sealed class AuctionProviderOptions
{
    public const string SectionName = "AuctionProviders:Primary";

    public bool MockMode { get; init; } = true;
    public string BaseUrl { get; init; } = "https://example-leiloeiro.local";
    public string LotsEndpoint { get; init; } = "/api/lots";
    public string? ApiKey { get; init; }
}
