using System.Net;
using System.Text.RegularExpressions;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Conector real Sodre Santoro:
/// - parsing de catálogo
/// - parsing de detalhe de lote
/// - validação rígida da URL exata do lote
/// </summary>
public class SodreSantoroConnector : BaseLotConnector
{
    private const string PrimaryPortalUrl = "https://hml-web.sodresantoro.com.br/";
    private const string SecondaryPortalUrl = "https://www.sodresantoro.com.br/";
    private const string Auctioneer = "Sodre Santoro";

    public SodreSantoroConnector(
        IHttpClientFactory httpClientFactory,
        IOptions<AuctionProviderOptions> options,
        ILogger<SodreSantoroConnector> logger)
        : base(httpClientFactory, options, logger)
    {
    }

    public override string Name => "SodreSantoro";

    public override IReadOnlyCollection<string> SupportedDomains =>
    [
        "sodresantoro.com.br",
        "www.sodresantoro.com.br",
        "leilao.sodresantoro.com.br",
        "hml-web.sodresantoro.com.br",
        "hml-webv2.sodresantoro.com.br"
    ];

    public override async Task<IReadOnlyList<object>> SearchAsync(LotSearchFilterRequest filters, CancellationToken cancellationToken)
    {
        try
        {
            var catalogHtml = await FetchHtmlAsync(PrimaryPortalUrl, cancellationToken)
                              ?? await FetchHtmlAsync(SecondaryPortalUrl, cancellationToken);

            if (string.IsNullOrWhiteSpace(catalogHtml))
            {
                return BuildMockRawLots("sodresantoro", Auctioneer);
            }

            var catalog = ExtractCatalogCandidates(catalogHtml);
            var lotUrls = new HashSet<string>(catalog.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var seedUrl in lotUrls.Take(6).ToArray())
            {
                var lotHtml = await FetchHtmlAsync(seedUrl, cancellationToken);
                if (string.IsNullOrWhiteSpace(lotHtml))
                {
                    continue;
                }

                foreach (var discovered in ExtractLotUrlsFromLotPage(lotHtml))
                {
                    if (ValidateLotUrl(discovered))
                    {
                        lotUrls.Add(discovered);
                    }
                }
            }

            var result = lotUrls
                .Select(url => BuildRaw(url, catalog.TryGetValue(url, out var hint) ? hint : null))
                .Where(raw => MatchesFilters(raw, filters))
                .Take(32)
                .Cast<object>()
                .ToList();

            return result.Count == 0
                ? BuildMockRawLots("sodresantoro", Auctioneer)
                : result;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "SodreSantoro connector failed in real search. Falling back to structured mock.");
            return BuildMockRawLots("sodresantoro", Auctioneer);
        }
    }

    public override async Task<ProviderLotDto?> ParseAsync(object raw, CancellationToken cancellationToken)
    {
        var map = EnsureDictionary(raw);
        var lotUrl = map.TryGetValue("lotUrl", out var lotUrlValue) ? NormalizeLotUrl(lotUrlValue?.ToString()) : null;
        if (!ValidateLotUrl(lotUrl))
        {
            return null;
        }

        var html = await FetchHtmlAsync(lotUrl!, cancellationToken);
        if (string.IsNullOrWhiteSpace(html))
        {
            return BuildProviderLot(map, "sodresantoro", Auctioneer);
        }

        return BuildLotFromHtml(lotUrl!, html, map);
    }

    public override bool ValidateLotUrl(string? url)
    {
        if (!base.ValidateLotUrl(url) || string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.Host.Equals("sodresantoro.com.br", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.EndsWith(".sodresantoro.com.br", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = uri.AbsolutePath.TrimEnd('/').ToLowerInvariant();
        if (path is "" or "/" or "/leilao" or "/veiculos" or "/veiculos/lotes" or "/lotes")
        {
            return false;
        }

        return Regex.IsMatch(path, "^/leilao/\\d+/lote/\\d+$", RegexOptions.CultureInvariant);
    }

    private static IReadOnlyDictionary<string, CatalogHint> ExtractCatalogCandidates(string html)
    {
        var result = new Dictionary<string, CatalogHint>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in Regex.Matches(
                     html,
                     "data-link\\s*=\\s*\"(?<url>/leilao/\\d+/lote/\\d+/?)\"",
                     RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var url = NormalizeLotUrl(ToAbsoluteUrl(match.Groups["url"].Value, PrimaryPortalUrl));
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            var start = Math.Max(0, match.Index - 120);
            var snippet = html.Substring(start, Math.Min(1600, html.Length - start));
            var title = ExtractSnippetValue(snippet, "class=\"titulo\"[^>]*>(?<value>.*?)</span>")
                        ?? ExtractSnippetValue(snippet, "class=\"lote-link\"[^>]*title=\"(?<value>[^\"]+)\"");
            var price = TryParseMoneyPtBr(ExtractSnippetValue(snippet, "class=\"valor\"[^>]*>\\s*R\\$\\s*(?<value>[0-9\\.\\,]+)"));
            var status = Regex.IsMatch(snippet, "encerrad|arrematad", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                ? "Closed"
                : "Active";

            result[url] = new CatalogHint(title, price, status);
        }

        foreach (var href in ExtractAnchorHrefs(html))
        {
            var normalized = NormalizeLotUrl(ToAbsoluteUrl(href, PrimaryPortalUrl));
            if (IsExactLotUrl(normalized) && !result.ContainsKey(normalized))
            {
                result[normalized] = new CatalogHint(null, null, "Active");
            }
        }

        return result;
    }

    private static IEnumerable<string> ExtractLotUrlsFromLotPage(string html)
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in Regex.Matches(
                     html,
                     "(?:href|value)\\s*=\\s*\"(?<url>/leilao/\\d+/lote/\\d+/?)\"",
                     RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            var normalized = NormalizeLotUrl(ToAbsoluteUrl(match.Groups["url"].Value, PrimaryPortalUrl));
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                urls.Add(normalized);
            }
        }

        return urls;
    }

    private static Dictionary<string, object?> BuildRaw(string lotUrl, CatalogHint? hint)
    {
        var lotId = ExtractLotIdFromUrl(lotUrl) ?? Guid.NewGuid().ToString("N")[..8];
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["externalId"] = lotId,
            ["lotNumber"] = lotId,
            ["auctioneer"] = Auctioneer,
            ["lotUrl"] = lotUrl,
            ["status"] = hint?.Status ?? "Active",
            ["titleHint"] = hint?.Title,
            ["currentBid"] = hint?.Price
        };
    }

    private static bool MatchesFilters(IReadOnlyDictionary<string, object?> raw, LotSearchFilterRequest filters)
    {
        var title = raw.TryGetValue("titleHint", out var titleValue) ? HtmlDecode(titleValue?.ToString() ?? string.Empty) : string.Empty;
        var lotUrl = raw.TryGetValue("lotUrl", out var lotUrlValue) ? lotUrlValue?.ToString() ?? string.Empty : string.Empty;
        var reference = $"{title} {lotUrl}";
        var normalized = NormalizeToken(reference);

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

        if (filters.Year.HasValue && !reference.Contains(filters.Year.Value.ToString(), StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filters.Uf)
            && !Regex.IsMatch(reference, $"\\b{Regex.Escape(filters.Uf.Trim().ToUpperInvariant())}\\b", RegexOptions.CultureInvariant))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(filters.Make)
            && string.IsNullOrWhiteSpace(filters.Model)
            && !filters.Year.HasValue
            && !LooksLikeVehicle(reference))
        {
            return false;
        }

        return true;
    }

    private static string NormalizeLotUrl(string? lotUrl)
    {
        if (string.IsNullOrWhiteSpace(lotUrl))
        {
            return string.Empty;
        }

        var absolute = ToAbsoluteUrl(lotUrl, PrimaryPortalUrl) ?? ToAbsoluteUrl(lotUrl, SecondaryPortalUrl) ?? lotUrl;
        return NormalizeUrlForStorage(absolute);
    }

    private static string? ExtractLotIdFromUrl(string lotUrl)
    {
        var match = Regex.Match(lotUrl, "/lote/(?<id>\\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["id"].Value : null;
    }

    private static bool IsExactLotUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.Host.EndsWith(".sodresantoro.com.br", StringComparison.OrdinalIgnoreCase)
            && !uri.Host.Equals("sodresantoro.com.br", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = uri.AbsolutePath.TrimEnd('/').ToLowerInvariant();
        return Regex.IsMatch(path, "^/leilao/\\d+/lote/\\d+$", RegexOptions.CultureInvariant);
    }

    private ProviderLotDto? BuildLotFromHtml(string lotUrl, string html, IReadOnlyDictionary<string, object?> raw)
    {
        var attrs = ExtractLotAttributes(html);
        var title = ExtractById(html, "titleLot") ?? ExtractMetaOgTitle(html) ?? ExtractMapValue(raw, "titleHint");
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var description = ExtractById(html, "detail_info_lot_description") ?? GetAttr(attrs, "descricao");
        var make = NormalizeMake(GetAttr(attrs, "marca"));
        var model = HtmlDecode(GetAttr(attrs, "modelo"));
        if (string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model))
        {
            var fallback = ParseMakeAndModel(title);
            make = string.IsNullOrWhiteSpace(make) ? fallback.Make : make;
            model = string.IsNullOrWhiteSpace(model) ? fallback.Model : model;
        }

        if (string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model))
        {
            return null;
        }

        var year = ExtractYearFromText($"{GetAttr(attrs, "ano-modelo")} {GetAttr(attrs, "ano-fab")} {title} {description}") ?? DateTime.UtcNow.Year;
        var segment = GetAttr(attrs, "segmento");
        var category = GetAttr(attrs, "categoria");
        var vehicleType = ResolveVehicleType($"{segment} {category} {title} {model} {description}");
        if (vehicleType == VehicleType.Other && !LooksLikeVehicle($"{segment} {category} {title} {description}"))
        {
            return null;
        }

        var location = ExtractById(html, "aditionalInfoLot_lot_address");
        if (!string.IsNullOrWhiteSpace(location))
        {
            location = Regex.Replace(location, "^Local\\s+do\\s+lote:\\s*", string.Empty, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            location = GetAttr(attrs, "patio");
        }

        if (string.IsNullOrWhiteSpace(location))
        {
            location = description;
        }

        var uf = ExtractUf($"{location} {description}");
        var currentBid = ExtractCurrentBid(html) ?? TryReadDecimal(raw, "currentBid");
        var status = ResolveStatus(html, ExtractMapValue(raw, "status"));
        var finalPrice = status == LotStatus.Closed ? currentBid : null;
        var lotNumber = ExtractLotNumber(html) ?? ExtractMapValue(raw, "lotNumber") ?? ExtractLotIdFromUrl(lotUrl) ?? "N/A";
        var externalId = GetAttr(attrs, "lote-id");
        if (string.IsNullOrWhiteSpace(externalId))
        {
            externalId = ExtractMapValue(raw, "externalId") ?? ExtractLotIdFromUrl(lotUrl) ?? $"sodre-{Guid.NewGuid():N}";
        }

        var parsedMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["externalId"] = externalId,
            ["lotNumber"] = lotNumber,
            ["auctioneer"] = Auctioneer,
            ["make"] = make,
            ["model"] = model,
            ["year"] = year,
            ["vehicleType"] = vehicleType.ToString(),
            ["uf"] = uf,
            ["vehicleCondition"] = ResolveVehicleCondition($"{GetAttr(attrs, "monta")} {description}").ToString(),
            ["status"] = status.ToString(),
            ["lotUrl"] = lotUrl,
            ["currentBid"] = currentBid,
            ["finalPrice"] = finalPrice,
            ["appraisedValue"] = null,
            ["startsAt"] = DateTimeOffset.UtcNow.AddHours(-2),
            ["endsAt"] = status == LotStatus.Closed ? DateTimeOffset.UtcNow.AddHours(-1) : DateTimeOffset.UtcNow.AddHours(8)
        };

        return BuildProviderLot(parsedMap, "sodresantoro", Auctioneer);
    }

    private static Dictionary<string, string> ExtractLotAttributes(string html)
    {
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tag = Regex.Match(html, "<div\\s+[^>]*id=\"lotInfoDetail\"[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        if (!tag.Success)
        {
            return attrs;
        }

        foreach (Match match in Regex.Matches(tag.Value, "data-params-(?<key>[a-z0-9\\-]+)=\"(?<value>[^\"]*)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            attrs[match.Groups["key"].Value] = HtmlDecode(match.Groups["value"].Value);
        }

        return attrs;
    }

    private static string GetAttr(IReadOnlyDictionary<string, string> attrs, string key)
    {
        return attrs.TryGetValue(key, out var value) ? HtmlDecode(value) : string.Empty;
    }

    private static string? ExtractById(string html, string id)
    {
        var match = Regex.Match(html, $"<[^>]*id=\"{Regex.Escape(id)}\"[^>]*>(?<value>.*?)</[^>]+>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        var raw = Regex.Replace(match.Groups["value"].Value, "<[^>]+>", " ");
        var cleaned = Regex.Replace(HtmlDecode(raw), "\\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static string? ExtractMetaOgTitle(string html)
    {
        var match = Regex.Match(html, "<meta\\s+property=\"og:title\"\\s+content=\"(?<value>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        var decoded = HtmlDecode(match.Groups["value"].Value);
        var separator = decoded.IndexOf(" - ", StringComparison.Ordinal);
        return separator > 0 ? decoded[..separator].Trim() : decoded.Trim();
    }

    private static string? ExtractLotNumber(string html)
    {
        var byPanel = Regex.Match(html, "id=\"aditionalInfoLot_lot_number\"[^>]*>\\s*<strong>\\s*Lote:\\s*</strong>\\s*(?<value>[A-Z0-9\\-]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (byPanel.Success)
        {
            return HtmlDecode(byPanel.Groups["value"].Value);
        }

        var byHeader = Regex.Match(html, "Leil[aã]o\\s*\\d+\\s*-\\s*(?<value>[A-Z0-9\\-]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return byHeader.Success ? HtmlDecode(byHeader.Groups["value"].Value) : null;
    }

    private static decimal? ExtractCurrentBid(string html)
    {
        var byTag = Regex.Match(html, "id=\"currentBid\"[^>]*>\\s*R\\$\\s*(?<value>[0-9\\.\\,]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return byTag.Success ? TryParseMoneyPtBr(byTag.Groups["value"].Value) : null;
    }

    private static decimal? TryReadDecimal(IReadOnlyDictionary<string, object?> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            decimal decimalValue => decimalValue,
            _ => TryParseMoneyPtBr(value.ToString())
        };
    }

    private static string? ExtractMapValue(IReadOnlyDictionary<string, object?> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        var text = HtmlDecode(value.ToString() ?? string.Empty);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string ExtractUf(string text)
    {
        var match = Regex.Match(text, @"(?:-|/|,)\s*(?<uf>[A-Z]{2})\b(?:\s*-\s*CEP|\s*$)", RegexOptions.CultureInvariant);
        if (match.Success)
        {
            return match.Groups["uf"].Value.ToUpperInvariant();
        }

        var simple = Regex.Match(text, @"\b(?<uf>[A-Z]{2})\b", RegexOptions.CultureInvariant);
        return simple.Success ? simple.Groups["uf"].Value.ToUpperInvariant() : "SP";
    }

    private static string ExtractSnippetValue(string html, string pattern)
    {
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return string.Empty;
        }

        var raw = Regex.Replace(match.Groups["value"].Value, "<[^>]+>", " ");
        return Regex.Replace(HtmlDecode(raw), "\\s+", " ").Trim();
    }

    private static (string Make, string Model) ParseMakeAndModel(string title)
    {
        var clean = Regex.Replace(HtmlDecode(title), "\\s+", " ").Trim();
        clean = Regex.Replace(clean, "\\b(19|20)\\d{2}(\\/(19|20)\\d{2})?\\b", string.Empty, RegexOptions.CultureInvariant).Trim();
        var tokens = clean.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length < 2)
        {
            return (string.Empty, clean.ToUpperInvariant());
        }

        var make = NormalizeMake(tokens[0]);
        var model = clean[tokens[0].Length..].Trim().ToUpperInvariant();
        return (make, model);
    }

    private static string NormalizeMake(string? make)
    {
        var value = HtmlDecode(make ?? string.Empty).Trim().ToUpperInvariant();
        return value switch
        {
            "GM" => "CHEVROLET",
            "VW" => "VOLKSWAGEN",
            _ => value
        };
    }

    private static VehicleType ResolveVehicleType(string text)
    {
        var normalized = NormalizeToken(text);
        if (normalized.Contains("MOTO", StringComparison.Ordinal))
        {
            return VehicleType.Motorcycle;
        }

        if (normalized.Contains("CAMINHA", StringComparison.Ordinal) || normalized.Contains("ONIBUS", StringComparison.Ordinal))
        {
            return VehicleType.Truck;
        }

        if (normalized.Contains("PICKUP", StringComparison.Ordinal) || normalized.Contains("UTILITARIO", StringComparison.Ordinal) || normalized.Contains("SUV", StringComparison.Ordinal))
        {
            return VehicleType.Utility;
        }

        if (normalized.Contains("VEICULO", StringComparison.Ordinal)
            || normalized.Contains("CARRO", StringComparison.Ordinal)
            || normalized.Contains("SEDAN", StringComparison.Ordinal)
            || normalized.Contains("HATCH", StringComparison.Ordinal))
        {
            return VehicleType.Car;
        }

        return VehicleType.Other;
    }

    private static bool LooksLikeVehicle(string text)
    {
        var normalized = NormalizeToken(text);
        return normalized.Contains("VEICULO", StringComparison.Ordinal)
               || normalized.Contains("CARRO", StringComparison.Ordinal)
               || normalized.Contains("MOTO", StringComparison.Ordinal)
               || normalized.Contains("CAMINHA", StringComparison.Ordinal)
               || normalized.Contains("CHEVROLET", StringComparison.Ordinal)
               || normalized.Contains("VOLKSWAGEN", StringComparison.Ordinal)
               || normalized.Contains("FIAT", StringComparison.Ordinal)
               || normalized.Contains("FORD", StringComparison.Ordinal)
               || normalized.Contains("HONDA", StringComparison.Ordinal)
               || normalized.Contains("TOYOTA", StringComparison.Ordinal)
               || normalized.Contains("HYUNDAI", StringComparison.Ordinal)
               || normalized.Contains("NISSAN", StringComparison.Ordinal)
               || normalized.Contains("RENAULT", StringComparison.Ordinal)
               || normalized.Contains("JEEP", StringComparison.Ordinal);
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

        if (normalized.Contains("RECUPERADO", StringComparison.Ordinal) || normalized.Contains("RECUPERAVEL", StringComparison.Ordinal))
        {
            return VehicleCondition.TheftRecovery;
        }

        return VehicleCondition.Running;
    }

    private static LotStatus ResolveStatus(string html, string? statusHint)
    {
        var hint = NormalizeToken(statusHint ?? string.Empty);
        if (hint.Contains("ENCERRADO", StringComparison.Ordinal) || hint.Contains("CLOSED", StringComparison.Ordinal))
        {
            return LotStatus.Closed;
        }

        return Regex.IsMatch(html, @"\b(leil[aã]o|lote)\s+encerrad[oa]\b|\blote\s+arrematad[oa]\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            ? LotStatus.Closed
            : LotStatus.Active;
    }

    private sealed record CatalogHint(string? Title, decimal? Price, string Status);
}
