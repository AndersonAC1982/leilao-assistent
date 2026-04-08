using LeilaoAuto.Domain.Common;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;

namespace LeilaoAuto.Domain.Entities;

public class Lot
{
    private Lot()
    {
    }

    public Lot(
        string sourceSite,
        string title,
        string? brand,
        string? model,
        int? year,
        VehicleType? type,
        string? uf,
        VehicleCondition? vehicleState,
        string lotUrl,
        string? imageUrl,
        string? description,
        decimal? currentPrice,
        decimal? finalPrice,
        LotStatus status,
        DateTime foundAt,
        DateTime? closedAt,
        string? rawDataJson)
    {
        Id = Guid.NewGuid();
        SourceSite = sourceSite.Trim();
        SetTitle(title);
        Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim();
        Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim();
        Year = year;
        Type = type;
        Uf = string.IsNullOrWhiteSpace(uf) ? null : uf.Trim().ToUpperInvariant();
        VehicleState = vehicleState;
        LotUrl = lotUrl.Trim();
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        Description = description;
        CurrentPrice = currentPrice;
        FinalPrice = finalPrice;
        FoundAt = foundAt;
        ClosedAt = closedAt;
        RawDataJson = rawDataJson;

        SetStatus(status);
    }

    public Guid Id { get; private set; }
    public string SourceSite { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string NormalizedTitle { get; private set; } = string.Empty;
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public int? Year { get; private set; }
    public VehicleType? Type { get; private set; }
    public string? Uf { get; private set; }
    public VehicleCondition? VehicleState { get; private set; }
    public string LotUrl { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public string? Description { get; private set; }
    public decimal? CurrentPrice { get; private set; }
    public decimal? FinalPrice { get; private set; }
    public LotStatus Status { get; private set; }
    public DateTime FoundAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? RawDataJson { get; private set; }

    public bool IsActive => Status == LotStatus.Active || Status == LotStatus.Confirmed;
    public bool IsClosed => Status == LotStatus.Closed;

    public void SetStatus(LotStatus status)
    {
        if (status == LotStatus.Confirmed && !LotUrlGuard.IsValidLotUrl(LotUrl))
        {
            throw new DomainRuleException("A confirmed lot must have a valid lot URL.");
        }

        Status = status;
    }

    public void SetTitle(string title)
    {
        Title = title.Trim();
        NormalizedTitle = ModelNormalizer.NormalizeComparable(Title);
    }
}
