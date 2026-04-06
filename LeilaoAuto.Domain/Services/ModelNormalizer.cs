using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LeilaoAuto.Domain.Services;

public static partial class ModelNormalizer
{
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
        var clean = MultipleSpacesRegex().Replace(NonAlphanumericRegex().Replace(withoutAccent, " "), " ");
        return clean.Trim();
    }

    [GeneratedRegex("[^A-Z0-9 ]+", RegexOptions.Compiled)]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();
}
