using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Lots;

public sealed record LotDto(
    Guid Id,
    string Auctioneer,
    string LotNumber,
    string Make,
    string Model,
    int Year,
    VehicleType VehicleType,
    string Uf,
    VehicleCondition VehicleCondition,
    LotStatus Status,
    decimal? CurrentBid,
    decimal? FinalPrice,
    string LotUrl,
    decimal OpportunityScore,
    decimal RiskScore,
    DateTimeOffset UpdatedAtUtc);
