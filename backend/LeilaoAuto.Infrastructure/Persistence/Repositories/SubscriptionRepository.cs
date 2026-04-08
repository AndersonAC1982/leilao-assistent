using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository : BaseRepository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(LeilaoAutoDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await DbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.UserId == userId)
            .OrderByDescending(subscription => subscription.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await DbContext.Subscriptions
            .AsNoTracking()
            .Where(subscription => subscription.UserId == userId)
            .OrderByDescending(subscription => subscription.StartedAt)
            .FirstOrDefaultAsync(
                subscription => subscription.Status == Domain.Enums.SubscriptionStatus.Active
                                || subscription.Status == Domain.Enums.SubscriptionStatus.PastDue
                                || subscription.Status == Domain.Enums.SubscriptionStatus.Pending,
                cancellationToken);
    }

    public async Task<Subscription?> GetByExternalSubscriptionIdAsync(string externalSubscriptionId, CancellationToken cancellationToken)
    {
        var normalizedExternalId = externalSubscriptionId.Trim();
        return await DbContext.Subscriptions
            .FirstOrDefaultAsync(subscription => subscription.ExternalSubscriptionId == normalizedExternalId, cancellationToken);
    }
}
