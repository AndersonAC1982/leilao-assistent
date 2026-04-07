using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Lots;

public sealed record LotDto(
    Guid Id,
    string Title,
    string? Description,
    string Source,
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
    decimal? ReferenceAveragePrice,
    string LotUrl,
    decimal OpportunityScore,
    string OpportunityLabel,
    decimal RiskScore,
    string DamageLevel,
    string RiskDecision,
    DateTimeOffset UpdatedAtUtc);
