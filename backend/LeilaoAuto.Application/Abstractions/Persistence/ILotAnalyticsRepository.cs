using LeilaoAuto.Domain.Entities;

namespace LeilaoAuto.Application.Abstractions.Persistence;

public interface ILotAnalyticsRepository : IBaseRepository<LotAnalytics>
{
    Task<LotAnalytics?> GetByNormalizedModelAsync(string normalizedModel, CancellationToken cancellationToken);
}
