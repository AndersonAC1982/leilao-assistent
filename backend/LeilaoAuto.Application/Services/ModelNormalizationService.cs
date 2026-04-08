using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Domain.Services;

namespace LeilaoAuto.Application.Services;

public class ModelNormalizationService : IModelNormalizationService
{
    public string Normalize(string? value)
    {
        return ModelNormalizer.Normalize(value);
    }

    public string NormalizeComparable(string? model, string? brand = null)
    {
        return ModelNormalizer.NormalizeComparable(model, brand);
    }

    public double Similarity(string? left, string? right)
    {
        return ModelMatcher.Similarity(left, right);
    }

    public bool IsSimilar(string? left, string? right, double threshold = 0.6)
    {
        return ModelMatcher.IsMatch(left, right, threshold);
    }
}
