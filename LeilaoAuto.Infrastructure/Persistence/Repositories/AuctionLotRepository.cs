using LeilaoAuto.Application.Abstractions.Persistence;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Entities;
using LeilaoAuto.Domain.Enums;
using LeilaoAuto.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace LeilaoAuto.Infrastructure.Persistence.Repositories;

public class AuctionLotRepository : IAuctionLotRepository
{
    private readonly LeilaoAutoDbContext _dbContext;

    public AuctionLotRepository(LeilaoAutoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AuctionLot>> GetActiveLotsAsync(CancellationToken cancellationToken)
    {
        var lots = await _dbContext.AuctionLots
            .AsNoTracking()
            .Where(lot => lot.Status == LotStatus.Active || lot.Status == LotStatus.Confirmed)
            .OrderByDescending(lot => lot.UpdatedAtUtc)
            .Take(2000)
            .ToListAsync(cancellationToken);

        return lots.Where(lot => lot.HasValidLotUrl()).ToList();
    }

    public async Task<IReadOnlyList<AuctionLot>> GetClosedLotsAsync(CancellationToken cancellationToken)
    {
        var lots = await _dbContext.AuctionLots
            .AsNoTracking()
            .Where(lot => lot.Status == LotStatus.Closed)
            .OrderByDescending(lot => lot.EndsAt)
            .Take(5000)
            .ToListAsync(cancellationToken);

        return lots.Where(lot => lot.HasValidLotUrl()).ToList();
    }

    public async Task<IReadOnlyList<AuctionLot>> SearchActiveAsync(LotSearchFilterRequest filter, CancellationToken cancellationToken)
    {
        var query = _dbContext.AuctionLots
            .AsNoTracking()
            .Where(lot => lot.Status == LotStatus.Active || lot.Status == LotStatus.Confirmed);

        query = ApplyFilter(query, filter);

        var lots = await query
            .OrderByDescending(lot => lot.UpdatedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        return lots.Where(lot => lot.HasValidLotUrl()).ToList();
    }

    public async Task<IReadOnlyList<AuctionLot>> SearchClosedAsync(LotSearchFilterRequest filter, CancellationToken cancellationToken)
    {
        var query = _dbContext.AuctionLots
            .AsNoTracking()
            .Where(lot => lot.Status == LotStatus.Closed);

        query = ApplyFilter(query, filter);

        var lots = await query
            .OrderByDescending(lot => lot.EndsAt)
            .Take(1000)
            .ToListAsync(cancellationToken);

        return lots.Where(lot => lot.HasValidLotUrl()).ToList();
    }

    public async Task<AuctionLot?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var lot = await _dbContext.AuctionLots
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (lot is null)
        {
            return null;
        }

        if (lot.Status == LotStatus.Confirmed && !lot.HasValidLotUrl())
        {
            return null;
        }

        return lot.HasValidLotUrl() ? lot : null;
    }

    public async Task<AuctionLot?> FindExactActiveAsync(string auctioneer, string lotNumber, CancellationToken cancellationToken)
    {
        var normalizedAuctioneer = auctioneer.Trim().ToLowerInvariant();
        var normalizedLotNumber = lotNumber.Trim().ToLowerInvariant();

        var lot = await _dbContext.AuctionLots
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => (item.Status == LotStatus.Active || item.Status == LotStatus.Confirmed)
                        && item.Auctioneer.ToLower() == normalizedAuctioneer
                        && item.LotNumber.ToLower() == normalizedLotNumber,
                cancellationToken);

        return lot is not null && lot.HasValidLotUrl() ? lot : null;
    }

    public async Task<IReadOnlyList<AuctionLot>> GetClosedByNormalizedModelsAsync(
        IReadOnlyCollection<string> normalizedModels,
        LotSearchFilterRequest? filter,
        CancellationToken cancellationToken)
    {
        if (normalizedModels.Count == 0)
        {
            return [];
        }

        var query = _dbContext.AuctionLots
            .AsNoTracking()
            .Where(lot => lot.Status == LotStatus.Closed);

        query = ApplyFilter(query, filter);

        var normalizedSet = normalizedModels
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model.Trim())
            .Distinct()
            .ToArray();

