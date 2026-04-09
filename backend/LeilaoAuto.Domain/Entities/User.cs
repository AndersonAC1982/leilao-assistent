using LeilaoAuto.Domain.Common;
using LeilaoAuto.Domain.Enums;

namespace LeilaoAuto.Domain.Entities;

public class User
{
    private User()
    {
    }

    public User(string email, string passwordHash, UserRole role = UserRole.User, PlanType plan = PlanType.Free)
    {
        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        Plan = plan;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public PlanType Plan { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<MonitoredVehicle> MonitoredVehicles { get; private set; } = new List<MonitoredVehicle>();
    public ICollection<Subscription> Subscriptions { get; private set; } = new List<Subscription>();
    public UserSettings? Settings { get; private set; }

    public void AddMonitoredVehicle(MonitoredVehicle vehicle)
    {
        if (MonitoredVehicles.Count >= BusinessRules.MaxMonitoredVehiclesPerUser)
        {
            throw new DomainRuleException($"Each user can monitor up to {BusinessRules.MaxMonitoredVehiclesPerUser} vehicles.");
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

    public void ChangePlan(PlanType plan)
    {
        Plan = plan;
    }
}
