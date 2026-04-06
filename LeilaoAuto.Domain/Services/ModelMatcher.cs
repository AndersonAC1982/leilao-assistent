namespace LeilaoAuto.Domain.Services;

public static class ModelMatcher
{
    public static bool IsMatch(string? left, string? right, double threshold = 0.55)
    {
        return Similarity(left, right) >= threshold;
    }

    public static double Similarity(string? left, string? right)
    {
        var normalizedLeft = ModelNormalizer.Normalize(left);
        var normalizedRight = ModelNormalizer.Normalize(right);

        if (string.IsNullOrWhiteSpace(normalizedLeft) || string.IsNullOrWhiteSpace(normalizedRight))
        {
            return 0;
        }

        if (normalizedLeft == normalizedRight)
        {
            return 1;
        }

        var leftTokens = normalizedLeft.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var rightTokens = normalizedRight.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var leftSet = leftTokens.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightSet = rightTokens.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var intersection = leftSet.Intersect(rightSet, StringComparer.OrdinalIgnoreCase).Count();
        var union = leftSet.Union(rightSet, StringComparer.OrdinalIgnoreCase).Count();
        var tokenScore = union == 0 ? 0 : (double)intersection / union;

        var prefixScore = normalizedLeft.StartsWith(normalizedRight, StringComparison.OrdinalIgnoreCase)
            || normalizedRight.StartsWith(normalizedLeft, StringComparison.OrdinalIgnoreCase)
            ? 0.2
            : 0;

        return Math.Min(1, tokenScore + prefixScore);
    }
}
