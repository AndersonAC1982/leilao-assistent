using LeilaoAuto.Domain.Common;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;

namespace LeilaoAuto.Domain.Entities;

public class AuctionLot
{
    private AuctionLot()
    {
    }

    public AuctionLot(
        string externalId,
        string auctioneer,
        string lotNumber,
        string make,
        string model,
        int year,
        VehicleType vehicleType,
        string uf,
        VehicleCondition vehicleCondition,
        LotStatus status,
        string lotUrl,
        decimal? currentBid,
        decimal? finalPrice,
        decimal? appraisedValue,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt)
    {
        if (!LotUrlGuard.IsValidLotUrl(lotUrl))
        {
            throw new DomainRuleException("Toda listagem precisa de lotUrl válida e específica do lote.");
        }

        Id = Guid.NewGuid();
        ExternalId = externalId.Trim();
        Auctioneer = auctioneer.Trim();
        LotNumber = lotNumber.Trim();
        Make = make.Trim();
        Model = model.Trim();
        NormalizedModel = ModelNormalizer.NormalizeComparable(Model, Make);
        Year = year;
        VehicleType = vehicleType;
        Uf = uf.Trim().ToUpperInvariant();
        VehicleCondition = vehicleCondition;
        Status = status;
        LotUrl = lotUrl.Trim();
        CurrentBid = currentBid;
        FinalPrice = finalPrice;
        AppraisedValue = appraisedValue;
        StartsAt = startsAt;
        EndsAt = endsAt;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string Auctioneer { get; private set; } = string.Empty;
    public string LotNumber { get; private set; } = string.Empty;
    public string Make { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string NormalizedModel { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public VehicleType VehicleType { get; private set; }
    public string Uf { get; private set; } = string.Empty;
    public VehicleCondition VehicleCondition { get; private set; }
    public LotStatus Status { get; private set; }
    public string LotUrl { get; private set; } = string.Empty;
    public decimal? CurrentBid { get; private set; }
    public decimal? FinalPrice { get; private set; }
    public decimal? AppraisedValue { get; private set; }
    public DateTimeOffset? StartsAt { get; private set; }
    public DateTimeOffset? EndsAt { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool HasValidLotUrl() => LotUrlGuard.IsValidLotUrl(LotUrl);

    public bool IsExactLot(string auctioneer, string lotNumber)
    {
        return Auctioneer.Equals(auctioneer, StringComparison.OrdinalIgnoreCase)
               && LotNumber.Equals(lotNumber, StringComparison.OrdinalIgnoreCase);
    }

    public void RefreshFrom(
        string auctioneer,
        string lotNumber,
        string make,
        string model,
        int year,
        VehicleType vehicleType,
        string uf,
        VehicleCondition vehicleCondition,
        LotStatus status,
        string lotUrl,
        decimal? currentBid,
        decimal? finalPrice,
        decimal? appraisedValue,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt)
    {
        if (!LotUrlGuard.IsValidLotUrl(lotUrl))
        {
            throw new DomainRuleException("A lotUrl do lote deve apontar para a página exata do item.");
        }

        Auctioneer = auctioneer.Trim();
        LotNumber = lotNumber.Trim();
        Make = make.Trim();
        Model = model.Trim();
        NormalizedModel = ModelNormalizer.NormalizeComparable(Model, Make);
        Year = year;
        VehicleType = vehicleType;
        Uf = uf.Trim().ToUpperInvariant();
        VehicleCondition = vehicleCondition;
        Status = status;
        LotUrl = lotUrl.Trim();
        CurrentBid = currentBid;
        FinalPrice = finalPrice;
        AppraisedValue = appraisedValue;
        StartsAt = startsAt;
        EndsAt = endsAt;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
