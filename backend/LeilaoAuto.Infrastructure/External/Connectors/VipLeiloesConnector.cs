using System.Text.RegularExpressions;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector real da VIP Leiloes:
/// - coleta links reais de lotes no portal
/// - parseia detalhes do lote por pagina
/// - valida URL exata de anuncio
/// </summary>
public class VipLeiloesConnector : BaseLotConnector
{
    private const string PortalUrl = "https://www.vipleiloes.com.br/";

    public VipLeiloesConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger<VipLeiloesConnector> logger)
        : base(httpClientFactory, options, logger)
    {
    }

    public override string Name => "VipLeiloes";

    public override IReadOnlyCollection<string> SupportedDomains =>
    [
        "vipleiloes.com.br",
        "www.vipleiloes.com.br"
    ];

    public override async Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken)
    {
        try
        {
            var html = await FetchHtmlAsync(PortalUrl, cancellationToken);
            if (string.IsNullOrWhiteSpace(html))
            {
                return BuildMockRawLots("vipleiloes", "Vip Leiloes");
            }

            var links = ExtractAnchorHrefs(html)
                .Select(href => ToAbsoluteUrl(href, PortalUrl))
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => NormalizeUrlForStorage(url!))
                .Where(ValidateLotUrl)
                .Where(url => MatchesFilters(url, filters))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(24)
                .ToList();

            if (links.Count == 0)
            {
                return BuildMockRawLots("vipleiloes", "Vip Leiloes");
            }

            return links
                .Select(url =>
                {
                    var lotNumber = ExtractLotNumberFromUrl(url) ?? Guid.NewGuid().ToString("N")[..8];
                    return (object)new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["externalId"] = lotNumber,
                        ["lotNumber"] = lotNumber,
                        ["lotUrl"] = url,
                        ["auctioneer"] = "Vip Leiloes",
                        ["status"] = "Active"
                    };
                })
                .ToList();
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "VipLeiloes connector failed in real search. Falling back to structured mock.");
            return BuildMockRawLots("vipleiloes", "Vip Leiloes");
        }
    }

    public override async Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        var map = EnsureDictionary(raw);
        var lotUrl = map.TryGetValue("lotUrl", out var value) ? value?.ToString() : null;
        if (!ValidateLotUrl(lotUrl))
        {
            return null;
        }

        var html = await FetchHtmlAsync(lotUrl!, cancellationToken);
        if (string.IsNullOrWhiteSpace(html))
        {
            return BuildProviderLot(map, "vipleiloes", "Vip Leiloes");
        }

        var vehicleText = ExtractTableValue(html, "Veículo");
        var yearText = ExtractTableValue(html, "Ano");
        var locationText = ExtractTableValue(html, "Localização");
        var plateText = ExtractTableValue(html, "Final da placa");
        var provenanceText = ExtractTableValue(html, "Procedência");
        var observations = ExtractObservations(html);

        var (make, model) = ParseMakeAndModel(vehicleText, lotUrl!);
        if (string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        var year = ExtractYearFromText(yearText) ?? ExtractYearFromText(lotUrl!) ?? DateTime.UtcNow.Year;
        var currentBid = ExtractCurrentBid(html);
        var initialBid = ExtractInitialBid(html);
        var status = ResolveStatus(html);
        map.TryGetValue("externalId", out var extValue);

        var externalId = ExtractAnuncioId(html)
                         ?? extValue?.ToString()
                         ?? ExtractLotNumberFromUrl(lotUrl!)
                         ?? $"vip-{Guid.NewGuid():N}";

        var lotNumber = ExtractLotNumberFromUrl(lotUrl!) ?? externalId;
        var uf = ExtractUf(locationText, plateText);

        var vehicleCondition = ResolveVehicleCondition($"{provenanceText} {observations}");
        var vehicleType = ResolveVehicleType(vehicleText, lotUrl!);

        var parsedMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["externalId"] = externalId,
            ["lotNumber"] = lotNumber,
            ["auctioneer"] = "Vip Leiloes",
            ["make"] = make,
            ["model"] = model,
            ["year"] = year,
            ["vehicleType"] = vehicleType.ToString(),
            ["uf"] = uf,
            ["vehicleCondition"] = vehicleCondition.ToString(),
            ["status"] = status.ToString(),
            ["lotUrl"] = NormalizeUrlForStorage(lotUrl!),
            ["currentBid"] = currentBid ?? initialBid,
            ["finalPrice"] = status == LotStatus.Closed ? (currentBid ?? initialBid) : null,
            ["appraisedValue"] = null,
            ["startsAt"] = DateTimeOffset.UtcNow.AddHours(-2),
            ["endsAt"] = status == LotStatus.Closed ? DateTimeOffset.UtcNow.AddHours(-1) : DateTimeOffset.UtcNow.AddHours(6)
        };

        return BuildProviderLot(parsedMap, "vipleiloes", "Vip Leiloes");
    }

    public override bool ValidateLotUrl(string? url)
    {
        if (!base.ValidateLotUrl(url) || string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.Host.Equals("www.vipleiloes.com.br", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.EndsWith(".vipleiloes.com.br", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.Equals("vipleiloes.com.br", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = uri.AbsolutePath.TrimEnd('/').ToLowerInvariant();
        if (!path.StartsWith("/evento/anuncio/", StringComparison.Ordinal))
        {
            return false;
        }

        if (path is "/evento" or "/evento/anuncio" or "/evento/detalhes")
        {
            return false;
        }

        var lastSegment = path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        return Regex.IsMatch(lastSegment, "-\\d{4,}$", RegexOptions.CultureInvariant);
    }

    private static bool MatchesFilters(string url, LotSearchFilterRequest filters)
    {
        var slug = url.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        var normalized = NormalizeToken(slug);

        if (!string.IsNullOrWhiteSpace(filters.Make) && !normalized.Contains(NormalizeToken(filters.Make), StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filters.Model) && !normalized.Contains(NormalizeToken(filters.Model), StringComparison.Ordinal))
        {
            return false;
        }

        if (filters.Year.HasValue && !slug.Contains(filters.Year.Value.ToString(), StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static string? ExtractLotNumberFromUrl(string url)
    {
        var match = Regex.Match(url, "-(\\d{4,})$", RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string ExtractTableValue(string html, string header)
    {
        var pattern = $"<th[^>]*>\\s*{Regex.Escape(header)}\\s*</th>\\s*<td[^>]*>(?<value>.*?)</td>";
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return string.Empty;
        }

        var raw = Regex.Replace(match.Groups["value"].Value, "<[^>]+>", " ");
        return HtmlDecode(raw);
    }

    private static string? ExtractAnuncioId(string html)
    {
        var match = Regex.Match(
            html,
            "id=\"anuncioId\"\\s+value=\"(?<value>[a-f0-9\\-]{20,})\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return match.Success ? match.Groups["value"].Value : null;
    }

    private static string ExtractObservations(string html)
    {
        var block = Regex.Match(
            html,
            "<div class=\"offer-text\">\\s*<p>(?<value>.*?)</p>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        if (!block.Success)
        {
            return string.Empty;
        }

        var raw = Regex.Replace(block.Groups["value"].Value, "<[^>]+>", " ");
        return HtmlDecode(raw);
    }

    private static decimal? ExtractCurrentBid(string html)
    {
        var match = Regex.Match(
            html,
            "data-bind-valorAtual[^>]*>\\s*R\\$\\s*(?<value>[0-9\\.\\,]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return match.Success ? TryParseMoneyPtBr(match.Groups["value"].Value) : null;
    }

    private static decimal? ExtractInitialBid(string html)
    {
        var match = Regex.Match(
            html,
            "data-bind-valorInicial[^>]*>\\s*(?<value>[0-9\\.\\,]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return match.Success ? TryParseMoneyPtBr(match.Groups["value"].Value) : null;
    }

    private static LotStatus ResolveStatus(string html)
    {
        if (Regex.IsMatch(html, "lote\\s+encerrado|leil[aã]o\\s+encerrado", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return LotStatus.Closed;
        }

        return LotStatus.Active;
    }

    private static (string Make, string Model) ParseMakeAndModel(string vehicleText, string lotUrl)
    {
        var decoded = HtmlDecode(vehicleText);
        if (string.IsNullOrWhiteSpace(decoded))
        {
            var slug = lotUrl.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
            var tokens = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 3)
            {
                return (string.Empty, string.Empty);
            }

            var makeFromSlug = tokens[0].ToUpperInvariant();
            var modelFromSlug = string.Join(' ', tokens.Skip(1).Take(tokens.Length - 2)).ToUpperInvariant();
            return (makeFromSlug, modelFromSlug);
        }

        var separators = new[] { " - ", " – ", " — " };
        foreach (var separator in separators)
        {
            var split = decoded.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (split.Length >= 2)
            {
                return (split[0].ToUpperInvariant(), split[1].ToUpperInvariant());
            }
        }

        var words = decoded.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length < 2)
        {
            return (decoded.ToUpperInvariant(), decoded.ToUpperInvariant());
        }

        return (words[0].ToUpperInvariant(), string.Join(' ', words.Skip(1)).ToUpperInvariant());
    }

    private static VehicleType ResolveVehicleType(string vehicleText, string lotUrl)
    {
        var reference = $"{vehicleText} {lotUrl}";
        var normalized = NormalizeToken(reference);
        if (normalized.Contains("MOTO", StringComparison.Ordinal)
            || normalized.Contains("S1000", StringComparison.Ordinal)
            || normalized.Contains("CG", StringComparison.Ordinal))
        {
            return VehicleType.Motorcycle;
        }

        if (normalized.Contains("CAMINHAO", StringComparison.Ordinal)
            || normalized.Contains("CARRETA", StringComparison.Ordinal))
        {
            return VehicleType.Truck;
        }

        if (normalized.Contains("PICKUP", StringComparison.Ordinal)
            || normalized.Contains("RANGER", StringComparison.Ordinal)
            || normalized.Contains("HILUX", StringComparison.Ordinal)
            || normalized.Contains("RENEGADE", StringComparison.Ordinal)
            || normalized.Contains("TORO", StringComparison.Ordinal))
        {
            return VehicleType.Utility;
        }

        return VehicleType.Car;
    }

    private static VehicleCondition ResolveVehicleCondition(string text)
    {
        var normalized = NormalizeToken(text);
        if (normalized.Contains("SUCATA", StringComparison.Ordinal))
        {
            return VehicleCondition.Scrap;
        }

        if (normalized.Contains("ENCHENTE", StringComparison.Ordinal))
        {
            return VehicleCondition.Flooded;
        }

        if (normalized.Contains("SINISTRO", StringComparison.Ordinal)
            || normalized.Contains("GRANDEMONTA", StringComparison.Ordinal)
            || normalized.Contains("MEDIAMONTA", StringComparison.Ordinal)
            || normalized.Contains("PEQUENAMONTA", StringComparison.Ordinal))
        {
            return VehicleCondition.Damaged;
        }

        if (normalized.Contains("RECUPERADO", StringComparison.Ordinal)
            || normalized.Contains("RECUPERAVEL", StringComparison.Ordinal))
        {
            return VehicleCondition.TheftRecovery;
        }

        return VehicleCondition.Running;
    }

    private static string ExtractUf(string locationText, string plateText)
    {
        var locationMatch = Regex.Match(locationText ?? string.Empty, "\\b([A-Z]{2})\\s*-\\s*CEP\\b", RegexOptions.CultureInvariant);
        if (locationMatch.Success)
        {
            return locationMatch.Groups[1].Value;
        }

        var plateMatch = Regex.Match(plateText ?? string.Empty, "-\\s*([A-Z]{2})\\b", RegexOptions.CultureInvariant);
        if (plateMatch.Success)
        {
            return plateMatch.Groups[1].Value;
        }

        return "SP";
    }
}
