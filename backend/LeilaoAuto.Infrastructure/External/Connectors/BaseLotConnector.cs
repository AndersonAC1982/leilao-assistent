using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;
using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Base tecnica para conectores de leiloeiros:
/// - centraliza validacao de lotUrl
/// - centraliza parsing de campos comuns
/// - preserva regra: sem lotUrl exata nao retorna lote confirmado
/// </summary>
public abstract class BaseLotConnector : ILotConnector
{
    private readonly IHttpClientFactory _httpClientFactory;
    protected readonly ILogger Logger;

    protected BaseLotConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        Logger = logger;
    }

    public abstract string Name { get; }
    public abstract IReadOnlyCollection<string> SupportedDomains { get; }
    public abstract Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken);
    public abstract Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken);

    public virtual bool ValidateLotUrl(string? url)
    {
        return LotUrlGuard.IsValidLotUrl(url);
    }

    protected HttpClient CreateHttpClient()
    {
        return _httpClientFactory.CreateClient("lot-connectors");
    }

    protected async Task<string?> FetchHtmlAsync(string absoluteUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        var client = CreateHttpClient();
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    protected static IReadOnlyList<string> ExtractAnchorHrefs(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var matches = Regex.Matches(
            html,
            "<a\\s+[^>]*href\\s*=\\s*[\"'](?<href>[^\"']+)[\"'][^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return matches
            .Select(match => WebUtility.HtmlDecode(match.Groups["href"].Value.Trim()))
            .Where(href => !string.IsNullOrWhiteSpace(href))
            .ToList();
    }

    protected static string? ToAbsoluteUrl(string href, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        if (Uri.TryCreate(href, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            return null;
        }

        if (Uri.TryCreate(baseUri, href, out var relativeAbsolute))
        {
            return relativeAbsolute.ToString();
        }

        return null;
    }

    protected static string NormalizeUrlForStorage(string absoluteUrl)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
        {
            return absoluteUrl;
        }

        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };

        return builder.Uri.ToString().TrimEnd('/');
    }

    protected static string NormalizeToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decoded = WebUtility.HtmlDecode(value);
        return ModelNormalizer.Normalize(decoded).Replace(" ", string.Empty);
    }

    protected static string HtmlDecode(string value)
    {
        return WebUtility.HtmlDecode(value ?? string.Empty).Trim();
    }

    protected static decimal? TryParseMoneyPtBr(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var cleaned = raw
            .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(",", ".", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    protected static int? ExtractYearFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var years = Regex.Matches(text, "\\b(19|20)\\d{2}\\b")
            .Select(match => int.TryParse(match.Value, out var year) ? year : 0)
            .Where(year => year >= 1960 && year <= DateTime.UtcNow.Year + 1)
            .ToList();

        return years.Count == 0 ? null : years.Max();
    }

    protected async Task<IReadOnlyList<object>> FetchRawArrayAsync(string endpoint, CancellationToken cancellationToken)
    {
        var client = CreateHttpClient();
        using var response = await client.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        using var jsonDocument = JsonDocument.Parse(raw);
        var root = jsonDocument.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray().Select(ToDictionary).Cast<object>().ToList();
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
            {
                return itemsElement.EnumerateArray().Select(ToDictionary).Cast<object>().ToList();
            }

            return [ToDictionary(root)];
        }

        return [];
    }

    protected static Dictionary<string, object?> EnsureDictionary(object raw)
    {
        if (raw is Dictionary<string, object?> map)
        {
            return map;
        }

        if (raw is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
        {
            return ToDictionary(jsonElement);
        }

        if (raw is string rawString && rawString.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            using var document = JsonDocument.Parse(rawString);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                return ToDictionary(document.RootElement);
            }
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    protected ProviderLotDto? BuildProviderLot(
        IReadOnlyDictionary<string, object?> raw,
        string connectorCode,
        string auctioneerName)
    {
        var externalId = GetString(raw, "externalId", "id", "codigo");
        if (string.IsNullOrWhiteSpace(externalId))
        {
            externalId = $"{connectorCode}-{Guid.NewGuid():N}";
        }

        var lotNumber = GetString(raw, "lotNumber", "lote", "number", "id");
        if (string.IsNullOrWhiteSpace(lotNumber))
        {
            lotNumber = externalId;
        }

        var make = GetString(raw, "make", "brand", "marca");
        var model = GetString(raw, "model", "modelo", "title", "titulo");

        if (string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        var year = GetInt(raw, "year", "ano") ?? DateTime.UtcNow.Year;
        var vehicleType = GetVehicleType(raw, "vehicleType", "type", "tipo") ?? VehicleType.Other;
        var uf = GetString(raw, "uf", "estado", "state", "region");
        if (string.IsNullOrWhiteSpace(uf))
        {
            uf = "SP";
        }

        var vehicleCondition = GetVehicleCondition(raw, "vehicleCondition", "condition", "estadoVeiculo") ?? VehicleCondition.Unknown;
        var status = GetLotStatus(raw, "status", "lotStatus") ?? LotStatus.Active;

        var lotUrl = GetString(raw, "lotUrl", "url", "link");
        if (status == LotStatus.Confirmed && !ValidateLotUrl(lotUrl))
        {
            status = LotStatus.Active;
        }

        if (!ValidateLotUrl(lotUrl))
        {
            return null;
        }

        var currentBid = GetDecimal(raw, "currentBid", "currentPrice", "precoAtual");
        var finalPrice = GetDecimal(raw, "finalPrice", "precoFinal");
        var appraisedValue = GetDecimal(raw, "appraisedValue", "avaliacao");
        var startsAt = GetDateTimeOffset(raw, "startsAt", "inicio");
        var endsAt = GetDateTimeOffset(raw, "endsAt", "fim", "closedAt");

        return new ProviderLotDto(
            ExternalId: externalId,
            Auctioneer: GetString(raw, "auctioneer", "source", "leiloeiro") ?? auctioneerName,
            LotNumber: lotNumber,
            Make: make,
            Model: model,
            Year: year,
            VehicleType: vehicleType,
            Uf: uf.ToUpperInvariant(),
            VehicleCondition: vehicleCondition,
            Status: status,
            LotUrl: lotUrl,
            CurrentBid: currentBid,
            FinalPrice: finalPrice,
            AppraisedValue: appraisedValue,
            StartsAt: startsAt,
            EndsAt: endsAt);
    }

    private static Dictionary<string, object?> ToDictionary(JsonElement element)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in element.EnumerateObject())
        {
            dictionary[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.TryGetInt64(out var longValue)
                    ? longValue
                    : property.Value.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => property.Value.ToString()
            };
        }

        return dictionary;
    }

    private static string GetString(IReadOnlyDictionary<string, object?> raw, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!raw.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            var text = value.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return string.Empty;
    }

    private static int? GetInt(IReadOnlyDictionary<string, object?> raw, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!raw.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            if (int.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static decimal? GetDecimal(IReadOnlyDictionary<string, object?> raw, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!raw.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            if (value is decimal decimalValue)
            {
                return decimalValue;
            }

            if (decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static DateTimeOffset? GetDateTimeOffset(IReadOnlyDictionary<string, object?> raw, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!raw.TryGetValue(key, out var value) || value is null)
            {
                continue;
            }

            if (value is DateTimeOffset dto)
            {
                return dto;
            }

            if (value is DateTime dt)
            {
                return new DateTimeOffset(dt, TimeSpan.Zero);
            }

            if (DateTimeOffset.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private static VehicleType? GetVehicleType(IReadOnlyDictionary<string, object?> raw, params string[] keys)
    {
        var value = GetString(raw, keys);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.TryParse<VehicleType>(value, true, out var enumValue))
        {
            return enumValue;
        }

        return value.ToUpperInvariant() switch
        {
            "CARRO" => VehicleType.Car,
            "MOTO" => VehicleType.Motorcycle,
            "CAMINHAO" => VehicleType.Truck,
            _ => VehicleType.Other
        };
    }

    private static VehicleCondition? GetVehicleCondition(IReadOnlyDictionary<string, object?> raw, params string[] keys)
    {
        var value = GetString(raw, keys);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.TryParse<VehicleCondition>(value, true, out var enumValue))
        {
            return enumValue;
        }

        var normalized = ModelNormalizer.Normalize(value);
        return normalized switch
        {
            "RODANDO" => VehicleCondition.Running,
            "SINISTRO" => VehicleCondition.Damaged,
            "ENCHENTE" => VehicleCondition.Flooded,
            "RECUPERAVEL" => VehicleCondition.TheftRecovery,
            "SUCATA" => VehicleCondition.Scrap,
            _ => VehicleCondition.Unknown
        };
    }

    private LotStatus? GetLotStatus(IReadOnlyDictionary<string, object?> raw, params string[] keys)
    {
        var value = GetString(raw, keys);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.TryParse<LotStatus>(value, true, out var enumValue))
        {
            return enumValue;
        }

        var normalized = ModelNormalizer.Normalize(value);
        return normalized switch
        {
            "ATIVO" => LotStatus.Active,
            "ANDAMENTO" => LotStatus.Active,
            "ENCERRADO" => LotStatus.Closed,
            "CONFIRMADO" => LotStatus.Confirmed,
            _ => LotStatus.Active
        };
    }
}
