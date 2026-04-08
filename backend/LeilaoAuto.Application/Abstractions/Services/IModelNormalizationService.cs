namespace LeilaoAuto.Application.Abstractions.Services;

public interface IModelNormalizationService
{
    string Normalize(string? value);
    string NormalizeComparable(string? model, string? brand = null);
    double Similarity(string? left, string? right);
    bool IsSimilar(string? left, string? right, double threshold = 0.6);
}
