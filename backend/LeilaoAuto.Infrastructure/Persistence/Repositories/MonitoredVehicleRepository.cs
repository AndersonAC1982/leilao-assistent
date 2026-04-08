using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Domain.Common;
using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence.Repositories;

public class MonitoredVehicleRepository : BaseRepository<MonitoredVehicle>, IMonitoredVehicleRepository
{
    public MonitoredVehicleRepository(LeilaoAutoDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<MonitoredVehicle>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await DbContext.MonitoredVehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.UserId == userId)
            .OrderByDescending(vehicle => vehicle.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await DbContext.MonitoredVehicles.CountAsync(vehicle => vehicle.UserId == userId, cancellationToken);
    }

    public override async Task AddAsync(MonitoredVehicle entity, CancellationToken cancellationToken)
    {
        var monitoredCount = await CountByUserIdAsync(entity.UserId, cancellationToken);
        if (monitoredCount >= BusinessRules.MaxMonitoredVehiclesPerUser)
        {
            throw new DomainRuleException($"Each user can monitor up to {BusinessRules.MaxMonitoredVehiclesPerUser} vehicles.");
        }

        await base.AddAsync(entity, cancellationToken);
    }
}
