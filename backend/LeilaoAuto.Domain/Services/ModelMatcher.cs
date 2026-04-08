namespace LeilaoAuto.Domain.Services;

public static class ModelMatcher
{
    public static bool IsMatch(string? left, string? right, double threshold = 0.55)
    {
        return Similarity(left, right) >= threshold;
    }

    public static double Similarity(string? left, string? right)
    {
        var normalizedLeft = ModelNormalizer.NormalizeComparable(left);
        var normalizedRight = ModelNormalizer.NormalizeComparable(right);

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
        var charScore = DiceCoefficient(normalizedLeft.Replace(" ", string.Empty), normalizedRight.Replace(" ", string.Empty));

        var prefixScore = normalizedLeft.StartsWith(normalizedRight, StringComparison.OrdinalIgnoreCase)
            || normalizedRight.StartsWith(normalizedLeft, StringComparison.OrdinalIgnoreCase)
            ? 0.2
            : 0;

        var score = (tokenScore * 0.7) + (charScore * 0.3) + prefixScore;
        return Math.Min(1, Math.Round(score, 4));
    }

    private static double DiceCoefficient(string left, string right)
    {
        if (left == right)
        {
            return 1;
        }

        if (left.Length < 2 || right.Length < 2)
        {
            return 0;
        }

        var leftBigrams = BuildBigramFrequency(left);
        var rightBigrams = BuildBigramFrequency(right);
        var intersection = 0;

        foreach (var (bigram, leftCount) in leftBigrams)
        {
            if (rightBigrams.TryGetValue(bigram, out var rightCount))
            {
                intersection += Math.Min(leftCount, rightCount);
            }
        }

        var total = leftBigrams.Values.Sum() + rightBigrams.Values.Sum();
        return total == 0 ? 0 : (2d * intersection) / total;
    }

    private static Dictionary<string, int> BuildBigramFrequency(string value)
    {
        var dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < value.Length - 1; index++)
        {
            var bigram = value.Substring(index, 2);
            dictionary[bigram] = dictionary.TryGetValue(bigram, out var current) ? current + 1 : 1;
        }

        return dictionary;
    }
}
