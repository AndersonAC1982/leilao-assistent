namespace LeilaoAuto.Domain.Entities;

public class UserSettings
{
    private UserSettings()
    {
    }

    public UserSettings(
        Guid userId,
        string search,
        string source,
        decimal minScore,
        int? vehicleType,
        string? region,
        bool advancedFiltersEnabled,
        DateTime updatedAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Update(search, source, minScore, vehicleType, region, advancedFiltersEnabled, updatedAt);
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Search { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public decimal MinScore { get; private set; }
    public int? VehicleType { get; private set; }
    public string? Region { get; private set; }
    public bool AdvancedFiltersEnabled { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public User? User { get; private set; }

    public void Update(
        string search,
        string source,
        decimal minScore,
        int? vehicleType,
        string? region,
        bool advancedFiltersEnabled,
        DateTime updatedAt)
    {
        Search = search.Trim();
        Source = source.Trim();
        MinScore = minScore;
        VehicleType = vehicleType;
        Region = string.IsNullOrWhiteSpace(region) ? null : region.Trim().ToUpperInvariant();
        AdvancedFiltersEnabled = advancedFiltersEnabled;
        UpdatedAt = updatedAt;
    }
}