        if (normalizedSet.Length > 0)
        {
            query = query.Where(lot => normalizedSet.Contains(lot.NormalizedModel));
        }

        var lots = await query
            .OrderByDescending(lot => lot.EndsAt)
            .Take(1000)
            .ToListAsync(cancellationToken);

        return lots.Where(lot => lot.HasValidLotUrl()).ToList();
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetAverageFinalPriceByNormalizedModelsAsync(
        IReadOnlyCollection<string> normalizedModels,
        CancellationToken cancellationToken)
    {
        if (normalizedModels.Count == 0)
        {
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }

        var normalizedSet = normalizedModels
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Distinct()
            .ToArray();

        var averages = await _dbContext.AuctionLots
            .AsNoTracking()
            .Where(lot => lot.Status == LotStatus.Closed
                          && lot.FinalPrice.HasValue
                          && normalizedSet.Contains(lot.NormalizedModel))
            .GroupBy(lot => lot.NormalizedModel)
            .Select(group => new
            {
                group.Key,
                Average = group.Average(item => item.FinalPrice!.Value)
            })
            .ToListAsync(cancellationToken);

        return averages.ToDictionary(item => item.Key, item => decimal.Round(item.Average, 2), StringComparer.OrdinalIgnoreCase);
    }

    public async Task UpsertRangeAsync(IEnumerable<AuctionLot> lots, CancellationToken cancellationToken)
    {
        var lotList = lots.ToList();
        if (lotList.Count == 0)
        {
            return;
        }

        var processedAt = DateTimeOffset.UtcNow;
        var externalIds = lotList.Select(lot => lot.ExternalId).Distinct().ToArray();
        var existingLots = await _dbContext.AuctionLots
            .Where(lot => externalIds.Contains(lot.ExternalId))
            .ToDictionaryAsync(lot => lot.ExternalId, cancellationToken);

        foreach (var lot in lotList)
        {
            if (!LotUrlGuard.IsValidLotUrl(lot.LotUrl))
            {
                continue;
            }

            if (existingLots.TryGetValue(lot.ExternalId, out var existing))
            {
                existing.RefreshFrom(
                    lot.Auctioneer,
                    lot.LotNumber,
                    lot.Make,
                    lot.Model,
                    lot.Year,
                    lot.VehicleType,
                    lot.Uf,
                    lot.VehicleCondition,
                    lot.Status,
                    lot.LotUrl,
                    lot.CurrentBid,
                    lot.FinalPrice,
                    lot.AppraisedValue,
                    lot.StartsAt,
                    lot.EndsAt);

                existing.MarkProcessed(processedAt);
            }
            else
            {
                lot.MarkProcessed(processedAt);
                await _dbContext.AuctionLots.AddAsync(lot, cancellationToken);
            }
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<AuctionLot> ApplyFilter(IQueryable<AuctionLot> query, LotSearchFilterRequest? filter)
    {
        if (filter is null)
        {
            return query;
        }

        if (!string.IsNullOrWhiteSpace(filter.Make))
        {
            var make = filter.Make.Trim();
            query = query.Where(lot => EF.Functions.ILike(lot.Make, $"%{make}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Model))
        {
            var normalizedFilter = ModelNormalizer.Normalize(filter.Model);
            query = query.Where(lot => lot.NormalizedModel.Contains(normalizedFilter));
        }

        if (filter.YearFrom.HasValue)
        {
            query = query.Where(lot => lot.Year >= filter.YearFrom.Value);
        }

        if (filter.YearTo.HasValue)
        {
            query = query.Where(lot => lot.Year <= filter.YearTo.Value);
        }

        if (filter.Year.HasValue)
        {
            query = query.Where(lot => lot.Year == filter.Year.Value);
        }

        if (filter.VehicleType.HasValue)
        {
            query = query.Where(lot => lot.VehicleType == filter.VehicleType.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Uf))
        {
            var uf = filter.Uf.Trim().ToUpperInvariant();
            query = query.Where(lot => lot.Uf == uf);
        }

        if (filter.VehicleCondition.HasValue)
        {
            query = query.Where(lot => lot.VehicleCondition == filter.VehicleCondition.Value);
        }

        return query.Where(lot => !string.IsNullOrWhiteSpace(lot.LotUrl));
    }
}
