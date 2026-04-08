using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LeilaoAuto.Application.Abstractions.External;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External;

public sealed class FipeHttpPriceProvider : IFipePriceProvider
{
    private readonly HttpClient _httpClient;
    private readonly FipeOptions _options;
    private readonly ILogger<FipeHttpPriceProvider> _logger;
    private readonly Dictionary<string, string> _modelToCodeMap;

    public FipeHttpPriceProvider(
        HttpClient httpClient,
        IOptions<FipeOptions> options,
        ILogger<FipeHttpPriceProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _modelToCodeMap = new Dictionary<string, string>(_options.ModelCodeMappings, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<decimal?> GetPriceByNormalizedModelAsync(string normalizedModel, int year, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedModel))
        {
            return null;
        }

        if (!_options.Enabled)
        {
            return BuildEstimatedFallback(normalizedModel, year);
        }

        if (!_modelToCodeMap.TryGetValue(normalizedModel, out var fipeCode) || string.IsNullOrWhiteSpace(fipeCode))
        {
            _logger.LogDebug(
                "FIPE code mapping not found for model {Model}. Configure Fipe:ModelCodeMappings in appsettings.",
                normalizedModel);

            return _options.UseEstimatedFallback
                ? BuildEstimatedFallback(normalizedModel, year)
                : null;
        }

        try
        {
            var endpoint = _options.PriceByCodeEndpoint.Replace("{code}", Uri.EscapeDataString(fipeCode), StringComparison.OrdinalIgnoreCase);
            var response = await _httpClient.GetFromJsonAsync<List<FipePriceEntry>>(endpoint, cancellationToken);
            if (response is null || response.Count == 0)
            {
                return _options.UseEstimatedFallback
                    ? BuildEstimatedFallback(normalizedModel, year)
                    : null;
            }

            var selected = response
                .OrderBy(entry => Math.Abs(entry.YearModel - year))
                .FirstOrDefault();

            if (selected is null)
            {
                return _options.UseEstimatedFallback
                    ? BuildEstimatedFallback(normalizedModel, year)
                    : null;
            }

            var parsed = TryParseBrazilianCurrency(selected.Price);
            if (parsed.HasValue)
            {
                return parsed;
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to fetch FIPE price for model {Model} and code {Code}.", normalizedModel, fipeCode);
        }

        return _options.UseEstimatedFallback
            ? BuildEstimatedFallback(normalizedModel, year)
            : null;
    }

    private static decimal? TryParseBrazilianCurrency(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Currency, new CultureInfo("pt-BR"), out var direct))
        {
            return decimal.Round(direct, 2);
        }

        var sanitized = rawValue
            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return decimal.TryParse(sanitized, NumberStyles.Number, new CultureInfo("pt-BR"), out var normalized)
            ? decimal.Round(normalized, 2)
            : null;
    }

    private static decimal? BuildEstimatedFallback(string normalizedModel, int year)
    {
        var seed = normalizedModel.GetHashCode(StringComparison.OrdinalIgnoreCase);
        var agePenalty = Math.Max(0, DateTime.UtcNow.Year - year) * 400;
        var baseline = Math.Abs(seed % 30000) + 25000 - agePenalty;
        return baseline > 0 ? decimal.Round(baseline, 2) : null;
    }

    private sealed record FipePriceEntry(
        [property: JsonPropertyName("valor")] string Price,
        [property: JsonPropertyName("anoModelo")] int YearModel);
}
