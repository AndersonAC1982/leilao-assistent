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
        string make,
        string model,
        int? yearFrom,
        int? yearTo,
        VehicleType? vehicleType,
        string? uf,
        VehicleCondition? vehicleCondition)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Update(make, model, yearFrom, yearTo, vehicleType, uf, vehicleCondition);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Make { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string NormalizedModel { get; private set; } = string.Empty;
    public int? YearFrom { get; private set; }
    public int? YearTo { get; private set; }
    public VehicleType? VehicleType { get; private set; }
    public string? Uf { get; private set; }
    public VehicleCondition? VehicleCondition { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public User? User { get; private set; }

    public void Update(
        string make,
        string model,
        int? yearFrom,
        int? yearTo,
        VehicleType? vehicleType,
        string? uf,
        VehicleCondition? vehicleCondition)
    {
        Make = make.Trim();
        Model = model.Trim();
        NormalizedModel = ModelNormalizer.Normalize(Model);
        YearFrom = yearFrom;
        YearTo = yearTo;
        VehicleType = vehicleType;
        Uf = string.IsNullOrWhiteSpace(uf) ? null : uf.Trim().ToUpperInvariant();
        VehicleCondition = vehicleCondition;
    }
}
