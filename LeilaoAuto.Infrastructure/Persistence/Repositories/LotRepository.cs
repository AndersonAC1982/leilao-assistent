using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Domain.Common;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence.Repositories;

public class LotRepository : BaseRepository<Lot>, ILotRepository
{
    public LotRepository(LeilaoAutoDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Lot>> GetActiveLotsAsync(CancellationToken cancellationToken)
    {
        return await DbContext.Lots
            .AsNoTracking()
            .Where(lot => lot.Status == LotStatus.Active || lot.Status == LotStatus.Confirmed)
            .OrderByDescending(lot => lot.FoundAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lot>> GetClosedLotsAsync(CancellationToken cancellationToken)
    {
        return await DbContext.Lots
            .AsNoTracking()
            .Where(lot => lot.Status == LotStatus.Closed)
            .OrderByDescending(lot => lot.ClosedAt)
            .ToListAsync(cancellationToken);
    }

    public override async Task AddAsync(Lot entity, CancellationToken cancellationToken)
    {
        if (entity.Status == LotStatus.Confirmed && !LotUrlGuard.IsValidLotUrl(entity.LotUrl))
        {
            throw new DomainRuleException("A confirmed lot must have a valid lot URL.");
        }

        await base.AddAsync(entity, cancellationToken);
    }
}
