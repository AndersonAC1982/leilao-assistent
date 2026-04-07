using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence.Repositories;

public class ConnectorExecutionLogRepository : BaseRepository<ConnectorExecutionLog>, IConnectorExecutionLogRepository
{
    public ConnectorExecutionLogRepository(LeilaoAutoDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<ConnectorExecutionLog>> GetRecentAsync(int take, CancellationToken cancellationToken)
    {
        var safeTake = take <= 0 ? 20 : take;
        return await DbContext.ConnectorExecutionLogs
            .AsNoTracking()
            .OrderByDescending(log => log.ExecutedAt)
            .Take(safeTake)
            .ToListAsync(cancellationToken);
    }
}
