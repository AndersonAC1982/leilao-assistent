using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector real da Mega Leiloes:
/// - coleta links reais em /veiculos/carros e /veiculos/motos
/// - parseia detalhes principais do lote (titulo, valores, localidade, lote)
/// - valida URL exata do lote por padrao do dominio
/// </summary>
public class MegaLeiloesConnector : BaseLotConnector
{
    private const string PortalUrl = "https://www.megaleiloes.com.br/";
    private const string CarsUrl = "https://www.megaleiloes.com.br/veiculos/carros";
    private const string MotosUrl = "https://www.megaleiloes.com.br/veiculos/motos";

    public MegaLeiloesConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger<MegaLeiloesConnector> logger)
        : base(httpClientFactory, options, logger)
    {
    }

    public override string Name => "MegaLeiloes";

    public override IReadOnlyCollection<string> SupportedDomains =>
    [
        "megaleiloes.com.br",
        "www.megaleiloes.com.br"
    ];

    public override async Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken)
    {
        try
        {
            var searchUrls = GetSearchUrlsByFilter(filters);
            var lotUrls = new List<string>();

            foreach (var searchUrl in searchUrls)
            {
                var html = await FetchHtmlAsync(searchUrl, cancellationToken);
                if (string.IsNullOrWhiteSpace(html))
                {
                    continue;
                }

                var urls = ExtractAnchorHrefs(html)
                    .Select(href => ToAbsoluteUrl(href, PortalUrl))
                    .Where(url => !string.IsNullOrWhiteSpace(url))
                    .Select(url => NormalizeUrlForStorage(url!))
                    .Where(ValidateLotUrl)
                    .Where(url => MatchesFilters(url, filters))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(50);

                lotUrls.AddRange(urls);
            }

            lotUrls = lotUrls
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(40)
                .ToList();

            if (lotUrls.Count == 0)
            {
                return [];
            }

            return lotUrls
                .Select(url =>
                {
                    var lotNumber = ExtractLotNumberFromUrl(url) ?? Guid.NewGuid().ToString("N")[..8];
                    return (object)new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["externalId"] = lotNumber,
                        ["lotNumber"] = lotNumber,
                        ["lotUrl"] = url,
                        ["auctioneer"] = "Mega Leiloes",
                        ["status"] = "Active"
                    };
                })
                .ToList();
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "MegaLeiloes connector failed in real search. Returning empty result.");
            return [];
        }
    }

    public override async Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        var map = EnsureDictionary(raw);
        var lotUrl = map.TryGetValue("lotUrl", out var lotUrlValue) ? lotUrlValue?.ToString() : null;
        if (!ValidateLotUrl(lotUrl))
        {
            return null;
        }

        var html = await FetchHtmlAsync(lotUrl!, cancellationToken);
        if (string.IsNullOrWhiteSpace(html))
        {
            return BuildProviderLot(map, "megaleiloes", "Mega Leiloes");
        }

        var title = ExtractTitle(html, lotUrl!);
        var lotValues = ExtractHeaderValues(html);

        var description = ExtractDescription(html);
        var lastBidText = TryGetHeaderValue(lotValues, "Último Lance", "Ultimo Lance");
        var initialPriceText = TryGetHeaderValue(lotValues, "Valor inicial");
        var appraisalText = TryGetHeaderValue(lotValues, "Valor de Avaliação", "Valor de Avaliacao");
        var lotCode = TryGetHeaderValue(lotValues, "Código Lote", "Codigo Lote") ?? ExtractLotNumberFromUrl(lotUrl!);

        var (make, model) = ParseMakeAndModel(title, lotUrl!);
        if (string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        var year = ExtractYearFromText(title) ?? ExtractYearFromText(description) ?? DateTime.UtcNow.Year;
        var vehicleType = ResolveVehicleType(lotUrl!, title);
        var uf = ResolveUf(lotUrl!, TryGetHeaderValue(lotValues, "Localização", "Localizacao"));
        var condition = ResolveVehicleCondition($"{title} {description}");
        var status = ResolveStatus(html, lastBidText);

        var currentBid = TryParseMoneyPtBr(lastBidText) ?? TryParseMoneyPtBr(initialPriceText);
        var finalPrice = status == LotStatus.Closed ? currentBid : null;
        var appraisal = TryParseMoneyPtBr(appraisalText);
        var normalizedLotUrl = NormalizeUrlForStorage(lotUrl!);

        var parsedMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["externalId"] = lotCode ?? ExtractLotNumberFromUrl(normalizedLotUrl) ?? $"mega-{Guid.NewGuid():N}",
            ["lotNumber"] = lotCode ?? ExtractLotNumberFromUrl(normalizedLotUrl) ?? "N/A",
            ["auctioneer"] = "Mega Leiloes",
            ["make"] = make,
            ["model"] = model,
            ["year"] = year,
            ["vehicleType"] = vehicleType.ToString(),
            ["uf"] = uf,
            ["vehicleCondition"] = condition.ToString(),
            ["status"] = status.ToString(),
            ["lotUrl"] = normalizedLotUrl,
            ["currentBid"] = currentBid,
            ["finalPrice"] = finalPrice,
            ["appraisedValue"] = appraisal,
            ["startsAt"] = DateTimeOffset.UtcNow.AddHours(-2),
            ["endsAt"] = status == LotStatus.Closed ? DateTimeOffset.UtcNow.AddHours(-1) : DateTimeOffset.UtcNow.AddHours(8)
        };

        return BuildProviderLot(parsedMap, "megaleiloes", "Mega Leiloes");
    }

    public override bool ValidateLotUrl(string? url)
    {
        if (!base.ValidateLotUrl(url) || string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.Host.Equals("www.megaleiloes.com.br", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.EndsWith(".megaleiloes.com.br", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.Equals("megaleiloes.com.br", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = uri.AbsolutePath.TrimEnd('/').ToLowerInvariant();
        if (!path.StartsWith("/veiculos/", StringComparison.Ordinal))
        {
            return false;
        }

        if (path is "/veiculos" or "/veiculos/carros" or "/veiculos/motos" or "/veiculos/caminhoes" or "/veiculos/onibus")
        {
            return false;
        }

        var lastSegment = path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        return Regex.IsMatch(lastSegment, @"-[jx]\d{5,}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static IReadOnlyList<string> GetSearchUrlsByFilter(LotSearchFilterRequest filters)
    {
        if (filters.VehicleType == VehicleType.Motorcycle)
        {
            return [MotosUrl];
        }

        if (filters.VehicleType is VehicleType.Car or VehicleType.Utility or VehicleType.Truck or null)
        {
            return [CarsUrl, MotosUrl];
        }

        return [CarsUrl];
    }

    private static bool MatchesFilters(string lotUrl, LotSearchFilterRequest filters)
    {
        var slug = lotUrl.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        var normalized = NormalizeToken(slug);

        if (!string.IsNullOrWhiteSpace(filters.Make)
            && !normalized.Contains(NormalizeToken(filters.Make), StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filters.Model)
            && !normalized.Contains(NormalizeToken(filters.Model), StringComparison.Ordinal))
        {
            return false;
        }

        if (filters.Year.HasValue && !slug.Contains(filters.Year.Value.ToString(), StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filters.Uf)
            && !lotUrl.Contains($"/{filters.Uf.Trim().ToLowerInvariant()}/", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private static string ExtractTitle(string html, string lotUrl)
    {
        var h1 = Regex.Match(
            html,
            "<h1[^>]*class=\"section-header\"[^>]*>(?<value>.*?)</h1>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        if (h1.Success)
        {
            var text = Regex.Replace(h1.Groups["value"].Value, "<[^>]+>", " ");
            return HtmlDecode(text);
        }

        var title = Regex.Match(
            html,
            "<title>(?<value>.*?)</title>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        if (title.Success)
        {
            var normalized = HtmlDecode(title.Groups["value"].Value);
            var separatorIndex = normalized.IndexOf('|');
            if (separatorIndex > 0)
            {
                return normalized[..separatorIndex].Trim();
            }

            return normalized;
        }

        var slug = lotUrl.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        return slug.Replace("-", " ", StringComparison.Ordinal).ToUpperInvariant();
    }

    private static Dictionary<string, string> ExtractHeaderValues(string html)
    {
        var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var matches = Regex.Matches(
            html,
            "<div\\s+class=\"header\">\\s*(?<header>[^<]+?)\\s*</div>\\s*<div\\s+class=\"value\">\\s*(?<value>.*?)\\s*</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        foreach (Match match in matches)
        {
            var header = HtmlDecode(match.Groups["header"].Value);
            var rawValue = Regex.Replace(match.Groups["value"].Value, "<[^>]+>", " ");
            var value = Regex.Replace(HtmlDecode(rawValue), "\\s+", " ");
            if (!string.IsNullOrWhiteSpace(header) && !dictionary.ContainsKey(header))
            {
                dictionary[header] = value.Trim();
            }
        }

        return dictionary;
    }

    private static string? TryGetHeaderValue(IReadOnlyDictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string ExtractDescription(string html)
    {
        var match = Regex.Match(
            html,
            "<div\\s+id=\"tab-description\"[^>]*>\\s*<div\\s+class=\"content\">(?<value>.*?)</div>\\s*</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return string.Empty;
        }

        var raw = Regex.Replace(match.Groups["value"].Value, "<[^>]+>", " ");
        return Regex.Replace(HtmlDecode(raw), "\\s+", " ").Trim();
    }

    private static string? ExtractLotNumberFromUrl(string url)
    {
        var match = Regex.Match(url, "-([jx]\\d{5,})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    private static (string Make, string Model) ParseMakeAndModel(string title, string lotUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return ParseFromSlug(lotUrl);
        }

        var cleanedTitle = Regex.Replace(title, "\\s+", " ").Trim();
        var noPrefix = Regex.Replace(cleanedTitle, "^(CARRO|VEICULO|CAMINHONETE|MOTO|ONIBUS|JIPE|DIREITOS SOBRE CARRO)\\s+", string.Empty, RegexOptions.IgnoreCase);
        var withoutYear = Regex.Replace(noPrefix, "\\s*[-–—]\\s*(19|20)\\d{2}(\\/(19|20)\\d{2})?\\s*$", string.Empty, RegexOptions.CultureInvariant);
        var tokens = withoutYear.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length < 2)
        {
            return ParseFromSlug(lotUrl);
        }

        var make = tokens[0].ToUpperInvariant();
        var model = string.Join(' ', tokens.Skip(1)).ToUpperInvariant();
        return (make, model);
    }

    private static (string Make, string Model) ParseFromSlug(string lotUrl)
    {
        var slug = lotUrl.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
        var slugWithoutCode = Regex.Replace(slug, "-[jx]\\d{5,}$", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var tokens = slugWithoutCode.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 1)
            .ToArray();

        if (tokens.Length < 2)
        {
            return (string.Empty, string.Empty);
        }

        var skipWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "carro",
            "veiculo",
            "caminhonete",
            "moto",
            "direitos",
            "sobre"
        };

        var candidateTokens = tokens.Where(token => !skipWords.Contains(token)).ToArray();
        if (candidateTokens.Length < 2)
        {
            candidateTokens = tokens;
        }

        var make = candidateTokens[0].ToUpperInvariant();
        var model = string.Join(' ', candidateTokens.Skip(1)).ToUpperInvariant();
        return (make, model);
    }

    private static string ResolveUf(string lotUrl, string? locationText)
    {
        var segments = lotUrl.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ufSegment = segments.FirstOrDefault(segment => Regex.IsMatch(segment, "^[a-z]{2}$", RegexOptions.CultureInvariant));

        if (!string.IsNullOrWhiteSpace(ufSegment)
            && !ufSegment.Equals("ni", StringComparison.OrdinalIgnoreCase)
            && !ufSegment.Equals("si", StringComparison.OrdinalIgnoreCase))
        {
            return ufSegment.ToUpperInvariant();
        }

        var match = Regex.Match(locationText ?? string.Empty, "\\b([A-Z]{2})\\b", RegexOptions.CultureInvariant);
        if (match.Success)
        {
            return match.Groups[1].Value.ToUpperInvariant();
        }

        return "SP";
    }

    private static VehicleType ResolveVehicleType(string lotUrl, string title)
    {
        var reference = $"{lotUrl} {title}".ToLowerInvariant();
        if (reference.Contains("/motos", StringComparison.Ordinal)
            || reference.Contains("moto", StringComparison.Ordinal))
        {
            return VehicleType.Motorcycle;
        }

        if (reference.Contains("/caminhoes", StringComparison.Ordinal)
            || reference.Contains("caminhao", StringComparison.Ordinal))
        {
            return VehicleType.Truck;
        }

        if (reference.Contains("caminhonete", StringComparison.Ordinal)
            || reference.Contains("pickup", StringComparison.Ordinal)
            || reference.Contains("pick-up", StringComparison.Ordinal))
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

        if (normalized.Contains("RECUPERAVEL", StringComparison.Ordinal)
            || normalized.Contains("RECUPERADO", StringComparison.Ordinal))
        {
            return VehicleCondition.TheftRecovery;
        }

        return VehicleCondition.Running;
    }

    private static LotStatus ResolveStatus(string html, string? lastBidText)
    {
        if (Regex.IsMatch(html, "lote\\s+encerrado|leil[aã]o\\s+encerrad|arrematad", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return LotStatus.Closed;
        }

        if (!string.IsNullOrWhiteSpace(lastBidText)
            && lastBidText.Contains("Faça sua oferta", StringComparison.OrdinalIgnoreCase))
        {
            return LotStatus.Active;
        }

        if (!string.IsNullOrWhiteSpace(lastBidText) && TryParseMoneyPtBr(lastBidText).HasValue)
        {
            return LotStatus.Active;
        }

        return LotStatus.Active;
    }
}
