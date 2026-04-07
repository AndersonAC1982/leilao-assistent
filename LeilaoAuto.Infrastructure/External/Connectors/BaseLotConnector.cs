using System.Globalization;
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
    private readonly AuctionProviderOptions _options;
    protected readonly ILogger Logger;

    protected BaseLotConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
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

    protected static IReadOnlyList<object> BuildMockRawLots(string connectorCode, string auctioneer)
    {
        return
        [
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["externalId"] = $"{connectorCode}-active-001",
                ["auctioneer"] = auctioneer,
                ["lotNumber"] = "1001",
                ["make"] = "Volkswagen",
                ["model"] = "Gol 1.6 MSI",
                ["year"] = 2020,
                ["vehicleType"] = "Car",
                ["uf"] = "SP",
                ["vehicleCondition"] = "Running",
                ["status"] = "Active",
                ["lotUrl"] = $"https://{connectorCode}.example/lote/1001",
                ["currentBid"] = 27900m,
                ["finalPrice"] = null,
                ["appraisedValue"] = 34500m,
                ["startsAt"] = DateTimeOffset.UtcNow.AddHours(-2),
                ["endsAt"] = DateTimeOffset.UtcNow.AddHours(5)
            },
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["externalId"] = $"{connectorCode}-closed-001",
                ["auctioneer"] = auctioneer,
                ["lotNumber"] = "2001",
                ["make"] = "Honda",
                ["model"] = "CG 160 Fan",
                ["year"] = 2022,
                ["vehicleType"] = "Motorcycle",
                ["uf"] = "MG",
                ["vehicleCondition"] = "Running",
                ["status"] = "Closed",
                ["lotUrl"] = $"https://{connectorCode}.example/lote/2001",
                ["currentBid"] = null,
                ["finalPrice"] = 9800m,
                ["appraisedValue"] = 10900m,
                ["startsAt"] = DateTimeOffset.UtcNow.AddDays(-5),
                ["endsAt"] = DateTimeOffset.UtcNow.AddDays(-4)
            }
        ];
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
