namespace LeilaoAuto.Infrastructure.External;

public sealed class FipeOptions
{
    public const string SectionName = "Fipe";

    public bool Enabled { get; init; } = true;
    public string BaseUrl { get; init; } = "https://brasilapi.com.br";
    public string PriceByCodeEndpoint { get; init; } = "/api/fipe/preco/v1/{code}";
    public bool UseEstimatedFallback { get; init; } = true;

    // TODO: Replace this map with a persistent catalog table when the FIPE module evolves.
    public Dictionary<string, string> ModelCodeMappings { get; init; } = [];
}
