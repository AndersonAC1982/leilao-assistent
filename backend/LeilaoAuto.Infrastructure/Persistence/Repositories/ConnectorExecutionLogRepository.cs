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

    public async Task<IReadOnlyList<ConnectorExecutionLog>> GetRecentByUserIdAsync(Guid userId, int take, CancellationToken cancellationToken)
    {
        var safeTake = take <= 0 ? 20 : take;
        return await DbContext.ConnectorExecutionLogs
            .AsNoTracking()
            .Where(log => log.UserId == null || log.UserId == userId)
            .OrderByDescending(log => log.ExecutedAt)
            .Take(safeTake)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByUserAndConnectorAsync(
        Guid userId,
        string connectorName,
        DateTime fromInclusiveUtc,
        DateTime toExclusiveUtc,
        CancellationToken cancellationToken)
    {
        return DbContext.ConnectorExecutionLogs
            .AsNoTracking()
            .Where(log =>
                log.UserId == userId
                && log.ConnectorName == connectorName
                && log.ExecutedAt >= fromInclusiveUtc
                && log.ExecutedAt < toExclusiveUtc)
            .CountAsync(cancellationToken);
    }
}
