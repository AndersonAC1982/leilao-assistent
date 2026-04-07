using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;

namespace LeilaoAuto.Domain.Entities;

public class MonitoredVehicle
{
    private MonitoredVehicle()
    {
    }

    public MonitoredVehicle(
        Guid userId,
        string brand,
        string model,
        int year,
        VehicleType type,
        string uf,
        VehicleCondition vehicleState)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Update(brand, model, year, type, uf, vehicleState);
        CreatedAt = DateTime.UtcNow;
    }

    // Compatibility constructor to preserve existing API/application contracts.
    public MonitoredVehicle(
        Guid userId,
        string make,
        string model,
        int? yearFrom,
        int? yearTo,
        VehicleType? vehicleType,
        string? uf,
        VehicleCondition? vehicleCondition)
        : this(
            userId,
            make,
            model,
            yearFrom ?? yearTo ?? DateTime.UtcNow.Year,
            vehicleType ?? Enums.VehicleType.Other,
            string.IsNullOrWhiteSpace(uf) ? "SP" : uf.Trim().ToUpperInvariant(),
            vehicleCondition ?? Enums.VehicleCondition.Unknown)
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int Year { get; private set; }
    public VehicleType Type { get; private set; }
    public string Uf { get; private set; } = string.Empty;
    public VehicleCondition VehicleState { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public string NormalizedModel { get; private set; } = string.Empty;
    public User? User { get; private set; }

    // Backward-compatible aliases for existing phase-1 contracts.
    public string Make => Brand;
    public int? YearFrom => Year;
    public int? YearTo => Year;
    public VehicleType? VehicleType => Type;
    public VehicleCondition? VehicleCondition => VehicleState;
    public DateTime CreatedAtUtc => CreatedAt;

    public void Update(
        string brand,
        string model,
        int year,
        VehicleType type,
        string uf,
        VehicleCondition vehicleState)
    {
        Brand = brand.Trim();
        Model = model.Trim();
        Year = year;
        Type = type;
        Uf = uf.Trim().ToUpperInvariant();
        VehicleState = vehicleState;
        NormalizedModel = ModelNormalizer.Normalize(Model);
    }

    // Backward-compatible update overload.
    public void Update(
        string make,
        string model,
        int? yearFrom,
        int? yearTo,
        VehicleType? vehicleType,
        string? uf,
        VehicleCondition? vehicleCondition)
    {
        Update(
            make,
            model,
            yearFrom ?? yearTo ?? Year,
            vehicleType ?? Type,
            string.IsNullOrWhiteSpace(uf) ? Uf : uf.Trim().ToUpperInvariant(),
            vehicleCondition ?? VehicleState);
    }
}
