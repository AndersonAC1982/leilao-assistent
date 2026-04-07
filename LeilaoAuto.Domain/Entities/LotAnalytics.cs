namespace LeilaoAuto.Domain.Entities;

public class LotAnalytics
{
    private LotAnalytics()
    {
    }

    public LotAnalytics(
        string normalizedModel,
        decimal averagePrice,
        decimal minPrice,
        decimal maxPrice,
        int sampleSize,
        DateTime updatedAt)
    {
        Id = Guid.NewGuid();
        NormalizedModel = normalizedModel.Trim();
        AveragePrice = averagePrice;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        SampleSize = sampleSize;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }
    public string NormalizedModel { get; private set; } = string.Empty;
    public decimal AveragePrice { get; private set; }
    public decimal MinPrice { get; private set; }
    public decimal MaxPrice { get; private set; }
    public int SampleSize { get; private set; }
    public DateTime UpdatedAt { get; private set; }
}
