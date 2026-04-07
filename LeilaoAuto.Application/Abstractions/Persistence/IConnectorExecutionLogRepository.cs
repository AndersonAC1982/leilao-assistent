using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Abstractions.Persistence;

public interface IConnectorExecutionLogRepository : IBaseRepository<ConnectorExecutionLog>
{
    Task<IReadOnlyList<ConnectorExecutionLog>> GetRecentAsync(int take, CancellationToken cancellationToken);
}
