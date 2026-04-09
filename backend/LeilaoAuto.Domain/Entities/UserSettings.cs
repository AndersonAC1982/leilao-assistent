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
        DateTime updatedAt,
        string? category = null,
        string? activeSources = null,
        decimal? maxPrice = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Update(
            search,
            source,
            minScore,
            vehicleType,
            region,
            advancedFiltersEnabled,
            updatedAt,
            category,
            activeSources,
            maxPrice);
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Search { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public decimal MinScore { get; private set; }
    public int? VehicleType { get; private set; }
    public string? Region { get; private set; }
    public bool AdvancedFiltersEnabled { get; private set; }
    public string Category { get; private set; } = "Todas";
    public string ActiveSources { get; private set; } = string.Empty;
    public decimal? MaxPrice { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public User? User { get; private set; }

    public void Update(
        string search,
        string source,
        decimal minScore,
        int? vehicleType,
        string? region,
        bool advancedFiltersEnabled,
        DateTime updatedAt,
        string? category = null,
        string? activeSources = null,
        decimal? maxPrice = null)
    {
        Search = search.Trim();
        Source = source.Trim();
        MinScore = minScore;
        VehicleType = vehicleType;
        Region = string.IsNullOrWhiteSpace(region) ? null : region.Trim().ToUpperInvariant();
        AdvancedFiltersEnabled = advancedFiltersEnabled;
        Category = string.IsNullOrWhiteSpace(category) ? "Todas" : category.Trim();
        ActiveSources = NormalizeActiveSources(activeSources);
        MaxPrice = maxPrice.HasValue && maxPrice.Value > 0 ? maxPrice : null;
        UpdatedAt = updatedAt;
    }

    private static string NormalizeActiveSources(string? sources)
    {
        if (string.IsNullOrWhiteSpace(sources))
        {
            return string.Empty;
        }

        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<string>();

        foreach (var source in sources.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (source.Length == 0 || !unique.Add(source))
            {
                continue;
            }

            normalized.Add(source);
        }

        return string.Join('|', normalized);
    }
}
