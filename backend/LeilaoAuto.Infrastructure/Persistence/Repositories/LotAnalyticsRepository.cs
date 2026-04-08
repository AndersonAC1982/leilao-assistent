using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence.Repositories;

public class LotAnalyticsRepository : BaseRepository<LotAnalytics>, ILotAnalyticsRepository
{
    public LotAnalyticsRepository(LeilaoAutoDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<LotAnalytics?> GetByNormalizedModelAsync(string normalizedModel, CancellationToken cancellationToken)
    {
        return await DbContext.LotAnalytics
            .AsNoTracking()
            .FirstOrDefaultAsync(analytics => analytics.NormalizedModel == normalizedModel, cancellationToken);
    }
}
