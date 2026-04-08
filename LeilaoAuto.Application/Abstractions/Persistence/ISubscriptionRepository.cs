using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Abstractions.Persistence;

public interface ISubscriptionRepository : IBaseRepository<Subscription>
{
    Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Subscription?> GetByExternalSubscriptionIdAsync(string externalSubscriptionId, CancellationToken cancellationToken);
}
