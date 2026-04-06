using System.Text.RegularExpressions;

namespace LeilaoAuto.Domain.Services;

public static partial class LotUrlGuard
{
    private static readonly HashSet<string> InvalidPaths =
    [
        "/",
        "/home",
        "/inicio",
        "/index",
        "/default",
        "/default.aspx"
    ];

    public static bool IsValidLotUrl(string? lotUrl)
    {
        if (string.IsNullOrWhiteSpace(lotUrl))
        {
            return false;
        }

        if (!Uri.TryCreate(lotUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var normalizedPath = uri.AbsolutePath.Trim().TrimEnd('/').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return false;
        }

        if (InvalidPaths.Contains(normalizedPath))
        {
            return false;
        }

        return HasDigitsRegex().IsMatch(uri.PathAndQuery);
    }

    [GeneratedRegex(@"\d", RegexOptions.Compiled)]
    private static partial Regex HasDigitsRegex();
}
