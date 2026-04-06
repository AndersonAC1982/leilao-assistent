using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Application.Contracts.Lots;

public sealed record ProviderLotDto(
    string ExternalId,
    string Auctioneer,
    string LotNumber,
    string Make,
    string Model,
    int Year,
    VehicleType VehicleType,
    string Uf,
    VehicleCondition VehicleCondition,
    LotStatus Status,
    string LotUrl,
    decimal? CurrentBid,
    decimal? FinalPrice,
    decimal? AppraisedValue,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt);
