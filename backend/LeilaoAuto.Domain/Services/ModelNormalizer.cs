using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LeilaoAuto.Domain.Services;

public static partial class ModelNormalizer
{
    private static readonly HashSet<string> KnownBrands =
    [
        "AUDI", "BMW", "BYD", "CHEVROLET", "CHERY", "CITROEN", "FIAT", "FORD", "GWM", "HONDA", "HYUNDAI",
        "JEEP", "KIA", "LAND", "LEXUS", "MERCEDES", "MITSUBISHI", "NISSAN", "PEUGEOT", "PORSCHE",
        "RAM", "RENAULT", "TOYOTA", "VOLKSWAGEN", "VW", "YAMAHA"
    ];

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var withoutAccent = builder.ToString().Normalize(NormalizationForm.FormC);
        var withSeparatedAlphaNumeric = AlphaDigitBoundaryRegex().Replace(withoutAccent, "$1 $2");
        withSeparatedAlphaNumeric = DigitAlphaBoundaryRegex().Replace(withSeparatedAlphaNumeric, "$1 $2");
        var clean = MultipleSpacesRegex().Replace(NonAlphanumericRegex().Replace(withSeparatedAlphaNumeric, " "), " ");
        return clean.Trim();
    }

    public static string NormalizeComparable(string? model, string? brand = null)
    {
        var normalizedModel = Normalize(model);
        if (string.IsNullOrWhiteSpace(normalizedModel))
        {
            return string.Empty;
        }

        var modelTokens = normalizedModel
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        RemoveBrandPrefix(modelTokens, brand);
        RemoveYearTokens(modelTokens);

        return string.Join(' ', modelTokens).Trim();
    }

    private static void RemoveBrandPrefix(List<string> tokens, string? brand)
    {
        if (tokens.Count == 0)
        {
            return;
        }

        var normalizedBrand = Normalize(brand);
        if (!string.IsNullOrWhiteSpace(normalizedBrand))
        {
            var brandTokens = normalizedBrand.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (brandTokens.Length > 0
                && tokens.Count >= brandTokens.Length
                && StartsWith(tokens, brandTokens))
            {
                tokens.RemoveRange(0, brandTokens.Length);
            }

            return;
        }

        if (KnownBrands.Contains(tokens[0]))
        {
            tokens.RemoveAt(0);
        }
    }

    private static void RemoveYearTokens(List<string> tokens)
    {
        tokens.RemoveAll(static token =>
            token.Length == 4
            && int.TryParse(token, out var year)
            && year >= 1950
            && year <= DateTime.UtcNow.Year + 1);
    }

    private static bool StartsWith(IReadOnlyList<string> tokens, IReadOnlyList<string> prefix)
    {
        for (var index = 0; index < prefix.Count; index++)
        {
            if (!tokens[index].Equals(prefix[index], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex("[^A-Z0-9 ]+", RegexOptions.Compiled)]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex("([A-Z])([0-9])", RegexOptions.Compiled)]
    private static partial Regex AlphaDigitBoundaryRegex();

    [GeneratedRegex("([0-9])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex DigitAlphaBoundaryRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();
}
