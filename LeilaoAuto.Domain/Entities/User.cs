using LeilaoAuto.Domain.Common;

namespace LeilaoAuto.Domain.Entities;

public class User
{
    private User()
    {
    }

    public User(string email, string passwordHash)
    {
        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public ICollection<MonitoredVehicle> MonitoredVehicles { get; private set; } = new List<MonitoredVehicle>();

    public void AddMonitoredVehicle(MonitoredVehicle vehicle)
    {
        if (MonitoredVehicles.Count >= BusinessRules.MaxMonitoredVehiclesPerUser)
        {
            throw new DomainRuleException($"Cada usuário pode monitorar até {BusinessRules.MaxMonitoredVehiclesPerUser} veículos.");
        }

        MonitoredVehicles.Add(vehicle);
    }

    public void RemoveMonitoredVehicle(Guid vehicleId)
    {
        var toRemove = MonitoredVehicles.FirstOrDefault(vehicle => vehicle.Id == vehicleId);
        if (toRemove is not null)
        {
            MonitoredVehicles.Remove(toRemove);
        }
    }
}
